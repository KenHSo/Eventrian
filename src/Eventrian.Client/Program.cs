using Eventrian.Client;
using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Client.Features.Auth.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// Root Components
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ---------------------
// HTTP Clients
// ---------------------

var apiBaseUrl = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5000/");

// Unauthenticated HttpClient (manual use in AuthService and TokenRefresher)
builder.Services.AddHttpClient("UnprotectedApi", client => client.BaseAddress = apiBaseUrl);

// Authorized HttpClient with TokenRefreshHandler
builder.Services.AddHttpClient("Authorized", client => { client.BaseAddress = apiBaseUrl;})
    .AddHttpMessageHandler<TokenRefreshHandler>();

// Make "Authorized" the default HttpClient for injection
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("Authorized");
});

// ---------------------
// Auth Services
// ---------------------

// Token Storage
builder.Services.AddScoped<IRefreshTokenStorage, RefreshTokenStorage>();
builder.Services.AddSingleton<IAccessTokenStorage, AccessTokenStorage>();

// Token Logic
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenRefresher, TokenRefresher>();
builder.Services.AddScoped<IUserSessionTerminator, UserSessionTerminator>();

// Http Handler
builder.Services.AddTransient<TokenRefreshHandler>();

// Auth State (enables <AuthorizeView> & [Authorize])
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<ICustomAuthStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddAuthorizationCore();

// ---------------------
// Start
// ---------------------

var host = builder.Build();

var refresher = host.Services.GetRequiredService<ITokenRefresher>();
await refresher.InitializeAsync();

await host.RunAsync();