namespace Models.Requests;

public class ActualizarPerfilRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Apellido { get; set; }
    public string? Telefono { get; set; }
}
