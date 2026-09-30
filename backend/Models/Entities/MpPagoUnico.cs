using Dapper.Contrib.Extensions;

namespace Models.Entities;

[Table("MpPagosUnicos")]
public class MpPagoUnico
{
    [Key]
    public int MpPagoUnicoId { get; set; }
    public int ClienteId { get; set; }
    public int MpPlanId { get; set; }

    public int? MpSuscripcionId { get; set; }
    public DateTime? FechaReanudacion { get; set; }

    public string MpPreferenceId  { get; set; } = string.Empty;
    public string ExternalReference { get; set; } = string.Empty;
    public string? MpPaymentId    { get; set; }
    public string InitPoint       { get; set; } = string.Empty;

    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "ARS";
    public string Estado { get; set; } = "pending";

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaPago { get; set; }
}
