using Models.Helpers;
using UnitOfWork;

namespace BusinessLogic;

public abstract class BaseLogic<T>
{
    protected readonly IUnitOfWork _uow;

    protected BaseLogic(IUnitOfWork uow) => _uow = uow;

    protected RespuestaResultado<T> RespuestaExito(T contenido, string mensaje = "")
        => new() { Exitoso = true, Mensaje = mensaje, Contenido = contenido };

    protected RespuestaResultado<T> RespuestaError(string mensaje)
        => new() { Exitoso = false, Mensaje = mensaje };

    protected async Task RegistrarAuditoria(
        string entidad, int id, string accion, int usuarioId, string? datosJson)
    {
        // Adaptar al repo de auditoría del sistema existente.
        await Task.CompletedTask;
    }
}
