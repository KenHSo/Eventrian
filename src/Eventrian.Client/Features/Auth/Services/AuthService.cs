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
        return await HandleAuthResponseAsync(response, "Login");
    }

    public async Task LogoutAsync()
    {
        await _tokenStorage.RemoveTokenAsync();
        _authStateProvider.NotifyUserLogout();
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", request);
        return await HandleAuthResponseAsync(response, "Registration");
    }

    //TODO: Move to a shared utility when needed elsewhere
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

    /// <summary>
    /// Handles a standard authentication HTTP response by validating the response,
    /// extracting the token, storing it, and updating the authentication state.
    /// </summary>
    /// <param name="response">The HTTP response returned by the authentication endpoint.</param>
    /// <param name="actionName">A short label (e.g., "Login", "Registration") used in error messages.</param>
    /// <returns>
    /// A <see cref="LoginResponseDto"/> representing the result of the authentication attempt,
    /// including any error messages or the issued JWT token.
    /// </returns>
    private async Task<LoginResponseDto> HandleAuthResponseAsync(HttpResponseMessage response, string actionName)
    {
        // Parse JSON response
        var result = await TryReadJsonAsync<LoginResponseDto>(response.Content);
        if (result is null)
            return LoginResponseDto.FailureResponse($"{actionName} failed with status {response.StatusCode}.");

        // Check HTTP status
        if (!response.IsSuccessStatusCode)
            return LoginResponseDto.FailureResponse(result.Message ?? $"{actionName} failed with status {response.StatusCode}.");

        // Check success flag and token presence
        if (!result.Success || string.IsNullOrEmpty(result.Token))
            return LoginResponseDto.FailureResponse(result.Message ?? $"{actionName} failed. Server error or invalid credentials.");

        // Store token and update authentication state
        await _tokenStorage.SetTokenAsync(result.Token!);
        await _authStateProvider.NotifyUserAuthentication();

        return result;
    }
}
