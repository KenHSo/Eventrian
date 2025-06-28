using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Shared.Dtos.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace Eventrian.Client.Features.Auth.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly IRefreshTokenStorage _refreshTokenStorage;
    private readonly IAccessTokenStorage _accessTokenStorage;
    private readonly ITokenRefresher _tokenRefresher;
    private readonly ICustomAuthStateProvider _authStateProvider;
    private readonly IAuthBroadcastService _authBroadcastService;

    public AuthService(IHttpClientFactory factory, IRefreshTokenStorage refreshTokenStorage, IAccessTokenStorage accessTokenStorage, ITokenRefresher tokenRefresher, ICustomAuthStateProvider authStateProvider, IAuthBroadcastService authBroadcastService)
    {
        _http = factory.CreateClient("UnprotectedApi");
        _refreshTokenStorage = refreshTokenStorage;
        _accessTokenStorage = accessTokenStorage;
        _tokenRefresher = tokenRefresher;
        _authStateProvider = authStateProvider;
        _authBroadcastService = authBroadcastService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        // Ensure access token can be set (in case logout previously blocked it)
        _accessTokenStorage.AllowTokenUpdates();

        var response = await _http.PostAsJsonAsync("api/auth/login", request);
        return await HandleAuthResponseAsync(response, request.RememberMe);
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        _accessTokenStorage.AllowTokenUpdates();

        var response = await _http.PostAsJsonAsync("api/auth/register", request);     
        return await HandleAuthResponseAsync(response);
    }

    public async Task LogoutAsync(bool fromBroadcast = false)
    {
        _accessTokenStorage.BlockTokenUpdates();

        var accessToken = _accessTokenStorage.GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {           
            return; // Already logged out or invalid state — do nothing
        }
        // Extract user ID before clearing access token (needed for broadcast)
        var userId = TokenHelper.GetUserIdFromAccessToken(accessToken);

        _accessTokenStorage.ClearAccessToken();
        _tokenRefresher.Stop();

        if (!fromBroadcast)
        {
            var refreshToken = await _refreshTokenStorage.GetRefreshTokenAsync();
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _http.PostAsJsonAsync("api/auth/logout",
                    new LogoutRequestDto { RefreshToken = refreshToken });
            }

            // Broadcast logout to all other tabs for this user
            await _authBroadcastService.BroadcastLogoutAsync(userId);
        }

        await _refreshTokenStorage.RemoveRefreshTokenAsync();
        await _authStateProvider.NotifyUserLogout();
        await _authBroadcastService.ClearUserAsync();
    }

    // --- Helpers ---

    /// <summary>
    /// Processes a login or registration response:
    /// validates structure, stores tokens, starts refresh cycle, and sets up logout sync.
    /// </summary>
    /// <param name="response">The HTTP response returned from the login or register endpoint.</param>
    /// <param name="rememberMe">True if refresh token should persist beyond tab lifetime.</param>
    /// <param name="caller">Auto-filled caller name used in error messages for context.</param>
    /// <returns>The parsed and validated response DTO, or a failure result with error message.</returns>

    private async Task<LoginResponseDto> HandleAuthResponseAsync(HttpResponseMessage response, bool rememberMe = false, [CallerMemberName] string? caller = null)
    {
        if (!response.IsSuccessStatusCode)
            return LoginResponseDto.FailureResponse($"[{caller}] HTTP request failed with status code {response.StatusCode}.");

        var result = await JsonHelper.TryReadJsonAsync<LoginResponseDto>(response.Content, caller);
        
        if (!TokenHelper.IsValidTokenResponse(result))
            return LoginResponseDto.FailureResponse(result?.Message ?? $"[{caller}] Invalid response format or missing tokens.");

        // At this point, the response is valid — begin setting up the session

        _accessTokenStorage.SetAccessToken(result!.AccessToken!);
        await _refreshTokenStorage.SetRefreshTokenAsync(result.RefreshToken!, rememberMe);

        _tokenRefresher.Start();

        // Set this tab's identity so it can respond to logout broadcasts from other tabs
        var userId = TokenHelper.GetUserIdFromAccessToken(result.AccessToken!);
        await _authBroadcastService.InitLogoutBroadcastAsync(userId);

        return result;
    }

}
