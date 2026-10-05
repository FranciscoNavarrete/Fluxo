namespace Models.Requests;

public class CrearPlanRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "ARS";
    public string TipoFrecuencia { get; set; } = string.Empty;
    public int Frecuencia { get; set; }
    public int DiasGratis { get; set; }
    public int? Repeticiones { get; set; }
    public decimal? MontoPrimerCobro { get; set; }
}
