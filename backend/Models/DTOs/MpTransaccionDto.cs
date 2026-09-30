namespace Models.DTOs;

public class MpTransaccionDto
{
    public int MpTransaccionId { get; set; }
    public int MpSuscripcionId { get; set; }

    // JOIN MpSuscripciones
    public string GatewaySuscripcionId { get; set; } = string.Empty;

    // JOIN Clientes
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteEmail { get; set; } = string.Empty;

    public string GatewayPagoId { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? EstadoDetalle { get; set; }
    public int NumeroIntento { get; set; }
    public DateTime FechaProcesado { get; set; }
}
