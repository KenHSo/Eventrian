using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Shared.Dtos.Auth;
using System.Net.Http.Headers;
using System.Net.Http.Json;

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly IRefreshTokenStorage _refreshTokenStorage;
    private readonly IAccessTokenStorage _accessTokenStorage;
    private readonly ITokenRefresher _tokenRefresher;
    private readonly ICustomAuthStateProvider _authStateProvider;

    public AuthService(IHttpClientFactory factory, IRefreshTokenStorage refreshTokenStorage, IAccessTokenStorage accessTokenProvider, ITokenRefresher tokenRefresher, ICustomAuthStateProvider authStateProvider)
    {
        _http = factory.CreateClient("NoAuth");
        _refreshTokenStorage = refreshTokenStorage;
        _accessTokenStorage = accessTokenProvider;
        _tokenRefresher = tokenRefresher;
        _authStateProvider = authStateProvider;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        _accessTokenStorage.AllowTokenUpdates(); // Allow updates to the access token during login 

        var response = await _http.PostAsJsonAsync("api/auth/login", request);
        var result = await ParseJsonAsync<LoginResponseDto>(response.Content);

        if (!IsValidLoginResponse(response, result))
        {
            return LoginResponseDto.FailureResponse(
                result?.Message ??
                $"Login failed with status {response.StatusCode}."
            );
        }

        ApplyAccessToken(result!.AccessToken!);
        await _refreshTokenStorage.SetRefreshTokenAsync(result.RefreshToken!, request.RememberMe);

        _tokenRefresher.Start();

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

        ApplyAccessToken(result!.AccessToken!);
        await _refreshTokenStorage.SetRefreshTokenAsync(result.RefreshToken!, false);

        _tokenRefresher.Start();

        return result;
    }

    public async Task LogoutAsync()
    {
        _accessTokenStorage.BlockTokenUpdates(); // Prevent further updates to the access token while logging out
        _accessTokenStorage.ClearAccessToken();
        _http.DefaultRequestHeaders.Authorization = null;

        _tokenRefresher.Stop();

        await _refreshTokenStorage.RemoveRefreshTokenAsync();
        await _authStateProvider.NotifyUserLogout();
    }


    // --- Helpers ---

    /// <summary> Sets the access token in memory and attaches it to the Authorization header of outgoing HTTP requests</summary>

    private void ApplyAccessToken(string accessToken)
    {
        _accessTokenStorage.SetAccessToken(accessToken);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    // TODO: Move this to a shared utility class + add error handling
    private static async Task<T?> ParseJsonAsync<T>(HttpContent content)
    {
        try { return await content.ReadFromJsonAsync<T>(); }
        catch
        {
            Console.WriteLine($"[AuthService] Failed to parse JSON for type {typeof(T).Name}.");
            return default;
        }
    }

    private static bool IsValidLoginResponse(HttpResponseMessage response, LoginResponseDto? result)
    {
        return result is not null &&
               response.IsSuccessStatusCode &&
               result.Success &&
               !string.IsNullOrEmpty(result.AccessToken) &&
               !string.IsNullOrEmpty(result.RefreshToken);
    }
   
}
