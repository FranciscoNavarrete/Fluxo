using FluentValidation;
using MercadoPago.Client.Preapproval;
using MercadoPago.Config;
using Microsoft.Extensions.Configuration;
using Models.DTOs;
using Models.Entities;
using Models.Helpers;
using Models.Requests;
using UnitOfWork;

namespace BusinessLogic;

public class MpSuscripcionLogic : BaseLogic<MpSuscripcionDto>, IMpSuscripcionLogic
{
    private readonly IValidator<CrearSuscripcionRedirectRequest> _redirectValidator;
    private readonly IValidator<CrearSuscripcionConTokenRequest> _tokenValidator;
    private readonly IConfiguration _config;

    public MpSuscripcionLogic(
        IUnitOfWork uow,
        IValidator<CrearSuscripcionRedirectRequest> redirectValidator,
        IValidator<CrearSuscripcionConTokenRequest> tokenValidator,
        IConfiguration config) : base(uow)
    {
        _redirectValidator = redirectValidator;
        _tokenValidator = tokenValidator;
        _config = config;
    }

    // ------------------------------------------------------------------
    // Opción B — redirect: MP devuelve init_point para redirigir al usuario
    // ------------------------------------------------------------------
    public async Task<RespuestaResultado<CrearSuscripcionResultado>> CrearConRedirectAsync(
        CrearSuscripcionRedirectRequest request, int usuarioId, string ipCliente, string userAgent)
    {
        var validacion = await _redirectValidator.ValidateAsync(request);
        if (!validacion.IsValid)
            return new RespuestaResultado<CrearSuscripcionResultado>
            {
                Exitoso = false,
                Mensaje = validacion.Errors.First().ErrorMessage
            };

        var suscripcionExistente = await _uow.MpSuscripcion.ObtenerActivaPorClienteAsync(request.ClienteId);
        if (suscripcionExistente is not null)
            return new RespuestaResultado<CrearSuscripcionResultado>
            {
                Exitoso = false,
                Mensaje = "El cliente ya tiene una suscripción activa."
            };

        var plan = await _uow.MpPlan.GetByIdAsync(request.MpPlanId);
        if (plan is null || !plan.Activo)
            return new RespuestaResultado<CrearSuscripcionResultado>
            {
                Exitoso = false,
                Mensaje = "El plan seleccionado no existe o no está activo."
            };

        MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
        var mpClient = new PreapprovalClient();

        var mpRequest = new PreapprovalCreateRequest
        {
            Reason = plan.Nombre,
            ExternalReference = $"client_{request.ClienteId}_plan_{request.MpPlanId}",
            BackUrl = request.BackUrl ?? _config["MercadoPago:BackUrl"],
            PayerEmail = string.IsNullOrEmpty(request.PayerEmail) ? null : request.PayerEmail,
            AutoRecurring = new PreApprovalAutoRecurringCreateRequest
            {
                Frequency = plan.Frecuencia,
                FrequencyType = plan.TipoFrecuencia,
                TransactionAmount = plan.Monto,
                CurrencyId = plan.Moneda,
                StartDate = FechaCobroHelper.PrimerCobro(DateTime.UtcNow, plan.DiasGratis),
                EndDate = DateTime.UtcNow.AddYears(10)
            },
            Status = "pending"
        };

        MercadoPago.Resource.PreApproval.Preapproval? mpResponse = null;
        try
        {
            mpResponse = await mpClient.CreateAsync(mpRequest);
        }
        catch (Exception ex)
        {
            return new RespuestaResultado<CrearSuscripcionResultado>
            {
                Exitoso = false,
                Mensaje = $"Error al comunicarse con Mercado Pago: {ex.Message}"
            };
        }

        if (string.IsNullOrEmpty(mpResponse?.Id))
            return new RespuestaResultado<CrearSuscripcionResultado>
            {
                Exitoso = false,
                Mensaje = "Mercado Pago no devolvió un ID de suscripción. Verificá el AccessToken y los datos del plan."
            };

        var ahora = DateTime.UtcNow;
        var suscripcion = new MpSuscripcion
        {
            ClienteId = request.ClienteId,
            MpPlanId = request.MpPlanId,
            GatewaySuscripcionId = mpResponse.Id,
            GatewayProveedor = "MercadoPago",
            Estado = "pending",
            FechaInicio = ahora,
            DiaCobro = request.DiaCobro,
            ProximoCobro = FechaCobroHelper.PrimerCobro(ahora, plan.DiasGratis),
            IntentosReintento = 0,
            MaxReintentos = 3,
            InitPoint = mpResponse.InitPoint,
            ConsentimientoFecha = ahora,
            ConsentimientoIp = ipCliente,
            ConsentimientoUserAgent = userAgent,
            TerminosVersion = request.TerminosVersion,
            UsuarioCreacionId = usuarioId,
            FechaHoraCreacion = ahora
        };

        int id;
        try
        {
            id = await _uow.MpSuscripcion.InsertAsync(suscripcion);
        }
        catch (Exception ex)
        {
            return new RespuestaResultado<CrearSuscripcionResultado>
            {
                Exitoso = false,
                Mensaje = $"Error al guardar la suscripción en la base de datos: {ex.Message}"
            };
        }

        await RegistrarAuditoria("MpSuscripciones", id, "INSERT", usuarioId, null);

        return new RespuestaResultado<CrearSuscripcionResultado>
        {
            Exitoso = true,
            Mensaje = "Suscripción iniciada. Redirigir al cliente al init_point.",
            Contenido = new CrearSuscripcionResultado
            {
                MpSuscripcionId = id,
                GatewaySuscripcionId = mpResponse.Id,
                InitPoint = mpResponse.InitPoint ?? string.Empty,
                Estado = "pending"
            }
        };
    }

