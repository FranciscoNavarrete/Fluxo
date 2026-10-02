using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MercadoPago.Client.Preapproval;
using MercadoPago.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Models.DTOs;
using Models.Entities;
using Models.Helpers;
using Models.Requests;
using UnitOfWork;

namespace BusinessLogic;

public class AuthLogic : IAuthLogic
{
    private readonly IUnitOfWork       _uow;
    private readonly IConfiguration    _config;
    private readonly ILogger<AuthLogic> _logger;

    public AuthLogic(IUnitOfWork uow, IConfiguration config, ILogger<AuthLogic> logger)
    {
        _uow    = uow;
        _config = config;
        _logger = logger;
    }

    // ── Login ──────────────────────────────────────────────────────────────
    public async Task<RespuestaResultado<LoginDto>> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Error<LoginDto>("Email y contraseña son obligatorios.");

        var usuario = await _uow.Usuario.ObtenerPorEmailAsync(request.Email.Trim().ToLower());

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
            return Error<LoginDto>("Credenciales incorrectas.");

        var cliente = usuario.ClienteId.HasValue
            ? await _uow.Cliente.GetByIdAsync(usuario.ClienteId.Value)
            : null;

        string nombre = cliente is not null
            ? $"{cliente.Nombre} {cliente.Apellido}".Trim()
            : usuario.Email;

