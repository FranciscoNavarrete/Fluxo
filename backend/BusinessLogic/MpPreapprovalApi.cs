using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BusinessLogic;

/// <summary>
/// Consultas a la API REST de preapproval que el SDK 2.4.x no cubre bien: cuántos cobros se hicieron
/// y cambio del monto de una suscripción ya creada.
/// </summary>
internal static class MpPreapprovalApi
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://api.mercadopago.com"),
        Timeout = TimeSpan.FromSeconds(30),
    };

    internal record Datos(int CobrosRealizados, DateTime? UltimoCobroUtc, DateTime? ProximoCobroUtc, decimal? Monto);

    internal static async Task<(bool Ok, Datos? Datos, string? Error)> ObtenerAsync(string accessToken, string preapprovalId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/preapproval/{Uri.EscapeDataString(preapprovalId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await Http.SendAsync(request);
            var texto = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return (false, null, $"HTTP {(int)response.StatusCode}: {texto}");

            using var doc = JsonDocument.Parse(texto);
            var raiz = doc.RootElement;

            int cobros = 0;
            DateTime? ultimo = null;
            if (raiz.TryGetProperty("summarized", out var resumen) && resumen.ValueKind == JsonValueKind.Object)
            {
                if (resumen.TryGetProperty("charged_quantity", out var q) && q.ValueKind == JsonValueKind.Number)
                    cobros = q.GetInt32();
                ultimo = LeerFecha(resumen, "last_charged_date");
            }

            decimal? monto = null;
            if (raiz.TryGetProperty("auto_recurring", out var recurrente) && recurrente.ValueKind == JsonValueKind.Object
                && recurrente.TryGetProperty("transaction_amount", out var m) && m.ValueKind == JsonValueKind.Number)
                monto = m.GetDecimal();

            return (true, new Datos(cobros, ultimo, LeerFecha(raiz, "next_payment_date"), monto), null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    internal static async Task<(bool Ok, IReadOnlyList<MpCobro> Cobros, string? Error)> ObtenerCobrosAsync(
        string accessToken, string preapprovalId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"/authorized_payments/search?preapproval_id={Uri.EscapeDataString(preapprovalId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await Http.SendAsync(request);
            var texto = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return (false, [], $"HTTP {(int)response.StatusCode}: {texto}");

            using var doc = JsonDocument.Parse(texto);
            var cobros = new List<MpCobro>();
            if (doc.RootElement.TryGetProperty("results", out var resultados) && resultados.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in resultados.EnumerateArray())
                {
                    string? pagoEstado = null, pagoDetalle = null;
                    if (r.TryGetProperty("payment", out var pago) && pago.ValueKind == JsonValueKind.Object)
                    {
                        pagoEstado = LeerTexto(pago, "status");
                        pagoDetalle = LeerTexto(pago, "status_detail");
                    }

                    cobros.Add(new MpCobro(
                        Id: r.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number ? id.GetInt64() : 0,
                        Estado: LeerTexto(r, "status") ?? string.Empty,
                        Monto: r.TryGetProperty("transaction_amount", out var m) && m.ValueKind == JsonValueKind.Number ? m.GetDecimal() : 0,
                        Fecha: LeerFecha(r, "debit_date"),
                        Intento: r.TryGetProperty("retry_attempt", out var ia) && ia.ValueKind == JsonValueKind.Number ? ia.GetInt32() : 0,
                        ProximoReintento: LeerFecha(r, "next_retry_date"),
                        PagoEstado: pagoEstado,
                        PagoDetalle: pagoDetalle));
                }
            }

            return (true, cobros, null);
        }
        catch (Exception ex)
        {
            return (false, [], ex.Message);
        }
    }

    private static string? LeerTexto(JsonElement elemento, string propiedad)
        => elemento.TryGetProperty(propiedad, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    /// <summary>Cambia la tarjeta de una suscripción: el token lo genera el navegador (Card Payment Brick).</summary>
    internal static async Task<(bool Ok, string? Error, string? ErrorOriginal)> ActualizarTarjetaAsync(
        string accessToken, string preapprovalId, string cardTokenId)
    {
        var cuerpo = new { card_token_id = cardTokenId };

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/preapproval/{Uri.EscapeDataString(preapprovalId)}")
        {
            Content = new StringContent(JsonSerializer.Serialize(cuerpo), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await Http.SendAsync(request);
            if (response.IsSuccessStatusCode) return (true, null, null);

            var texto = await response.Content.ReadAsStringAsync();
            string? mensaje = null;
            try
            {
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(texto) ? "{}" : texto);
                mensaje = LeerTexto(doc.RootElement, "message");
            }
            catch (JsonException) { }

            var amigable = mensaje is not null && (mensaje.Contains("card token", StringComparison.OrdinalIgnoreCase)
                                                   || mensaje.Contains("card_token", StringComparison.OrdinalIgnoreCase))
                ? "No se pudo validar la tarjeta. Revisá los datos y probá de nuevo."
                : "Mercado Pago no aceptó la tarjeta nueva. Revisá los datos o probá con otra tarjeta.";
            return (false, amigable, $"HTTP {(int)response.StatusCode}: {texto}");
        }
        catch (Exception ex)
        {
            return (false, "No se pudo comunicar con Mercado Pago. Probá de nuevo en un momento.", ex.Message);
        }
    }

    internal static async Task<(bool Ok, string? Error)> ActualizarMontoAsync(
        string accessToken, string preapprovalId, decimal monto, string moneda)
    {
        var cuerpo = new { auto_recurring = new { transaction_amount = monto, currency_id = moneda } };

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/preapproval/{Uri.EscapeDataString(preapprovalId)}")
        {
            Content = new StringContent(JsonSerializer.Serialize(cuerpo), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await Http.SendAsync(request);
            if (response.IsSuccessStatusCode) return (true, null);
            var texto = await response.Content.ReadAsStringAsync();
            return (false, $"HTTP {(int)response.StatusCode}: {texto}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static DateTime? LeerFecha(JsonElement elemento, string propiedad)
    {
        if (elemento.TryGetProperty(propiedad, out var valor) && valor.ValueKind == JsonValueKind.String
            && DateTime.TryParse(valor.GetString(), System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var fecha))
            return fecha;
        return null;
    }
}
