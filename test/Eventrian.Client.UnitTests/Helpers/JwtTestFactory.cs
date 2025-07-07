using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Eventrian.Client.UnitTests.Helpers;

public static class JwtTestFactory
{
    /// Generates a mock JWT access token with the given user ID as the 'sub' claim.
    public static string GenerateTestAccessToken(string userId, int expiresInMinutes = 10)
    {
        var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, userId),
        new(JwtRegisteredClaimNames.Email, "test@example.com"),
        new(JwtRegisteredClaimNames.UniqueName, "testuser"),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

        // Needed because ClaimsPrincipal maps standard JWT claim types (like 'email') to ClaimTypes.Email when parsing.
        new(ClaimTypes.Email, "test@example.com"),
        new(ClaimTypes.Role, "User")
    };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-secret-key-must-be-32-bytes!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
