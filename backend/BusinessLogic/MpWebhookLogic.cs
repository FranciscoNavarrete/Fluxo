using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preapproval;
using MercadoPago.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models.Entities;
using UnitOfWork;

namespace BusinessLogic;

public class MpWebhookLogic : IMpWebhookLogic
{
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;
    private readonly ILogger<MpWebhookLogic> _logger;

    public MpWebhookLogic(
        IUnitOfWork uow,
        IConfiguration config,
        ILogger<MpWebhookLogic> logger)
    {
        _uow = uow;
        _config = config;
        _logger = logger;
    }

    // ------------------------------------------------------------------
    // Validación de firma HMAC-SHA256 según especificación de Mercado Pago
    // Manifest: id:<data.id>;request-id:<x-request-id>;ts:<ts>;
    // ------------------------------------------------------------------
    public Task<bool> ValidarFirmaAsync(string xSignature, string xRequestId, string dataId)
    {
        try
        {
            var partes = xSignature.Split(',')
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim(), p => p[1].Trim());

            if (!partes.TryGetValue("ts", out var ts) || !partes.TryGetValue("v1", out var v1))
                return Task.FromResult(false);

            string manifest = $"id:{dataId};request-id:{xRequestId};ts:{ts};";
            string secret   = _config["MercadoPago:WebhookSecret"] ?? string.Empty;

            byte[] hash = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(secret),
                Encoding.UTF8.GetBytes(manifest));
            string computedHash = Convert.ToHexString(hash).ToLowerInvariant();

            return Task.FromResult(string.Equals(computedHash, v1, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validando firma webhook MP.");
            return Task.FromResult(false);
        }
    }

