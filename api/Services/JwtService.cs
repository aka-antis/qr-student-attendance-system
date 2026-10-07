using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

public class JwtService(IConfiguration cfg)
{
    public string CreateToken(Teacher t)
    {
        var key = cfg["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        var issuer = cfg["Jwt:Issuer"] ?? "AttendanceQr";
        var audience = cfg["Jwt:Audience"] ?? "AttendanceQr";
        var minutes = int.TryParse(cfg["Jwt:ExpiresMinutes"], out var m) ? m : 720;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, t.Id.ToString()),
            new Claim(ClaimTypes.Name, t.Username),
            new Claim(ClaimTypes.Role, t.Role),
            new Claim("fullName", t.FullName),
        };
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(issuer, audience, claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
