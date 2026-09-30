using BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Requests;

namespace WebApp.Controllers;

[ApiController]
[Route("api/mp/pagos-unicos")]
[Authorize]
public class MpPagosUnicosController : BaseApiController
{
    private readonly IMpPagoUnicoLogic _logic;

    public MpPagosUnicosController(IMpPagoUnicoLogic logic)
    {
        _logic = logic;
    }

    /// <summary>
    /// Crea un pago único para el monto de un plan.
    /// Si el cliente tiene suscripción activa, la pausa automáticamente.
    /// Admin puede crear para cualquier cliente. El cliente solo puede crear para sí mismo.
    /// </summary>
    [HttpPost("crear")]
    public async Task<IActionResult> Crear([FromBody] CrearPagoUnicoRequest request)
    {
        var esAdmin = User.IsInRole("ADMINISTRADOR") || User.IsInRole("SISTEMA");

        if (!esAdmin)
        {
            var clienteIdJwt = ObtenerClienteId();
            if (clienteIdJwt == null) return Forbid();

            // El cliente solo puede crear pagos para sí mismo
            request.ClienteId = clienteIdJwt.Value;

            // Si no viene email en el request, usar el del JWT
            if (string.IsNullOrEmpty(request.PayerEmail))
                request.PayerEmail = User.FindFirst("email")?.Value ?? string.Empty;
        }

        var resultado = await _logic.CrearPagoUnicoAsync(request, ObtenerUsuarioId());
        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }

    /// <summary>
    /// Devuelve el historial de pagos únicos del cliente autenticado.
    /// Admin puede consultar cualquier cliente pasando clienteId por query.
    /// </summary>
    [HttpGet("mis-pagos")]
    public async Task<IActionResult> MisPagos([FromQuery] int? clienteId = null)
    {
        var esAdmin = User.IsInRole("ADMINISTRADOR") || User.IsInRole("SISTEMA");

        int idConsulta;
        if (esAdmin && clienteId.HasValue)
        {
            idConsulta = clienteId.Value;
        }
        else
        {
            var clienteIdJwt = ObtenerClienteId();
            if (clienteIdJwt == null) return Forbid();
            idConsulta = clienteIdJwt.Value;
        }

        var resultado = await _logic.ObtenerPorClienteAsync(idConsulta);
        return Ok(resultado);
    }

    /// <summary>
    /// Elimina un pago único pendiente. El cliente solo puede eliminar los suyos.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var esAdmin = User.IsInRole("ADMINISTRADOR") || User.IsInRole("SISTEMA");
        int? clienteIdJwt = esAdmin ? null : ObtenerClienteId();

        if (!esAdmin && clienteIdJwt == null) return Forbid();

        var resultado = await _logic.EliminarPendienteAsync(id, clienteIdJwt);
        return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
    }
}
