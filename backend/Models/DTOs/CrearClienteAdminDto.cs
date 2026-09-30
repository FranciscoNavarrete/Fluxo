namespace Models.DTOs;

public class CrearClienteAdminDto
{
    public int ClienteId { get; set; }
    public int UsuarioId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordTemporal { get; set; } = string.Empty;   // mostrar UNA vez al admin
    public string? InitPoint { get; set; }                          // link de MP si se creó suscripción
    public int? MpSuscripcionId { get; set; }
}
