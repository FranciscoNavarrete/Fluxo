namespace BusinessLogic;

public static class CobroInicialHelper
{
    /// <summary>El plan tiene un precio promocional por los primeros meses.</summary>
    public static bool TienePromo(Models.Entities.MpPlan plan) => plan.MontoPromo.HasValue && plan.MesesPromo is > 0;

    /// <summary>Lo que se cobra por mes en el mes número <paramref name="mes"/> del plan (1 = el primero):
    /// el precio promocional mientras dure la promoción y después el normal.</summary>
    public static decimal MontoDelMes(Models.Entities.MpPlan plan, int mes) =>
        TienePromo(plan) && mes <= plan.MesesPromo!.Value ? plan.MontoPromo!.Value : plan.Monto;

    /// <summary>Número de mes del plan que corresponde al próximo cobro, dados los cobros ya realizados.</summary>
    public static int MesDelProximoCobro(Models.Entities.MpSuscripcion suscripcion, int cobrosRealizados) =>
        Math.Max(0, cobrosRealizados - suscripcion.CobrosBase) + 1;

    /// <summary>Monto que tiene que tener la suscripción en MP para el próximo cobro. El primer cobro de una
    /// suscripción nueva es el del alta (MontoInicial); después rige el precio del mes que toque.</summary>
    public static decimal MontoEsperado(Models.Entities.MpPlan plan, Models.Entities.MpSuscripcion suscripcion, int cobrosRealizados)
    {
        if (suscripcion.CobrosBase == 0 && cobrosRealizados == 0 && !suscripcion.SinAlta) return MontoInicial(plan);
        return MontoDelMes(plan, MesDelProximoCobro(suscripcion, cobrosRealizados));
    }

    /// <summary>Con el primer pago hecho a mano, la suscripción arranca en el mes 2 del plan (no cobra el inicial).</summary>
    public static decimal MontoDeSuscripcion(Models.Entities.MpPlan plan, bool primerPagoManual, bool sinAlta = false) =>
        primerPagoManual ? MontoDelMes(plan, 2) : sinAlta ? MontoDelMes(plan, 1) : MontoInicial(plan);

    /// <summary>La promoción en curso de la suscripción, o null si el plan no tiene o ya terminó.</summary>
    public static Models.DTOs.PromoDto? InfoPromo(
        Models.Entities.MpPlan plan, Models.Entities.MpSuscripcion suscripcion, int cobrosRealizados, DateTime? proximoCobro)
    {
        if (!TienePromo(plan)) return null;
        var mes = MesDelProximoCobro(suscripcion, cobrosRealizados);
        var meses = plan.MesesPromo!.Value;
        if (mes > meses) return null;

        DateTime? Fecha(int periodos) => proximoCobro is null ? null
            : plan.TipoFrecuencia == "months" ? proximoCobro.Value.AddMonths(plan.Frecuencia * periodos)
            : proximoCobro.Value.AddDays(plan.Frecuencia * periodos);

        return new Models.DTOs.PromoDto
        {
            MontoPromo = plan.MontoPromo!.Value,
            MesesPromo = meses,
            MesActual = Math.Max(1, mes),
            MontoNormal = plan.Monto,
            UltimoCobroPromo = Fecha(meses - mes),
            NormalDesde = Fecha(meses - mes + 1),
        };
    }

    /// <summary>CobrosBase de una suscripción nueva: con el primer mes pagado a mano, el primer cobro de MP es el mes 2.</summary>
    public static int CobrosBaseInicial(bool primerPagoManual) => primerPagoManual ? -1 : 0;

    /// <summary>Con el primer pago hecho a mano, el primer cobro de la suscripción llega un período después del alta.</summary>
    public static DateTime PrimerCobro(Models.Entities.MpPlan plan, DateTime desde, bool primerPagoManual) =>
        primerPagoManual
            ? FechaCobroHelper.Calcular(desde, plan.Frecuencia, plan.TipoFrecuencia, null)
            : FechaCobroHelper.PrimerCobro(desde, plan.DiasGratis);

    /// <summary>Monto con el que se crea la suscripción en MP: el del primer cobro si el plan lo define.</summary>
    public static decimal MontoInicial(Models.Entities.MpPlan plan) => plan.MontoPrimerCobro ?? MontoDelMes(plan, 1);

    /// <summary>Si el primer cobro difiere del que sigue, hay que ajustar el monto cuando ese cobro se aprueba.</summary>
    public static bool RequiereAjuste(Models.Entities.MpPlan plan)
        => MontoInicial(plan) != MontoDelMes(plan, 2);
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
