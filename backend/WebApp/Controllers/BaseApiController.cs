using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebApp.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    // Extrae el userId del JWT claim "userId" (o NameIdentifier como fallback)
    protected int ObtenerUsuarioId()
    {
        var claim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out int id) ? id : 0;
    }

    protected int? ObtenerClienteId()
    {
        var claim = User.FindFirst("clienteId");
        if (claim is not null && int.TryParse(claim.Value, out int id)) return id;
        return null;
    }
}
