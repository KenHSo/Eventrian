using Eventrian.Client.Features.Auth.Interfaces;
using Microsoft.JSInterop;

public class AuthBroadcastService : IAuthBroadcastService, IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private DotNetObjectReference<AuthBroadcastService>? _dotNetRef;

    public event Action? OnLogoutBroadcasted;

    public AuthBroadcastService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitLogoutBroadcastAsync(string userId)
    {
        // Load JS module if not already loaded (once per lifetime)
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/authSync.js");

        // Create a .NET reference so JS can call back into this instance
        _dotNetRef ??= DotNetObjectReference.Create(this);

        // Store the current user ID in JS memory (used to match broadcast events)
        // Begin listening for logout events from other tabs
        await _module.InvokeVoidAsync("initLogoutSync", userId, _dotNetRef);

    }

    public async Task BroadcastLogoutAsync(string userId)
    {
        // Notify other tabs that this user has logged out
        if (_module is not null)
            await _module.InvokeVoidAsync("broadcastLogout", userId);
    }

    public async Task ClearUserAsync()
    {
        // Remove the current user ID from JS memory
        // Prevents this tab from reacting to logout events after logout
        if (_module is not null)
            await _module.InvokeVoidAsync("clearCurrentUserId");
    }

    [JSInvokable]
    public void OnBroadcastLogoutMatch()
    {
        // JS received a logout event for this user - trigger .NET logout
        Console.WriteLine("[Broadcast] Received logout match — terminating session");
        OnLogoutBroadcasted?.Invoke();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "No finalizer is defined.")]
    public async ValueTask DisposeAsync()
    {
        if (_dotNetRef is not null)
        {
            _dotNetRef.Dispose();
            _dotNetRef = null; // Optional, prevents double-dispose
        }

        if (_module is not null)
        {
            await _module.DisposeAsync();
            _module = null;
        }
    }
}
