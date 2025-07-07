using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Client.Features.Auth.Services;
using Eventrian.Client.UnitTests.Helpers;
using Eventrian.Shared.Dtos.Auth;
using Moq;
using Moq.Protected;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;

namespace Eventrian.Client.UnitTests.Features.Auth;

public class TokenRefresherTests
{
    private readonly Mock<IAccessTokenStorage> _accessTokenStorage = new();
    private readonly Mock<IRefreshTokenStorage> _refreshTokenStorage = new();
    private readonly Mock<IAuthBroadcastService> _broadcastService = new();

    private HttpClient CreateMockHttpClient(HttpStatusCode statusCode, RefreshResponseDto? content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = content != null
                    ? JsonContent.Create(content)
                    : new StringContent("")
            });

        return new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://fakeapi")
        };
    }

    private TokenRefresher CreateSut(HttpClient client)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("UnprotectedApi")).Returns(client);

        return new TokenRefresher(factory.Object, _accessTokenStorage.Object, _refreshTokenStorage.Object, _broadcastService.Object);
    }

    [Fact]
    public async Task TryRefreshTokenAsync_ReturnsTrue_WhenSuccessful()
    {
        // Arrange
        var dto = new RefreshResponseDto
        {
            Success = true,
            AccessToken = "access",
            RefreshToken = "refresh"
        };

        var client = CreateMockHttpClient(HttpStatusCode.OK, dto);
        var sut = CreateSut(client);

        _refreshTokenStorage.Setup(x => x.IsRefreshInProgressAsync()).ReturnsAsync(false);
        _refreshTokenStorage.Setup(x => x.GetRefreshTokenAsync()).ReturnsAsync("existing-token");
        _refreshTokenStorage.Setup(x => x.HasLocalStorageTokenAsync()).ReturnsAsync(true);

        // Act
        var result = await sut.TryRefreshTokenAsync();

        // Assert
        Assert.True(result);
        _accessTokenStorage.Verify(x => x.SetAccessToken("access"), Times.Once);
        _refreshTokenStorage.Verify(x => x.SetRefreshTokenAsync("refresh", true), Times.Once);
    }

    [Fact]
    public async Task TryRefreshTokenAsync_ReturnsFalse_WhenNoRefreshToken()
    {
        // Arrange
        var client = CreateMockHttpClient(HttpStatusCode.OK, null);
        var sut = CreateSut(client);

        _refreshTokenStorage.Setup(x => x.IsRefreshInProgressAsync()).ReturnsAsync(false);
        _refreshTokenStorage.Setup(x => x.GetRefreshTokenAsync()).ReturnsAsync(string.Empty);

        // Act
        var result = await sut.TryRefreshTokenAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryRefreshTokenAsync_ReturnsFalse_WhenRequestFails()
    {
        // Arrange
        var client = CreateMockHttpClient(HttpStatusCode.BadRequest, null);
        var sut = CreateSut(client);

        _refreshTokenStorage.Setup(x => x.IsRefreshInProgressAsync()).ReturnsAsync(false);
        _refreshTokenStorage.Setup(x => x.GetRefreshTokenAsync()).ReturnsAsync("existing-token");

        // Act
        var result = await sut.TryRefreshTokenAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckAndRefreshTokenAsync_CallsRefresh_WhenTokenIsExpired()
    {
        // Arrange
        var wasCalled = false;

        var client = CreateMockHttpClient(HttpStatusCode.OK, null);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("UnprotectedApi")).Returns(client);

        var sut = new TestableTokenRefresher(factory.Object, _accessTokenStorage.Object, _refreshTokenStorage.Object, _broadcastService.Object)
        {
            OnTryRefreshToken = () =>
            {
                wasCalled = true;
                return Task.FromResult(true);
            }
        };

        var tokenStr = JwtTestFactory.GenerateTestAccessToken("user-id", expiresInMinutes: 3);

        _accessTokenStorage.Setup(x => x.GetAccessToken()).Returns(tokenStr);
        
        // Act
        await sut.CheckAndRefreshTokenAsync();

        // Assert
        Assert.True(wasCalled);
    }

    private class TestableTokenRefresher : TokenRefresher
    {
        public Func<Task<bool>>? OnTryRefreshToken;

        public TestableTokenRefresher(
            IHttpClientFactory factory,
            IAccessTokenStorage accessTokenStorage,
            IRefreshTokenStorage refreshTokenStorage,
            IAuthBroadcastService broadcastService)
            : base(factory, accessTokenStorage, refreshTokenStorage, broadcastService)
        {
        }

        public override Task<bool> TryRefreshTokenAsync()
        {
            return OnTryRefreshToken?.Invoke() ?? Task.FromResult(false);
        }
    }
}
