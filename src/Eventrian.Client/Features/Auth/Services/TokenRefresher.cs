using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Shared.Dtos.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Eventrian.Client.Features.Auth.Services;

public class TokenRefresher : ITokenRefresher
{
    private readonly HttpClient _http;
    private readonly IAccessTokenStorage _accessTokenStorage;
    private readonly IRefreshTokenStorage _refreshTokenStorage;

    private Timer? _refreshTimer;
    private const int RefreshIntervalMinutes = 2;

    public TokenRefresher(
        IHttpClientFactory factory,
        IAccessTokenStorage accessTokenStorage,
        IRefreshTokenStorage refreshTokenStorage)
    {
        _http = factory.CreateClient("UnprotectedApi");
        _accessTokenStorage = accessTokenStorage;
        _refreshTokenStorage = refreshTokenStorage;
    }

    public async Task InitializeAsync()
    {
        var success = await TryRefreshTokenAsync();
        if (!success)
        {
            _accessTokenStorage.ClearAccessToken();
            _http.DefaultRequestHeaders.Authorization = null;
        }

        Start();
    }

    public void Start()
    {
        Stop();
        _refreshTimer = new Timer(
            async _ => await CheckAndRefreshTokenAsync(),
            null,
            TimeSpan.FromMinutes(RefreshIntervalMinutes),
            TimeSpan.FromMinutes(RefreshIntervalMinutes));
    }

    public void Stop()
    {
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }

    public async Task CheckAndRefreshTokenAsync()
    {
        var accessToken = _accessTokenStorage.GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
            return;

        var handler = new JwtSecurityTokenHandler();
        JwtSecurityToken? jwt;
        try
        {
            jwt = handler.ReadJwtToken(accessToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Access token is invalid: {ex.Message}");
            await TryRefreshTokenAsync(); // attempt recovery
            return;
        }

        var expiresIn = jwt.ValidTo - DateTime.UtcNow;
        if (expiresIn < TimeSpan.FromMinutes(5))
        {
            await TryRefreshTokenAsync();
        }
    }

    public async Task<bool> TryRefreshTokenAsync()
    {
        var refreshToken = await _refreshTokenStorage.GetRefreshTokenAsync();
        if (string.IsNullOrWhiteSpace(refreshToken)) return false;

        var response = await _http.PostAsJsonAsync("api/auth/refresh", new RefreshRequestDto
        {
            RefreshToken = refreshToken
        });

        var result = await ParseJsonAsync<RefreshResponseDto>(response.Content);
        if (!IsValidResponse(response, result)) return false;

        _accessTokenStorage.SetAccessToken(result!.AccessToken!);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken!);

        await _refreshTokenStorage.SetRefreshTokenAsync(
            result.RefreshToken!,
            await _refreshTokenStorage.HasLocalStorageTokenAsync());

        return true;
    }

    // --- Helpers ---

    private static bool IsValidResponse(HttpResponseMessage response, RefreshResponseDto? result)
    {
        return result is not null &&
               response.IsSuccessStatusCode &&
               result.Success &&
               !string.IsNullOrWhiteSpace(result.AccessToken) &&
               !string.IsNullOrWhiteSpace(result.RefreshToken);
    }

    private static async Task<T?> ParseJsonAsync<T>(HttpContent content)
    {
        try { return await content.ReadFromJsonAsync<T>(); }
        catch { return default; }
    }
}
