using Dapper.Contrib.Extensions;

namespace Models.Entities;

[Table("MpSuscripciones")]
public class MpSuscripcion
{
    [Key]
    public int MpSuscripcionId { get; set; }
    public int ClienteId { get; set; }
    public int MpPlanId { get; set; }

    // pre_approval_id devuelto por Mercado Pago
    public string GatewaySuscripcionId { get; set; } = string.Empty;
    public string GatewayProveedor { get; set; } = "MercadoPago";

    // Día del mes preferido para el cobro (1-31). NULL = según frecuencia del plan.
    public byte? DiaCobro { get; set; }

    // payer.id y payer.email devueltos por MP al completar el checkout
    public string? MpPayerId { get; set; }
    public string? MpPayerEmail { get; set; }

    // pending | authorized | paused | suspended | cancelled
    public string Estado { get; set; } = "pending";

    public DateTime FechaInicio { get; set; }
    public DateTime? ProximoCobro { get; set; }
    public DateTime? UltimoCobro { get; set; }
    public DateTime? FechaSuspension { get; set; }
    public DateTime? FechaCancelacion { get; set; }
    public string? MotivoCancelacion { get; set; }

    public int IntentosReintento { get; set; }
    public int MaxReintentos { get; set; } = 3;

    // Mandato de débito: datos del consentimiento del cliente
    public DateTime? ConsentimientoFecha { get; set; }
    public string? ConsentimientoIp { get; set; }
    public string? ConsentimientoUserAgent { get; set; }
    public string? TerminosVersion { get; set; }

    public string? InitPoint { get; set; }

    // La suscripción se creó en MP con el monto del primer cobro; cuando ese cobro se aprueba hay
    // que bajar el monto al mensual del plan.
    public bool AjusteMontoPendiente { get; set; }

    public int UsuarioCreacionId { get; set; }
    public DateTime FechaHoraCreacion { get; set; }
    public int? UsuarioUltActualizacionId { get; set; }
    public DateTime? FechaHoraUltActualizacion { get; set; }
}
