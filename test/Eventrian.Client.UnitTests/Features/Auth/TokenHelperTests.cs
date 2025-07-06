using Eventrian.Client.Helpers;
using Eventrian.Client.UnitTests.Helpers;
using Eventrian.Shared.Dtos.Auth.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace Eventrian.Client.UnitTests.Features.Auth;

public class TokenHelperTests
{
    private class FakeAuthResponse : IAuthResponse
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }

    [Fact]
    public void IsValidTokenResponse_ReturnsTrue_WhenAllFieldsAreValid()
    {
        // Arrange
        var dto = new FakeAuthResponse
        {
            Success = true,
            AccessToken = "access",
            RefreshToken = "refresh"
        };

        // Act
        var result = TokenHelper.IsValidTokenResponse(dto);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(null, "refresh")]
    [InlineData("access", null)]
    [InlineData("", "refresh")]
    [InlineData("access", "")]
    public void IsValidTokenResponse_ReturnsFalse_WhenTokensAreMissing(string? access, string? refresh)
    {
        // Arrange
        var dto = new FakeAuthResponse
        {
            Success = true,
            AccessToken = access,
            RefreshToken = refresh
        };

        // Act
        var result = TokenHelper.IsValidTokenResponse(dto);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidTokenResponse_ReturnsFalse_WhenSuccessIsFalse()
    {
        // Arrange
        var dto = new FakeAuthResponse
        {
            Success = false,
            AccessToken = "access",
            RefreshToken = "refresh"
        };

        // Act
        var result = TokenHelper.IsValidTokenResponse(dto);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsExpired_ReturnsTrue_ForExpiredToken()
    {
        // Arrange
        var token = new JwtSecurityToken(expires: DateTime.UtcNow.AddMinutes(-5));
        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

        // Act
        var result = TokenHelper.IsExpired(tokenStr);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsExpired_ReturnsFalse_ForValidToken()
    {
        // Arrange
        var token = new JwtSecurityToken(expires: DateTime.UtcNow.AddMinutes(10));
        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

        // Act
        var result = TokenHelper.IsExpired(tokenStr);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsExpired_ReturnsTrue_ForInvalidToken()
    {
        // Arrange
        var invalidToken = "invalid-token";

        // Act
        var result = TokenHelper.IsExpired(invalidToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryParseJwt_ReturnsToken_ForValidJwt()
    {
        // Arrange
        var token = new JwtSecurityToken();
        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

        // Act
        var result = TokenHelper.TryParseJwt(tokenStr);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void TryParseJwt_ReturnsNull_ForInvalidJwt()
    {
        // Arrange
        var invalidToken = "not-a-jwt";

        // Act
        var result = TokenHelper.TryParseJwt(invalidToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetUserIdFromAccessToken_ReturnsSubjectClaim()
    {
        // Arrange
        var tokenStr = JwtTestFactory.GenerateTestAccessToken("user-123");

        // Act
        var result = TokenHelper.GetUserIdFromAccessToken(tokenStr);

        // Assert
        Assert.Equal("user-123", result);
    }

    [Fact]
    public void GetUserIdFromAccessToken_ReturnsEmpty_WhenInvalidToken()
    {
        // Arrange
        var invalidToken = "bad-token";

        // Act
        var result = TokenHelper.GetUserIdFromAccessToken(invalidToken);

        // Assert
        Assert.Equal("", result);
    }
}
