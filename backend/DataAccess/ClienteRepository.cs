using System.Data;
using Dapper;
using Dapper.Contrib.Extensions;
using Models.Entities;
using Repositories;

namespace DataAccess;

public class ClienteRepository : IClienteRepository
{
    private readonly IDbConnection _db;
    public ClienteRepository(IDbConnection db) => _db = db;

    public Task<Cliente?> GetByIdAsync(int id)
        => _db.GetAsync<Cliente>(id)!;

    public async Task<int> InsertAsync(Cliente entity)
        => (int)await _db.InsertAsync(entity);

    public Task<bool> UpdateAsync(Cliente entity)
        => _db.UpdateAsync(entity);

    public Task<bool> DeleteAsync(Cliente entity)
        => _db.DeleteAsync(entity);

    public Task<Cliente?> ObtenerPorEmailAsync(string email)
        => _db.QueryFirstOrDefaultAsync<Cliente>(
            "SELECT * FROM clientes WHERE email = @Email AND activo = TRUE",
            new { Email = email });

    public async Task<bool> ExisteEmailAsync(string email)
        => await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Clientes WHERE Email = @Email",
            new { Email = email }) > 0;
}
