using BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Requests;

namespace WebApp.Controllers;

/// <summary>Integración server-to-server con GestorPOS: al dar de alta un negocio, el vendedor
/// llama acá para arrancar la suscripción del negocio en Fluxo. Gateado por API key propia
/// (VendedorApiKeyMiddleware), no por el JWT de cliente/admin — no hay un usuario humano logueado.</summary>
[ApiController]
[Route("api/vendedor")]
[AllowAnonymous]
public class VendedorController : BaseApiController
{
    private readonly IAuthLogic _authLogic;

    public VendedorController(IAuthLogic authLogic) => _authLogic = authLogic;

    /// <summary>
    /// Crea el cliente + usuario + suscripción en Fluxo para un negocio nuevo de GestorPOS.
    /// Devuelve la contraseña temporal (por si el negocio necesita entrar directo a Fluxo) y el
    /// init_point de Mercado Pago para que el dueño del negocio cargue su tarjeta.
    /// </summary>
    [HttpPost("suscripciones/iniciar")]
    public async Task<IActionResult> IniciarSuscripcion([FromBody] CrearClienteAdminRequest request)
    {
        var usuarioSistemaId = await _authLogic.ObtenerOCrearUsuarioSistemaIdAsync();
        var resultado = await _authLogic.CrearClienteAdminAsync(request, usuarioSistemaId);
        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }
}
