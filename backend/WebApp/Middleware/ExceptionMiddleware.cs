using System.Net;
using System.Text.Json;
using Models.Helpers;

namespace WebApp.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no manejada. TraceId={TraceId}", context.TraceIdentifier);
            await EscribirRespuestaErrorAsync(context, ex);
        }
    }

    private static Task EscribirRespuestaErrorAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;

        var respuesta = new RespuestaResultado<object>
        {
            Exitoso  = false,
            Mensaje  = $"Error interno del servidor. TraceId: {context.TraceIdentifier}",
            Contenido = null
        };

        var json = JsonSerializer.Serialize(respuesta, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }
}
