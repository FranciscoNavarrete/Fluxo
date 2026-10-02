namespace Models.DTOs;

public class EstadoSuscripcionDto
{
    public int MpSuscripcionId { get; set; }
    public string Estado { get; set; } = string.Empty;

    /// <summary>La suscripción llegó a autorizarse alguna vez (aunque después se haya cancelado).
    /// Sirve para saber si una venta se concretó: un "cancelled" puede ser una baja posterior o
    /// una suscripción que nunca se pagó.</summary>
    public bool Confirmada { get; set; }
}
