using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Client.Features.Auth.Services;
using Eventrian.Client.UnitTests.Helpers;
using Eventrian.Shared.Dtos.Auth;
using Moq;
using System.Net;

namespace Eventrian.Client.UnitTests.Features.Auth;

public class AuthServiceTests
{
    private readonly Mock<IRefreshTokenStorage> _refreshTokenStorage = new();
    private readonly Mock<IAccessTokenStorage> _accessTokenStorage = new();
    private readonly Mock<ITokenRefresher> _tokenRefresher = new();
    private readonly Mock<ICustomAuthStateProvider> _authStateProvider = new();
    private readonly Mock<IAuthBroadcastService> _authBroadcastService = new();
    private readonly Mock<HttpMessageHandler> _handler = new();

    private AuthService CreateService()
    {
        var client = new HttpClient(_handler.Object)
        {
            BaseAddress = new Uri("https://localhost")
        };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("UnprotectedApi")).Returns(client);

        return new AuthService(
            factory.Object,
            _refreshTokenStorage.Object,
            _accessTokenStorage.Object,
            _tokenRefresher.Object,
            _authStateProvider.Object,
            _authBroadcastService.Object
        );
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFailure_IfHttpFails()
    {
        _handler.SetupRequest(HttpMethod.Post, "https://localhost/api/auth/login")
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequestDto());

        Assert.False(result.Success);
        Assert.Contains("LoginAsync", result.Message);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_IfHttpFails()
    {
        _handler.SetupRequest(HttpMethod.Post, "https://localhost/api/auth/register")
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        var service = CreateService();

        var result = await service.RegisterAsync(new RegisterRequestDto());

        Assert.False(result.Success);
        Assert.Contains("RegisterAsync", result.Message);
    }

    [Fact]
    public async Task LoginAsync_ShouldStoreTokens_AndBroadcastLogoutSync()
    {
        var responseDto = new LoginResponseDto
        {
            Success = true,
            AccessToken = JwtTestFactory.GenerateTestAccessToken("user-id"),
            RefreshToken = "refresh-token"
        };

        _handler.SetupRequest(HttpMethod.Post, "https://localhost/api/auth/login")
                .ReturnsJson(responseDto);

        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequestDto());

        Assert.True(result.Success);
        _accessTokenStorage.Verify(x => x.SetAccessToken(responseDto.AccessToken!), Times.Once);
        _refreshTokenStorage.Verify(x => x.SetRefreshTokenAsync("refresh-token", false), Times.Once);
        _tokenRefresher.Verify(x => x.Start(), Times.Once);
        _authBroadcastService.Verify(x => x.InitLogoutBroadcastAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_ShouldClearTokens_AndBroadcastIfNotFromBroadcast()
    {
        _accessTokenStorage.Setup(x => x.GetAccessToken()).Returns(JwtTestFactory.GenerateTestAccessToken("user-id"));
        _refreshTokenStorage.Setup(x => x.GetRefreshTokenAsync()).ReturnsAsync("refresh-token");

        _handler.SetupRequest(HttpMethod.Post, "https://localhost/api/auth/logout")
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var service = CreateService();

        await service.LogoutAsync(fromBroadcast: false);

        _accessTokenStorage.Verify(x => x.ClearAccessToken(), Times.Once);
        _refreshTokenStorage.Verify(x => x.RemoveRefreshTokenAsync(), Times.Once);
        _tokenRefresher.Verify(x => x.Stop(), Times.Once);
        _authBroadcastService.Verify(x => x.BroadcastLogoutAsync("user-id"), Times.Once);
        _authStateProvider.Verify(x => x.NotifyUserLogout(), Times.Once);
        _authBroadcastService.Verify(x => x.ClearUserAsync(), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_ShouldSkipLogoutEndpoint_IfFromBroadcast()
    {
        _accessTokenStorage.Setup(x => x.GetAccessToken()).Returns(JwtTestFactory.GenerateTestAccessToken("user-id"));

        var service = CreateService();

        await service.LogoutAsync(fromBroadcast: true);

        _handler.VerifyNoOutstandingRequest(); // logout endpoint should not be called
        _authBroadcastService.Verify(x => x.BroadcastLogoutAsync(It.IsAny<string>()), Times.Never);
    }
}
