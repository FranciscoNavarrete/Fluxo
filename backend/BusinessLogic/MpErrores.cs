namespace BusinessLogic;

/// <summary>
/// Convierte los mensajes técnicos (en inglés) que devuelve Mercado Pago en algo que el vendedor
/// pueda entender y accionar. Lo que no se reconoce se muestra con el texto original, para no
/// esconder ni inventar nada; el original queda siempre en el log.
/// </summary>
internal static class MpErrores
{
    internal static string Traducir(string? original)
    {
        if (string.IsNullOrWhiteSpace(original))
            return "Mercado Pago rechazó el alta.";

        var texto = original.Trim();

        if (Contiene(texto, "different site"))
            return "Ese email está asociado a una cuenta de Mercado Pago de otro país. Usá otro email.";

        if (Contiene(texto, "rate_limited") || Contiene(texto, "rate limit") || Contiene(texto, "too many requests"))
            return "Mercado Pago está limitando los intentos. Esperá unos minutos y probá de nuevo.";

        if (Contiene(texto, "card token") || Contiene(texto, "card_token"))
            return "No se pudo validar la tarjeta. Revisá los datos y probá de nuevo.";

        if (Contiene(texto, "payer_email") || Contiene(texto, "payer email")
            || (Contiene(texto, "email") && (Contiene(texto, "invalid") || Contiene(texto, "valid email"))))
            return "El email no es válido. Revisalo.";

        return $"Mercado Pago rechazó el alta: {texto}";
    }

    private static bool Contiene(string texto, string fragmento)
        => texto.Contains(fragmento, StringComparison.OrdinalIgnoreCase);
}
