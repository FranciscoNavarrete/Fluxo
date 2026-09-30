using System.Data;
using Dapper;
using Dapper.Contrib.Extensions;
using Models.Entities;
using Repositories;

namespace DataAccess;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly IDbConnection _db;
    public UsuarioRepository(IDbConnection db) => _db = db;

    public Task<Usuario?> GetByIdAsync(int id)
        => _db.GetAsync<Usuario>(id)!;

    public async Task<int> InsertAsync(Usuario entity)
        => (int)await _db.InsertAsync(entity);

    public Task<bool> UpdateAsync(Usuario entity)
        => _db.UpdateAsync(entity);

    public Task<bool> DeleteAsync(Usuario entity)
        => _db.DeleteAsync(entity);

    public Task<Usuario?> ObtenerPorEmailAsync(string email)
        => _db.QueryFirstOrDefaultAsync<Usuario>(
            "SELECT * FROM usuarios WHERE email = @Email AND activo = TRUE",
            new { Email = email });

    public async Task<bool> ExisteEmailAsync(string email)
        => await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Usuarios WHERE Email = @Email",
            new { Email = email }) > 0;

    public Task GuardarTokenResetAsync(int usuarioId, string token, DateTime expiry)
        => _db.ExecuteAsync(
            "UPDATE Usuarios SET ResetPasswordToken = @Token, ResetPasswordTokenExpiry = @Expiry WHERE UsuarioId = @Id",
            new { Token = token, Expiry = expiry, Id = usuarioId });

    public Task<Usuario?> ObtenerPorTokenResetAsync(string token)
        => _db.QueryFirstOrDefaultAsync<Usuario>(
            "SELECT * FROM usuarios WHERE resetpasswordtoken = @Token AND resetpasswordtokenexpiry > NOW() AND activo = TRUE",
            new { Token = token });
}
