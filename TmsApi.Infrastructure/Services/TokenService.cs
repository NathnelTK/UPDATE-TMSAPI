using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Services;

/// <summary>
/// M11 Session 2 - Exercise 4: issues signed JWT access tokens.
/// The signing key is read from configuration (user-secrets in Development, never
/// committed to source). Each token carries the standard identity claims plus the
/// TMS FirstName claim and one Role claim per assigned role, signed with HMAC-SHA256
/// over the symmetric key.
/// </summary>
public class TokenService(IConfiguration configuration)
{
    public string GenerateJwt(TmsUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("FirstName", user.FirstName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiryMinutes = int.Parse(configuration["Jwt:ExpiryMinutes"] ?? "15");

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
