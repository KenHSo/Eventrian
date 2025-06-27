using Eventrian.Client.Features.Auth.Components;
using Eventrian.Client.Features.Auth.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;

namespace Eventrian.Client.Features.Auth.Services;

public class TokenRefreshHandler : DelegatingHandler
{
    private readonly ITokenRefresher _tokenRefresher;
    private readonly IAccessTokenStorage _accessTokenStorage;
    private readonly IUserSessionTerminator _terminator;
    private readonly IRefreshTokenStorage _refreshTokenStorage;

    public TokenRefreshHandler(
        ITokenRefresher tokenRefresher,
        IAccessTokenStorage accessTokenStorage,
        IUserSessionTerminator terminator,
        IRefreshTokenStorage refreshTokenStorage)
    {
        _tokenRefresher = tokenRefresher;
        _accessTokenStorage = accessTokenStorage;
        _terminator = terminator;
        _refreshTokenStorage = refreshTokenStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Guard 0: Guard clause – if both tokens are missing, terminate immediately
        var accessToken = _accessTokenStorage.GetAccessToken();
        var refreshToken = await _refreshTokenStorage.GetRefreshTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken) && string.IsNullOrWhiteSpace(refreshToken))
        {
            Console.WriteLine("[TokenRefreshHandler] Both tokens missing. Terminating session.");
            await _terminator.TerminateUserSessionAsync();
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        // Guard 1: avoid accidental login after logout
        if (_accessTokenStorage.TokenUpdateBlocked)
        {
            Console.WriteLine("[TokenRefreshHandler] Token updates are blocked. Skipping request.");
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        // Guard 2: Proactive refresh if needed
        var token = _accessTokenStorage.GetAccessToken();

        if (string.IsNullOrWhiteSpace(token) || IsExpired(token))
        {
            Console.WriteLine("[TokenRefreshHandler] No token or expired token. Attempting refresh before sending request.");
            var refreshedBeforeRequest = await _tokenRefresher.TryRefreshTokenAsync();

            if (!refreshedBeforeRequest)
            {
                Console.WriteLine("[TokenRefreshHandler] Proactive refresh failed. Terminating session.");
                await _terminator.TerminateUserSessionAsync();
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            token = _accessTokenStorage.GetAccessToken();
        }

        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);


        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // Retry if 401
        var refreshedAfter401 = await _tokenRefresher.TryRefreshTokenAsync();
        if (!refreshedAfter401)
        {
            Console.WriteLine("[TokenRefreshHandler] Refresh failed — terminating session.");
            await _terminator.TerminateUserSessionAsync();
            return response;
        }

        var newToken = _accessTokenStorage.GetAccessToken();
        if (string.IsNullOrWhiteSpace(newToken))
            return response;

        var clonedRequest = await CloneHttpRequestAsync(request);
        clonedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
        Console.WriteLine($"[TokenRefreshHandler] Retrying request to {request.RequestUri} with new token.");

        return await base.SendAsync(clonedRequest, cancellationToken);
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);

            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        clone.Version = request.Version;

        foreach (var kvp in request.Options)
            clone.Options.TryAdd(kvp.Key, kvp.Value);

        return clone;
    }
    private static bool IsExpired(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.ValidTo < DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TokenRefreshHandler] Failed to parse token: {ex.Message}");
            return true; // Treat malformed tokens as expired
        }
    }
}