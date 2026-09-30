using Dapper.Contrib.Extensions;

namespace Models.Entities;

[Table("Usuarios")]
public class Usuario
{
    [Key]
    public int UsuarioId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = "CLIENTE";
    public int? ClienteId { get; set; }
    public bool Activo { get; set; } = true;
    public bool DebeCambiarPassword { get; set; } = false;
    public DateTime FechaHoraCreacion { get; set; }
    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetPasswordTokenExpiry { get; set; }
}
