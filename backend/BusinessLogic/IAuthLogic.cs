using Models.DTOs;
using Models.Helpers;
using Models.Requests;
using Models.Entities;

namespace BusinessLogic;

public interface IAuthLogic
{
    Task<RespuestaResultado<LoginDto>>            LoginAsync(LoginRequest request);
    Task<RespuestaResultado<LoginDto>>            RegistroAsync(RegistroRequest request);
    Task<RespuestaResultado<CrearClienteAdminDto>> CrearClienteAdminAsync(CrearClienteAdminRequest request, int usuarioAdminId);
    Task<RespuestaResultado<LoginDto>>            CambiarPasswordAsync(int usuarioId, string nuevaPassword);
    Task<RespuestaResultado<PerfilDto>>           ObtenerPerfilAsync(int clienteId);
    Task<RespuestaResultado<PerfilDto>>           ActualizarPerfilAsync(int clienteId, ActualizarPerfilRequest request);
    Task<RespuestaResultado<SolicitarResetDto>>   SolicitarResetAsync(string email);
    Task<RespuestaResultado<object>>              ResetPasswordAsync(string token, string nuevaPassword);
}
