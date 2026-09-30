namespace Models.Helpers;

public class ResultadoListaPaginada<T>
{
    public IEnumerable<T> ListaResultado { get; set; } = Enumerable.Empty<T>();
    public int TotalFilas { get; set; }
}
