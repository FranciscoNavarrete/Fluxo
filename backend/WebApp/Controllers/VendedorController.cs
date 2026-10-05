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
    private readonly IAuthLogic          _authLogic;
    private readonly IMpPlanLogic        _planLogic;
    private readonly IMpSuscripcionLogic _suscripcionLogic;

    public VendedorController(IAuthLogic authLogic, IMpPlanLogic planLogic, IMpSuscripcionLogic suscripcionLogic)
    {
        _authLogic = authLogic;
        _planLogic = planLogic;
        _suscripcionLogic = suscripcionLogic;
    }

    /// <summary>
    /// Crea el cliente + usuario + suscripción en Fluxo para un negocio nuevo de GestorPOS.
    /// Devuelve la contraseña temporal (por si el negocio necesita entrar directo a Fluxo) y el
    /// init_point de Mercado Pago para que el dueño del negocio cargue su tarjeta.
    /// </summary>
    [HttpPost("suscripciones/iniciar")]
    public async Task<IActionResult> IniciarSuscripcion([FromBody] CrearClienteAdminRequest request)
    {
        var usuarioSistemaId = await _authLogic.ObtenerOCrearUsuarioSistemaIdAsync();
        var resultado = await _authLogic.CrearClienteAdminAsync(request, usuarioSistemaId, exigirSuscripcion: true);
        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }

    /// <summary>Planes activos, para que GestorPOS deje elegir cuál asignarle al negocio nuevo.</summary>
    [HttpGet("planes")]
    public async Task<IActionResult> ListarPlanes()
    {
        var resultado = await _planLogic.ListarActivosAsync();
        return Ok(resultado);
    }

    /// <summary>Historial de cobros de una suscripción (con motivo de los rechazos) y próximo cobro.</summary>
    [HttpGet("suscripciones/{mpSuscripcionId:int}/cobros")]
    public async Task<IActionResult> ObtenerCobros(int mpSuscripcionId)
    {
        var resultado = await _suscripcionLogic.ObtenerCobrosAsync(mpSuscripcionId);
        return resultado.Exitoso ? Ok(resultado) : NotFound(resultado);
    }

    /// <summary>Link de pago de una suscripción pendiente, para que GestorPOS lo pueda volver a mostrar.</summary>
    [HttpGet("suscripciones/{mpSuscripcionId:int}/link")]
    public async Task<IActionResult> ObtenerLinkPago(int mpSuscripcionId)
    {
        var resultado = await _suscripcionLogic.ObtenerLinkPagoAsync(mpSuscripcionId);
        return resultado.Exitoso ? Ok(resultado) : NotFound(resultado);
    }

    /// <summary>
    /// Estado (pending/authorized/paused/...) de un lote de suscripciones, para que GestorPOS
    /// pinte "Pendiente"/"Suscripto" en su listado de negocios sin pegarle a Fluxo una vez por
    /// cada uno. ids va separado por comas: ?ids=1,2,3
    /// </summary>
    [HttpGet("suscripciones/estados")]
    public async Task<IActionResult> ObtenerEstadosSuscripciones([FromQuery] string ids)
    {
        var idsParseados = (ids ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => int.TryParse(id, out var n) ? n : (int?)null)
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .ToArray();

        if (idsParseados.Length == 0)
            return Ok(new { exitoso = true, mensaje = "", contenido = Array.Empty<object>() });

        var resultado = await _suscripcionLogic.ObtenerEstadosAsync(idsParseados);
        return Ok(resultado);
    }
}
