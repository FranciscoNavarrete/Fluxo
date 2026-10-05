namespace BusinessLogic;

/// <summary>Un cobro de una suscripción tal como lo informa Mercado Pago (authorized_payments).</summary>
internal record MpCobro(
    long Id, string Estado, decimal Monto, DateTime? Fecha, int Intento, DateTime? ProximoReintento,
    string? PagoEstado, string? PagoDetalle);

internal static class MpCobros
{
    internal const string Aprobado = "aprobado";
    internal const string Rechazado = "rechazado";
    internal const string Programado = "programado";
    internal const string Pendiente = "pendiente";
    internal const string Cancelado = "cancelado";

    /// <summary>Estado simple para mostrar: aprobado, rechazado, programado, pendiente o cancelado.</summary>
    internal static string Clasificar(MpCobro cobro)
    {
        if (cobro.PagoEstado == "approved") return Aprobado;
        if (cobro.Estado == "scheduled") return Programado;
        if (cobro.PagoEstado is "in_process" or "pending") return Pendiente;
        if (cobro.Estado == "recycling" || cobro.PagoEstado == "rejected") return Rechazado;
        if (cobro.Estado == "cancelled") return cobro.PagoEstado is null ? Cancelado : Rechazado;
        if (cobro.Estado == "processed" && cobro.PagoEstado is null) return Aprobado;
        return Pendiente;
    }

    /// <summary>El cobro más reciente: si está rechazado, la suscripción tiene un cobro en problemas.</summary>
    internal static (bool Rechazado, string? Motivo, DateTime? ProximoReintento) AnalizarRechazo(IReadOnlyList<MpCobro> cobros)
    {
        var ultimo = cobros.OrderByDescending(c => c.Fecha ?? DateTime.MinValue).FirstOrDefault();
        if (ultimo is null || Clasificar(ultimo) != Rechazado) return (false, null, null);
        return (true, Motivo(ultimo), ultimo.ProximoReintento);
    }

    /// <summary>Motivo en español a partir del status_detail del pago de Mercado Pago.</summary>
    internal static string? Motivo(MpCobro cobro) => Traducir(cobro.PagoDetalle);

    internal static string? Traducir(string? detalle) => detalle switch
    {
        null or "" => null,
        "accredited" => "Acreditado",
        "cc_rejected_insufficient_amount" => "Fondos insuficientes",
        "cc_rejected_bad_filled_card_number" or "cc_rejected_bad_filled_date"
            or "cc_rejected_bad_filled_security_code" or "cc_rejected_bad_filled_other"
            => "Datos de la tarjeta incorrectos o tarjeta vencida",
        "cc_rejected_call_for_authorize" => "El banco necesita que el titular autorice el pago",
        "cc_rejected_card_disabled" => "Tarjeta deshabilitada",
        "cc_rejected_duplicated_payment" => "Pago duplicado",
        "cc_rejected_high_risk" => "Rechazado por seguridad",
        "cc_rejected_max_attempts" => "Se superó el máximo de intentos",
        "cc_rejected_blacklist" => "Tarjeta no permitida",
        "cc_rejected_invalid_installments" => "Cuotas no válidas",
        "cc_amount_rate_limit_exceeded" => "Límite de monto de la tarjeta excedido",
        "cc_rejected_card_error" or "cc_rejected_other_reason" => "El banco rechazó el pago",
        _ => "Mercado Pago rechazó el cobro",
    };
}
