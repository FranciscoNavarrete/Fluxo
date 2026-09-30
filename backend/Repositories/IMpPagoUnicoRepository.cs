using Models.Entities;

namespace Repositories;

public interface IMpPagoUnicoRepository : IRepository<MpPagoUnico>
{
    Task<MpPagoUnico?> ObtenerPorPreferenceIdAsync(string mpPreferenceId);
    Task<MpPagoUnico?> ObtenerPorExternalReferenceAsync(string externalReference);
    Task<IEnumerable<MpPagoUnico>> ListarPorClienteAsync(int clienteId);
    Task<IEnumerable<MpPagoUnico>> ObtenerPendientesDeReanudacionAsync(DateTime hasta);
    Task<MpPagoUnico?> ObtenerPorIdAsync(int mpPagoUnicoId);
    Task<bool> EliminarAsync(int mpPagoUnicoId);
}
