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

    public Task<int> InsertAsync(MpPlan entity)
        => _db.InsertLowercaseAsync(entity);

    public Task<bool> UpdateAsync(MpPlan entity)
        => _db.UpdateLowercaseAsync(entity);

    public Task<bool> DeleteAsync(MpPlan entity)
        => _db.DeleteAsync(entity);

    public async Task<MpPlan?> ObtenerPorMpExternoIdAsync(string mpPlanExternoId)
    {
        const string sql = """
            SELECT * FROM mpplanes
            WHERE mpplanexternoid = @MpPlanExternoId AND activo = TRUE
            """;
        return await _db.QueryFirstOrDefaultAsync<MpPlan>(sql, new { MpPlanExternoId = mpPlanExternoId });
    }

    public async Task<IEnumerable<MpPlanDto>> ListarActivosAsync()
    {
        const string sql = """
            SELECT
                MpPlanId, Nombre, Descripcion, Monto, Moneda,
                TipoFrecuencia, Frecuencia, DiasGratis, Repeticiones, MontoPrimerCobro,
                MpPlanExternoId, Activo, FechaHoraCreacion, UsuarioCreacionId
            FROM mpplanes
            WHERE activo = TRUE
            ORDER BY Nombre
            """;
        return await _db.QueryAsync<MpPlanDto>(sql);
    }
}
