// ============================================================
// AGREGADO A LA INTERFAZ IUnitOfWork.cs EXISTENTE
// ============================================================
// Agregar las 4 propiedades de solo lectura nuevas:
//
//    IMpPlanRepository        MpPlan        { get; }
//    IMpSuscripcionRepository MpSuscripcion { get; }
//    IMpTransaccionRepository MpTransaccion { get; }
//    IMpDunningLogRepository  MpDunningLog  { get; }
//
// ============================================================
// EJEMPLO COMPLETO de cómo queda la interfaz (fragmento):

/*
using Repositories;

public interface IUnitOfWork
{
    // ... propiedades existentes ...

    IMpPlanRepository        MpPlan        { get; }
    IMpSuscripcionRepository MpSuscripcion { get; }
    IMpTransaccionRepository MpTransaccion { get; }
    IMpDunningLogRepository  MpDunningLog  { get; }
}
*/
