using BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Requests;

namespace WebApp.Controllers;

[ApiController]
[Route("api/admin/clientes")]
[Authorize(Roles = "ADMINISTRADOR,SISTEMA")]
public class AdminClientesController : BaseApiController
{
    private readonly IAuthLogic _authLogic;

    public AdminClientesController(IAuthLogic authLogic) => _authLogic = authLogic;

    /// <summary>
    /// Crea un nuevo cliente + usuario + suscripción (opcional) en un solo paso.
    /// Devuelve la contraseña temporal (mostrar UNA sola vez al admin) y el init_point de MP.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearClienteAdminRequest request)
    {
        var resultado = await _authLogic.CrearClienteAdminAsync(request, ObtenerUsuarioId());
        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }
}
