namespace Models.DTOs;

/// <summary>Historial de cobros de una suscripción y el próximo cobro programado.</summary>
public class CobrosSuscripcionDto
{
    public int MpSuscripcionId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal MontoMensual { get; set; }
    public string Moneda { get; set; } = "ARS";

    /// <summary>La tarjeta se puede cambiar desde GestorPOS (la suscripción se creó con tarjeta, no con link).</summary>
    public bool TarjetaEditable { get; set; }
    public bool CobroRechazado { get; set; }
    public DateTime? ProximoCobro { get; set; }
    public decimal? ProximoMonto { get; set; }
    public int MpPlanId { get; set; }
    public string? PlanNombre { get; set; }

    /// <summary>Precio normal del plan (distinto de MontoMensual mientras dure una promoción).</summary>
    public decimal MontoNormal { get; set; }
    public PromoDto? Promo { get; set; }
    public List<CobroDto> Cobros { get; set; } = [];
}

public class CobroDto
{
    public DateTime? Fecha { get; set; }
    public decimal Monto { get; set; }

    /// <summary>aprobado | rechazado | programado | pendiente | cancelado</summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>Motivo en español (acreditado, fondos insuficientes, etc.).</summary>
    public string? Motivo { get; set; }
    public int Intento { get; set; }
    public DateTime? ProximoReintento { get; set; }
    public bool EsPrimerCobro { get; set; }
}
