namespace Models.DTOs;

public class LinkPagoSuscripcionDto
{
    public int MpSuscripcionId { get; set; }
    public string Estado { get; set; } = string.Empty;

    /// <summary>Solo viene mientras la suscripción está pendiente; si ya se pagó o se canceló, null.</summary>
    public string? InitPoint { get; set; }
}
