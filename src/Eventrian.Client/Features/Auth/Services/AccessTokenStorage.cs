using Eventrian.Client.Features.Auth.Interfaces;

namespace Eventrian.Client.Features.Auth.Services;

public class AccessTokenStorage : IAccessTokenStorage
{
    private string? _accessToken;

    public string? GetAccessToken() => _accessToken;

    public void SetAccessToken(string token)
    {
        _accessToken = token;
    }

    public void ClearAccessToken()
    {
        _accessToken = null;
    }
}