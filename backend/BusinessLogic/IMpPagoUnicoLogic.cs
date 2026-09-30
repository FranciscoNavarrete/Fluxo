using Models.Entities;
using Models.Helpers;
using Models.Requests;

namespace BusinessLogic;

public interface IMpPagoUnicoLogic
{
    Task<RespuestaResultado<PagoUnicoResultado>> CrearPagoUnicoAsync(
        CrearPagoUnicoRequest request, int usuarioId);

    Task<RespuestaResultado<IEnumerable<MpPagoUnico>>> ObtenerPorClienteAsync(int clienteId);
    Task<RespuestaResultado<bool>> EliminarPendienteAsync(int mpPagoUnicoId, int? clienteId);
}

public class PagoUnicoResultado
{
    public int    MpPagoUnicoId    { get; set; }
    public string InitPoint        { get; set; } = string.Empty;
    public decimal Monto           { get; set; }
    public string Moneda           { get; set; } = "ARS";
    public bool   SuscripcionPausada { get; set; }
    public DateTime? FechaReanudacion { get; set; }
}
