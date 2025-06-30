using Microsoft.JSInterop;
using Eventrian.Client.Features.Auth.Interfaces;

namespace Eventrian.Client.Features.Auth.Services;

public class RefreshTokenStorage : IRefreshTokenStorage
{
    private readonly IJSRuntime _js;

    private const string RefreshTokenKey = "refresh_token";
    private const string RefreshInProgressKey = "refresh_in_progress";
    private const string IsPersistentKey = "is_persistent"; // "true" or "false"

    public RefreshTokenStorage(IJSRuntime js)
    {
        _js = js;
    }

    public async Task SetRefreshTokenAsync(string token, bool rememberMe)
    {
        if (rememberMe)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, token);
            await _js.InvokeVoidAsync("localStorage.setItem", IsPersistentKey, "true");
            await _js.InvokeVoidAsync("sessionStorage.removeItem", RefreshTokenKey);
            await _js.InvokeVoidAsync("sessionStorage.removeItem", IsPersistentKey);
        }
        else
        {
            await _js.InvokeVoidAsync("sessionStorage.setItem", RefreshTokenKey, token);
            await _js.InvokeVoidAsync("sessionStorage.setItem", IsPersistentKey, "false");
            await _js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
            await _js.InvokeVoidAsync("localStorage.removeItem", IsPersistentKey);
        }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        var isPersistent =
            await _js.InvokeAsync<string>("localStorage.getItem", IsPersistentKey) ??
            await _js.InvokeAsync<string>("sessionStorage.getItem", IsPersistentKey);

        if (isPersistent == "true")
            return await TryGetToken("localStorage");

        if (isPersistent == "false")
            return await TryGetToken("sessionStorage");

        // Fallback: likely new tab, check both
        var fallback = await TryGetToken("localStorage");
        if (fallback is not null)
        {
            Console.WriteLine("[RefreshTokenStorage] Fallback to localStorage succeeded.");
            return fallback;
        }

        Console.WriteLine("[RefreshTokenStorage] No refresh token found.");
        return null;
    }

    public async Task RemoveRefreshTokenAsync()
    {
        var isPersistent =
            await _js.InvokeAsync<string>("localStorage.getItem", IsPersistentKey) ??
            await _js.InvokeAsync<string>("sessionStorage.getItem", IsPersistentKey);

        if (isPersistent == "true")
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
            await _js.InvokeVoidAsync("localStorage.removeItem", IsPersistentKey);
        }
        else if (isPersistent == "false")
        {
            await _js.InvokeVoidAsync("sessionStorage.removeItem", RefreshTokenKey);
            await _js.InvokeVoidAsync("sessionStorage.removeItem", IsPersistentKey);
        }

        // Just in case both exist (bad state), clear both
        await _js.InvokeVoidAsync("localStorage.removeItem", RefreshInProgressKey);
        await _js.InvokeVoidAsync("sessionStorage.removeItem", RefreshInProgressKey);
    }

    public async Task<bool> HasLocalStorageTokenAsync()
    {
        var token = await _js.InvokeAsync<string>("localStorage.getItem", RefreshTokenKey);
        return !string.IsNullOrWhiteSpace(token);
    }

    public async Task<bool> IsRefreshInProgressAsync()
    {
        var value = await _js.InvokeAsync<string>("sessionStorage.getItem", RefreshInProgressKey);
        return value == "true";
    }

    public async Task SetRefreshInProgressAsync(bool isInProgress)
    {
        if (isInProgress)
            await _js.InvokeVoidAsync("sessionStorage.setItem", RefreshInProgressKey, "true");
        else
            await _js.InvokeVoidAsync("sessionStorage.removeItem", RefreshInProgressKey);
    }

    // --- Helpers ---

    private async Task<string?> TryGetToken(string storage)
    {
        var token = await _js.InvokeAsync<string>($"{storage}.getItem", RefreshTokenKey);
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }
}
