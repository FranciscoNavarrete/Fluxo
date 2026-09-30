using MercadoPago.Client.Preapproval;
using MercadoPago.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Models.Entities;
using UnitOfWork;

namespace BusinessLogic;

/// <summary>
/// Corre cada 6 horas. Para cada suscripción cuyo ProximoCobro ya venció,
/// aplica acciones escalonadas según los días de vencimiento.
///
/// Escalación:
///   Día  1-3  → email
///   Día  3-7  → email + whatsapp
///   Día  7-9  → email + whatsapp + warning
///   Día  9-15 → suspend en MP
///   Día 15+   → cancel en MP
/// </summary>
public class DunningBackgroundService : BackgroundService
{
    private static readonly TimeSpan _intervalo = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<DunningBackgroundService> _logger;

    public DunningBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<DunningBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_intervalo);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await EjecutarCicloDunningAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ciclo de dunning.");
            }
        }
    }

    public Task EjecutarCicloDunningPublicAsync(CancellationToken ct) => EjecutarCicloDunningAsync(ct);

    private async Task EjecutarCicloDunningAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificador = scope.ServiceProvider.GetRequiredService<IDunningNotificador>();

        var vencidas = await uow.MpSuscripcion.ObtenerVencidasParaDunningAsync();

        foreach (var suscripcion in vencidas)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                await ProcesarSuscripcionAsync(suscripcion, uow, notificador);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando dunning para suscripción {Id}.", suscripcion.MpSuscripcionId);
            }
        }

        // Reanudar suscripciones pausadas por pago único cuya fecha de reanudación llegó
        await ReanudarSuscripcionesPausadasAsync(uow, ct);
    }

    private async Task ReanudarSuscripcionesPausadasAsync(IUnitOfWork uow, CancellationToken ct)
    {
        var pendientes = await uow.PagoUnico.ObtenerPendientesDeReanudacionAsync(DateTime.UtcNow);

        foreach (var pagoUnico in pendientes)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var suscripcion = await uow.MpSuscripcion.GetByIdAsync(pagoUnico.MpSuscripcionId!.Value);
                if (suscripcion is null || suscripcion.Estado != "paused")
                    continue;

                MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
                await new PreapprovalClient().UpdateAsync(
                    suscripcion.GatewaySuscripcionId,
                    new PreapprovalUpdateRequest { Status = "authorized" });

                suscripcion.Estado                    = "authorized";
                suscripcion.FechaSuspension           = null;
                suscripcion.FechaHoraUltActualizacion = DateTime.UtcNow;
                await uow.MpSuscripcion.UpdateAsync(suscripcion);

                // Marcar como procesado poniendo FechaReanudacion en null para no volver a procesarlo
                pagoUnico.FechaReanudacion = null;
                await uow.PagoUnico.UpdateAsync(pagoUnico);

                _logger.LogInformation(
                    "Suscripción {SuscId} reanudada automáticamente por pago único {PagoId}.",
                    suscripcion.MpSuscripcionId, pagoUnico.MpPagoUnicoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error reanudando suscripción pausada por pago único {Id}.",
                    pagoUnico.MpPagoUnicoId);
            }
        }
    }

    private async Task ProcesarSuscripcionAsync(
        MpSuscripcion suscripcion,
        IUnitOfWork uow,
        IDunningNotificador notificador)
    {
        DateTime vencimientoRef = suscripcion.ProximoCobro ?? suscripcion.FechaInicio;
        int diasVencido = (int)(DateTime.UtcNow - vencimientoRef).TotalDays;

        if (diasVencido < 1) return;

        // Evitar ejecutar la misma acción dos veces en el mismo día
        var logsHoy = (await uow.MpDunningLog.ObtenerPorSuscripcionAsync(suscripcion.MpSuscripcionId))
            .Where(l => l.FechaEjecucion.Date == DateTime.UtcNow.Date)
            .Select(l => l.Accion)
            .ToHashSet();

        if (diasVencido is >= 1 and <= 3)
        {
            if (!logsHoy.Contains("email"))
            {
                await notificador.EnviarEmailAsync(suscripcion, diasVencido, "vencimiento_leve");
                await RegistrarDunningLogAsync(uow, suscripcion.MpSuscripcionId, "email", diasVencido, "email");
                _logger.LogInformation("Dunning email suscripción {Id} día {D}.", suscripcion.MpSuscripcionId, diasVencido);
            }
        }
        else if (diasVencido is > 3 and <= 7)
        {
            if (!logsHoy.Contains("email"))
            {
                await notificador.EnviarEmailAsync(suscripcion, diasVencido, "vencimiento_moderado");
                await RegistrarDunningLogAsync(uow, suscripcion.MpSuscripcionId, "email", diasVencido, "email");
            }
            if (!logsHoy.Contains("whatsapp"))
            {
                await notificador.EnviarWhatsAppAsync(suscripcion, diasVencido, "vencimiento_moderado");
                await RegistrarDunningLogAsync(uow, suscripcion.MpSuscripcionId, "whatsapp", diasVencido, "whatsapp");
                _logger.LogInformation("Dunning email+whatsapp suscripción {Id} día {D}.", suscripcion.MpSuscripcionId, diasVencido);
            }
        }
        else if (diasVencido is > 7 and <= 9)
        {
            if (!logsHoy.Contains("warning"))
            {
                await notificador.EnviarEmailAsync(suscripcion, diasVencido, "warning_suspension");
                await notificador.EnviarWhatsAppAsync(suscripcion, diasVencido, "warning_suspension");
                await RegistrarDunningLogAsync(uow, suscripcion.MpSuscripcionId, "warning", diasVencido, "email+whatsapp");
                _logger.LogWarning("Dunning warning suscripción {Id} día {D}.", suscripcion.MpSuscripcionId, diasVencido);
            }
        }
        else if (diasVencido is > 9 and <= 15)
        {
            if (!logsHoy.Contains("suspend") && suscripcion.Estado != "suspended")
            {
                await SuspenderEnMpAsync(suscripcion, uow);
                await notificador.EnviarEmailAsync(suscripcion, diasVencido, "suspension");
                await RegistrarDunningLogAsync(uow, suscripcion.MpSuscripcionId, "suspend", diasVencido, "mp_api+email");
                _logger.LogWarning("Dunning SUSPEND suscripción {Id} día {D}.", suscripcion.MpSuscripcionId, diasVencido);
            }
        }
        else if (diasVencido > 15)
        {
            if (!logsHoy.Contains("cancel") && suscripcion.Estado != "cancelled")
            {
                await CancelarEnMpAsync(suscripcion, uow);
                await notificador.EnviarEmailAsync(suscripcion, diasVencido, "cancelacion");
                await RegistrarDunningLogAsync(uow, suscripcion.MpSuscripcionId, "cancel", diasVencido, "mp_api+email");
                _logger.LogWarning("Dunning CANCEL suscripción {Id} día {D}.", suscripcion.MpSuscripcionId, diasVencido);
            }
        }
    }

    private async Task SuspenderEnMpAsync(MpSuscripcion suscripcion, IUnitOfWork uow)
    {
        MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
        await new PreapprovalClient().UpdateAsync(
            suscripcion.GatewaySuscripcionId,
            new PreapprovalUpdateRequest { Status = "paused" });

        suscripcion.Estado = "suspended";
        suscripcion.FechaSuspension = DateTime.UtcNow;
        suscripcion.FechaHoraUltActualizacion = DateTime.UtcNow;
        await uow.MpSuscripcion.UpdateAsync(suscripcion);
    }

    private async Task CancelarEnMpAsync(MpSuscripcion suscripcion, IUnitOfWork uow)
    {
        MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
        await new PreapprovalClient().UpdateAsync(
            suscripcion.GatewaySuscripcionId,
            new PreapprovalUpdateRequest { Status = "cancelled" });

        suscripcion.Estado = "cancelled";
        suscripcion.FechaCancelacion = DateTime.UtcNow;
        suscripcion.MotivoCancelacion = "Cancelación automática por falta de pago (dunning día 15+).";
        suscripcion.FechaHoraUltActualizacion = DateTime.UtcNow;
        await uow.MpSuscripcion.UpdateAsync(suscripcion);
    }

    private static async Task RegistrarDunningLogAsync(
        IUnitOfWork uow, int mpSuscripcionId, string accion, int diasVencido, string canal)
    {
        await uow.MpDunningLog.InsertAsync(new MpDunningLog
        {
            MpSuscripcionId = mpSuscripcionId,
            Accion = accion,
            DiasVencido = diasVencido,
            Canal = canal,
            FechaEjecucion = DateTime.UtcNow
        });
    }
}

// ------------------------------------------------------------------
// Contrato de notificaciones — implementar según el sistema existente
// (email, WhatsApp, push, etc.)
// ------------------------------------------------------------------
public interface IDunningNotificador
{
    Task EnviarEmailAsync(MpSuscripcion suscripcion, int diasVencido, string plantilla);
    Task EnviarWhatsAppAsync(MpSuscripcion suscripcion, int diasVencido, string plantilla);
}
