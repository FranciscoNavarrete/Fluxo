using System.Security.Cryptography;
using System.Text;

namespace WebApp.Middleware;

/// <summary>Protege /api/vendedor con una API key propia — es la integración server-to-server
/// que usa GestorPOS para iniciar una suscripción al dar de alta un negocio, no un usuario humano
/// logueado con JWT, así que no tiene sentido meterlo en el esquema de auth de clientes/admin.</summary>
public class VendedorApiKeyMiddleware
{
    private const string HeaderName = "X-Vendedor-Api-Key";
    private readonly RequestDelegate _next;

    public VendedorApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        if (!context.Request.Path.StartsWithSegments("/api/vendedor"))
        {
            await _next(context);
            return;
        }

        var apiKeyConfigurada = configuration["Vendedor:ApiKey"];
        var apiKeyRecibida = context.Request.Headers[HeaderName].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKeyConfigurada) ||
            string.IsNullOrEmpty(apiKeyRecibida) ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(apiKeyRecibida), Encoding.UTF8.GetBytes(apiKeyConfigurada)))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API key de vendedor inválida o faltante." });
            return;
        }

        await _next(context);
    }
}
