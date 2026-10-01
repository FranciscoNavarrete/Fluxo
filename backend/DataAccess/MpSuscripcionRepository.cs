using System.Data;
using Dapper;
using Dapper.Contrib.Extensions;
using Models.DTOs;
using Models.Entities;
using Models.Helpers;
using Repositories;

namespace DataAccess;

public class MpSuscripcionRepository : IMpSuscripcionRepository
{
    private readonly IDbConnection _db;

    public MpSuscripcionRepository(IDbConnection db) => _db = db;

    public Task<MpSuscripcion?> GetByIdAsync(int id)
        => _db.GetAsync<MpSuscripcion>(id)!;

    public Task<int> InsertAsync(MpSuscripcion entity)
        => _db.InsertLowercaseAsync(entity);

    public Task<bool> UpdateAsync(MpSuscripcion entity)
        => _db.UpdateLowercaseAsync(entity);

    public Task<bool> DeleteAsync(MpSuscripcion entity)
        => _db.DeleteAsync(entity);

    public async Task<IEnumerable<MpSuscripcion>> ObtenerPorIdsAsync(IEnumerable<int> ids)
    {
        const string sql = """
            SELECT * FROM MpSuscripciones
            WHERE MpSuscripcionId = ANY(@Ids)
            """;
        return await _db.QueryAsync<MpSuscripcion>(sql, new { Ids = ids.ToArray() });
    }

    public async Task<MpSuscripcionDto?> ObtenerConDetalleAsync(int mpSuscripcionId)
    {
        const string sql = """
            SELECT
                s.MpSuscripcionId,
                s.ClienteId,
                c.Nombre        AS ClienteNombre,
                c.Email         AS ClienteEmail,
                s.MpPlanId,
                p.Nombre        AS PlanNombre,
                p.Monto         AS PlanMonto,
                p.Moneda        AS PlanMoneda,
                p.TipoFrecuencia,
                p.Frecuencia,
                s.GatewaySuscripcionId,
                s.GatewayProveedor,
                s.MpPayerId,
                s.InitPoint,
                s.Estado,
                s.FechaInicio,
                s.ProximoCobro,
                s.UltimoCobro,
                s.FechaSuspension,
                s.FechaCancelacion,
                s.MotivoCancelacion,
                s.IntentosReintento,
                s.MaxReintentos,
                s.ConsentimientoFecha,
                s.ConsentimientoIp,
                s.TerminosVersion,
                s.FechaHoraCreacion,
                s.UsuarioCreacionId
            FROM MpSuscripciones s
            INNER JOIN Clientes    c ON c.ClienteId = s.ClienteId
            INNER JOIN MpPlanes    p ON p.MpPlanId  = s.MpPlanId
            WHERE s.MpSuscripcionId = @MpSuscripcionId
            """;
        return await _db.QueryFirstOrDefaultAsync<MpSuscripcionDto>(sql, new { MpSuscripcionId = mpSuscripcionId });
    }

    public async Task<MpSuscripcion?> ObtenerPorGatewayIdAsync(string gatewaySuscripcionId)
    {
        const string sql = """
            SELECT * FROM MpSuscripciones
            WHERE GatewaySuscripcionId = @GatewaySuscripcionId
            """;
        return await _db.QueryFirstOrDefaultAsync<MpSuscripcion>(sql, new { GatewaySuscripcionId = gatewaySuscripcionId });
    }

    public async Task<MpSuscripcion?> ObtenerActivaPorClienteAsync(int clienteId)
    {
        const string sql = """
            SELECT * FROM MpSuscripciones
            WHERE ClienteId = @ClienteId
              AND Estado IN ('authorized', 'pending', 'paused')
            """;
        return await _db.QueryFirstOrDefaultAsync<MpSuscripcion>(sql, new { ClienteId = clienteId });
    }

    public async Task<ResultadoListaPaginada<MpSuscripcionDto>> ListarPaginadoAsync(
        int pagina, int tamanioPagina, string? estado = null, int? clienteId = null)
    {
        var whereClausulas = new List<string>();
        var parametros = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(estado))
        {
            whereClausulas.Add("s.Estado = @Estado");
            parametros.Add("Estado", estado);
        }
        if (clienteId.HasValue)
        {
            whereClausulas.Add("s.ClienteId = @ClienteId");
            parametros.Add("ClienteId", clienteId.Value);
        }

        string where = whereClausulas.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClausulas)
            : string.Empty;

        int offset = (pagina - 1) * tamanioPagina;
        parametros.Add("Offset", offset);
        parametros.Add("TamanioPagina", tamanioPagina);

        string sqlData = $"""
            SELECT
                s.MpSuscripcionId,
                s.ClienteId,
                c.Nombre        AS ClienteNombre,
                c.Email         AS ClienteEmail,
                s.MpPlanId,
                p.Nombre        AS PlanNombre,
                p.Monto         AS PlanMonto,
                p.Moneda        AS PlanMoneda,
                p.TipoFrecuencia,
                p.Frecuencia,
                s.GatewaySuscripcionId,
                s.GatewayProveedor,
                s.MpPayerId,
                s.InitPoint,
                s.Estado,
                s.FechaInicio,
                s.ProximoCobro,
                s.UltimoCobro,
                s.FechaSuspension,
                s.FechaCancelacion,
                s.MotivoCancelacion,
                s.IntentosReintento,
                s.MaxReintentos,
                s.ConsentimientoFecha,
                s.ConsentimientoIp,
                s.TerminosVersion,
                s.FechaHoraCreacion,
                s.UsuarioCreacionId
            FROM MpSuscripciones s
            INNER JOIN Clientes c ON c.ClienteId = s.ClienteId
            INNER JOIN MpPlanes p ON p.MpPlanId  = s.MpPlanId
            {where}
            ORDER BY s.FechaHoraCreacion DESC
            LIMIT @TamanioPagina OFFSET @Offset
            """;

        string sqlCount = $"""
            SELECT COUNT(*)
            FROM MpSuscripciones s
            INNER JOIN Clientes c ON c.ClienteId = s.ClienteId
            INNER JOIN MpPlanes p ON p.MpPlanId  = s.MpPlanId
            {where}
            """;

        var lista = await _db.QueryAsync<MpSuscripcionDto>(sqlData, parametros);
        int total = await _db.ExecuteScalarAsync<int>(sqlCount, parametros);

        return new ResultadoListaPaginada<MpSuscripcionDto>
        {
            ListaResultado = lista,
            TotalFilas = total
        };
    }

    // Retorna suscripciones activas/pausadas cuyo ProximoCobro ya venció
    public async Task<IEnumerable<MpSuscripcion>> ObtenerVencidasParaDunningAsync()
    {
        const string sql = """
            SELECT * FROM MpSuscripciones
            WHERE Estado IN ('authorized', 'paused')
              AND proximocobro < NOW()
              AND FechaCancelacion IS NULL
            """;
        return await _db.QueryAsync<MpSuscripcion>(sql);
    }
}
