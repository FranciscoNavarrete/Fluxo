using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UnitOfWork;

namespace BusinessLogic;

/// <summary>
/// Cada pocos minutos le pregunta a Mercado Pago por las suscripciones que siguen en "pending" y
/// guarda el estado real. No depende del webhook: MP no avisa de forma confiable cuándo se autoriza
/// una suscripción (y QAS ni siquiera recibe webhooks, porque comparte cuenta con PROD).
/// </summary>
public class SincronizacionSuscripcionesBackgroundService : BackgroundService
{
    private const int MaximoPorCiclo = 50;
    private static readonly TimeSpan VentanaPendientes = TimeSpan.FromDays(7);
    private static readonly TimeSpan VentanaCobros = TimeSpan.FromDays(60);
    private const int CadaCuantosCiclosVentana = 10;
    private int _ciclo;
    private static readonly TimeSpan PausaEntreConsultas = TimeSpan.FromMilliseconds(300);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<SincronizacionSuscripcionesBackgroundService> _logger;

    public SincronizacionSuscripcionesBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<SincronizacionSuscripcionesBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int segundos = int.TryParse(_config["Sincronizacion:IntervaloSegundos"], out var configurado) ? configurado : 180;
        if (segundos <= 0)
        {
            _logger.LogInformation("Sincronización de suscripciones pendientes desactivada.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(segundos, 30)));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await EjecutarCicloAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en la sincronización de suscripciones pendientes.");
            }
        }
    }

    private async Task EjecutarCicloAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_config["MercadoPago:AccessToken"]))
            return;

        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var webhookLogic = scope.ServiceProvider.GetRequiredService<IMpWebhookLogic>();

        var pendientes = await uow.MpSuscripcion.ObtenerPendientesParaSincronizarAsync(
            DateTime.UtcNow - VentanaPendientes, MaximoPorCiclo);

        var lista = pendientes.ToList();
        if (lista.Count > 0)
            _logger.LogInformation("Sincronización: consultando {Cantidad} suscripciones pendientes en Mercado Pago.", lista.Count);

        foreach (var suscripcion in lista)
        {
            if (ct.IsCancellationRequested) break;

            await webhookLogic.SincronizarSuscripcionAsync(suscripcion.GatewaySuscripcionId);
            await Task.Delay(PausaEntreConsultas, ct);
        }

        var autorizadas = (await uow.MpSuscripcion.ObtenerAutorizadasParaSincronizarCobrosAsync(
            DateTime.UtcNow - VentanaCobros, MaximoPorCiclo)).ToList();

        // Las que están en su ventana de cobro (o con un cobro rechazado) se revisan menos seguido: es
        // donde pueden aparecer rechazos de los cobros mensuales.
        _ciclo++;
        if (_ciclo % CadaCuantosCiclosVentana == 0)
        {
            var ids = autorizadas.Select(a => a.MpSuscripcionId).ToHashSet();
            autorizadas.AddRange((await uow.MpSuscripcion.ObtenerAutorizadasEnVentanaDeCobroAsync(MaximoPorCiclo))
                .Where(a => !ids.Contains(a.MpSuscripcionId)));
        }

        foreach (var suscripcion in autorizadas)
        {
            if (ct.IsCancellationRequested) break;

            await webhookLogic.SincronizarCobrosAsync(suscripcion.GatewaySuscripcionId);
            await Task.Delay(PausaEntreConsultas, ct);
        }
    }
}
