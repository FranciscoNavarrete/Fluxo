namespace Models.DTOs;

public class EstadoSuscripcionDto
{
    public int MpSuscripcionId { get; set; }
    public string Estado { get; set; } = string.Empty;

    /// <summary>La suscripción llegó a autorizarse alguna vez (aunque después se haya cancelado).
    /// Sirve para saber si una venta se concretó: un "cancelled" puede ser una baja posterior o
    /// una suscripción que nunca se pagó.</summary>
    public bool Confirmada { get; set; }

    /// <summary>Mercado Pago ya aprobó al menos un cobro de la suscripción.</summary>
    public bool PrimerCobroAprobado { get; set; }

    /// <summary>El primer cobro se aprobó pero todavía no se pudo bajar el monto al mensual del plan.</summary>
    public bool AjusteMontoPendiente { get; set; }

    /// <summary>El último cobro fue rechazado (el primero o uno mensual); MotivoRechazo ya viene en español.</summary>
    public bool CobroRechazado { get; set; }
    public string? MotivoRechazo { get; set; }
    public DateTime? ProximoReintento { get; set; }

    // Datos para el resumen financiero de GestorPOS.
    /// <summary>Lo que paga por mes según el plan.</summary>
    public decimal MontoMensual { get; set; }

    /// <summary>Lo que se le va a cobrar en el próximo cobro (el primero puede ser distinto del mensual).</summary>
    public decimal MontoProximoCobro { get; set; }
    public DateTime? ProximoCobro { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaCancelacion { get; set; }
}
