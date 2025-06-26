using Eventrian.Client;
using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Client.Features.Auth.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri("https://localhost:5000/") });

// Auth Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRefreshTokenStorage, RefreshTokenStorage>();
builder.Services.AddScoped<IAccessTokenStorage, AccessTokenStorage>();
builder.Services.AddScoped<ITokenRefresher, TokenRefresher>();

// Auth State (enables <AuthorizeView> & [Authorize])
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<ICustomAuthStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddAuthorizationCore();

//await builder.Build().RunAsync();

var host = builder.Build();

var refresher = host.Services.GetRequiredService<ITokenRefresher>();
await refresher.InitializeAsync();

await host.RunAsync();