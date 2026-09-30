namespace Models.Helpers;

public class RespuestaResultado<T>
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public T? Contenido { get; set; }
}
