using Models.DTOs;
using Models.Helpers;
using Models.Requests;
using Models.Entities;

namespace BusinessLogic;

public interface IAuthLogic
{
    Task<RespuestaResultado<LoginDto>>            LoginAsync(LoginRequest request);
    Task<RespuestaResultado<LoginDto>>            RegistroAsync(RegistroRequest request);
    /// <summary>Con <paramref name="exigirSuscripcion"/> (alta desde GestorPOS), si se pidió un plan y la suscripción
    /// no se puede crear, el cliente recién creado se da de baja (email liberado) y se devuelve error, en vez
    /// de dejar un cliente sin suscripción. El panel admin de Fluxo lo deja en false a propósito.</summary>
    Task<RespuestaResultado<CrearClienteAdminDto>> CrearClienteAdminAsync(
        CrearClienteAdminRequest request, int usuarioAdminId, bool exigirSuscripcion = false);
    Task<RespuestaResultado<LoginDto>>            CambiarPasswordAsync(int usuarioId, string nuevaPassword);
    Task<RespuestaResultado<PerfilDto>>           ObtenerPerfilAsync(int clienteId);
    Task<RespuestaResultado<PerfilDto>>           ActualizarPerfilAsync(int clienteId, ActualizarPerfilRequest request);
    Task<RespuestaResultado<SolicitarResetDto>>   SolicitarResetAsync(string email);
    Task<RespuestaResultado<object>>              ResetPasswordAsync(string token, string nuevaPassword);

    /// <summary>Usuario reservado (rol SISTEMA) para atribuir acciones hechas por integraciones
    /// externas (ej. GestorPOS) en vez de un admin humano. Se crea la primera vez que se necesita.</summary>
    Task<int> ObtenerOCrearUsuarioSistemaIdAsync();
}
