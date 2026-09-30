using BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/mp/webhooks")]
[AllowAnonymous]
public class MpWebhooksController : ControllerBase
{
    private readonly IMpWebhookLogic _webhookLogic;
    private readonly ILogger<MpWebhooksController> _logger;

    public MpWebhooksController(
        IMpWebhookLogic webhookLogic,
        ILogger<MpWebhooksController> logger)
    {
        _webhookLogic = webhookLogic;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint de notificaciones de Mercado Pago.
    /// - Responde 200 inmediatamente (MP descarta la notificación si no recibe 200 rápido).
    /// - Valida firma HMAC antes de encolar el procesamiento.
    /// - Procesamiento real ocurre en background (fire-and-forget con scope propio).
    /// </summary>
    [HttpPost]
    public IActionResult Recibir(
        [FromQuery] string? type,
        [FromQuery(Name = "data.id")] string? dataId)
    {
        string xSignature  = Request.Headers["x-signature"].ToString();
        string xRequestId  = Request.Headers["x-request-id"].ToString();

        if (string.IsNullOrEmpty(dataId) || string.IsNullOrEmpty(type))
        {
            _logger.LogWarning("Webhook MP recibido sin type o data.id.");
            return Ok(); // Siempre 200 para evitar reintentos innecesarios de MP
        }

        // Fire-and-forget: validamos firma y procesamos en background
        _ = Task.Run(async () =>
        {
            try
            {
                if (!string.IsNullOrEmpty(xSignature))
                {
                    bool firmaValida = await _webhookLogic.ValidarFirmaAsync(xSignature, xRequestId, dataId);
                    if (!firmaValida)
                    {
                        _logger.LogWarning("Webhook MP firma inválida. xRequestId={XRId} dataId={DataId}.",
                            xRequestId, dataId);
                        return;
                    }
                }

                await _webhookLogic.ProcesarAsync(type, dataId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando webhook MP en background. type={Type} dataId={DataId}.",
                    type, dataId);
            }
        });

        return Ok();
    }
}
