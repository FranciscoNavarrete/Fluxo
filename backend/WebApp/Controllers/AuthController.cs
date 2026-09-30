using BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Requests;
using Models.DTOs;

namespace WebApp.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthLogic _authLogic;

    public AuthController(IAuthLogic authLogic) => _authLogic = authLogic;

    /// <summary>Login con email y contraseña. Devuelve JWT.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var resultado = await _authLogic.LoginAsync(request);
        return resultado.Exitoso ? Ok(resultado) : Unauthorized(resultado);
    }

    /// <summary>Registro de un nuevo cliente. Crea cuenta y devuelve JWT.</summary>
    [HttpPost("registro")]
    public async Task<IActionResult> Registro([FromBody] RegistroRequest request)
    {
        var resultado = await _authLogic.RegistroAsync(request);
        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }

    /// <summary>Cambia la contraseña del usuario autenticado (primer login con contraseña temporal).</summary>
    [HttpPost("cambiar-password")]
    [Authorize]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordRequest request)
    {
        var userIdStr = User.FindFirst("userId")?.Value;
        if (!int.TryParse(userIdStr, out int usuarioId))
            return Unauthorized();

        var resultado = await _authLogic.CambiarPasswordAsync(usuarioId, request.NuevaPassword);
        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }

    /// <summary>Solicita un token de reset de contraseña para el email dado.</summary>
    [HttpPost("solicitar-reset")]
    public async Task<IActionResult> SolicitarReset([FromBody] SolicitarResetRequest request)
    {
        var resultado = await _authLogic.SolicitarResetAsync(request.Email);
        return Ok(resultado); // siempre 200 para no revelar si el email existe
    }

    /// <summary>Cambia la contraseña usando el token de reset.</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var resultado = await _authLogic.ResetPasswordAsync(request.Token, request.NuevaPassword);
        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }
}
