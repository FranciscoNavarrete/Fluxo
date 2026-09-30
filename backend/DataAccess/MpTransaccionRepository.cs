using System.Data;
using Dapper;
using Dapper.Contrib.Extensions;
using Models.DTOs;
using Models.Entities;
using Repositories;

namespace DataAccess;

public class MpTransaccionRepository : IMpTransaccionRepository
{
    private readonly IDbConnection _db;

    public MpTransaccionRepository(IDbConnection db) => _db = db;

    public Task<MpTransaccion?> GetByIdAsync(int id)
        => _db.GetAsync<MpTransaccion>(id)!;

    public async Task<int> InsertAsync(MpTransaccion entity)
        => (int)await _db.InsertAsync(entity);

    public Task<bool> UpdateAsync(MpTransaccion entity)
        => _db.UpdateAsync(entity);

    public Task<bool> DeleteAsync(MpTransaccion entity)
        => _db.DeleteAsync(entity);

    public async Task<MpTransaccion?> ObtenerPorGatewayPagoIdAsync(string gatewayPagoId)
    {
        const string sql = """
            SELECT * FROM MpTransacciones
            WHERE GatewayPagoId = @GatewayPagoId
            """;
        return await _db.QueryFirstOrDefaultAsync<MpTransaccion>(sql, new { GatewayPagoId = gatewayPagoId });
    }

    public async Task<IEnumerable<MpTransaccionDto>> ListarPorSuscripcionAsync(int mpSuscripcionId)
    {
        const string sql = """
            SELECT
                t.MpTransaccionId,
                t.MpSuscripcionId,
                s.GatewaySuscripcionId,
                s.ClienteId,
                c.Nombre    AS ClienteNombre,
                c.Email     AS ClienteEmail,
                t.GatewayPagoId,
                t.Monto,
                t.Moneda,
                t.Estado,
                t.EstadoDetalle,
                t.NumeroIntento,
                t.FechaProcesado
            FROM MpTransacciones t
            INNER JOIN MpSuscripciones s ON s.MpSuscripcionId = t.MpSuscripcionId
            INNER JOIN Clientes        c ON c.ClienteId       = s.ClienteId
            WHERE t.MpSuscripcionId = @MpSuscripcionId
            ORDER BY t.FechaProcesado DESC
            """;
        return await _db.QueryAsync<MpTransaccionDto>(sql, new { MpSuscripcionId = mpSuscripcionId });
    }

    public async Task<int> ContarIntentosPorSuscripcionAsync(int mpSuscripcionId)
    {
        const string sql = """
            SELECT COUNT(*) FROM MpTransacciones
            WHERE MpSuscripcionId = @MpSuscripcionId
            """;
        return await _db.ExecuteScalarAsync<int>(sql, new { MpSuscripcionId = mpSuscripcionId });
    }
}
