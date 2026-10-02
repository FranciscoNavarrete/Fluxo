using Models.Helpers;

namespace BusinessLogic;

public interface IMpWebhookLogic
{
    /// <summary>
    /// Valida la firma HMAC del webhook y procesa el evento de forma asíncrona.
    /// Retorna false si la firma es inválida (debe responderse 401).
    /// </summary>
    Task<bool> ValidarFirmaAsync(
        string xSignature,
        string xRequestId,
        string dataId);

    /// <summary>
    /// Procesa el payload del webhook. Idempotente por GatewayPagoId.
    /// </summary>
    Task ProcesarAsync(string tipo, string dataId);

    /// <summary>
    /// Le pregunta a Mercado Pago el estado actual de una suscripción y lo guarda. Es lo mismo
    /// que hace el webhook de suscripción, pero iniciado por nosotros (no depende de que MP avise).
    /// No tira excepciones: si falla lo registra en el log.
    /// </summary>
    Task SincronizarSuscripcionAsync(string gatewaySuscripcionId);
}