    public async Task ProcesarAsync(string tipo, string dataId)
    {
        try
        {
            switch (tipo)
            {
                case "payment":
                    await ProcesarPagoAsync(dataId);
                    break;
                case "subscription_preapproval":
                    await ProcesarCambioEstadoSuscripcionAsync(dataId);
                    break;
                default:
                    _logger.LogInformation("Webhook MP tipo '{Tipo}' no manejado.", tipo);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando webhook MP tipo={Tipo} id={DataId}.", tipo, dataId);
        }
    }

    private async Task ProcesarPagoAsync(string paymentId)
    {
        MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
        var mpPayment = await new PaymentClient().GetAsync(long.Parse(paymentId));

        if (mpPayment is null)
        {
            _logger.LogWarning("Pago {PaymentId} no encontrado en MP.", paymentId);
            return;
        }

        // ── Pago único: ExternalReference empieza con "pu_" ──────────────────
        if (!string.IsNullOrEmpty(mpPayment.ExternalReference) &&
            mpPayment.ExternalReference.StartsWith("pu_"))
        {
            await ProcesarPagoUnicoAsync(paymentId, mpPayment.ExternalReference, mpPayment);
            return;
        }

        // ── Pago de suscripción ───────────────────────────────────────────────

        // Idempotencia
        var existente = await _uow.MpTransaccion.ObtenerPorGatewayPagoIdAsync(paymentId);
        if (existente is not null)
        {
            _logger.LogInformation("Pago {PaymentId} ya procesado (idempotente).", paymentId);
            return;
        }

        // MP no manda el preapproval_id en Metadata para pagos generados por una suscripción
        // recurrente — viene en point_of_interaction.transaction_data.subscription_id. Metadata
        // queda como fallback por si algún día se crea un pago con esa clave seteada a mano.
        string? preApprovalId = mpPayment.PointOfInteraction?.TransactionData?.SubscriptionId;

        if (string.IsNullOrEmpty(preApprovalId))
        {
            preApprovalId = mpPayment.Metadata?.TryGetValue("preapproval_id", out var pid) == true
                ? pid?.ToString()
                : null;
        }

        MpSuscripcion? suscripcion;
        if (!string.IsNullOrEmpty(preApprovalId))
        {
            suscripcion = await _uow.MpSuscripcion.ObtenerPorGatewayIdAsync(preApprovalId);
            if (suscripcion is null)
            {
                _logger.LogWarning("Suscripción con GatewayId={PreApprovalId} no encontrada.", preApprovalId);
                return;
            }
        }
        else
        {
            // Mercado Pago no siempre informa la suscripción dentro del pago. Se resuelve por el
            // external_reference que le pusimos al crearla, validando contra el pagador.
            suscripcion = await ObtenerPorExternalReferenceAsync(mpPayment.ExternalReference, mpPayment.Payer?.Email);
            if (suscripcion is null)
            {
                _logger.LogWarning(
                    "Pago {PaymentId}: no se puede determinar la suscripción (external_reference={Ref}).",
                    paymentId, mpPayment.ExternalReference);
                return;
            }
        }

        int intentos = await _uow.MpTransaccion.ContarIntentosPorSuscripcionAsync(suscripcion.MpSuscripcionId);

        var transaccion = new MpTransaccion
        {
            MpSuscripcionId = suscripcion.MpSuscripcionId,
            GatewayPagoId   = paymentId,
            Monto           = (decimal)(mpPayment.TransactionAmount ?? 0),
            Moneda          = mpPayment.CurrencyId ?? "ARS",
            Estado          = mpPayment.Status ?? string.Empty,
            EstadoDetalle   = mpPayment.StatusDetail,
            NumeroIntento   = intentos + 1,
            FechaProcesado  = mpPayment.DateApproved ?? DateTime.UtcNow
        };

        await _uow.MpTransaccion.InsertAsync(transaccion);

        var ahora = DateTime.UtcNow;

        if (mpPayment.Status == "approved")
        {
            suscripcion.Estado            = "authorized";
            suscripcion.UltimoCobro       = transaccion.FechaProcesado;
            suscripcion.IntentosReintento = 0;

            var plan = await _uow.MpPlan.GetByIdAsync(suscripcion.MpPlanId);
            if (plan is not null)
            {
                suscripcion.ProximoCobro = FechaCobroHelper.Calcular(
                    ahora, plan.Frecuencia, plan.TipoFrecuencia, suscripcion.DiaCobro);
            }

            _logger.LogInformation("Pago {PaymentId} aprobado para suscripción {Id}.",
                paymentId, suscripcion.MpSuscripcionId);
        }
        else if (mpPayment.Status is "rejected" or "cancelled")
        {
            suscripcion.IntentosReintento++;
            _logger.LogWarning("Pago {PaymentId} rechazado ({Detalle}) para suscripción {Id}.",
                paymentId, mpPayment.StatusDetail, suscripcion.MpSuscripcionId);
        }

        suscripcion.FechaHoraUltActualizacion = ahora;
        await _uow.MpSuscripcion.UpdateAsync(suscripcion);
    }

    private static readonly Regex ExternalReferenceSuscripcion =
        new(@"^client_(\d+)_plan_(\d+)$", RegexOptions.Compiled);

    // QAS y PROD comparten la cuenta de Mercado Pago (y el webhook llega solo a PROD), y los ids de
    // cliente/plan se repiten entre las dos bases: por eso, además del external_reference, el email
    // del pagador tiene que coincidir con el del cliente. Si no, el pago es de otro ambiente.
    private async Task<MpSuscripcion?> ObtenerPorExternalReferenceAsync(string? externalReference, string? emailPagador)
    {
        if (string.IsNullOrEmpty(externalReference) || string.IsNullOrWhiteSpace(emailPagador))
            return null;

        var match = ExternalReferenceSuscripcion.Match(externalReference);
        if (!match.Success)
            return null;

        int clienteId = int.Parse(match.Groups[1].Value);
        int planId    = int.Parse(match.Groups[2].Value);

        var cliente = await _uow.Cliente.GetByIdAsync(clienteId);
        if (cliente is null || !string.Equals(cliente.Email, emailPagador.Trim(), StringComparison.OrdinalIgnoreCase))
            return null;

        var suscripcion = await _uow.MpSuscripcion.ObtenerActivaPorClienteAsync(clienteId);
        return suscripcion is not null && suscripcion.MpPlanId == planId ? suscripcion : null;
    }

    private async Task ProcesarPagoUnicoAsync(
        string paymentId,
        string externalReference,
        MercadoPago.Resource.Payment.Payment mpPayment)
    {
        var pagoUnico = await _uow.PagoUnico.ObtenerPorExternalReferenceAsync(externalReference);
        if (pagoUnico is null)
        {
            _logger.LogWarning("Pago único con ExternalReference={Ref} no encontrado.", externalReference);
            return;
        }

        // Idempotencia
        if (!string.IsNullOrEmpty(pagoUnico.MpPaymentId))
        {
            _logger.LogInformation("Pago único {Id} ya procesado (idempotente).", pagoUnico.MpPagoUnicoId);
            return;
        }

        pagoUnico.MpPaymentId = paymentId;
        pagoUnico.Estado      = mpPayment.Status ?? "pending";

        if (mpPayment.Status == "approved")
        {
            pagoUnico.FechaPago = mpPayment.DateApproved ?? DateTime.UtcNow;
            _logger.LogInformation("Pago único {Id} aprobado.", pagoUnico.MpPagoUnicoId);
        }
        else if (mpPayment.Status is "rejected" or "cancelled")
        {
            // Si el pago fue rechazado y había pausado una suscripción, la reanudamos
            if (pagoUnico.MpSuscripcionId.HasValue)
            {
                var suscripcion = await _uow.MpSuscripcion.GetByIdAsync(pagoUnico.MpSuscripcionId.Value);
                if (suscripcion is not null && suscripcion.Estado == "paused")
                {
                    try
                    {
                        MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
                        await new PreapprovalClient().UpdateAsync(
                            suscripcion.GatewaySuscripcionId,
                            new PreapprovalUpdateRequest { Status = "authorized" });

                        suscripcion.Estado                    = "authorized";
                        suscripcion.FechaSuspension           = null;
                        suscripcion.FechaHoraUltActualizacion = DateTime.UtcNow;
                        await _uow.MpSuscripcion.UpdateAsync(suscripcion);

                        _logger.LogInformation(
                            "Suscripción {Id} reanudada por pago único rechazado.",
                            suscripcion.MpSuscripcionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error reanudando suscripción {Id} tras pago único rechazado.",
                            suscripcion.MpSuscripcionId);
                    }
                }
            }

            _logger.LogWarning("Pago único {Id} rechazado ({Detalle}).",
                pagoUnico.MpPagoUnicoId, mpPayment.StatusDetail);
        }

        await _uow.PagoUnico.UpdateAsync(pagoUnico);
    }

    private async Task ProcesarCambioEstadoSuscripcionAsync(string preApprovalId)
    {
        MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
        var mpSuscripcion = await new PreapprovalClient().GetAsync(preApprovalId);

        if (mpSuscripcion is null) return;

        var suscripcion = await _uow.MpSuscripcion.ObtenerPorGatewayIdAsync(preApprovalId);
        if (suscripcion is null) return;

        string nuevoEstado = mpSuscripcion.Status ?? suscripcion.Estado;
        if (suscripcion.Estado == nuevoEstado) return;

        suscripcion.Estado = nuevoEstado;
        suscripcion.MpPayerId    = mpSuscripcion.PayerId?.ToString();
        suscripcion.MpPayerEmail = mpSuscripcion.PayerEmail ?? suscripcion.MpPayerEmail;
        suscripcion.FechaHoraUltActualizacion = DateTime.UtcNow;

        if (nuevoEstado == "cancelled")
            suscripcion.FechaCancelacion = DateTime.UtcNow;
        else if (nuevoEstado == "paused")
            suscripcion.FechaSuspension = DateTime.UtcNow;

        await _uow.MpSuscripcion.UpdateAsync(suscripcion);

        _logger.LogInformation("Suscripción {Id} cambió estado a {Estado}.",
            suscripcion.MpSuscripcionId, nuevoEstado);
    }
}
