using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Shared.Dtos.Auth;
using System.Net.Http.Json;

namespace Eventrian.Client.Features.Auth.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly ITokenStorageService _tokenStorage;
    private readonly ICustomAuthStateProvider _authStateProvider;

    public AuthService(HttpClient http, ITokenStorageService tokenStorage, ICustomAuthStateProvider authStateProvider)
    {
        _http = http;
        _tokenStorage = tokenStorage;
        _authStateProvider = authStateProvider;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request);
        var result = await ParseJsonAsync<LoginResponseDto>(response.Content);

        if (!IsValidAuthResponse(response, result))
        {
            return LoginResponseDto.FailureResponse(
                result?.Message ??
                $"Login failed with status {response.StatusCode}."
            );
        }

        await SetTokenAndNotifyAsync(result!.Token!, request.RememberMe);
        return result;
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", request);
        var result = await ParseJsonAsync<LoginResponseDto>(response.Content);

        if (!IsValidAuthResponse(response, result))
        {
            return LoginResponseDto.FailureResponse(
                result?.Message ??
                $"Registration failed with status {response.StatusCode}."
            );
        }

        await SetTokenAndNotifyAsync(result!.Token!, rememberMe: false);
        return result;
    }

    public async Task LogoutAsync()
    {
        await _tokenStorage.RemoveTokenAsync();
        _authStateProvider.NotifyUserLogout();
    }

    // --- Helpers ---

    private static bool IsValidAuthResponse(HttpResponseMessage response, LoginResponseDto? result)
    {
        return result is not null &&
               response.IsSuccessStatusCode &&
               result.Success &&
               !string.IsNullOrEmpty(result.Token);
    }

    private static async Task<T?> ParseJsonAsync<T>(HttpContent content)
    {
        try { return await content.ReadFromJsonAsync<T>(); }
        catch { return default; }
    }
    
    private async Task SetTokenAndNotifyAsync(string token, bool rememberMe)
    {
        await _tokenStorage.SetTokenAsync(token, rememberMe);
        await _authStateProvider.NotifyUserAuthentication();
    }
}
