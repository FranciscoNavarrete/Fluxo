using System.Data;
using Repositories;
using UnitOfWork;

namespace DataAccess;

public class DataAccessUnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _connection;

    // ── Auth ────────────────────────────────────────────────────
    private IUsuarioRepository?       _usuario;
    private IClienteRepository?       _cliente;

    // ── Mercado Pago ────────────────────────────────────────────
    private IMpPlanRepository?        _mpPlan;
    private IMpSuscripcionRepository? _mpSuscripcion;
    private IMpTransaccionRepository? _mpTransaccion;
    private IMpDunningLogRepository?  _mpDunningLog;
    private IMpPagoUnicoRepository?   _pagoUnico;

    public DataAccessUnitOfWork(IDbConnection connection)
        => _connection = connection;

    public IUsuarioRepository       Usuario       => _usuario       ??= new UsuarioRepository(_connection);
    public IClienteRepository       Cliente       => _cliente       ??= new ClienteRepository(_connection);
    public IMpPlanRepository        MpPlan        => _mpPlan        ??= new MpPlanRepository(_connection);
    public IMpSuscripcionRepository MpSuscripcion => _mpSuscripcion ??= new MpSuscripcionRepository(_connection);
    public IMpTransaccionRepository MpTransaccion => _mpTransaccion ??= new MpTransaccionRepository(_connection);
    public IMpDunningLogRepository  MpDunningLog  => _mpDunningLog  ??= new MpDunningLogRepository(_connection);
    public IMpPagoUnicoRepository   PagoUnico     => _pagoUnico     ??= new MpPagoUnicoRepository(_connection);
}
