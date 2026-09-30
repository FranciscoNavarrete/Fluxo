namespace Models.DTOs;

public class MpPlanDto
{
    public int MpPlanId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string TipoFrecuencia { get; set; } = string.Empty;
    public int Frecuencia { get; set; }
    public int DiasGratis { get; set; }
    public string? MpPlanExternoId { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaHoraCreacion { get; set; }
    public int UsuarioCreacionId { get; set; }
}
