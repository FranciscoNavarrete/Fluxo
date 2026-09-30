namespace Models.DTOs;

public class PerfilDto
{
    public int ClienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Apellido { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
}
