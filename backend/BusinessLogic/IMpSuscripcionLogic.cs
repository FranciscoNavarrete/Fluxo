using Models.DTOs;
using Models.Helpers;
using Models.Requests;

namespace BusinessLogic;

public interface IMpSuscripcionLogic
{
    /// <summary>
    /// Modo Redirect (Opción B): crea pre_approval sin card_token y devuelve init_point.
    /// </summary>
    Task<RespuestaResultado<CrearSuscripcionResultado>> CrearConRedirectAsync(
        CrearSuscripcionRedirectRequest request, int usuarioId, string ipCliente, string userAgent);

    /// <summary>
    /// Modo Token (Opción A): crea pre_approval con card_token de MP Bricks.
    /// </summary>
    Task<RespuestaResultado<MpSuscripcionDto>> CrearConTokenAsync(
        CrearSuscripcionConTokenRequest request, int usuarioId, string ipCliente, string userAgent);

    Task<RespuestaResultado<MpSuscripcionDto>> ObtenerPorIdAsync(int mpSuscripcionId);

    Task<RespuestaResultado<ResultadoListaPaginada<MpSuscripcionDto>>> ListarAsync(
        int pagina, int tamanioPagina, string? estado = null, int? clienteId = null);

    Task<RespuestaResultado<bool>> CancelarAsync(CancelarSuscripcionRequest request, int usuarioId);

    Task<RespuestaResultado<bool>> ActualizarMedioPagoAsync(
        ActualizarMedioPagoRequest request, int usuarioId);

    Task<RespuestaResultado<IEnumerable<MpTransaccionDto>>> ListarTransaccionesAsync(
        int mpSuscripcionId, int? clienteIdJwt);
}

public class CrearSuscripcionResultado
{
    public int MpSuscripcionId { get; set; }
    public string GatewaySuscripcionId { get; set; } = string.Empty;

    // URL de checkout de Mercado Pago para redirigir al cliente
    public string InitPoint { get; set; } = string.Empty;
    public string Estado { get; set; } = "pending";
}
