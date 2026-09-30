using BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Requests;

namespace WebApp.Controllers;

[ApiController]
[Route("api/mp/suscripciones")]
[Authorize]
public class MpSuscripcionesController : BaseApiController
{
    private readonly IMpSuscripcionLogic _suscripcionLogic;

    public MpSuscripcionesController(IMpSuscripcionLogic suscripcionLogic)
    {
        _suscripcionLogic = suscripcionLogic;
    }

    /// <summary>
    /// Lista suscripciones con paginación.
    /// Admin/Sistema: puede filtrar por cualquier clienteId.
    /// Cliente: solo ve sus propias suscripciones (clienteId tomado del JWT).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanioPagina = 20,
        [FromQuery] string? estado = null,
        [FromQuery] int? clienteId = null)
    {
        var esAdmin = User.IsInRole("ADMINISTRADOR") || User.IsInRole("SISTEMA");
        if (!esAdmin)
        {
            // El cliente solo puede consultar sus propias suscripciones
            var clienteIdJwt = ObtenerClienteId();
            if (clienteIdJwt == null) return Forbid();
            clienteId = clienteIdJwt;
        }

        var resultado = await _suscripcionLogic.ListarAsync(pagina, tamanioPagina, estado, clienteId);
        return Ok(resultado);
    }

    /// <summary>Obtiene el detalle de una suscripción con datos del plan y del cliente.</summary>
    [HttpGet("{mpSuscripcionId:int}")]
    public async Task<IActionResult> ObtenerPorId(int mpSuscripcionId)
    {
        var resultado = await _suscripcionLogic.ObtenerPorIdAsync(mpSuscripcionId);
        return resultado.Exitoso ? Ok(resultado) : NotFound(resultado);
    }

    /// <summary>
    /// Opción B (principal): crea la suscripción en MP sin tarjeta y devuelve init_point.
    /// El frontend redirige al cliente a esa URL para que ingrese su tarjeta en el checkout de MP.
    /// </summary>
    [HttpPost("crear-con-redirect")]
    public async Task<IActionResult> CrearConRedirect([FromBody] CrearSuscripcionRedirectRequest request)
    {
        // Capturar IP y UserAgent del cliente para el mandato de débito
        string ipCliente = ObtenerIpCliente();
        string userAgent = Request.Headers.UserAgent.ToString();

        var resultado = await _suscripcionLogic.CrearConRedirectAsync(
            request, ObtenerUsuarioId(), ipCliente, userAgent);

        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }

    /// <summary>
    /// Opción A: crea la suscripción con un card_token generado por MP Bricks en el frontend.
    /// La tarjeta queda guardada en MP y los cobros se ejecutan automáticamente.
    /// </summary>
    [HttpPost("crear-con-token")]
    public async Task<IActionResult> CrearConToken([FromBody] CrearSuscripcionConTokenRequest request)
    {
        string ipCliente = ObtenerIpCliente();
        string userAgent = Request.Headers.UserAgent.ToString();

        var resultado = await _suscripcionLogic.CrearConTokenAsync(
            request, ObtenerUsuarioId(), ipCliente, userAgent);

        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }

    /// <summary>
    /// Lista las transacciones de una suscripción.
    /// Admin/Sistema: acceso a cualquier suscripción.
    /// Cliente: solo puede ver las transacciones de sus propias suscripciones.
    /// </summary>
    [HttpGet("{mpSuscripcionId:int}/transacciones")]
    public async Task<IActionResult> ListarTransacciones(int mpSuscripcionId)
    {
        var esAdmin = User.IsInRole("ADMINISTRADOR") || User.IsInRole("SISTEMA");
        int? clienteIdJwt = esAdmin ? null : ObtenerClienteId();

        if (!esAdmin && clienteIdJwt == null) return Forbid();

        var resultado = await _suscripcionLogic.ListarTransaccionesAsync(mpSuscripcionId, clienteIdJwt);
        return resultado.Exitoso ? Ok(resultado) : NotFound(resultado);
    }

    /// <summary>Cancela una suscripción activa tanto localmente como en Mercado Pago.</summary>
    [HttpPost("cancelar")]
    public async Task<IActionResult> Cancelar([FromBody] CancelarSuscripcionRequest request)
    {
        var resultado = await _suscripcionLogic.CancelarAsync(request, ObtenerUsuarioId());
        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }

    /// <summary>Actualiza el medio de pago asociado a la suscripción con un nuevo card_token.</summary>
    [HttpPut("actualizar-medio-pago")]
    public async Task<IActionResult> ActualizarMedioPago([FromBody] ActualizarMedioPagoRequest request)
    {
        var resultado = await _suscripcionLogic.ActualizarMedioPagoAsync(request, ObtenerUsuarioId());
        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }

    // ------------------------------------------------------------------
    private string ObtenerIpCliente()
    {
        // Respetar el header X-Forwarded-For si hay proxy/load balancer (ej: AWS ALB)
        string? forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
