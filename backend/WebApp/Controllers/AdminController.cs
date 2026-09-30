using BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMINISTRADOR")]
public class AdminController : BaseApiController
{
    private readonly DunningBackgroundService _dunningService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        DunningBackgroundService dunningService,
        ILogger<AdminController> logger)
    {
        _dunningService = dunningService;
        _logger = logger;
    }

    /// <summary>Dispara un ciclo de dunning inmediatamente. Solo para testing/admin.</summary>
    [HttpPost("dunning/ejecutar")]
    public async Task<IActionResult> EjecutarDunning(CancellationToken ct)
    {
        _logger.LogInformation("Ciclo de dunning disparado manualmente por usuario {Id}.", ObtenerUsuarioId());
        await _dunningService.EjecutarCicloDunningPublicAsync(ct);
        return Ok(new { mensaje = "Ciclo de dunning ejecutado." });
    }
}
