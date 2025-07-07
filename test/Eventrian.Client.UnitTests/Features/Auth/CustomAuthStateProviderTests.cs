using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Client.UnitTests.Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using System.Security.Claims;

namespace Eventrian.Client.UnitTests.Features.Auth;

public class CustomAuthStateProviderTests
{
    private readonly Mock<IAccessTokenStorage> _accessTokenStorage = new();
    private CustomAuthStateProvider CreateAuthProvider() => new(_accessTokenStorage.Object);

    [Fact]
    public async Task GetAuthenticationStateAsync_ShouldReturnEmptyIdentity_IfNoToken()
    {
        _accessTokenStorage.Setup(x => x.GetAccessToken()).Returns((string?)null);

        var authProvider = CreateAuthProvider();
        var state = await authProvider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ShouldReturnValidIdentity_IfTokenIsValid()
    {
        var token = JwtTestFactory.GenerateTestAccessToken("user-id");
        _accessTokenStorage.Setup(x => x.GetAccessToken()).Returns(token);

        var authProvider = CreateAuthProvider();
        var state = await authProvider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("test@example.com", state.User.FindFirst(ClaimTypes.Email)?.Value);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ShouldReturnEmptyIdentity_IfTokenMalformed()
    {
        _accessTokenStorage.Setup(x => x.GetAccessToken()).Returns("invalid-token");

        var authProvider = CreateAuthProvider();
        var state = await authProvider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task NotifyUserAuthentication_ShouldBroadcast_ValidIdentity()
    {
        var token = JwtTestFactory.GenerateTestAccessToken("user-id");
        _accessTokenStorage.Setup(x => x.GetAccessToken()).Returns(token);

        var authProvider = CreateAuthProvider();

        AuthenticationState? receivedState = null;
        authProvider.AuthenticationStateChanged += async task => receivedState = await task;

        await authProvider.NotifyUserAuthentication();

        Assert.NotNull(receivedState);
        Assert.True(receivedState!.User.Identity?.IsAuthenticated);
        Assert.Equal("test@example.com", receivedState.User.FindFirst(ClaimTypes.Email)?.Value);
    }

    [Fact]
    public async Task NotifyUserLogout_ShouldBroadcast_EmptyIdentity()
    {
        var authProvider = CreateAuthProvider();

        AuthenticationState? receivedState = null;
        authProvider.AuthenticationStateChanged += async task => receivedState = await task;

        await authProvider.NotifyUserLogout();

        Assert.NotNull(receivedState);
        Assert.False(receivedState!.User.Identity?.IsAuthenticated);
    }
}
