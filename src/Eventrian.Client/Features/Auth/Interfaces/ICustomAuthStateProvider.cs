namespace Eventrian.Client.Features.Auth.Interfaces;

public interface ICustomAuthStateProvider
{
    Task NotifyUserAuthentication();
    void NotifyUserLogout();
    bool IsTokenExpired(string token);
}

