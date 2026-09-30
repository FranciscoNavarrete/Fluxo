using Repositories;

namespace UnitOfWork;

public interface IUnitOfWork
{
    // ── Auth ────────────────────────────────────────────────────
    IUsuarioRepository       Usuario       { get; }
    IClienteRepository       Cliente       { get; }

    // ── Mercado Pago ────────────────────────────────────────────
    IMpPlanRepository        MpPlan        { get; }
    IMpSuscripcionRepository MpSuscripcion { get; }
    IMpTransaccionRepository MpTransaccion { get; }
    IMpDunningLogRepository  MpDunningLog  { get; }
    IMpPagoUnicoRepository   PagoUnico     { get; }
}
