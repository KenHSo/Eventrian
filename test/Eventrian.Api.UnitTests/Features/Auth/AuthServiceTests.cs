using Eventrian.Api.Features.Auth.Interfaces;
using Eventrian.Api.Features.Auth.Services;
using Eventrian.Api.Models;
using Eventrian.Api.UnitTests.Helpers;
using Eventrian.Shared.Dtos.Auth;
using Microsoft.Extensions.Logging;
using Moq;

namespace Eventrian.Api.UnitTests.Features.Auth;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ReturnsSuccess_WhenCredentialsAreValid()
    {
        // Arrange
        var userId = "user-123";
        var email = "test@example.com";
        var username = "testuser";
        var password = "Password123!";
        var roles = new List<string> { "Customer" };

        var user = new ApplicationUser { Id = userId, Email = email, UserName = username };

        var userManagerMock = MockHelpers.CreateMockUserManager<ApplicationUser>();
        userManagerMock.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.CheckPasswordAsync(user, password)).ReturnsAsync(true);
        userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(roles);

        var accessTokenServiceMock = new Mock<IAccessTokenService>();
        accessTokenServiceMock
            .Setup(m => m.CreateAccessToken(userId, email, username, roles))
            .Returns("access-token");

        var refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        refreshTokenServiceMock
            .Setup(m => m.IssueRefreshTokenAsync(userId, true))
            .ReturnsAsync("refresh-token");

        var loggerMock = new Mock<ILogger<AuthService>>();

        var service = new AuthService(
            userManagerMock.Object,
            accessTokenServiceMock.Object,
            refreshTokenServiceMock.Object,
            loggerMock.Object
        );

        var request = new LoginRequestDto
        {
            Email = email,
            Password = password,
            RememberMe = true
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal(email, result.Email);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFailure_WhenCredentialsAreInvalid()
    {
        // Arrange
        var email = "wrong@example.com";
        var password = "invalid";

        var user = new ApplicationUser { Email = email };

        var userManagerMock = MockHelpers.CreateMockUserManager<ApplicationUser>();
        userManagerMock.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.CheckPasswordAsync(user, password)).ReturnsAsync(false);

        var service = new AuthService(userManagerMock.Object,
            Mock.Of<IAccessTokenService>(),
            Mock.Of<IRefreshTokenService>(),
            Mock.Of<ILogger<AuthService>>());

        var request = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.Message);
    }

}
