using BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Requests;

namespace WebApp.Controllers;

[ApiController]
[Route("api/mp/planes")]
[Authorize]
public class MpPlanesController : BaseApiController
{
    private readonly IMpPlanLogic _planLogic;

    public MpPlanesController(IMpPlanLogic planLogic)
    {
        _planLogic = planLogic;
    }

    /// <summary>Lista todos los planes activos. Accesible para cualquier usuario autenticado.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _planLogic.ListarActivosAsync();
        return Ok(resultado);
    }

    /// <summary>Obtiene un plan por su ID interno.</summary>
    [HttpGet("{mpPlanId:int}")]
    public async Task<IActionResult> ObtenerPorId(int mpPlanId)
    {
        var resultado = await _planLogic.ObtenerPorIdAsync(mpPlanId);
        return resultado.Exitoso ? Ok(resultado) : NotFound(resultado);
    }

    /// <summary>Crea un nuevo plan y lo registra en Mercado Pago.</summary>
    [HttpPost]
    [Authorize(Roles = "SISTEMA,ADMINISTRADOR")]
    public async Task<IActionResult> Crear([FromBody] CrearPlanRequest request)
    {
        var resultado = await _planLogic.CrearAsync(request, ObtenerUsuarioId());
        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }

    /// <summary>Edita un plan existente.</summary>
    [HttpPut]
    [Authorize(Roles = "SISTEMA,ADMINISTRADOR")]
    public async Task<IActionResult> Editar([FromBody] EditarPlanRequest request)
    {
        var resultado = await _planLogic.EditarAsync(request, ObtenerUsuarioId());
        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }

    /// <summary>Baja lógica del plan (Activo = false).</summary>
    [HttpDelete("{mpPlanId:int}")]
    [Authorize(Roles = "SISTEMA,ADMINISTRADOR")]
    public async Task<IActionResult> Eliminar(int mpPlanId)
    {
        var resultado = await _planLogic.EliminarAsync(mpPlanId, ObtenerUsuarioId());
        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }
}
