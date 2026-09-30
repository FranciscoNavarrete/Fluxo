using System.Data;
using Dapper;
using Dapper.Contrib.Extensions;
using Models.Entities;
using Repositories;

namespace DataAccess;

public class MpPagoUnicoRepository : IMpPagoUnicoRepository
{
    private readonly IDbConnection _db;

    public MpPagoUnicoRepository(IDbConnection db) => _db = db;

    public Task<MpPagoUnico?> GetByIdAsync(int id)
        => _db.GetAsync<MpPagoUnico>(id)!;

    public async Task<int> InsertAsync(MpPagoUnico entity)
        => (int)await _db.InsertAsync(entity);

    public Task<bool> UpdateAsync(MpPagoUnico entity)
        => _db.UpdateAsync(entity);

    public Task<bool> DeleteAsync(MpPagoUnico entity)
        => _db.DeleteAsync(entity);

    public async Task<MpPagoUnico?> ObtenerPorPreferenceIdAsync(string mpPreferenceId)
    {
        const string sql = """
            SELECT * FROM MpPagosUnicos
            WHERE MpPreferenceId = @MpPreferenceId
            """;
        return await _db.QueryFirstOrDefaultAsync<MpPagoUnico>(sql, new { MpPreferenceId = mpPreferenceId });
    }

    public async Task<MpPagoUnico?> ObtenerPorExternalReferenceAsync(string externalReference)
    {
        const string sql = """
            SELECT * FROM MpPagosUnicos
            WHERE ExternalReference = @ExternalReference
            """;
        return await _db.QueryFirstOrDefaultAsync<MpPagoUnico>(sql, new { ExternalReference = externalReference });
    }

    public async Task<IEnumerable<MpPagoUnico>> ListarPorClienteAsync(int clienteId)
    {
        const string sql = """
            SELECT * FROM MpPagosUnicos
            WHERE ClienteId = @ClienteId
            ORDER BY FechaCreacion DESC
            """;
        return await _db.QueryAsync<MpPagoUnico>(sql, new { ClienteId = clienteId });
    }

    public async Task<MpPagoUnico?> ObtenerPorIdAsync(int mpPagoUnicoId)
        => await _db.GetAsync<MpPagoUnico>(mpPagoUnicoId);

    public async Task<bool> EliminarAsync(int mpPagoUnicoId)
    {
        const string sql = "DELETE FROM MpPagosUnicos WHERE MpPagoUnicoId = @Id AND Estado = 'pending'";
        return await _db.ExecuteAsync(sql, new { Id = mpPagoUnicoId }) > 0;
    }

    // Devuelve pagos únicos aprobados que pausaron una suscripción y cuya fecha de reanudación ya llegó
    public async Task<IEnumerable<MpPagoUnico>> ObtenerPendientesDeReanudacionAsync(DateTime hasta)
    {
        const string sql = """
            SELECT * FROM MpPagosUnicos
            WHERE MpSuscripcionId IS NOT NULL
              AND FechaReanudacion IS NOT NULL
              AND FechaReanudacion <= @Hasta
              AND Estado = 'approved'
            """;
        return await _db.QueryAsync<MpPagoUnico>(sql, new { Hasta = hasta });
    }
}
