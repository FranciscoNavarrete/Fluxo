using Models.Entities;

namespace Repositories;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> ObtenerPorEmailAsync(string email);
    Task<bool> ExisteEmailAsync(string email);
    Task GuardarTokenResetAsync(int usuarioId, string token, DateTime expiry);
    Task<Usuario?> ObtenerPorTokenResetAsync(string token);
}
