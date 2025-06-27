using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Shared.Dtos.Auth;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly IRefreshTokenStorage _refreshTokenStorage;
    private readonly IAccessTokenStorage _accessTokenStorage;
    private readonly ITokenRefresher _tokenRefresher;
    private readonly ICustomAuthStateProvider _authStateProvider;

    public AuthService(IHttpClientFactory factory, IRefreshTokenStorage refreshTokenStorage, IAccessTokenStorage accessTokenStorage, ITokenRefresher tokenRefresher, ICustomAuthStateProvider authStateProvider)
    {
        _http = factory.CreateClient("UnprotectedApi");
        _refreshTokenStorage = refreshTokenStorage;
        _accessTokenStorage = accessTokenStorage;
        _tokenRefresher = tokenRefresher;
        _authStateProvider = authStateProvider;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        _accessTokenStorage.AllowTokenUpdates(); // Allow setting new access token; must be called before handling response

        var response = await _http.PostAsJsonAsync("api/auth/login", request);
        return await HandleAuthResponseAsync(response, request.RememberMe);
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        _accessTokenStorage.AllowTokenUpdates();

        var response = await _http.PostAsJsonAsync("api/auth/register", request);     
        return await HandleAuthResponseAsync(response);
    }

    public async Task LogoutAsync()
    {
        _accessTokenStorage.BlockTokenUpdates(); // Prevent further updates to the access token while logging out
        _accessTokenStorage.ClearAccessToken();

        _tokenRefresher.Stop();

        await _refreshTokenStorage.RemoveRefreshTokenAsync();
        await _authStateProvider.NotifyUserLogout();
    }

    // --- Helpers ---

    /// <summary>
    /// Handles common response logic for auth endpoints.
    /// Checks HTTP status, parses JSON, and validates structure.
    /// </summary>
    /// <param name="response">The HTTP response to process.</param>
    /// <param name="caller">Optional caller name for logging.</param>
    /// <returns>The parsed and validated response DTO, or a failure response.</returns>

    private async Task<LoginResponseDto> HandleAuthResponseAsync(HttpResponseMessage response, bool rememberMe = false, [CallerMemberName] string? caller = null)
    {
        if (!response.IsSuccessStatusCode)
            return LoginResponseDto.FailureResponse($"[{caller}] HTTP request failed with status code {response.StatusCode}.");

        var result = await JsonHelper.TryReadJsonAsync<LoginResponseDto>(response.Content, caller);

        if (!TokenHelper.IsValidTokenResponse(result))
            return LoginResponseDto.FailureResponse(result?.Message ?? $"[{caller}] Invalid response format or missing tokens.");

        _accessTokenStorage.SetAccessToken(result!.AccessToken!);
        await _refreshTokenStorage.SetRefreshTokenAsync(result.RefreshToken!, rememberMe);
        _tokenRefresher.Start();

        return result;
    }
}
