using FluentValidation;
using MercadoPago.Client.Preapproval;
using MercadoPago.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<MpSuscripcionLogic> _logger;

    public MpSuscripcionLogic(
        IUnitOfWork uow,
        IValidator<CrearSuscripcionRedirectRequest> redirectValidator,
        IValidator<CrearSuscripcionConTokenRequest> tokenValidator,
        IConfiguration config,
        ILogger<MpSuscripcionLogic> logger) : base(uow)
    {
        _logger = logger;
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

        var mpResponse = await MpPreapprovalConTarjeta.CrearAsync(
            _config["MercadoPago:AccessToken"] ?? string.Empty,
            new MpPreapprovalConTarjeta.Datos(
                Reason:            plan.Nombre,
                ExternalReference: $"client_{request.ClienteId}_plan_{request.MpPlanId}",
                PayerEmail:        string.IsNullOrEmpty(request.PayerEmail) ? null : request.PayerEmail,
                CardTokenId:       null,
                BackUrl:           request.BackUrl ?? _config["MercadoPago:BackUrl"] ?? string.Empty,
                Frequency:         plan.Frecuencia,
                FrequencyType:     plan.TipoFrecuencia,
                TransactionAmount: CobroInicialHelper.MontoInicial(plan),
                CurrencyId:        plan.Moneda,
                StartDateUtc:      FechaCobroHelper.PrimerCobro(DateTime.UtcNow, plan.DiasGratis),
                EndDateUtc:        plan.Repeticiones.HasValue ? null : DateTime.UtcNow.AddYears(10),
                Repeticiones:      plan.Repeticiones,
                Status:            "pending"));

        if (!mpResponse.Exitoso)
            return new RespuestaResultado<CrearSuscripcionResultado>
            {
                Exitoso = false,
                Mensaje = mpResponse.Error ?? "Mercado Pago no devolvió un ID de suscripción. Verificá el AccessToken y los datos del plan."
            };

        var ahora = DateTime.UtcNow;
        var suscripcion = new MpSuscripcion
        {
            ClienteId = request.ClienteId,
            MpPlanId = request.MpPlanId,
            GatewaySuscripcionId = mpResponse.Id!,
            GatewayProveedor = "MercadoPago",
            Estado = "pending",
            FechaInicio = ahora,
            DiaCobro = request.DiaCobro,
            ProximoCobro = FechaCobroHelper.PrimerCobro(ahora, plan.DiasGratis),
            AjusteMontoPendiente = CobroInicialHelper.RequiereAjuste(plan),
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
                GatewaySuscripcionId = mpResponse.Id!,
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

        var mpResponse = await MpPreapprovalConTarjeta.CrearAsync(
            _config["MercadoPago:AccessToken"] ?? string.Empty,
            new MpPreapprovalConTarjeta.Datos(
                Reason:            plan.Nombre,
                ExternalReference: $"client_{request.ClienteId}_plan_{request.MpPlanId}",
                PayerEmail:        request.PayerEmail,
                CardTokenId:       request.CardTokenId,
                BackUrl:           _config["MercadoPago:BackUrl"] ?? string.Empty,
                Frequency:         plan.Frecuencia,
                FrequencyType:     plan.TipoFrecuencia,
                TransactionAmount: CobroInicialHelper.MontoInicial(plan),
                CurrencyId:        plan.Moneda,
                StartDateUtc:      FechaCobroHelper.PrimerCobro(DateTime.UtcNow, plan.DiasGratis),
                EndDateUtc:        plan.Repeticiones.HasValue ? null : DateTime.UtcNow.AddYears(10),
                Repeticiones:      plan.Repeticiones));

        if (!mpResponse.Exitoso)
            return RespuestaError(mpResponse.Error ?? "Error al crear la suscripción en Mercado Pago.");

        var ahora = DateTime.UtcNow;
        var suscripcion = new MpSuscripcion
        {
            ClienteId = request.ClienteId,
            MpPlanId = request.MpPlanId,
            GatewaySuscripcionId = mpResponse.Id!,
            GatewayProveedor = "MercadoPago",
            MpPayerId = mpResponse.PayerId,
            Estado = "authorized",
            FechaInicio = ahora,
            DiaCobro = request.DiaCobro,
            ProximoCobro = FechaCobroHelper.PrimerCobro(ahora, plan.DiasGratis),
            AjusteMontoPendiente = CobroInicialHelper.RequiereAjuste(plan),
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
        var suscripciones = (await _uow.MpSuscripcion.ObtenerPorIdsAsync(ids)).ToList();

        // Pocos planes distintos: se leen una vez cada uno para saber cuánto paga cada suscripción.
        var planes = new Dictionary<int, MpPlan?>();
        foreach (var planId in suscripciones.Select(s => s.MpPlanId).Distinct())
            planes[planId] = await _uow.MpPlan.GetByIdAsync(planId);

        var estados = suscripciones.Select(s =>
        {
            var plan = planes.GetValueOrDefault(s.MpPlanId);
            var mensual = plan is null ? 0 : CobroInicialHelper.MontoDelMes(plan, CobroInicialHelper.MesDelProximoCobro(s, s.CobrosRealizados));
            // Con el ajuste pendiente (o sin primer cobro todavía) lo próximo es el monto inicial del plan.
            var proximo = plan is null
                ? 0
                : s.CobrosBase == 0 && !s.PrimerPagoManual && (s.AjusteMontoPendiente || !s.UltimoCobro.HasValue)
                    ? CobroInicialHelper.MontoInicial(plan)
                    : CobroInicialHelper.MontoEsperado(plan, s, s.CobrosRealizados);
            return new EstadoSuscripcionDto
            {
                MpSuscripcionId = s.MpSuscripcionId,
                Estado = s.Estado,
                Confirmada = s.Estado == "authorized" || !string.IsNullOrEmpty(s.MpPayerId),
                PrimerCobroAprobado = s.UltimoCobro.HasValue,
                AjusteMontoPendiente = s.UltimoCobro.HasValue && s.AjusteMontoPendiente,
                CobroRechazado = s.CobroRechazado,
                MotivoRechazo = s.CobroRechazado ? s.MotivoRechazo : null,
                ProximoReintento = s.CobroRechazado ? s.ProximoReintento : null,
                MontoMensual = mensual,
                MontoProximoCobro = proximo,
                ProximoCobro = s.ProximoCobro,
                FechaInicio = s.FechaInicio,
                FechaCancelacion = s.FechaCancelacion,
                MpPlanId = s.MpPlanId,
                PlanNombre = plan?.Nombre,
                Promo = plan is null ? null : CobroInicialHelper.InfoPromo(plan, s, s.CobrosRealizados, s.ProximoCobro),
            };
        }).ToList();
        return new RespuestaResultado<IEnumerable<EstadoSuscripcionDto>> { Exitoso = true, Contenido = estados };
    }

    public async Task<RespuestaResultado<CobrosSuscripcionDto>> ObtenerCobrosAsync(int mpSuscripcionId)
    {
        var suscripcion = await _uow.MpSuscripcion.GetByIdAsync(mpSuscripcionId);
        if (suscripcion is null)
            return new RespuestaResultado<CobrosSuscripcionDto> { Exitoso = false, Mensaje = "Suscripción no encontrada." };

        var plan = await _uow.MpPlan.GetByIdAsync(suscripcion.MpPlanId);
        var token = _config["MercadoPago:AccessToken"] ?? string.Empty;

        var cobrosMp = await MpPreapprovalApi.ObtenerCobrosAsync(token, suscripcion.GatewaySuscripcionId);
        if (!cobrosMp.Ok)
            return new RespuestaResultado<CobrosSuscripcionDto>
            {
                Exitoso = false,
                Mensaje = "No se pudieron consultar los cobros en Mercado Pago. Probá de nuevo en un momento.",
            };

        var consulta = await MpPreapprovalApi.ObtenerAsync(token, suscripcion.GatewaySuscripcionId);

        var ordenados = cobrosMp.Cobros.OrderBy(c => c.Fecha ?? DateTime.MaxValue).ToList();
        var primero = ordenados.FirstOrDefault(c => MpCobros.Clasificar(c) != MpCobros.Programado);

        var cobrosRealizados = consulta.Ok && consulta.Datos is not null ? consulta.Datos.CobrosRealizados : suscripcion.CobrosRealizados;
        var proximoCobroInfo = consulta.Ok && consulta.Datos?.ProximoCobroUtc is { } p ? p : suscripcion.ProximoCobro;

        var dto = new CobrosSuscripcionDto
        {
            MpSuscripcionId = suscripcion.MpSuscripcionId,
            Estado = suscripcion.Estado,
            MontoMensual = plan is null ? 0 : CobroInicialHelper.MontoDelMes(plan, CobroInicialHelper.MesDelProximoCobro(suscripcion, cobrosRealizados)),
            MpPlanId = suscripcion.MpPlanId,
            PlanNombre = plan?.Nombre,
            MontoNormal = plan?.Monto ?? 0,
            Promo = plan is null ? null : CobroInicialHelper.InfoPromo(plan, suscripcion, cobrosRealizados, proximoCobroInfo),
            Moneda = plan?.Moneda ?? "ARS",
            // Solo las suscripciones creadas con tarjeta (sin link de pago) permiten cambiarla desde acá.
            TarjetaEditable = string.IsNullOrEmpty(suscripcion.InitPoint) && suscripcion.Estado is not ("cancelled" or "pending"),
            CobroRechazado = suscripcion.CobroRechazado,
            Cobros = ordenados
                .Where(c => MpCobros.Clasificar(c) != MpCobros.Programado)
                .OrderByDescending(c => c.Fecha ?? DateTime.MinValue)
                .Select(c => new CobroDto
                {
                    Fecha = c.Fecha,
                    Monto = c.Monto,
                    Estado = MpCobros.Clasificar(c),
                    Motivo = MpCobros.Motivo(c),
                    Intento = c.Intento,
                    ProximoReintento = c.ProximoReintento,
                    EsPrimerCobro = !suscripcion.PrimerPagoManual && ReferenceEquals(c, primero),
                })
                .ToList(),
        };

        if (suscripcion.Estado == "authorized" && consulta.Ok && consulta.Datos is not null)
        {
            dto.ProximoCobro = consulta.Datos.ProximoCobroUtc ?? suscripcion.ProximoCobro;
            dto.ProximoMonto = consulta.Datos.Monto;
        }

        return new RespuestaResultado<CobrosSuscripcionDto> { Exitoso = true, Contenido = dto };
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

    public async Task<RespuestaResultado<bool>> CambiarPlanAsync(int mpSuscripcionId, CambiarPlanRequest request, int usuarioId)
    {
        var suscripcion = await _uow.MpSuscripcion.GetByIdAsync(mpSuscripcionId);
        if (suscripcion is null)
            return new RespuestaResultado<bool> { Exitoso = false, Mensaje = "Suscripción no encontrada." };
        if (suscripcion.Estado != "authorized")
            return new RespuestaResultado<bool> { Exitoso = false, Mensaje = "Solo se puede cambiar el plan de una suscripción activa." };
        if (suscripcion.MpPlanId == request.MpPlanId)
            return new RespuestaResultado<bool> { Exitoso = false, Mensaje = "El negocio ya tiene ese plan." };

        var actual = await _uow.MpPlan.GetByIdAsync(suscripcion.MpPlanId);
        var nuevo = await _uow.MpPlan.GetByIdAsync(request.MpPlanId);
        if (nuevo is null || !nuevo.Activo)
            return new RespuestaResultado<bool> { Exitoso = false, Mensaje = "El plan elegido no existe o no está activo." };
        if (actual is not null && (nuevo.Moneda != actual.Moneda || nuevo.Frecuencia != actual.Frecuencia || nuevo.TipoFrecuencia != actual.TipoFrecuencia))
            return new RespuestaResultado<bool>
            {
                Exitoso = false,
                Mensaje = "Solo se puede cambiar a un plan con la misma moneda y frecuencia de cobro.",
            };

        var token = _config["MercadoPago:AccessToken"] ?? string.Empty;
        var consulta = await MpPreapprovalApi.ObtenerAsync(token, suscripcion.GatewaySuscripcionId);
        if (!consulta.Ok || consulta.Datos is null)
            return new RespuestaResultado<bool> { Exitoso = false, Mensaje = "No se pudo consultar la suscripción en Mercado Pago. Probá de nuevo en un momento." };

        // Hasta que no hubo un primer cobro no hay de dónde contar los meses del plan nuevo.
        var cobros = consulta.Datos.CobrosRealizados;
        if (cobros < 1)
            return new RespuestaResultado<bool> { Exitoso = false, Mensaje = "Todavía no se cobró nada a este negocio: el plan se puede cambiar después de su primer cobro." };

        // El plan nuevo empieza a contar desde ahora: su mes 1 es el próximo cobro (sin alta).
        suscripcion.CobrosBase = cobros;
        suscripcion.CobrosRealizados = cobros;
        var esperado = CobroInicialHelper.MontoEsperado(nuevo, suscripcion, cobros);

        if (consulta.Datos.Monto != esperado)
        {
            var ajuste = await MpPreapprovalApi.ActualizarMontoAsync(token, suscripcion.GatewaySuscripcionId, esperado, nuevo.Moneda);
            if (!ajuste.Ok)
            {
                _logger.LogWarning("No se pudo cambiar el monto de la suscripción {Id} al cambiar de plan: {Error}", suscripcion.MpSuscripcionId, ajuste.Error);
                return new RespuestaResultado<bool> { Exitoso = false, Mensaje = "Mercado Pago no pudo actualizar el monto. Probá de nuevo en un momento." };
            }
        }

        suscripcion.MpPlanId = nuevo.MpPlanId;
        suscripcion.AjusteMontoPendiente = false;
        suscripcion.UsuarioUltActualizacionId = usuarioId;
        suscripcion.FechaHoraUltActualizacion = DateTime.UtcNow;
        await _uow.MpSuscripcion.UpdateAsync(suscripcion);
        await RegistrarAuditoria("MpSuscripciones", suscripcion.MpSuscripcionId, "CAMBIO_PLAN", usuarioId, null);

        return new RespuestaResultado<bool> { Exitoso = true, Mensaje = "Plan cambiado.", Contenido = true };
    }

    public async Task<RespuestaResultado<bool>> ActualizarMedioPagoAsync(
        ActualizarMedioPagoRequest request, int usuarioId)
    {
        if (string.IsNullOrWhiteSpace(request.CardTokenId))
            return new RespuestaResultado<bool> { Exitoso = false, Mensaje = "Falta el token de la tarjeta." };

        var suscripcion = await _uow.MpSuscripcion.GetByIdAsync(request.MpSuscripcionId);
        if (suscripcion is null)
            return new RespuestaResultado<bool> { Exitoso = false, Mensaje = "Suscripción no encontrada." };
        if (suscripcion.Estado is "cancelled" or "pending")
            return new RespuestaResultado<bool>
            {
                Exitoso = false,
                Mensaje = "La suscripción no está activa: no se puede cambiar la tarjeta.",
            };

        // El SDK 2.4.x no expone card_token_id en la actualización: se hace por la API REST.
        var resultado = await MpPreapprovalApi.ActualizarTarjetaAsync(
            _config["MercadoPago:AccessToken"] ?? string.Empty, suscripcion.GatewaySuscripcionId, request.CardTokenId.Trim());
        if (!resultado.Ok)
        {
            _logger.LogWarning("No se pudo cambiar la tarjeta de la suscripción {Id}: {Error}",
                suscripcion.MpSuscripcionId, resultado.ErrorOriginal);
            return new RespuestaResultado<bool> { Exitoso = false, Mensaje = resultado.Error };
        }

        suscripcion.UsuarioUltActualizacionId = usuarioId;
        suscripcion.FechaHoraUltActualizacion = DateTime.UtcNow;
        await _uow.MpSuscripcion.UpdateAsync(suscripcion);
        await RegistrarAuditoria("MpSuscripciones", suscripcion.MpSuscripcionId, "UPDATE_PAYMENT_METHOD", usuarioId, null);

        return new RespuestaResultado<bool> { Exitoso = true, Mensaje = "Tarjeta actualizada.", Contenido = true };
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
