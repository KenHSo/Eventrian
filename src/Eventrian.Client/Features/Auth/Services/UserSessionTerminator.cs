using Eventrian.Client.Features.Auth.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Eventrian.Client.Features.Auth.Services;

public class UserSessionTerminator : IUserSessionTerminator
{
    private readonly IAuthService _authService;
    private readonly NavigationManager _navigation;

    public UserSessionTerminator(IAuthService authService, NavigationManager navigation)
    {
        _authService = authService;
        _navigation = navigation;
    }

    public async Task TerminateUserSessionAsync()
    {
        await _authService.LogoutAsync();
        _navigation.NavigateTo("/login", forceLoad: true);
    }
}
