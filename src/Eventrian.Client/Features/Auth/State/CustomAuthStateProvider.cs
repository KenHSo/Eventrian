using Eventrian.Client.Features.Auth.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class CustomAuthStateProvider : AuthenticationStateProvider, ICustomAuthStateProvider
{
    private readonly IAccessTokenStorage _accessTokenProvider;

    public CustomAuthStateProvider(IAccessTokenStorage accessTokenProvider)
    {
        _accessTokenProvider = accessTokenProvider;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = _accessTokenProvider.GetAccessToken();
        var user = string.IsNullOrWhiteSpace(token)
            ? new ClaimsPrincipal(new ClaimsIdentity())
            : BuildClaimsPrincipal(token);

        return Task.FromResult(new AuthenticationState(user));
    }

    public Task NotifyUserAuthentication()
    {
        var token = _accessTokenProvider.GetAccessToken();
        var user = string.IsNullOrWhiteSpace(token)
            ? new ClaimsPrincipal(new ClaimsIdentity())
            : BuildClaimsPrincipal(token);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        return Task.CompletedTask;
    }

    public void NotifyUserLogout()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
    }

    private ClaimsPrincipal BuildClaimsPrincipal(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwt.Claims, "jwt");
        return new ClaimsPrincipal(identity);
    }
}
