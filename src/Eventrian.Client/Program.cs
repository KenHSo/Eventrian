using Eventrian.Client;
using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Client.Features.Auth.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient Setup

// NoAuth client (used manually by AuthService and TokenRefresher)
builder.Services.AddHttpClient("NoAuth", client =>
{
    client.BaseAddress = new Uri("https://localhost:5000/");
});

builder.Services.AddHttpClient("Authorized", client =>
{
    client.BaseAddress = new Uri("https://localhost:5000/");
}).AddHttpMessageHandler<TokenRefreshHandler>();

// Make "Authorized" the default HttpClient for injection
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("Authorized");
});

// Auth Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRefreshTokenStorage, RefreshTokenStorage>();
builder.Services.AddSingleton<IAccessTokenStorage, AccessTokenStorage>();
builder.Services.AddScoped<ITokenRefresher, TokenRefresher>();
builder.Services.AddScoped<IUserSessionTerminator, UserSessionTerminator>();
builder.Services.AddTransient<TokenRefreshHandler>();

// Auth State (enables <AuthorizeView> & [Authorize])
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<ICustomAuthStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddAuthorizationCore();

// --- Build + Start ---
var host = builder.Build();

var refresher = host.Services.GetRequiredService<ITokenRefresher>();
await refresher.InitializeAsync();

await host.RunAsync();