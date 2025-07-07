using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Client.Helpers;
using System.Net;
using System.Net.Http.Headers;

namespace Eventrian.Client.Features.Auth.Services;

public class TokenRefreshHandler : DelegatingHandler
{
    private readonly ITokenRefresher _tokenRefresher;
    private readonly IAccessTokenStorage _accessTokenStorage;
    private readonly IUserSessionTerminator _terminator;
    private readonly IRefreshTokenStorage _refreshTokenStorage;

    public TokenRefreshHandler(ITokenRefresher tokenRefresher, IAccessTokenStorage accessTokenStorage, IUserSessionTerminator terminator, IRefreshTokenStorage refreshTokenStorage)
    {
        _tokenRefresher = tokenRefresher;
        _accessTokenStorage = accessTokenStorage;
        _terminator = terminator;
        _refreshTokenStorage = refreshTokenStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // ─────────────────────────────────────────────────────────────
        // Guard 0: Both tokens missing → immediate termination
        // ─────────────────────────────────────────────────────────────
        var accessToken = _accessTokenStorage.GetAccessToken();
        var refreshToken = await _refreshTokenStorage.GetRefreshTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken) && string.IsNullOrWhiteSpace(refreshToken))
        {
            Console.WriteLine("[TokenRefreshHandler] Both tokens missing. Terminating session.");
            await _terminator.TerminateUserSessionAsync();
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        // ─────────────────────────────────────────────────────────────
        // Guard 1: Token updates are blocked (e.g. after logout)
        // ─────────────────────────────────────────────────────────────
        if (_accessTokenStorage.TokenUpdatesBlocked)
        {
            Console.WriteLine("[TokenRefreshHandler] Token updates are blocked. Skipping request.");
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        // ─────────────────────────────────────────────────────────────
        // Guard 2: Proactive token refresh (before sending request)
        // ─────────────────────────────────────────────────────────────
        accessToken = _accessTokenStorage.GetAccessToken(); // Re-fetch in case it was updated by another handler

        if (string.IsNullOrWhiteSpace(accessToken) || TokenHelper.IsExpired(accessToken))
        {
            Console.WriteLine("[TokenRefreshHandler] No token or expired token. Attempting refresh before sending request.");
            var isAccessTokenRefreshed = await _tokenRefresher.TryRefreshTokenAsync();

            if (!isAccessTokenRefreshed)
            {
                Console.WriteLine("[TokenRefreshHandler] Proactive refresh failed. Terminating session.");
                await _terminator.TerminateUserSessionAsync();
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            accessToken = _accessTokenStorage.GetAccessToken();
        }

        // ─────────────────────────────────────────────────────────────
        // Attach valid token (if available) to the outgoing request
        // ─────────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // ─────────────────────────────────────────────────────────────
        // Send original request
        // ─────────────────────────────────────────────────────────────
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // ─────────────────────────────────────────────────────────────
        // Handle 401 Unauthorized → reactive refresh and retry
        // ─────────────────────────────────────────────────────────────
        var isAccessTokenRefreshedAfter401 = await _tokenRefresher.TryRefreshTokenAsync();
        if (!isAccessTokenRefreshedAfter401)
        {
            Console.WriteLine("[TokenRefreshHandler] Refresh failed — terminating session.");
            await _terminator.TerminateUserSessionAsync();
            return response;
        }

        var newAccessToken = _accessTokenStorage.GetAccessToken();
        if (string.IsNullOrWhiteSpace(newAccessToken))
            return response;

        // Note: We clone the request here because HttpRequestMessage can only be sent once.
        var clonedRequest = await CloneHttpRequestAsync(request);
        clonedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
        Console.WriteLine($"[TokenRefreshHandler] Retrying request to {request.RequestUri} with new token.");

        // ─────────────────────────────────────────────────────────────
        // Retry request with new access token
        // ─────────────────────────────────────────────────────────────
        return await base.SendAsync(clonedRequest, cancellationToken);
    }

    /// <summary>
    /// Creates a deep clone of the given <see cref="HttpRequestMessage"/> to allow safe reuse.
    /// </summary>
    /// <param name="request">The original <see cref="HttpRequestMessage"/> to clone.</param>
    /// <returns>
    /// A new <see cref="HttpRequestMessage"/> instance with copied method, URI, content, headers, version, and options.
    /// </returns>
    /// <remarks>
    /// This method is used when retrying a request (e.g., after refreshing an expired token),
    /// since an <see cref="HttpRequestMessage"/> can only be sent once.
    /// </remarks>
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
}
