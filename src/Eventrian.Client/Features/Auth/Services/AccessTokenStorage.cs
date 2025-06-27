using Eventrian.Client.Features.Auth.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace Eventrian.Client.Features.Auth.Services;

public class AccessTokenStorage : IAccessTokenStorage
{
    private string? _accessToken;
    private readonly Guid _instanceId = Guid.NewGuid();
    // Prevent restoring stale tokens if session ends before access token is set
    public bool TokenUpdateBlocked { get; private set; } = true;

    public AccessTokenStorage()
    {
        Console.WriteLine($"[DI] AccessTokenStorage created with ID: {_instanceId}");
    }

    public void BlockTokenUpdates()
    {
        TokenUpdateBlocked = true;
        _accessToken = null;
    }
    public void AllowTokenUpdates() => TokenUpdateBlocked = false;

    public string? GetAccessToken()
    {
        Console.WriteLine($"[Storage:{_instanceId}] GetAccessToken => {_accessToken}");
        return _accessToken;
    }

    public void SetAccessToken(string token)
    {
        // Prevent async refresh or race conditions from restoring token after logout
        if (TokenUpdateBlocked)
        {
            Console.WriteLine("[AccessTokenStorage] Ignoring SetAccessToken after session terminated.");
            return;
        }
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("[Storage] Rejected invalid access token.");
            return;
        }
        Console.WriteLine($"[Storage:{_instanceId}] SetAccessToken: {token}");
        _accessToken = token;
        Console.WriteLine($"[Storage] SetAccessToken called with: {token}");
    }

    public void ClearAccessToken()
    {
        Console.WriteLine($"[Storage:{_instanceId}] ClearAccessToken");
        _accessToken = null;
    }
}