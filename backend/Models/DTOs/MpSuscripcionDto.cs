namespace Models.DTOs;

public class MpSuscripcionDto
{
    public int MpSuscripcionId { get; set; }

    // JOIN Clientes
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteEmail { get; set; } = string.Empty;

    // JOIN MpPlanes
    public int MpPlanId { get; set; }
    public string PlanNombre { get; set; } = string.Empty;
    public decimal PlanMonto { get; set; }
    public string PlanMoneda { get; set; } = string.Empty;
    public string TipoFrecuencia { get; set; } = string.Empty;
    public int Frecuencia { get; set; }

    public string GatewaySuscripcionId { get; set; } = string.Empty;
    public string GatewayProveedor { get; set; } = string.Empty;
    public string? MpPayerId { get; set; }
    public string? InitPoint { get; set; }
    public string Estado { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }
    public DateTime? ProximoCobro { get; set; }
    public DateTime? UltimoCobro { get; set; }
    public DateTime? FechaSuspension { get; set; }
    public DateTime? FechaCancelacion { get; set; }
    public string? MotivoCancelacion { get; set; }

    public int IntentosReintento { get; set; }
    public int MaxReintentos { get; set; }

    public DateTime? ConsentimientoFecha { get; set; }
    public string? ConsentimientoIp { get; set; }
    public string? TerminosVersion { get; set; }

    public DateTime FechaHoraCreacion { get; set; }
    public int UsuarioCreacionId { get; set; }
}
