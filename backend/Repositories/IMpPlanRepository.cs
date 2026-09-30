using Models.DTOs;
using Models.Entities;

namespace Repositories;

public interface IMpPlanRepository : IRepository<MpPlan>
{
    Task<MpPlan?> ObtenerPorMpExternoIdAsync(string mpPlanExternoId);
    Task<IEnumerable<MpPlanDto>> ListarActivosAsync();
}
