using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CampusEats.Core.Application.Common.Jwt;
public static class JwtTokenGenerator
{
    public static string GenerateToken(string userId, string userEmail, int expiresInMinutes = 60)
    {
        var keyVar = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "dev_only_jwt_key_at_least_32_chars_long!!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyVar));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, userEmail),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
