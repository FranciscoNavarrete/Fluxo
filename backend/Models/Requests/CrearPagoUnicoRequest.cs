namespace Models.Requests;

public class CrearPagoUnicoRequest
{
    public int    ClienteId  { get; set; }
    public int    MpPlanId   { get; set; }
    public string PayerEmail { get; set; } = string.Empty;
}