    // ------------------------------------------------------------------
    // Opción A — token: tarjeta capturada por MP Bricks en el frontend
    // ------------------------------------------------------------------
    public async Task<RespuestaResultado<MpSuscripcionDto>> CrearConTokenAsync(
        CrearSuscripcionConTokenRequest request, int usuarioId, string ipCliente, string userAgent)
    {
        var validacion = await _tokenValidator.ValidateAsync(request);
        if (!validacion.IsValid)
            return RespuestaError(validacion.Errors.First().ErrorMessage);

        var suscripcionExistente = await _uow.MpSuscripcion.ObtenerActivaPorClienteAsync(request.ClienteId);
        if (suscripcionExistente is not null)
            return RespuestaError("El cliente ya tiene una suscripción activa.");

        var plan = await _uow.MpPlan.GetByIdAsync(request.MpPlanId);
        if (plan is null || !plan.Activo)
            return RespuestaError("El plan seleccionado no existe o no está activo.");

        MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
        var mpClient = new PreapprovalClient();

        // El SDK 2.4.x no expone CardTokenId/PaymentMethodId en PreapprovalCreateRequest.
        // La suscripción se crea como pending y MP gestiona la captura de tarjeta internamente.
        var mpRequest = new PreapprovalCreateRequest
        {
            Reason = plan.Nombre,
            ExternalReference = $"client_{request.ClienteId}_plan_{request.MpPlanId}",
            BackUrl = _config["MercadoPago:BackUrl"],
            PayerEmail = request.PayerEmail,
            AutoRecurring = new PreApprovalAutoRecurringCreateRequest
            {
                Frequency = plan.Frecuencia,
                FrequencyType = plan.TipoFrecuencia,
                TransactionAmount = plan.Monto,
                CurrencyId = plan.Moneda,
                StartDate = FechaCobroHelper.PrimerCobro(DateTime.UtcNow, plan.DiasGratis),
                EndDate = DateTime.UtcNow.AddYears(10)
            },
            Status = "authorized"
        };

        var mpResponse = await mpClient.CreateAsync(mpRequest);

        if (string.IsNullOrEmpty(mpResponse.Id))
            return RespuestaError("Error al crear la suscripción en Mercado Pago.");

        var ahora = DateTime.UtcNow;
        var suscripcion = new MpSuscripcion
        {
            ClienteId = request.ClienteId,
            MpPlanId = request.MpPlanId,
            GatewaySuscripcionId = mpResponse.Id,
            GatewayProveedor = "MercadoPago",
            MpPayerId = mpResponse.PayerId?.ToString(),
            Estado = "authorized",
            FechaInicio = ahora,
            DiaCobro = request.DiaCobro,
            ProximoCobro = FechaCobroHelper.PrimerCobro(ahora, plan.DiasGratis),
            IntentosReintento = 0,
            MaxReintentos = 3,
            ConsentimientoFecha = ahora,
            ConsentimientoIp = ipCliente,
            ConsentimientoUserAgent = userAgent,
            TerminosVersion = request.TerminosVersion,
            UsuarioCreacionId = usuarioId,
            FechaHoraCreacion = ahora
        };

        int id = await _uow.MpSuscripcion.InsertAsync(suscripcion);
        await RegistrarAuditoria("MpSuscripciones", id, "INSERT", usuarioId, null);

        var dto = await _uow.MpSuscripcion.ObtenerConDetalleAsync(id);
        return RespuestaExito(dto!, "Suscripción creada exitosamente.");
    }

