using Dapper.Contrib.Extensions;

namespace Models.Entities;

[Table("MpDunningLogs")]
public class MpDunningLog
{
    [Key]
    public int MpDunningLogId { get; set; }
    public int MpSuscripcionId { get; set; }

    // email | whatsapp | warning | suspend | cancel
    public string Accion { get; set; } = string.Empty;
    public int DiasVencido { get; set; }
    public string Canal { get; set; } = string.Empty;
    public DateTime FechaEjecucion { get; set; }
}
