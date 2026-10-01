using Models.DTOs;
using Models.Entities;
using Models.Helpers;

namespace Repositories;

public interface IMpSuscripcionRepository : IRepository<MpSuscripcion>
{
    Task<MpSuscripcionDto?> ObtenerConDetalleAsync(int mpSuscripcionId);
    Task<IEnumerable<MpSuscripcion>> ObtenerPorIdsAsync(IEnumerable<int> ids);
    Task<MpSuscripcion?> ObtenerPorGatewayIdAsync(string gatewaySuscripcionId);
    Task<MpSuscripcion?> ObtenerActivaPorClienteAsync(int clienteId);
    Task<ResultadoListaPaginada<MpSuscripcionDto>> ListarPaginadoAsync(
        int pagina, int tamanioPagina, string? estado = null, int? clienteId = null);
    Task<IEnumerable<MpSuscripcion>> ObtenerVencidasParaDunningAsync();
}
