using FluentValidation;
using MercadoPago.Config;
using Microsoft.Extensions.Configuration;
using Models.DTOs;
using Models.Entities;
using Models.Helpers;
using Models.Requests;
using UnitOfWork;

namespace BusinessLogic;

public class MpPlanLogic : BaseLogic<MpPlanDto>, IMpPlanLogic
{
    private readonly IValidator<CrearPlanRequest> _crearValidator;
    private readonly IConfiguration _config;

    public MpPlanLogic(
        IUnitOfWork uow,
        IValidator<CrearPlanRequest> crearValidator,
        IConfiguration config) : base(uow)
    {
        _crearValidator = crearValidator;
        _config = config;
    }

    public async Task<RespuestaResultado<MpPlanDto>> CrearAsync(CrearPlanRequest request, int usuarioId)
    {
        var validacion = await _crearValidator.ValidateAsync(request);
        if (!validacion.IsValid)
            return RespuestaError(validacion.Errors.First().ErrorMessage);

        // El SDK mercadopago-sdk 2.x no expone PreApprovalPlanClient.
        // Los planes se gestionan localmente; el monto/frecuencia se pasa
        // directamente al crear cada PreApproval (suscripción individual).
        var plan = new MpPlan
        {
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            Monto = request.Monto,
            Moneda = request.Moneda.ToUpper(),
            TipoFrecuencia = request.TipoFrecuencia.ToLower(),
            Frecuencia = request.Frecuencia,
            DiasGratis = request.DiasGratis,
            MpPlanExternoId = null,
            Activo = true,
            UsuarioCreacionId = usuarioId,
            FechaHoraCreacion = DateTime.UtcNow
        };

        int id = await _uow.MpPlan.InsertAsync(plan);
        plan.MpPlanId = id;

        await RegistrarAuditoria("MpPlanes", id, "INSERT", usuarioId, null);

        return RespuestaExito(MapearDto(plan), "Plan creado exitosamente.");
    }

    public async Task<RespuestaResultado<MpPlanDto>> EditarAsync(EditarPlanRequest request, int usuarioId)
    {
        var plan = await _uow.MpPlan.GetByIdAsync(request.MpPlanId);
        if (plan is null)
            return RespuestaError("Plan no encontrado.");

        plan.Nombre = request.Nombre;
        plan.Descripcion = request.Descripcion;
        plan.Monto = request.Monto;
        plan.Moneda = request.Moneda.ToUpper();
        plan.TipoFrecuencia = request.TipoFrecuencia.ToLower();
        plan.Frecuencia = request.Frecuencia;
        plan.DiasGratis = request.DiasGratis;
        plan.Activo = request.Activo;
        plan.UsuarioUltActualizacionId = usuarioId;
        plan.FechaHoraUltActualizacion = DateTime.UtcNow;

        await _uow.MpPlan.UpdateAsync(plan);
        await RegistrarAuditoria("MpPlanes", plan.MpPlanId, "UPDATE", usuarioId, null);

        return RespuestaExito(MapearDto(plan), "Plan actualizado exitosamente.");
    }

    public async Task<RespuestaResultado<bool>> EliminarAsync(int mpPlanId, int usuarioId)
    {
        var plan = await _uow.MpPlan.GetByIdAsync(mpPlanId);
        if (plan is null)
            return new RespuestaResultado<bool> { Exitoso = false, Mensaje = "Plan no encontrado." };

        plan.Activo = false;
        plan.UsuarioUltActualizacionId = usuarioId;
        plan.FechaHoraUltActualizacion = DateTime.UtcNow;

        await _uow.MpPlan.UpdateAsync(plan);
        await RegistrarAuditoria("MpPlanes", plan.MpPlanId, "DELETE", usuarioId, null);

        return new RespuestaResultado<bool> { Exitoso = true, Mensaje = "Plan eliminado.", Contenido = true };
    }

    public async Task<RespuestaResultado<MpPlanDto>> ObtenerPorIdAsync(int mpPlanId)
    {
        var plan = await _uow.MpPlan.GetByIdAsync(mpPlanId);
        if (plan is null)
            return RespuestaError("Plan no encontrado.");

        return RespuestaExito(MapearDto(plan));
    }

    public async Task<RespuestaResultado<IEnumerable<MpPlanDto>>> ListarActivosAsync()
    {
        var lista = await _uow.MpPlan.ListarActivosAsync();
        return new RespuestaResultado<IEnumerable<MpPlanDto>> { Exitoso = true, Contenido = lista };
    }

    private static MpPlanDto MapearDto(MpPlan p) => new()
    {
        MpPlanId = p.MpPlanId,
        Nombre = p.Nombre,
        Descripcion = p.Descripcion,
        Monto = p.Monto,
        Moneda = p.Moneda,
        TipoFrecuencia = p.TipoFrecuencia,
        Frecuencia = p.Frecuencia,
        DiasGratis = p.DiasGratis,
        MpPlanExternoId = p.MpPlanExternoId,
        Activo = p.Activo,
        FechaHoraCreacion = p.FechaHoraCreacion,
        UsuarioCreacionId = p.UsuarioCreacionId
    };
}
