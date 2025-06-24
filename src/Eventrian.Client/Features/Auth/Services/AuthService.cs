using System.Net.Http.Json;
using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Shared.Dtos.Auth;

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

        // Deserialize JSON response content
        var result = await TryReadJsonAsync<LoginResponseDto>(response.Content);
        if (result is null)
            return LoginResponseDto.FailureResponse(
                $"Login failed with status {response.StatusCode}.");

        // Check HTTP status code (e.g. 401, 500)
        if (!response.IsSuccessStatusCode)
            return LoginResponseDto.FailureResponse(
                result.Message ?? $"Login failed with status {response.StatusCode}.");

        // Validate backend login success and token presence
        if (!result.Success || string.IsNullOrEmpty(result.Token))
            return LoginResponseDto.FailureResponse(
                result.Message ?? "Login failed. Invalid credentials or server error.");

        // Store token and update authentication state
        await _tokenStorage.SetTokenAsync(result.Token!);
        await _authStateProvider.NotifyUserAuthentication();

        return result;
    }

    public async Task LogoutAsync()
    {
        await _tokenStorage.RemoveTokenAsync();
    }

    /// <summary>
    /// Attempts to deserialize JSON content from an <see cref="HttpContent"/> object into the specified type.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="content">The HTTP response content.</param>
    /// <returns>
    /// A task that returns the deserialized object if successful.
    /// Returns <c>null</c> if deserialization fails or the content is not valid JSON.
    /// </returns>
    public static async Task<T?> TryReadJsonAsync<T>(HttpContent content)
    {
        try { return await content.ReadFromJsonAsync<T>(); }
        catch { return default; }
    }
}
