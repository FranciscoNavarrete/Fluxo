using Dapper.Contrib.Extensions;

namespace Models.Entities;

[Table("MpPlanes")]
public class MpPlan
{
    [Key]
    public int MpPlanId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "ARS";

    // "months" | "days"
    public string TipoFrecuencia { get; set; } = string.Empty;
    public int Frecuencia { get; set; }
    public int DiasGratis { get; set; }
    public int? Repeticiones { get; set; }

    // Monto del primer cobro (alta + primer mes). NULL = el primer cobro es el monto mensual.
    public decimal? MontoPrimerCobro { get; set; }

    // Promoción: precio por mes durante los primeros MesesPromo meses (el mes 1 incluye el alta si hay MontoPrimerCobro);
    // después rige Monto. NULL = sin promoción.
    public decimal? MontoPromo { get; set; }
    public int? MesesPromo { get; set; }

    // ID del plan en Mercado Pago (si se crea allá también)
    public string? MpPlanExternoId { get; set; }
    public bool Activo { get; set; } = true;

    public int UsuarioCreacionId { get; set; }
    public DateTime FechaHoraCreacion { get; set; }
    public int? UsuarioUltActualizacionId { get; set; }
    public DateTime? FechaHoraUltActualizacion { get; set; }
}
