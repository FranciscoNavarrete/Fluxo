using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebApp.Controllers;

/// <summary>
/// Endpoint de desarrollo — solo activo cuando IsDevelopment = true.
/// Genera un JWT firmado con la misma clave que usa el backend.
/// </summary>
[ApiController]
[Route("api/dev")]
public class DevController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public DevController(IConfiguration config, IWebHostEnvironment env)
    {
        _config = config;
        _env    = env;
    }

    [HttpGet("token")]
    public IActionResult GenerarToken([FromQuery] string rol = "ADMINISTRADOR")
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   "Dev Admin"),
            new Claim(JwtRegisteredClaimNames.Email, "admin@credigial.com"),
            new Claim("userId",                      "1"),
            new Claim("role",                        rol),   // short form: el frontend lo lee como c.role
            new Claim(ClaimTypes.Role,               rol),   // long form:  el [Authorize(Roles=...)] lo lee
        };

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddDays(90),
            signingCredentials: creds
        );

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}
