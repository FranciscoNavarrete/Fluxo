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
}
