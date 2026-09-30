using System.Data;
using Dapper;
using Dapper.Contrib.Extensions;
using Models.Entities;
using Repositories;

namespace DataAccess;

public class MpDunningLogRepository : IMpDunningLogRepository
{
    private readonly IDbConnection _db;

    public MpDunningLogRepository(IDbConnection db) => _db = db;

    public Task<MpDunningLog?> GetByIdAsync(int id)
        => _db.GetAsync<MpDunningLog>(id)!;

    public async Task<int> InsertAsync(MpDunningLog entity)
        => (int)await _db.InsertAsync(entity);

    public Task<bool> UpdateAsync(MpDunningLog entity)
        => _db.UpdateAsync(entity);

    public Task<bool> DeleteAsync(MpDunningLog entity)
        => _db.DeleteAsync(entity);

    public async Task<IEnumerable<MpDunningLog>> ObtenerPorSuscripcionAsync(int mpSuscripcionId)
    {
        const string sql = """
            SELECT * FROM MpDunningLogs
            WHERE MpSuscripcionId = @MpSuscripcionId
            ORDER BY FechaEjecucion DESC
            """;
        return await _db.QueryAsync<MpDunningLog>(sql, new { MpSuscripcionId = mpSuscripcionId });
    }
}
