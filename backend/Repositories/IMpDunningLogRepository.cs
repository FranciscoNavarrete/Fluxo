using Models.Entities;

namespace Repositories;

public interface IMpDunningLogRepository : IRepository<MpDunningLog>
{
    Task<IEnumerable<MpDunningLog>> ObtenerPorSuscripcionAsync(int mpSuscripcionId);
}
