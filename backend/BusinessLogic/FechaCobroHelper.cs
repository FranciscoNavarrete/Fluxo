namespace BusinessLogic;

public static class CobroInicialHelper
{
    /// <summary>Monto con el que se crea la suscripción en MP: el del primer cobro si el plan lo define.</summary>
    public static decimal MontoInicial(Models.Entities.MpPlan plan) => plan.MontoPrimerCobro ?? plan.Monto;

    /// <summary>Si el primer cobro difiere del mensual, hay que bajar el monto cuando ese cobro se aprueba.</summary>
    public static bool RequiereAjuste(Models.Entities.MpPlan plan)
        => plan.MontoPrimerCobro.HasValue && plan.MontoPrimerCobro.Value != plan.Monto;
}

public static class FechaCobroHelper
{
    /// <summary>
    /// Calcula la próxima fecha de cobro a partir de una fecha base, aplicando
    /// la frecuencia del plan y respetando el día de cobro preferido.
    ///
    /// Reglas de seguridad:
    ///   - DiaCobro 29/30/31 en febrero no bisiesto → usa el 28
    ///   - DiaCobro 31 en mes de 30 días → usa el 30
    ///   - DiaCobro 30/31 en febrero bisiesto → usa el 28 (feb solo tiene 29)
    /// </summary>
    public static DateTime Calcular(
        DateTime desde,
        int frecuencia,
        string tipoFrecuencia,
        byte? diaCobro)
    {
        // Avanzar según la frecuencia del plan
        DateTime candidato = tipoFrecuencia == "months"
            ? desde.AddMonths(frecuencia)
            : desde.AddDays(frecuencia);

        // Sin día preferido: devolver tal cual
        if (!diaCobro.HasValue || diaCobro.Value < 1 || diaCobro.Value > 31)
            return candidato;

        return AjustarDia(candidato.Year, candidato.Month, diaCobro.Value);
    }

    /// <summary>
    /// Fecha del primer cobro de una suscripción nueva. Es la misma fecha de inicio que se le manda a
    /// Mercado Pago (start_date): como MP exige una fecha futura, sin días gratis el primer cobro es
    /// 24 horas después de autorizar; con días gratis, esa cantidad de días después. El día de cobro
    /// preferido (DiaCobro) no interviene: MP cobra siempre desde start_date.
    /// </summary>
    public static DateTime PrimerCobro(DateTime desde, int diasGratis)
        => desde.AddDays(diasGratis > 0 ? diasGratis : 1);

    // ── Privados ─────────────────────────────────────────────────────────────

    /// Dado un año y mes, ajusta el día al máximo válido para ese mes.
    private static DateTime AjustarDia(int year, int month, int diaCobro)
    {
        int diasEnMes = DateTime.DaysInMonth(year, month);
        int diaReal   = Math.Min(diaCobro, diasEnMes);
        return new DateTime(year, month, diaReal, 0, 0, 0, DateTimeKind.Utc);
    }

    /// Devuelve la próxima fecha en que cae el DiaCobro a partir de 'desde'.
    /// Si el día todavía no llegó este mes, lo usa este mes. Si ya pasó, lo usa el mes siguiente.
    private static DateTime AjustarAlProximoDia(DateTime desde, int diaCobro)
    {
        int diasEsteMes = DateTime.DaysInMonth(desde.Year, desde.Month);
        int diaRealEsteMes = Math.Min(diaCobro, diasEsteMes);

        if (desde.Day <= diaRealEsteMes)
            return new DateTime(desde.Year, desde.Month, diaRealEsteMes, 0, 0, 0, DateTimeKind.Utc);

        // Ya pasó este mes → ir al siguiente
        var siguientesMes = desde.AddMonths(1);
        return AjustarDia(siguientesMes.Year, siguientesMes.Month, diaCobro);
    }
}
