using Eventrian.Api.Features.Auth.Services;
using Eventrian.Api.Settings;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Eventrian.Api.UnitTests.Features.Auth;

public class AccessTokenServiceTests
{
    private AccessTokenService CreateService()
    {
        var jwtSettings = new JwtSettings
        {
            SecretKey = "super-secret-test-key-1234567890", // min 32 chars
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 30
        };

        return new AccessTokenService(Options.Create(jwtSettings));
    }

    [Fact]
    public void CreateAccessToken_Returns_Valid_JWT_With_Correct_Claims()
    {
        // Arrange
        var service = CreateService();
        var userId = "test-user-id";
        var email = "test@example.com";
        var username = "testuser";
        var roles = new List<string> { "Admin", "Customer" };

        // Act
        var token = service.CreateAccessToken(userId, email, username, roles);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal(userId, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(email, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(username, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);

        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Contains("Admin", roleClaims);
        Assert.Contains("Customer", roleClaims);

        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Equal("TestAudience", jwt.Audiences.First());
    }
}
