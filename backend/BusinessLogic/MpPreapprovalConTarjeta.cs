using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BusinessLogic;

/// <summary>
/// El SDK de Mercado Pago 2.4.x no expone card_token_id en PreapprovalCreateRequest, así que la
/// suscripción con tarjeta se crea pegándole directo a la API REST de preapproval. Sin CardTokenId y
/// con Status "pending" crea la suscripción para pagar por link (init_point); también cubre
/// repeticiones (Repetitions), que el SDK tampoco expone.
/// </summary>
internal static class MpPreapprovalConTarjeta
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://api.mercadopago.com"),
        Timeout = TimeSpan.FromSeconds(30),
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    internal record Datos(
        string Reason,
        string ExternalReference,
        string? PayerEmail,
        string? CardTokenId,
        string BackUrl,
        int Frequency,
        string FrequencyType,
        decimal TransactionAmount,
        string CurrencyId,
        DateTime StartDateUtc,
        DateTime? EndDateUtc,
        int? Repeticiones = null,
        string Status = "authorized");

    internal record Resultado(bool Exitoso, string? Id, string? Estado, string? PayerId, string? Error, string? ErrorOriginal = null, string? InitPoint = null);

    internal static async Task<Resultado> CrearAsync(string accessToken, Datos datos)
    {
        var cuerpo = new
        {
            datos.Reason,
            datos.ExternalReference,
            datos.PayerEmail,
            datos.CardTokenId,
            datos.BackUrl,
            AutoRecurring = new
            {
                datos.Frequency,
                datos.FrequencyType,
                StartDate = FormatearFecha(datos.StartDateUtc),
                EndDate = datos.EndDateUtc.HasValue ? FormatearFecha(datos.EndDateUtc.Value) : (string?)null,
                Repetitions = datos.Repeticiones,
                datos.TransactionAmount,
                datos.CurrencyId,
            },
            datos.Status,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/preapproval")
        {
            Content = new StringContent(JsonSerializer.Serialize(cuerpo, JsonOptions), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

        try
        {
            using var response = await Http.SendAsync(request);
            var texto = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(texto) ? "{}" : texto);
            var raiz = doc.RootElement;

            if (!response.IsSuccessStatusCode)
            {
                var (amigable, original) = ExtraerError(raiz, response.StatusCode);
                return new Resultado(false, null, null, null, amigable, original);
            }

            string? id = raiz.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrEmpty(id))
                return new Resultado(false, null, null, null, "Mercado Pago no devolvió el id de la suscripción.");

            string? estado = raiz.TryGetProperty("status", out var stEl) ? stEl.GetString() : null;
            string? initPoint = raiz.TryGetProperty("init_point", out var ipEl) && ipEl.ValueKind == JsonValueKind.String ? ipEl.GetString() : null;
            string? payerId = raiz.TryGetProperty("payer_id", out var pEl) && pEl.ValueKind != JsonValueKind.Null
                ? pEl.ToString()
                : null;

            return new Resultado(true, id, estado, payerId, null, null, initPoint);
        }
        catch (Exception ex)
        {
            return new Resultado(false, null, null, null, $"No se pudo comunicar con Mercado Pago: {ex.Message}");
        }
    }

    private static string FormatearFecha(DateTime utc)
        => utc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");

    private static (string Amigable, string? Original) ExtraerError(JsonElement raiz, System.Net.HttpStatusCode status)
    {
        if (raiz.ValueKind == JsonValueKind.Object && raiz.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
        {
            var original = m.GetString();
            return (MpErrores.Traducir(original), original);
        }
        return ($"Mercado Pago rechazó la suscripción (HTTP {(int)status}).", null);
    }
}
