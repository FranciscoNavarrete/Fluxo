using Microsoft.Extensions.Logging;
using Models.Entities;

namespace BusinessLogic;

// Implementación de stub para entornos donde aún no está conectado el proveedor
// de notificaciones real. Reemplazar con la implementación concreta del sistema.
public class StubDunningNotificador : IDunningNotificador
{
    private readonly ILogger<StubDunningNotificador> _logger;

    public StubDunningNotificador(ILogger<StubDunningNotificador> logger)
        => _logger = logger;

    public Task EnviarEmailAsync(MpSuscripcion suscripcion, int diasVencido, string plantilla)
    {
        _logger.LogInformation(
            "[STUB] Email dunning → SuscripcionId={Id} DiasVencido={Dias} Plantilla={Plantilla}",
            suscripcion.MpSuscripcionId, diasVencido, plantilla);
        return Task.CompletedTask;
    }

    public Task EnviarWhatsAppAsync(MpSuscripcion suscripcion, int diasVencido, string plantilla)
    {
        _logger.LogInformation(
            "[STUB] WhatsApp dunning → SuscripcionId={Id} DiasVencido={Dias} Plantilla={Plantilla}",
            suscripcion.MpSuscripcionId, diasVencido, plantilla);
        return Task.CompletedTask;
    }
}
