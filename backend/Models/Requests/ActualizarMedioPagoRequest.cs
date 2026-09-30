namespace Models.Requests;

public class ActualizarMedioPagoRequest
{
    public int MpSuscripcionId { get; set; }
    public string CardTokenId { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
    public string? IssuerId { get; set; }
}
