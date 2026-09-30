namespace Models.Requests;

public class SolicitarResetRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Token          { get; set; } = string.Empty;
    public string NuevaPassword  { get; set; } = string.Empty;
}

public class CambiarPasswordRequest
{
    public string NuevaPassword { get; set; } = string.Empty;
}
