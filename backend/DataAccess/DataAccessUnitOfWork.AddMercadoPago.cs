// ============================================================
// AGREGADO AL DataAccessUnitOfWork.cs EXISTENTE
// ============================================================
// 1. Agregar los 4 campos privados (lazy init):
//
//    private IMpPlanRepository?        _mpPlan;
//    private IMpSuscripcionRepository? _mpSuscripcion;
//    private IMpTransaccionRepository? _mpTransaccion;
//    private IMpDunningLogRepository?  _mpDunningLog;
//
// 2. Agregar las 4 propiedades públicas:
//
//    public IMpPlanRepository MpPlan =>
//        _mpPlan ??= new MpPlanRepository(_connection);
//
//    public IMpSuscripcionRepository MpSuscripcion =>
//        _mpSuscripcion ??= new MpSuscripcionRepository(_connection);
//
//    public IMpTransaccionRepository MpTransaccion =>
//        _mpTransaccion ??= new MpTransaccionRepository(_connection);
//
//    public IMpDunningLogRepository MpDunningLog =>
//        _mpDunningLog ??= new MpDunningLogRepository(_connection);
//
// ============================================================
// EJEMPLO COMPLETO de cómo queda la clase (fragmento):

/*
using DataAccess;
using Repositories;
using System.Data;

public class DataAccessUnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _connection;

    // ... repos existentes ...

    private IMpPlanRepository?        _mpPlan;
    private IMpSuscripcionRepository? _mpSuscripcion;
    private IMpTransaccionRepository? _mpTransaccion;
    private IMpDunningLogRepository?  _mpDunningLog;

    public DataAccessUnitOfWork(IDbConnection connection)
    {
        _connection = connection;
    }

    // ... propiedades existentes ...

    public IMpPlanRepository MpPlan =>
        _mpPlan ??= new MpPlanRepository(_connection);

    public IMpSuscripcionRepository MpSuscripcion =>
        _mpSuscripcion ??= new MpSuscripcionRepository(_connection);

    public IMpTransaccionRepository MpTransaccion =>
        _mpTransaccion ??= new MpTransaccionRepository(_connection);

    public IMpDunningLogRepository MpDunningLog =>
        _mpDunningLog ??= new MpDunningLogRepository(_connection);
}
*/
