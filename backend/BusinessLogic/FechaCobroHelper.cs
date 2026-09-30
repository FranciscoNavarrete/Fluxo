namespace BusinessLogic;

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
    /// Calcula el primer cobro desde hoy, buscando la próxima ocurrencia del DiaCobro.
    /// Si el día ya pasó este mes, avanza al mes siguiente.
    /// </summary>
    public static DateTime PrimerCobro(
        DateTime desde,
        int frecuencia,
        string tipoFrecuencia,
        byte? diaCobro,
        int diasGratis)
    {
        // Si hay días gratis, el primer cobro es después del periodo de gracia
        if (diasGratis > 0)
        {
            var trasPeriodoGratis = desde.AddDays(diasGratis);
            return diaCobro.HasValue
                ? AjustarAlProximoDia(trasPeriodoGratis, diaCobro.Value)
                : trasPeriodoGratis;
        }

        if (!diaCobro.HasValue || diaCobro.Value < 1 || diaCobro.Value > 31)
            return tipoFrecuencia == "months"
                ? desde.AddMonths(frecuencia)
                : desde.AddDays(frecuencia);

        return AjustarAlProximoDia(desde, diaCobro.Value);
    }

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
