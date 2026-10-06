namespace Models.Requests;

public class CrearClienteAdminRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public int? MpPlanId { get; set; }   // si se envía, crea la suscripción en el mismo paso
    public byte? DiaCobro { get; set; }
    /// <summary>El primer pago (alta + primer mes) se cobró por fuera de Mercado Pago (efectivo o transferencia):
    /// la suscripción cobra solo el monto mensual y recién un período después.</summary>
    public bool PrimerPagoManual { get; set; }
    public string? CardTokenId { get; set; }   // si se envía, la suscripción se crea autorizada con esa tarjeta (sin link de pago)
}
