using Eventrian.Client.Features.Auth.Interfaces;

namespace Eventrian.Client.Features.Auth.Services;

public class AccessTokenStorage : IAccessTokenStorage
{
    private readonly object _lock = new();
    private string? _accessToken;
    private readonly Guid _instanceId = Guid.NewGuid();

    private bool _tokenUpdatesBlocked = true; // Prevent token overwrite after logout or during cleanup
    public bool TokenUpdatesBlocked
    {
        get
        {
            lock (_lock)
                return _tokenUpdatesBlocked;
        }
        private set
        {
            lock (_lock)
                _tokenUpdatesBlocked = value;
        }
    }

    public void BlockTokenUpdates() => TokenUpdatesBlocked = true;
    public void AllowTokenUpdates() => TokenUpdatesBlocked = false;

    public string? GetAccessToken()
    {
        lock (_lock)
        {
            Console.WriteLine($"[AccessTokenStorage:{_instanceId}] GetAccessToken => {_accessToken}");
            return _accessToken;
        }
    }

    public void SetAccessToken(string token)
    {
        lock (_lock)
        {
            // Prevent async refresh or race conditions from restoring token after logout
            if (TokenUpdatesBlocked)
            {
                Console.WriteLine("[AccessTokenStorage] Ignoring SetAccessToken after session terminated.");
                return;
            }
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("[AccessTokenStorage] Rejected invalid access token.");
                return;
            }
            Console.WriteLine($"[AccessTokenStorage:{_instanceId}] SetAccessToken: {token}");
            _accessToken = token;
        }
    }

    public void ClearAccessToken()
    {
        lock (_lock)
        {
            Console.WriteLine($"[AccessTokenStorage:{_instanceId}] ClearAccessToken");
            _accessToken = null;
        }
    }
}