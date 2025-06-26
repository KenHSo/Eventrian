using Eventrian.Client.Features.Auth.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class CustomAuthStateProvider : AuthenticationStateProvider, ICustomAuthStateProvider
{
    private readonly IAuthService _authService;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public CustomAuthStateProvider(IAuthService authService)
    {
        _authService = authService;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var accessToken = _authService.GetAccessToken();
        if (!string.IsNullOrWhiteSpace(accessToken))
            _currentUser = BuildClaimsPrincipal(accessToken);

        return Task.FromResult(new AuthenticationState(_currentUser));
    }

    public Task NotifyUserAuthentication()
    {
        var accessToken = _authService.GetAccessToken();
        if (!string.IsNullOrWhiteSpace(accessToken))
            _currentUser = BuildClaimsPrincipal(accessToken);

        var state = new AuthenticationState(_currentUser);
        NotifyAuthenticationStateChanged(Task.FromResult(state));
        return Task.CompletedTask;
    }

    public void NotifyUserLogout()
    {
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        var state = new AuthenticationState(_currentUser);
        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    public bool IsTokenExpired(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);
        return jwt.ValidTo < DateTime.UtcNow;
    }

    private ClaimsPrincipal BuildClaimsPrincipal(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);
        var identity = new ClaimsIdentity(jwt.Claims, "jwt");
        return new ClaimsPrincipal(identity);
    }
}