    public async Task<RespuestaResultado<MpSuscripcionDto>> ObtenerPorIdAsync(int mpSuscripcionId)
    {
        var dto = await _uow.MpSuscripcion.ObtenerConDetalleAsync(mpSuscripcionId);
        if (dto is null)
            return RespuestaError("Suscripción no encontrada.");
        return RespuestaExito(dto);
    }

    public async Task<RespuestaResultado<IEnumerable<EstadoSuscripcionDto>>> ObtenerEstadosAsync(IEnumerable<int> ids)
    {
        var suscripciones = await _uow.MpSuscripcion.ObtenerPorIdsAsync(ids);
        var estados = suscripciones.Select(s => new EstadoSuscripcionDto
        {
            MpSuscripcionId = s.MpSuscripcionId,
            Estado = s.Estado,
        });
        return new RespuestaResultado<IEnumerable<EstadoSuscripcionDto>> { Exitoso = true, Contenido = estados };
    }

    public async Task<RespuestaResultado<LinkPagoSuscripcionDto>> ObtenerLinkPagoAsync(int mpSuscripcionId)
    {
        var suscripcion = await _uow.MpSuscripcion.GetByIdAsync(mpSuscripcionId);
        if (suscripcion is null)
        {
            return new RespuestaResultado<LinkPagoSuscripcionDto>
            {
                Exitoso = false,
                Mensaje = "Suscripción no encontrada.",
            };
        }

        return new RespuestaResultado<LinkPagoSuscripcionDto>
        {
            Exitoso = true,
            Contenido = new LinkPagoSuscripcionDto
            {
                MpSuscripcionId = suscripcion.MpSuscripcionId,
                Estado          = suscripcion.Estado,
                InitPoint       = suscripcion.Estado == "pending" ? suscripcion.InitPoint : null,
            },
        };
    }

    public async Task<RespuestaResultado<ResultadoListaPaginada<MpSuscripcionDto>>> ListarAsync(
        int pagina, int tamanioPagina, string? estado = null, int? clienteId = null)
    {
        var resultado = await _uow.MpSuscripcion.ListarPaginadoAsync(pagina, tamanioPagina, estado, clienteId);
        return new RespuestaResultado<ResultadoListaPaginada<MpSuscripcionDto>>
        {
            Exitoso = true,
            Contenido = resultado
        };
    }