        var token = GenerarToken(usuario, nombre);
        return Exito(token);
    }

    // ── Registro (auto-registro del cliente) ───────────────────────────────
    public async Task<RespuestaResultado<LoginDto>> RegistroAsync(RegistroRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Error<LoginDto>("Email y contraseña son obligatorios.");

        if (request.Password.Length < 8)
            return Error<LoginDto>("La contraseña debe tener al menos 8 caracteres.");

        string email = request.Email.Trim().ToLower();

        if (await _uow.Usuario.ExisteEmailAsync(email))
            return Error<LoginDto>("Ya existe una cuenta con ese email.");

        // Crear cliente
        var cliente = new Cliente
        {
            Nombre           = request.Nombre.Trim(),
            Apellido         = request.Apellido?.Trim(),
            Email            = email,
            Telefono         = request.Telefono?.Trim(),
            Activo           = true,
            FechaHoraCreacion = DateTime.UtcNow,
        };
        int clienteId = await _uow.Cliente.InsertAsync(cliente);

        // Crear usuario
        var usuario = new Usuario
        {
            Email            = email,
            PasswordHash     = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            Rol              = "CLIENTE",
            ClienteId        = clienteId,
            Activo           = true,
            FechaHoraCreacion = DateTime.UtcNow,
        };
        await _uow.Usuario.InsertAsync(usuario);

        string nombre = $"{cliente.Nombre} {cliente.Apellido}".Trim();
        usuario.ClienteId = clienteId;
        var token = GenerarToken(usuario, nombre);
        return Exito(token);
    }

    // ── Crear cliente desde el admin (Opción C) ────────────────────────────
    public async Task<RespuestaResultado<CrearClienteAdminDto>> CrearClienteAdminAsync(
        CrearClienteAdminRequest request, int usuarioAdminId, bool exigirSuscripcion = false)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Error<CrearClienteAdminDto>("El email es obligatorio.");

        string email = request.Email.Trim().ToLower();

        if (await _uow.Usuario.ExisteEmailAsync(email))
            return Error<CrearClienteAdminDto>("Ya existe una cuenta con ese email.");

        // Con tarjeta el plan se valida ANTES de crear nada: si falla no queda un cliente a medias.
        MpPlan? planTarjeta = null;
        if (!string.IsNullOrWhiteSpace(request.CardTokenId))
        {
            if (!request.MpPlanId.HasValue)
                return Error<CrearClienteAdminDto>("Para cobrar con tarjeta hay que elegir un plan.");

            planTarjeta = await _uow.MpPlan.GetByIdAsync(request.MpPlanId.Value);
            if (planTarjeta is null || !planTarjeta.Activo)
                return Error<CrearClienteAdminDto>("El plan seleccionado no existe o no está activo.");
        }

        // Contraseña temporal aleatoria
        string passwordTemporal = GenerarPasswordAleatorio();

        // Crear cliente
        var cliente = new Cliente
        {
            Nombre            = request.Nombre.Trim(),
            Apellido          = request.Apellido?.Trim(),
            Email             = email,
            Telefono          = request.Telefono?.Trim(),
            Activo            = true,
            FechaHoraCreacion = DateTime.UtcNow,
        };
        int clienteId = await _uow.Cliente.InsertAsync(cliente);

        // Crear usuario
        var usuario = new Usuario
        {
            Email                 = email,
            PasswordHash          = BCrypt.Net.BCrypt.HashPassword(passwordTemporal, workFactor: 12),
            Rol                   = "CLIENTE",
            ClienteId             = clienteId,
            Activo                = true,
            DebeCambiarPassword   = true,
            FechaHoraCreacion     = DateTime.UtcNow,
        };
        int usuarioId = await _uow.Usuario.InsertAsync(usuario);

        var resultado = new CrearClienteAdminDto
        {
            ClienteId        = clienteId,
            UsuarioId        = usuarioId,
            Email            = email,
            PasswordTemporal = passwordTemporal,
        };

        if (planTarjeta is not null)
        {
            var tarjeta = await MpPreapprovalConTarjeta.CrearAsync(
                _config["MercadoPago:AccessToken"] ?? string.Empty,
                new MpPreapprovalConTarjeta.Datos(
                    Reason:            planTarjeta.Nombre,
                    ExternalReference: $"client_{clienteId}_plan_{planTarjeta.MpPlanId}",
                    PayerEmail:        email,
                    CardTokenId:       request.CardTokenId!.Trim(),
                    BackUrl:           _config["MercadoPago:BackUrl"] ?? string.Empty,
                    Frequency:         planTarjeta.Frecuencia,
                    FrequencyType:     planTarjeta.TipoFrecuencia,
                    TransactionAmount: planTarjeta.Monto,
                    CurrencyId:        planTarjeta.Moneda,
                    StartDateUtc:      DateTime.UtcNow.AddDays(planTarjeta.DiasGratis > 0 ? planTarjeta.DiasGratis : 1),
                    EndDateUtc:        DateTime.UtcNow.AddYears(10)));

            if (!tarjeta.Exitoso)
            {
                _logger.LogWarning(
                    "Mercado Pago rechazó la suscripción con tarjeta de clienteId={ClienteId} planId={PlanId}: {Error}",
                    clienteId, planTarjeta.MpPlanId, tarjeta.ErrorOriginal ?? tarjeta.Error);
                await DarDeBajaClienteAMedias(cliente, clienteId, usuario, usuarioId);
                return Error<CrearClienteAdminDto>(tarjeta.Error ?? "Mercado Pago rechazó la tarjeta.");
            }

            var ahoraTarjeta = DateTime.UtcNow;
            var suscripcionTarjeta = new MpSuscripcion
            {
                ClienteId            = clienteId,
                MpPlanId             = planTarjeta.MpPlanId,
                GatewaySuscripcionId = tarjeta.Id!,
                GatewayProveedor     = "MercadoPago",
                MpPayerId            = tarjeta.PayerId,
                MpPayerEmail         = email,
                Estado               = tarjeta.Estado ?? "authorized",
                FechaInicio          = ahoraTarjeta,
                DiaCobro             = request.DiaCobro,
                ProximoCobro         = FechaCobroHelper.PrimerCobro(
                    ahoraTarjeta, planTarjeta.Frecuencia, planTarjeta.TipoFrecuencia,
                    request.DiaCobro, planTarjeta.DiasGratis),
                IntentosReintento    = 0,
                MaxReintentos        = 3,
                ConsentimientoFecha  = ahoraTarjeta,
                ConsentimientoIp     = "admin",
                TerminosVersion      = "1.0",
                UsuarioCreacionId    = usuarioAdminId,
                FechaHoraCreacion    = ahoraTarjeta,
            };
            resultado.MpSuscripcionId   = await _uow.MpSuscripcion.InsertAsync(suscripcionTarjeta);
            resultado.EstadoSuscripcion = suscripcionTarjeta.Estado;

            return new RespuestaResultado<CrearClienteAdminDto>
            {
                Exitoso   = true,
                Mensaje   = "Cliente creado correctamente.",
                Contenido = resultado,
            };
        }

        // Crear suscripción si se pidió un plan
        string? falloSuscripcion = null;
        if (request.MpPlanId.HasValue)
        {
            var plan = await _uow.MpPlan.GetByIdAsync(request.MpPlanId.Value);
            if (plan is null || !plan.Activo)
            {
                falloSuscripcion = "El plan seleccionado no existe o no está activo.";
            }
            else
            {
                try
                {
                    MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
                    var mpRequest = new PreapprovalCreateRequest
                    {
                        Reason            = plan.Nombre,
                        ExternalReference = $"client_{clienteId}_plan_{plan.MpPlanId}",
                        BackUrl           = _config["MercadoPago:BackUrl"],
                        PayerEmail        = email,
                        AutoRecurring     = new PreApprovalAutoRecurringCreateRequest
                        {
                            Frequency         = plan.Frecuencia,
                            FrequencyType     = plan.TipoFrecuencia,
                            TransactionAmount = plan.Monto,
                            CurrencyId        = plan.Moneda,
                            StartDate         = DateTime.UtcNow.AddDays(plan.DiasGratis > 0 ? plan.DiasGratis : 1),
                            EndDate           = DateTime.UtcNow.AddYears(10),
                        },
                        Status = "pending",
                    };

                    var mpResponse = await new PreapprovalClient().CreateAsync(mpRequest);

                    if (!string.IsNullOrEmpty(mpResponse?.Id))
                    {
                        var ahora = DateTime.UtcNow;
                        var suscripcion = new MpSuscripcion
                        {
                            ClienteId                = clienteId,
                            MpPlanId                 = plan.MpPlanId,
                            GatewaySuscripcionId     = mpResponse.Id,
                            GatewayProveedor         = "MercadoPago",
                            Estado                   = "pending",
                            FechaInicio              = ahora,
                            DiaCobro                 = request.DiaCobro,
                            ProximoCobro             = FechaCobroHelper.PrimerCobro(
                                ahora, plan.Frecuencia, plan.TipoFrecuencia,
                                request.DiaCobro, plan.DiasGratis),
                            IntentosReintento        = 0,
                            MaxReintentos            = 3,
                            InitPoint                = mpResponse.InitPoint,
                            ConsentimientoFecha      = ahora,
                            ConsentimientoIp         = "admin",
                            TerminosVersion          = "1.0",
                            UsuarioCreacionId        = usuarioAdminId,
                            FechaHoraCreacion        = ahora,
                        };
                        int suscripcionId = await _uow.MpSuscripcion.InsertAsync(suscripcion);
                        resultado.MpSuscripcionId   = suscripcionId;
                        resultado.InitPoint         = mpResponse.InitPoint;
                        resultado.EstadoSuscripcion = "pending";
                    }
                    else
                    {
                        falloSuscripcion = "Mercado Pago no devolvió el id de la suscripción.";
                    }
                }
                catch (Exception ex)
                {
                    // Panel admin de Fluxo: el cliente queda creado y se arma la suscripción por separado.
                    // Alta desde GestorPOS (exigirSuscripcion): se revierte más abajo.
                    _logger.LogError(ex, "Falló la creación de la suscripción MP para clienteId={ClienteId} planId={PlanId}", clienteId, plan.MpPlanId);
                    falloSuscripcion = MpErrores.Traducir(ex.Message);
                }
            }
        }

        if (exigirSuscripcion && falloSuscripcion is not null)
        {
            await DarDeBajaClienteAMedias(cliente, clienteId, usuario, usuarioId);
            return Error<CrearClienteAdminDto>(falloSuscripcion);
        }

        return new RespuestaResultado<CrearClienteAdminDto>
        {
            Exitoso  = true,
            Mensaje  = "Cliente creado correctamente.",
            Contenido = resultado,
        };
    }

    // Si Mercado Pago rechaza la tarjeta, el cliente/usuario recién creados quedan inactivos y con el
    // email liberado (mismo mecanismo que al cancelar una suscripción) para poder reintentar el alta.
    private async Task DarDeBajaClienteAMedias(Cliente cliente, int clienteId, Usuario usuario, int usuarioId)
    {
        try
        {
            string prefijo = $"baja+{Guid.NewGuid():N}+";

            usuario.UsuarioId = usuarioId;
            usuario.Activo    = false;
            usuario.Email     = prefijo + usuario.Email;
            await _uow.Usuario.UpdateAsync(usuario);

            cliente.ClienteId = clienteId;
            cliente.Activo    = false;
            cliente.Email     = prefijo + cliente.Email;
            await _uow.Cliente.UpdateAsync(cliente);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo dar de baja el cliente a medias clienteId={ClienteId}", clienteId);
        }
    }

    // ── Cambiar contraseña (primer login con contraseña temporal) ──────────
    public async Task<RespuestaResultado<LoginDto>> CambiarPasswordAsync(int usuarioId, string nuevaPassword)
    {
        if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword.Length < 8)
            return Error<LoginDto>("La contraseña debe tener al menos 8 caracteres.");

        var usuario = await _uow.Usuario.GetByIdAsync(usuarioId);
        if (usuario is null)
            return Error<LoginDto>("Usuario no encontrado.");

        usuario.PasswordHash        = BCrypt.Net.BCrypt.HashPassword(nuevaPassword, workFactor: 12);
        usuario.DebeCambiarPassword = false;
        await _uow.Usuario.UpdateAsync(usuario);

        var cliente = usuario.ClienteId.HasValue
            ? await _uow.Cliente.GetByIdAsync(usuario.ClienteId.Value)
            : null;
        string nombre = cliente is not null
            ? $"{cliente.Nombre} {cliente.Apellido}".Trim()
            : usuario.Email;

        return Exito(GenerarToken(usuario, nombre));
    }

    // ── Perfil ─────────────────────────────────────────────────────────────
    public async Task<RespuestaResultado<PerfilDto>> ObtenerPerfilAsync(int clienteId)
    {
        var cliente = await _uow.Cliente.GetByIdAsync(clienteId);
        if (cliente is null)
            return Error<PerfilDto>("Cliente no encontrado.");

        return Exito(new PerfilDto
        {
            ClienteId = cliente.ClienteId,
            Nombre    = cliente.Nombre,
            Apellido  = cliente.Apellido,
            Email     = cliente.Email,
            Telefono  = cliente.Telefono,
        });
    }

    public async Task<RespuestaResultado<PerfilDto>> ActualizarPerfilAsync(int clienteId, ActualizarPerfilRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return Error<PerfilDto>("El nombre es obligatorio.");

        var cliente = await _uow.Cliente.GetByIdAsync(clienteId);
        if (cliente is null)
            return Error<PerfilDto>("Cliente no encontrado.");

        cliente.Nombre   = request.Nombre.Trim();
        cliente.Apellido = request.Apellido?.Trim();
        cliente.Telefono = request.Telefono?.Trim();
        await _uow.Cliente.UpdateAsync(cliente);

        return Exito(new PerfilDto
        {
            ClienteId = cliente.ClienteId,
            Nombre    = cliente.Nombre,
            Apellido  = cliente.Apellido,
            Email     = cliente.Email,
            Telefono  = cliente.Telefono,
        });
    }

    // ── Recuperar contraseña ───────────────────────────────────────────────
    public async Task<RespuestaResultado<SolicitarResetDto>> SolicitarResetAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Error<SolicitarResetDto>("El email es obligatorio.");

        var usuario = await _uow.Usuario.ObtenerPorEmailAsync(email.Trim().ToLower());

        // No revelamos si el email existe o no (seguridad)
        if (usuario is null)
            return new RespuestaResultado<SolicitarResetDto>
            {
                Exitoso  = true,
                Mensaje  = "Si el email existe, recibirás las instrucciones.",
                Contenido = new SolicitarResetDto { Token = string.Empty },
            };

        var token  = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                         .Replace("+", "-").Replace("/", "_").Replace("=", "");
        var expiry = DateTime.UtcNow.AddHours(1);

        await _uow.Usuario.GuardarTokenResetAsync(usuario.UsuarioId, token, expiry);

        return new RespuestaResultado<SolicitarResetDto>
        {
            Exitoso  = true,
            Mensaje  = "Si el email existe, recibirás las instrucciones.",
            Contenido = new SolicitarResetDto { Token = token },
        };
    }

    public async Task<RespuestaResultado<object>> ResetPasswordAsync(string token, string nuevaPassword)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(nuevaPassword))
            return Error<object>("Token y contraseña son obligatorios.");

        if (nuevaPassword.Length < 8)
            return Error<object>("La contraseña debe tener al menos 8 caracteres.");

        var usuario = await _uow.Usuario.ObtenerPorTokenResetAsync(token);

        if (usuario is null)
            return Error<object>("El enlace de recuperación es inválido o ya expiró.");

        usuario.PasswordHash          = BCrypt.Net.BCrypt.HashPassword(nuevaPassword, workFactor: 12);
        usuario.ResetPasswordToken    = null;
        usuario.ResetPasswordTokenExpiry = null;
        await _uow.Usuario.UpdateAsync(usuario);

        return new RespuestaResultado<object> { Exitoso = true, Mensaje = "Contraseña actualizada correctamente." };
    }

    // ── JWT ────────────────────────────────────────────────────────────────
    private LoginDto GenerarToken(Usuario usuario, string nombre)
    {
        var key      = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds    = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expira   = DateTime.UtcNow.AddHours(8);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   nombre),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new("userId",                      usuario.UsuarioId.ToString()),
            new("role",                        usuario.Rol),
            new(ClaimTypes.Role,               usuario.Rol),
            new("debeCambiarPassword",         usuario.DebeCambiarPassword.ToString().ToLower()),
        };

        if (usuario.ClienteId.HasValue)
            claims.Add(new Claim("clienteId", usuario.ClienteId.Value.ToString()));

        var jwt = new JwtSecurityToken(
            issuer:            _config["Jwt:Issuer"],
            audience:          _config["Jwt:Audience"],
            claims:            claims,
            notBefore:         DateTime.UtcNow,
            expires:           expira,
            signingCredentials: creds);

        return new LoginDto
        {
            Token      = new JwtSecurityTokenHandler().WriteToken(jwt),
            Expiracion = expira.ToString("o"),
            Rol        = usuario.Rol,
            Nombre     = nombre,
            Email      = usuario.Email,
        };
    }

    private const string EmailUsuarioSistema = "sistema@fluxo.internal";

    // ── Usuario reservado para integraciones (ej. GestorPOS) ───────────────
    public async Task<int> ObtenerOCrearUsuarioSistemaIdAsync()
    {
        var existente = await _uow.Usuario.ObtenerPorEmailAsync(EmailUsuarioSistema);
        if (existente is not null) return existente.UsuarioId;

        var usuario = new Usuario
        {
            Email               = EmailUsuarioSistema,
            // Nunca se loguea con password — solo se usa su Id para auditoría de acciones
            // hechas por integraciones externas autenticadas con API key.
            PasswordHash        = BCrypt.Net.BCrypt.HashPassword(GenerarPasswordAleatorio(), workFactor: 12),
            Rol                 = "SISTEMA",
            Activo              = true,
            DebeCambiarPassword = false,
            FechaHoraCreacion   = DateTime.UtcNow,
        };
        return await _uow.Usuario.InsertAsync(usuario);
    }

    private static string GenerarPasswordAleatorio()
    {
        const string chars = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#";
        var bytes = RandomNumberGenerator.GetBytes(10);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    private static RespuestaResultado<T> Error<T>(string mensaje)
        => new() { Exitoso = false, Mensaje = mensaje };

    private static RespuestaResultado<T> Exito<T>(T contenido, string mensaje = "")
        => new() { Exitoso = true, Mensaje = mensaje, Contenido = contenido };
}
