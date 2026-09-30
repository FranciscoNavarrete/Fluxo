using MercadoPago.Client.Preference;
using MercadoPago.Client.Preapproval;
using MercadoPago.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models.Entities;
using Models.Helpers;
using Models.Requests;
using UnitOfWork;

namespace BusinessLogic;

public class MpPagoUnicoLogic : IMpPagoUnicoLogic
{
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;
    private readonly ILogger<MpPagoUnicoLogic> _logger;

    public MpPagoUnicoLogic(IUnitOfWork uow, IConfiguration config, ILogger<MpPagoUnicoLogic> logger)
    {
        _uow    = uow;
        _config = config;
        _logger = logger;
    }

    public async Task<RespuestaResultado<PagoUnicoResultado>> CrearPagoUnicoAsync(
        CrearPagoUnicoRequest request, int usuarioId)
    {
        var plan = await _uow.MpPlan.GetByIdAsync(request.MpPlanId);
        if (plan is null || !plan.Activo)
            return Error("Plan no encontrado o inactivo.");

        // ── Pausar suscripción activa si existe ──────────────────────────────
        int? suscripcionId    = null;
        DateTime? fechaReanudacion = null;

        var suscripcion = await _uow.MpSuscripcion.ObtenerActivaPorClienteAsync(request.ClienteId);
        if (suscripcion is not null && suscripcion.Estado == "authorized")
        {
            // Fecha en que debería reanudarse = próximo cobro programado
            fechaReanudacion = suscripcion.ProximoCobro
                ?? DateTime.UtcNow.AddMonths(plan.TipoFrecuencia == "months" ? plan.Frecuencia : 0)
                              .AddDays(plan.TipoFrecuencia == "days"   ? plan.Frecuencia : 0);

            try
            {
                MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
                await new PreapprovalClient().UpdateAsync(
                    suscripcion.GatewaySuscripcionId,
                    new PreapprovalUpdateRequest { Status = "paused" });

                suscripcion.Estado = "paused";
                suscripcion.FechaSuspension          = DateTime.UtcNow;
                suscripcion.FechaHoraUltActualizacion = DateTime.UtcNow;
                await _uow.MpSuscripcion.UpdateAsync(suscripcion);

                suscripcionId = suscripcion.MpSuscripcionId;
                _logger.LogInformation(
                    "Suscripción {Id} pausada por pago único del cliente {ClienteId}.",
                    suscripcion.MpSuscripcionId, request.ClienteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausando suscripción {Id}.", suscripcion.MpSuscripcionId);
                return Error($"No se pudo pausar la suscripción: {ex.Message}");
            }
        }

        // ── Crear preferencia de pago en MP ──────────────────────────────────
        string preferenceId;
        string initPoint;
        string externalRef = $"pu_{request.ClienteId}_{DateTime.UtcNow.Ticks}";

        try
        {
            MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];

            var preference = await new PreferenceClient().CreateAsync(new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title      = $"Cuota {plan.Nombre}",
                        Quantity   = 1,
                        UnitPrice  = plan.Monto,
                        CurrencyId = plan.Moneda,
                    }
                },
                Payer             = new PreferencePayerRequest { Email = request.PayerEmail },
                ExternalReference = externalRef,
                BackUrls          = new PreferenceBackUrlsRequest
                {
                    Success = _config["MercadoPago:PagoUnicoBackUrl"],
                    Failure = _config["MercadoPago:PagoUnicoBackUrl"],
                    Pending = _config["MercadoPago:PagoUnicoBackUrl"],
                },
                AutoReturn        = "approved",
            });

            preferenceId = preference.Id;
            initPoint    = preference.InitPoint;
        }
        catch (Exception ex)
        {
            // Revertir pausa si falló la creación de la preferencia
            if (suscripcion is not null && suscripcionId.HasValue)
            {
                try
                {
                    await new PreapprovalClient().UpdateAsync(
                        suscripcion.GatewaySuscripcionId,
                        new PreapprovalUpdateRequest { Status = "authorized" });
                    suscripcion.Estado = "authorized";
                    await _uow.MpSuscripcion.UpdateAsync(suscripcion);
                }
                catch { /* best effort */ }
            }

            _logger.LogError(ex, "Error creando preferencia MP para cliente {ClienteId}.", request.ClienteId);
            return Error($"Error al crear el pago en Mercado Pago: {ex.Message}");
        }

        // ── Guardar en BD ────────────────────────────────────────────────────
        var pagoUnico = new MpPagoUnico
        {
            ClienteId         = request.ClienteId,
            MpPlanId          = request.MpPlanId,
            MpSuscripcionId   = suscripcionId,
            FechaReanudacion  = fechaReanudacion,
            MpPreferenceId    = preferenceId,
            ExternalReference = externalRef,
            InitPoint         = initPoint,
            Monto             = plan.Monto,
            Moneda            = plan.Moneda,
            Estado            = "pending",
            FechaCreacion     = DateTime.UtcNow,
        };

        int id = await _uow.PagoUnico.InsertAsync(pagoUnico);

        return new RespuestaResultado<PagoUnicoResultado>
        {
            Exitoso = true,
            Contenido = new PagoUnicoResultado
            {
                MpPagoUnicoId      = id,
                InitPoint          = initPoint,
                Monto              = plan.Monto,
                Moneda             = plan.Moneda,
                SuscripcionPausada = suscripcionId.HasValue,
                FechaReanudacion   = fechaReanudacion,
            }
        };
    }

    public async Task<RespuestaResultado<IEnumerable<MpPagoUnico>>> ObtenerPorClienteAsync(int clienteId)
    {
        var pagos = await _uow.PagoUnico.ListarPorClienteAsync(clienteId);
        return new RespuestaResultado<IEnumerable<MpPagoUnico>> { Exitoso = true, Contenido = pagos };
    }

    public async Task<RespuestaResultado<bool>> EliminarPendienteAsync(int mpPagoUnicoId, int? clienteId)
    {
        var pago = await _uow.PagoUnico.ObtenerPorIdAsync(mpPagoUnicoId);
        if (pago is null)
            return new() { Exitoso = false, Mensaje = "Pago no encontrado." };
        // clienteId null = admin, sin restricción de ownership
        if (clienteId.HasValue && pago.ClienteId != clienteId.Value)
            return new() { Exitoso = false, Mensaje = "Pago no encontrado." };
        if (pago.Estado != "pending")
            return new() { Exitoso = false, Mensaje = "Solo se pueden eliminar pagos pendientes." };

        var ok = await _uow.PagoUnico.EliminarAsync(mpPagoUnicoId);
        return new() { Exitoso = ok, Mensaje = ok ? "" : "No se pudo eliminar el pago." };
    }

    private static RespuestaResultado<PagoUnicoResultado> Error(string mensaje) =>
        new() { Exitoso = false, Mensaje = mensaje };
}
