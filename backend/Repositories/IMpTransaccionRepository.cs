using Models.DTOs;
using Models.Entities;

namespace Repositories;

public interface IMpTransaccionRepository : IRepository<MpTransaccion>
{
    Task<MpTransaccion?> ObtenerPorGatewayPagoIdAsync(string gatewayPagoId);
    Task<IEnumerable<MpTransaccionDto>> ListarPorSuscripcionAsync(int mpSuscripcionId);
    Task<int> ContarIntentosPorSuscripcionAsync(int mpSuscripcionId);
}
