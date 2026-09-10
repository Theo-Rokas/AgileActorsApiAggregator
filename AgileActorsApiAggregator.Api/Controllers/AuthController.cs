using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AgileActorsApiAggregator.Api.Controllers;

/// <summary>Issues JWT bearer tokens for demo authentication.</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IConfiguration config) : ControllerBase
{
    /// <summary>Login credentials supplied in the request body.</summary>
    public record LoginRequest(string Username, string Password);

    /// <summary>
    /// Returns a JWT bearer token. Use username: "demo", password: "demo".
    /// </summary>
    [HttpPost("login")]
    public ActionResult<string> Login([FromBody] LoginRequest req)
    {
        // Simple demo credential check — replace with real auth in production
        if (req.Username != "demo" || req.Password != "demo")
            return Unauthorized();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: [new Claim(ClaimTypes.Name, req.Username)],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}
