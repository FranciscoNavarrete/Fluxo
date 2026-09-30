using BusinessLogic;
using FluentValidation;
using MercadoPago.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Models.Requests;
using Models.Validators;

namespace WebApp.Extensions;

public static class MercadoPagoExtensions
{
    /// <summary>
    /// Registra todo lo necesario para el módulo de cobros recurrentes con Mercado Pago.
    /// Llamar desde Program.cs: builder.Services.AddMercadoPagoServices(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddMercadoPagoServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configurar el SDK de Mercado Pago con el access token
        // (se sobreescribe por request en cada llamada para soportar multi-tenant en el futuro)
        string accessToken = configuration["MercadoPago:AccessToken"]
            ?? throw new InvalidOperationException("MercadoPago:AccessToken no está configurado.");

        MercadoPagoConfig.AccessToken = accessToken;

        // --- Lógicas de negocio ---
        services.AddScoped<IMpPlanLogic, MpPlanLogic>();
        services.AddScoped<IMpSuscripcionLogic, MpSuscripcionLogic>();
        services.AddScoped<IMpWebhookLogic, MpWebhookLogic>();
        services.AddScoped<IMpPagoUnicoLogic, MpPagoUnicoLogic>();

        // --- Validators FluentValidation ---
        services.AddScoped<IValidator<CrearPlanRequest>, CrearPlanValidator>();
        services.AddScoped<IValidator<CrearSuscripcionRedirectRequest>, CrearSuscripcionRedirectValidator>();
        services.AddScoped<IValidator<CrearSuscripcionConTokenRequest>, CrearSuscripcionConTokenValidator>();

        // --- Servicio de dunning (background) ---
        // IDunningNotificador debe ser registrado por el sistema existente
        // (implementación concreta de email/WhatsApp).
        // Ejemplo: services.AddScoped<IDunningNotificador, SendGridDunningNotificador>();
        services.AddSingleton<DunningBackgroundService>();
        services.AddHostedService(sp => sp.GetRequiredService<DunningBackgroundService>());

        return services;
    }
}
