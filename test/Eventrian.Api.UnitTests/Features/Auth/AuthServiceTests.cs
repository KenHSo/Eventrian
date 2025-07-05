using Eventrian.Api.Features.Auth.Interfaces;
using Eventrian.Api.Features.Auth.Models;
using Eventrian.Api.Features.Auth.Results;
using Eventrian.Api.Features.Auth.Services;
using Eventrian.Api.UnitTests.Helpers;
using Eventrian.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Identity;
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

        var service = new AuthService(
            userManagerMock.Object,
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

    [Fact]
    public async Task RegisterAsync_ReturnsSuccess_WhenRegistrationIsValid()
    {
        // Arrange
        var email = "new@example.com";
        var password = "SecurePass123!";
        var userId = "user-001";
        var roles = new List<string> { "Customer" };

        var newUser = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email
        };

        var userManagerMock = MockHelpers.CreateMockUserManager<ApplicationUser>();

        // User does not exist initially
        userManagerMock
            .Setup(m => m.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser?)null);

        // When CreateAsync is called, simulate Identity setting ID
        userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((u, _) => u.Id = userId); // Set ID on the object

        userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock
            .Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(roles);

        var accessTokenMock = new Mock<IAccessTokenService>();
        accessTokenMock
            .Setup(s => s.CreateAccessToken(userId, email, email, roles))
            .Returns("access-token");

        var refreshTokenMock = new Mock<IRefreshTokenService>();
        refreshTokenMock
            .Setup(s => s.IssueRefreshTokenAsync(userId, false))
            .ReturnsAsync("refresh-token");

        var loggerMock = new Mock<ILogger<AuthService>>();

        var service = new AuthService(
            userManagerMock.Object,
            accessTokenMock.Object,
            refreshTokenMock.Object,
            loggerMock.Object
        );

        var request = new RegisterRequestDto
        {
            Email = email,
            Password = password,
            FirstName = "New",
            LastName = "User"
        };

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(email, result.Email);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal("User registered successfully.", result.Message);
    }


    [Fact]
    public async Task RegisterAsync_ReturnsFailure_WhenUserAlreadyExists()
    {
        // Arrange
        var email = "existing@example.com";

        var existingUser = new ApplicationUser { Email = email };

        var userManagerMock = MockHelpers.CreateMockUserManager<ApplicationUser>();
        userManagerMock.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(existingUser);

        var service = new AuthService(
            userManagerMock.Object,
            Mock.Of<IAccessTokenService>(),
            Mock.Of<IRefreshTokenService>(),
            Mock.Of<ILogger<AuthService>>()
        );

        var request = new RegisterRequestDto
        {
            Email = email,
            Password = "irrelevant",
            FirstName = "X",
            LastName = "Y"
        };

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User already exists.", result.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_ReturnsSuccess_WhenTokenIsValid()
    {
        // Arrange
        var refreshToken = "valid-refresh-token";
        var userId = "user-123";
        var email = "test@example.com";
        var username = "testuser";
        var roles = new List<string> { "Customer" };
        var newRefreshToken = "rotated-token";
        var newAccessToken = "new-access-token";

        var user = new ApplicationUser { Id = userId, Email = email, UserName = username };

        var refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        refreshTokenServiceMock
            .Setup(s => s.ValidateAndRotateAsync(refreshToken))
            .ReturnsAsync(new RefreshTokenValidationResult
            {
                IsValid = true,
                NewRefreshToken = newRefreshToken,
                UserId = userId
            });


        var userManagerMock = MockHelpers.CreateMockUserManager<ApplicationUser>();
        userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(roles);

        var accessTokenMock = new Mock<IAccessTokenService>();
        accessTokenMock
            .Setup(s => s.CreateAccessToken(userId, email, username, roles))
            .Returns(newAccessToken);

        var service = new AuthService(userManagerMock.Object, accessTokenMock.Object, refreshTokenServiceMock.Object, Mock.Of<ILogger<AuthService>>());

        var request = new RefreshRequestDto { RefreshToken = refreshToken };

        // Act
        var result = await service.RefreshTokenAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(newAccessToken, result.AccessToken);
        Assert.Equal(newRefreshToken, result.RefreshToken);
        Assert.Equal("Token refreshed.", result.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_ReturnsFailure_WhenTokenIsInvalid()
    {
        // Arrange
        var refreshToken = "invalid-token";

        var refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        refreshTokenServiceMock
            .Setup(s => s.ValidateAndRotateAsync(refreshToken))
            .ReturnsAsync(RefreshTokenValidationResult.Failure());

        var userManagerMock = MockHelpers.CreateMockUserManager<ApplicationUser>();

        var service = new AuthService(
            userManagerMock.Object,
            Mock.Of<IAccessTokenService>(),
            refreshTokenServiceMock.Object,
            Mock.Of<ILogger<AuthService>>());


        var request = new RefreshRequestDto { RefreshToken = refreshToken };

        // Act
        var result = await service.RefreshTokenAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid or expired refresh token.", result.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_ReturnsFailure_WhenUserNotFound()
    {
        // Arrange
        var refreshToken = "valid-token";
        var userId = "nonexistent-user";

        var refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        refreshTokenServiceMock
            .Setup(s => s.ValidateAndRotateAsync(refreshToken))
            .ReturnsAsync(RefreshTokenValidationResult.Success(userId, "new-token", false));

        var userManagerMock = MockHelpers.CreateMockUserManager<ApplicationUser>();
        userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

        var service = new AuthService(
            userManagerMock.Object,
            Mock.Of<IAccessTokenService>(),
            refreshTokenServiceMock.Object,
            Mock.Of<ILogger<AuthService>>());

        var request = new RefreshRequestDto { RefreshToken = refreshToken };

        // Act
        var result = await service.RefreshTokenAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User not found.", result.Message);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ReturnsSuccess_WhenTokenIsValid()
    {
        // Arrange
        var refreshToken = "valid-token";
        var userId = "user-123";

        var refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        refreshTokenServiceMock
            .Setup(s => s.GetUserIdForToken(refreshToken))
            .ReturnsAsync(userId);

        refreshTokenServiceMock
            .Setup(s => s.RevokeRefreshTokensAsync(refreshToken))
            .Returns(Task.CompletedTask);

        var loggerMock = new Mock<ILogger<AuthService>>();

        var userManagerMock = MockHelpers.CreateMockUserManager<ApplicationUser>();

        var service = new AuthService(
            userManagerMock.Object,
            Mock.Of<IAccessTokenService>(),
            refreshTokenServiceMock.Object,
            loggerMock.Object);

        var request = new LogoutRequestDto { RefreshToken = refreshToken };

        // Act
        var result = await service.RevokeRefreshTokenAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Logged out and refresh token invalidated.", result.Message);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ReturnsFailure_WhenTokenIsInvalid()
    {
        // Arrange
        var refreshToken = "invalid-token";

        var refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        refreshTokenServiceMock
            .Setup(s => s.GetUserIdForToken(refreshToken))
            .ReturnsAsync((string?)null); // Simulate invalid token

        var userManagerMock = MockHelpers.CreateMockUserManager<ApplicationUser>();

        var service = new AuthService(
            userManagerMock.Object,
            Mock.Of<IAccessTokenService>(),
            refreshTokenServiceMock.Object,
            Mock.Of<ILogger<AuthService>>());

        var request = new LogoutRequestDto { RefreshToken = refreshToken };

        // Act
        var result = await service.RevokeRefreshTokenAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid refresh token.", result.Message);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_CallsRevokeOnly_WhenUserIdIsValid()
    {
        // Arrange
        var refreshToken = "valid-token";
        var userId = "user-123";

        var refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        refreshTokenServiceMock
            .Setup(s => s.GetUserIdForToken(refreshToken))
            .ReturnsAsync(userId);

        var loggerMock = new Mock<ILogger<AuthService>>();

        var userManagerMock = MockHelpers.CreateMockUserManager<ApplicationUser>();

        var service = new AuthService(
            userManagerMock.Object,
            Mock.Of<IAccessTokenService>(),
            refreshTokenServiceMock.Object,
            loggerMock.Object);

        var request = new LogoutRequestDto { RefreshToken = refreshToken };

        // Act
        await service.RevokeRefreshTokenAsync(request);

        // Assert
        refreshTokenServiceMock.Verify(s => s.RevokeRefreshTokensAsync(refreshToken), Times.Once);
    }

}
