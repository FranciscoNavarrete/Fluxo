namespace Models.DTOs;

/// <summary>Promoción en curso de una suscripción: cuánto paga mientras dure, en qué mes está y cuándo pasa al precio normal.
/// Solo existe mientras la promoción no terminó.</summary>
public class PromoDto
{
    public decimal MontoPromo { get; set; }
    public int MesesPromo { get; set; }

    /// <summary>Mes de la promoción al que corresponde el próximo cobro (1 = el primero).</summary>
    public int MesActual { get; set; }
    public decimal MontoNormal { get; set; }

    /// <summary>Fecha del último cobro a precio promocional (aproximada: se calcula desde el próximo cobro).</summary>
    public DateTime? UltimoCobroPromo { get; set; }

    /// <summary>Fecha del primer cobro al precio normal (aproximada).</summary>
    public DateTime? NormalDesde { get; set; }
}
