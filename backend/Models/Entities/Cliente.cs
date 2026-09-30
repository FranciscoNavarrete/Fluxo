using Dapper.Contrib.Extensions;

namespace Models.Entities;

[Table("Clientes")]
public class Cliente
{
    [Key]
    public int ClienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Apellido { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaHoraCreacion { get; set; }
}