    public async Task<RespuestaResultado<bool>> CancelarAsync(CancelarSuscripcionRequest request, int usuarioId)
    {
        var suscripcion = await _uow.MpSuscripcion.GetByIdAsync(request.MpSuscripcionId);
        if (suscripcion is null)
            return new RespuestaResultado<bool> { Exitoso = false, Mensaje = "Suscripción no encontrada." };

        if (suscripcion.Estado == "cancelled")
            return new RespuestaResultado<bool> { Exitoso = false, Mensaje = "La suscripción ya está cancelada." };

        try
        {
            MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
            var mpClient = new PreapprovalClient();
            await mpClient.UpdateAsync(suscripcion.GatewaySuscripcionId,
                new PreapprovalUpdateRequest { Status = "cancelled" });
        }
        catch (Exception ex)
        {
            return new RespuestaResultado<bool>
            {
                Exitoso = false,
                Mensaje = $"Error al cancelar en Mercado Pago: {ex.Message}"
            };
        }

        var ahora = DateTime.UtcNow;
        suscripcion.Estado = "cancelled";
        suscripcion.FechaCancelacion = ahora;
        suscripcion.MotivoCancelacion = request.Motivo;
        suscripcion.UsuarioUltActualizacionId = usuarioId;
        suscripcion.FechaHoraUltActualizacion = ahora;

        await _uow.MpSuscripcion.UpdateAsync(suscripcion);
        await RegistrarAuditoria("MpSuscripciones", suscripcion.MpSuscripcionId, "CANCEL", usuarioId, null);

        // Libera el email del cliente: tanto Usuarios.Email como Clientes.Email tienen un índice
        // único, así que sin esto el mismo email queda bloqueado para siempre y no se puede volver
        // a dar de alta un cliente nuevo (ni desde el admin de GestorPOS ni el auto-registro).
        var usuarioCliente = await _uow.Usuario.ObtenerPorClienteIdAsync(suscripcion.ClienteId);
        if (usuarioCliente is not null)
        {
            usuarioCliente.Email = $"baja+{Guid.NewGuid():N}+{usuarioCliente.Email}";
            await _uow.Usuario.UpdateAsync(usuarioCliente);
            await RegistrarAuditoria("Usuarios", usuarioCliente.UsuarioId, "EMAIL_FREED", usuarioId, null);
        }

        var cliente = await _uow.Cliente.GetByIdAsync(suscripcion.ClienteId);
        if (cliente is not null)
        {
            cliente.Email = $"baja+{Guid.NewGuid():N}+{cliente.Email}";
            await _uow.Cliente.UpdateAsync(cliente);
            await RegistrarAuditoria("Clientes", cliente.ClienteId, "EMAIL_FREED", usuarioId, null);
        }

        return new RespuestaResultado<bool> { Exitoso = true, Mensaje = "Suscripción cancelada.", Contenido = true };
    }

    public async Task<RespuestaResultado<bool>> ActualizarMedioPagoAsync(
        ActualizarMedioPagoRequest request, int usuarioId)
    {
        var suscripcion = await _uow.MpSuscripcion.GetByIdAsync(request.MpSuscripcionId);
        if (suscripcion is null)
            return new RespuestaResultado<bool> { Exitoso = false, Mensaje = "Suscripción no encontrada." };

        try
        {
            MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
            // El SDK 2.4.x no expone CardTokenId/PaymentMethodId en PreapprovalUpdateRequest.
            // Para actualizar la tarjeta redirigir al cliente al init_point de la suscripción.
            var mpClient = new PreapprovalClient();
            await mpClient.UpdateAsync(suscripcion.GatewaySuscripcionId, new PreapprovalUpdateRequest
            {
                Status = suscripcion.Estado
            });
        }
        catch (Exception ex)
        {
            return new RespuestaResultado<bool>
            {
                Exitoso = false,
                Mensaje = $"Error al actualizar en Mercado Pago: {ex.Message}"
            };
        }

        suscripcion.UsuarioUltActualizacionId = usuarioId;
        suscripcion.FechaHoraUltActualizacion = DateTime.UtcNow;
        await _uow.MpSuscripcion.UpdateAsync(suscripcion);
        await RegistrarAuditoria("MpSuscripciones", suscripcion.MpSuscripcionId, "UPDATE_PAYMENT_METHOD", usuarioId, null);

        return new RespuestaResultado<bool> { Exitoso = true, Mensaje = "Medio de pago actualizado.", Contenido = true };
    }

    public async Task<RespuestaResultado<IEnumerable<MpTransaccionDto>>> ListarTransaccionesAsync(
        int mpSuscripcionId, int? clienteIdJwt)
    {
        // Si viene clienteIdJwt, verificar que la suscripción le pertenece
        if (clienteIdJwt.HasValue)
        {
            var suscripcion = await _uow.MpSuscripcion.GetByIdAsync(mpSuscripcionId);
            if (suscripcion is null || suscripcion.ClienteId != clienteIdJwt.Value)
                return new RespuestaResultado<IEnumerable<MpTransaccionDto>>
                {
                    Exitoso = false,
                    Mensaje = "Suscripción no encontrada."
                };
        }

        var transacciones = await _uow.MpTransaccion.ListarPorSuscripcionAsync(mpSuscripcionId);
        return new RespuestaResultado<IEnumerable<MpTransaccionDto>>
        {
            Exitoso   = true,
            Contenido = transacciones
        };
    }
}
