using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Shared.Dtos.Auth;
using System.Net.Http.Headers;
using System.Net.Http.Json;

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly IRefreshTokenStorageService _refreshTokenStorage;

    private string? _accessToken;

    public AuthService(HttpClient http, IRefreshTokenStorageService refreshTokenStorage)
    {
        _http = http;
        _refreshTokenStorage = refreshTokenStorage;
    }

    public async Task InitializeAsync()
    {
        var success = await TryRefreshTokenAsync();
        if (!success)
        {
            _accessToken = null;
            _http.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request);
        var result = await ParseJsonAsync<LoginResponseDto>(response.Content);

        if (!IsValidLoginResponse(response, result))
        {
            return LoginResponseDto.FailureResponse(
                result?.Message ?? 
                $"Login failed with status {response.StatusCode}."
            );
        }

        SetAccessToken(result!.AccessToken!);
        await _refreshTokenStorage.SetRefreshTokenAsync(result.RefreshToken!, request.RememberMe);
        return result;
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", request);
        var result = await ParseJsonAsync<LoginResponseDto>(response.Content);

        if (!IsValidLoginResponse(response, result))
        {
            return LoginResponseDto.FailureResponse(
                result?.Message ?? 
                $"Registration failed with status {response.StatusCode}."
            );
        }

        SetAccessToken(result!.AccessToken!);
        await _refreshTokenStorage.SetRefreshTokenAsync(result.RefreshToken!, false);
        return result;
    }

    public async Task LogoutAsync()
    {
        _accessToken = null;
        _http.DefaultRequestHeaders.Authorization = null;

        // TODO: In the future, consider supporting "soft logout" to only clear sessionStorage
        //       This would allow switching between demo users while keeping persistent refresh tokens

        await _refreshTokenStorage.RemoveRefreshTokenAsync();
    }

    public async Task<bool> TryRefreshTokenAsync()
    {
        var refreshToken = await _refreshTokenStorage.GetRefreshTokenAsync();
        if (string.IsNullOrWhiteSpace(refreshToken)) return false;

        Console.WriteLine($"[AuthService] RefreshToken: {refreshToken}");
        Console.WriteLine($"[AuthService] AccessToken: {_accessToken}");

        var response = await _http.PostAsJsonAsync("api/auth/refresh", new RefreshRequestDto
        {
            RefreshToken = refreshToken
        });

        var result = await ParseJsonAsync<RefreshResponseDto>(response.Content);
        if (!IsValidRefreshResponse(response, result)) return false;

        SetAccessToken(result!.AccessToken!);
        await _refreshTokenStorage.SetRefreshTokenAsync(
            result.RefreshToken!,
            await _refreshTokenStorage.HasLocalStorageTokenAsync()
        );

        return true;
    }

    public string? GetAccessToken() => _accessToken;

    // --- Helpers ---

    private void SetAccessToken(string accessToken)
    {
        _accessToken = accessToken;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static async Task<T?> ParseJsonAsync<T>(HttpContent content)
    {
        try { return await content.ReadFromJsonAsync<T>(); }
        catch { return default; }
    }

    private static bool IsValidLoginResponse(HttpResponseMessage response, LoginResponseDto? result)
    {
        return result is not null &&
               response.IsSuccessStatusCode &&
               result.Success &&
               !string.IsNullOrEmpty(result.AccessToken) &&
               !string.IsNullOrEmpty(result.RefreshToken);
    }

    private static bool IsValidRefreshResponse(HttpResponseMessage response, RefreshResponseDto? result)
    {
        return result is not null &&
               response.IsSuccessStatusCode &&
               result.Success &&
               !string.IsNullOrEmpty(result.AccessToken) &&
               !string.IsNullOrEmpty(result.RefreshToken);
    }
}
