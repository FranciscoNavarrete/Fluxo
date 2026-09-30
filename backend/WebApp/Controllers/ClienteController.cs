using BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Requests;

namespace WebApp.Controllers;

[ApiController]
[Route("api/cliente")]
[Authorize(Roles = "CLIENTE")]
public class ClienteController : BaseApiController
{
    private readonly IAuthLogic _authLogic;

    public ClienteController(IAuthLogic authLogic) => _authLogic = authLogic;

    [HttpGet("perfil")]
    public async Task<IActionResult> ObtenerPerfil()
    {
        var clienteId = ObtenerClienteId();
        if (clienteId is null) return Unauthorized();

        var resultado = await _authLogic.ObtenerPerfilAsync(clienteId.Value);
        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }

    [HttpPut("perfil")]
    public async Task<IActionResult> ActualizarPerfil([FromBody] ActualizarPerfilRequest request)
    {
        var clienteId = ObtenerClienteId();
        if (clienteId is null) return Unauthorized();

        var resultado = await _authLogic.ActualizarPerfilAsync(clienteId.Value, request);
        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }
}
