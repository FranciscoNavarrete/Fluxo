namespace Models.Requests;

public class CrearSuscripcionConTokenRequest
{
    public int ClienteId { get; set; }
    public int MpPlanId { get; set; }

    // Token de tarjeta generado por MP Bricks en el frontend
    public string CardTokenId { get; set; } = string.Empty;

    public string PayerEmail { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
    public string? IssuerId { get; set; }
    public string? TerminosVersion { get; set; }
    public byte? DiaCobro { get; set; }
}
