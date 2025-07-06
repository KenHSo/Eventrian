using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Client.Features.Auth.Services;
using Moq;
using Moq.Protected;
using System.Net;

namespace Eventrian.Client.UnitTests.Features.Auth;

public class TokenRefreshHandlerTests
{
    private readonly Mock<IAccessTokenStorage> _accessTokenStorage = new();
    private readonly Mock<IRefreshTokenStorage> _refreshTokenStorage = new();
    private readonly Mock<IUserSessionTerminator> _terminator = new();
    private readonly Mock<ITokenRefresher> _refresher = new();
    private readonly Mock<HttpMessageHandler> _innerHandler = new();


    private TokenRefreshHandler CreateHandler()
    {
        return new TokenRefreshHandler(
            _refresher.Object,
            _accessTokenStorage.Object,
            _terminator.Object,
            _refreshTokenStorage.Object
        )
        {
            InnerHandler = _innerHandler.Object
        };
    }

    [Fact]
    public async Task ShouldTerminateSession_IfBothTokensMissing()
    {
        _accessTokenStorage.Setup(x => x.GetAccessToken()).Returns((string?)null);
        _refreshTokenStorage.Setup(x => x.GetRefreshTokenAsync()).ReturnsAsync((string?)null);

        var handler = CreateHandler();
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://any");

        await invoker.SendAsync(request, default);

        _terminator.Verify(x => x.TerminateUserSessionAsync(false), Times.Once);
        _innerHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task ShouldSkipRequest_IfTokenUpdatesBlocked()
    {
        _accessTokenStorage.Setup(x => x.TokenUpdatesBlocked).Returns(true);

        var handler = CreateHandler();
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://any");

        var response = await invoker.SendAsync(request, default);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _innerHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task ShouldProactivelyRefresh_IfTokenMissingOrExpired()
    {
        _accessTokenStorage.Setup(x => x.GetAccessToken()).Returns((string?)null);
        _refresher.Setup(x => x.TryRefreshTokenAsync()).ReturnsAsync(true);
        _accessTokenStorage.Setup(x => x.GetAccessToken()).Returns("new-token");

        _innerHandler.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Headers.Authorization != null &&
                r.Headers.Authorization.Parameter == "new-token"),
            ItExpr.IsAny<CancellationToken>()
        ).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));


        var handler = CreateHandler();
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://any");

        var response = await invoker.SendAsync(request, default);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldTerminate_IfProactiveRefreshFails()
    {
        _accessTokenStorage.Setup(x => x.GetAccessToken()).Returns((string?)null);
        _refresher.Setup(x => x.TryRefreshTokenAsync()).ReturnsAsync(false);

        var handler = CreateHandler();
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://any");

        await invoker.SendAsync(request, default);

        _terminator.Verify(x => x.TerminateUserSessionAsync(false), Times.Once);
    }

    [Fact]
    public async Task ShouldRetryRefresh_IfInitialRequestFailsWith401()
    {
        // Arrange
        var expiredToken = "expired-token";
        var newToken = "new-token";
        var callCount = 0;

        _accessTokenStorage.Setup(x => x.GetAccessToken()).Returns(() =>
        {
            return callCount == 0 ? expiredToken : newToken;
        });

        _refreshTokenStorage.Setup(x => x.GetRefreshTokenAsync()).ReturnsAsync("dummy-refresh-token");
        _refresher.Setup(x => x.TryRefreshTokenAsync()).ReturnsAsync(true);

        _innerHandler.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        ).Returns<HttpRequestMessage, CancellationToken>((request, token) =>
        {
            var tokenUsed = request.Headers.Authorization?.Parameter;
            callCount++;

            if (callCount == 1)
            {
                Assert.Equal(expiredToken, tokenUsed);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }

            Assert.Equal(newToken, tokenUsed);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var handler = CreateHandler();
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://any")
        {
            Content = new StringContent("")
        };

        // Act
        var response = await invoker.SendAsync(request, default);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, callCount);
        _refresher.Verify(x => x.TryRefreshTokenAsync(), Times.Exactly(2)); // Accept 2 refresh calls
    }

    [Fact]
    public async Task ShouldTerminate_IfRetryRefreshFails()
    {
        var token = "expired-token";

        _accessTokenStorage.SetupSequence(x => x.GetAccessToken())
            .Returns(token)
            .Returns(token);

        _refresher.Setup(x => x.TryRefreshTokenAsync()).ReturnsAsync(false);

        _innerHandler.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        ).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var handler = CreateHandler();
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://any");

        await invoker.SendAsync(request, default);

        _terminator.Verify(x => x.TerminateUserSessionAsync(false), Times.Once);
    }
}
