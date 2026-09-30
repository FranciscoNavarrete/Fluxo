using Models.DTOs;
using Models.Helpers;
using Models.Requests;

namespace BusinessLogic;

public interface IMpPlanLogic
{
    Task<RespuestaResultado<MpPlanDto>> CrearAsync(CrearPlanRequest request, int usuarioId);
    Task<RespuestaResultado<MpPlanDto>> EditarAsync(EditarPlanRequest request, int usuarioId);
    Task<RespuestaResultado<bool>> EliminarAsync(int mpPlanId, int usuarioId);
    Task<RespuestaResultado<MpPlanDto>> ObtenerPorIdAsync(int mpPlanId);
    Task<RespuestaResultado<IEnumerable<MpPlanDto>>> ListarActivosAsync();
}
