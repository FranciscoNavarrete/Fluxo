using System.Data;
using Dapper;
using Dapper.Contrib.Extensions;
using Models.DTOs;
using Models.Entities;
using Repositories;

namespace DataAccess;

public class MpPlanRepository : IMpPlanRepository
{
    private readonly IDbConnection _db;

    public MpPlanRepository(IDbConnection db) => _db = db;

    public Task<MpPlan?> GetByIdAsync(int id)
        => _db.GetAsync<MpPlan>(id)!;

    public async Task<int> InsertAsync(MpPlan entity)
        => (int)await _db.InsertAsync(entity);

    public Task<bool> UpdateAsync(MpPlan entity)
        => _db.UpdateAsync(entity);

    public Task<bool> DeleteAsync(MpPlan entity)
        => _db.DeleteAsync(entity);

    public async Task<MpPlan?> ObtenerPorMpExternoIdAsync(string mpPlanExternoId)
    {
        const string sql = """
            SELECT * FROM MpPlanes
            WHERE MpPlanExternoId = @MpPlanExternoId AND Activo = 1
            """;
        return await _db.QueryFirstOrDefaultAsync<MpPlan>(sql, new { MpPlanExternoId = mpPlanExternoId });
    }

    public async Task<IEnumerable<MpPlanDto>> ListarActivosAsync()
    {
        const string sql = """
            SELECT
                MpPlanId, Nombre, Descripcion, Monto, Moneda,
                TipoFrecuencia, Frecuencia, DiasGratis,
                MpPlanExternoId, Activo, FechaHoraCreacion, UsuarioCreacionId
            FROM MpPlanes
            WHERE Activo = 1
            ORDER BY Nombre
            """;
        return await _db.QueryAsync<MpPlanDto>(sql);
    }
}
