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

    public async Task TerminateUserSessionAsync(bool fromBroadcast = false)
    {
        // If termination is triggered by a broadcast, do not rebroadcast — prevents logout loops
        await _authService.LogoutAsync(fromBroadcast);
        _navigation.NavigateTo("/", forceLoad: true);
    }
}
