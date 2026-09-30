using Dapper.Contrib.Extensions;

namespace Models.Entities;

[Table("MpTransacciones")]
public class MpTransaccion
{
    [Key]
    public int MpTransaccionId { get; set; }
    public int MpSuscripcionId { get; set; }

    // payment_id de Mercado Pago — UNIQUE, se usa para idempotencia
    public string GatewayPagoId { get; set; } = string.Empty;

    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "ARS";

    // approved | rejected | pending | cancelled | refunded
    public string Estado { get; set; } = string.Empty;
    public string? EstadoDetalle { get; set; }

    public int NumeroIntento { get; set; }
    public DateTime FechaProcesado { get; set; }
}
