using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Client.Helpers;
using Eventrian.Shared.Dtos.Auth;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Eventrian.Client.Features.Auth.Services;

public class TokenRefresher : ITokenRefresher
{
    private readonly HttpClient _http;
    private readonly IAccessTokenStorage _accessTokenStorage;
    private readonly IRefreshTokenStorage _refreshTokenStorage;
    private readonly IAuthBroadcastService _authBroadcastService;
    
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private DateTime _lastRefreshTime = DateTime.MinValue;
    private readonly TimeSpan _minRefreshInterval = TimeSpan.FromMilliseconds(1000);

    private Timer? _refreshTimer;
    private const int RefreshIntervalMinutes = 2;

    public TokenRefresher(IHttpClientFactory factory, IAccessTokenStorage accessTokenStorage, IRefreshTokenStorage refreshTokenStorage, IAuthBroadcastService authBroadcastService)
    {
        _http = factory.CreateClient("UnprotectedApi");
        _accessTokenStorage = accessTokenStorage;
        _refreshTokenStorage = refreshTokenStorage;
        _authBroadcastService = authBroadcastService;
    }

    public async Task InitializeAsync()
    {
        _accessTokenStorage.AllowTokenUpdates();

        // TryRefreshTokenAsync handles its own locking
        var success = await TryRefreshTokenAsync();

        var token = _accessTokenStorage.GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("[TokenRefresher] No access token after init");
            _http.DefaultRequestHeaders.Authorization = null;
        }

        Start();
    }

    public void Start()
    {
        Stop(); // prevent multiple timers
        _refreshTimer = new Timer(
            OnRefreshTimerTick, null,
            TimeSpan.FromMinutes(RefreshIntervalMinutes),
            TimeSpan.FromMinutes(RefreshIntervalMinutes));
    }

    #region Private Timer Helpers

    /// <summary>
    /// Timer callback required by <see cref="System.Threading.Timer"/>.
    /// Discards the task returned by <see cref="RunCheckAndRefreshTokenAsync"/> to avoid unobserved exceptions.
    /// </summary>
    /// <param name="_">Unused state parameter required by the timer delegate.</param>
    private void OnRefreshTimerTick(object? _) => _ = RunCheckAndRefreshTokenAsync();

    /// <summary>
    /// Performs token refresh logic inside a try-catch block to prevent timer thread crashes.
    /// This method is executed as a fire-and-forget task triggered by <see cref="OnRefreshTimerTick"/>.
    /// </summary>
    private async Task RunCheckAndRefreshTokenAsync()
    {
        try { await CheckAndRefreshTokenAsync(); }
        catch (Exception ex) { Console.WriteLine($"[TokenRefresher] Timer exception: {ex.Message}"); }
    }

    #endregion

    public void Stop()
    {
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }

    public async Task<bool> TryRefreshTokenAsync()
    {
        // Wait if another tab is refreshing
        int maxWaitMs = 1000;
        int waited = 0;
        while (await _refreshTokenStorage.IsRefreshInProgressAsync())
        {
            await Task.Delay(100);
            waited += 100;

            if (waited >= maxWaitMs)
            {
                Console.WriteLine("[TokenRefresher] Waited too long for other refresh. Giving up.");
                return false;
            }
        }

        await _refreshTokenStorage.SetRefreshInProgressAsync(true);
        await _refreshLock.WaitAsync();

        try
        {
            var now = DateTime.UtcNow;
            if (now - _lastRefreshTime < _minRefreshInterval)
            {
                Console.WriteLine("[TokenRefresher] Skipping redundant refresh.");
                return true;
            }

            // Fetch the latest token AFTER waiting + locking
            var refreshToken = await _refreshTokenStorage.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;

            var response = await _http.PostAsJsonAsync("api/auth/refresh", new RefreshRequestDto { RefreshToken = refreshToken });
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[TokenRefresher] Refresh request failed with status: {response.StatusCode}");
                return false;
            }

            var result = await JsonHelper.TryReadJsonAsync<RefreshResponseDto>(response.Content);
            if (!TokenHelper.IsValidTokenResponse(result))
            {
                Console.WriteLine("[TokenRefresher] Invalid refresh response.");
                return false;
            }

            _lastRefreshTime = now;

            _accessTokenStorage.SetAccessToken(result!.AccessToken!);
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken!);

            var rememberMe = await _refreshTokenStorage.HasLocalStorageTokenAsync();
            await _refreshTokenStorage.SetRefreshTokenAsync(result.RefreshToken!, rememberMe);

            var userId = TokenHelper.GetUserIdFromAccessToken(result.AccessToken!);
            await _authBroadcastService.InitLogoutBroadcastAsync(userId);

            return true;
        }
        finally
        {
            await _refreshTokenStorage.SetRefreshInProgressAsync(false);
            _refreshLock.Release();
        }
    }

    public async Task CheckAndRefreshTokenAsync()
    {
        var accessToken = _accessTokenStorage.GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
            return;

        var jwt = TokenHelper.TryParseJwt(accessToken);
        if (jwt == null)
        {
            Console.WriteLine("[TokenRefresher] Access token is invalid.");
            await TryRefreshTokenAsync(); // Attempt to refresh token if parsing failed
            return;
        }

        if (TokenHelper.IsExpired(accessToken, TimeSpan.FromMinutes(5)))
        {
            Console.WriteLine($"[TokenRefresher] Token expiring soon at: {jwt?.ValidTo:O}");
            await TryRefreshTokenAsync();
        }
    }
}
