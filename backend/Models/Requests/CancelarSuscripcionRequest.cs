namespace Models.Requests;

public class CancelarSuscripcionRequest
{
    public int MpSuscripcionId { get; set; }
    public string? Motivo { get; set; }
}
