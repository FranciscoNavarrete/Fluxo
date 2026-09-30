namespace Models.DTOs;

public class SolicitarResetDto
{
    /// <summary>
    /// Token de reset. En producción esto se envía por email; acá lo exponemos
    /// para facilitar el desarrollo sin SMTP configurado.
    /// </summary>
    public string Token { get; set; } = string.Empty;
}
