namespace Models.Requests;

public class CrearSuscripcionRedirectRequest
{
    public int ClienteId { get; set; }
    public int MpPlanId { get; set; }
    public string PayerEmail { get; set; } = string.Empty;

    // Día del mes preferido para el cobro (1-31). NULL = según frecuencia del plan.
    public byte? DiaCobro { get; set; }

    // URL a la que MP redirige tras completar el checkout
    public string? BackUrl { get; set; }

    // Versión de términos y condiciones aceptada
    public string? TerminosVersion { get; set; }
}
