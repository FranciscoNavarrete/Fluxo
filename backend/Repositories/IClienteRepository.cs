using Models.Entities;

namespace Repositories;

public interface IClienteRepository : IRepository<Cliente>
{
    Task<Cliente?> ObtenerPorEmailAsync(string email);
    Task<bool> ExisteEmailAsync(string email);
}
