using DotNetEnv;
using Eventrian.Api.Data;
using Eventrian.Api.Features.Auth.Interfaces;
using Eventrian.Api.Features.Auth.Models;
using Eventrian.Api.Features.Auth.Repository;
using Eventrian.Api.Features.Auth.Services;
using Eventrian.Api.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// Configure Services
// -----------------------------

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings (relaxed policy for development / testing)
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 1;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
});

// Authentication JWT 
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero, // strict expiry (default is + 5 minutes)
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.")))
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"[JWT] Auth failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("[JWT] Token validated.");
            return Task.CompletedTask;
        }
    };
});

// Configuration binding
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings")
);

// Register application services
builder.Services.AddScoped<IdentitySeeder>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccessTokenService, AccessTokenService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
});

// CORS
var allowedOriginsRaw = builder.Configuration["Cors:AllowedOrigins"];
var allowedOrigins = allowedOriginsRaw?.Split(",") ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
    policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// -----------------------------
// Build the application
// -----------------------------

var app = builder.Build();

// -----------------------------
// Run startup tasks
// -----------------------------

using (var scope = app.Services.CreateScope())
{
    // Seed initial data
    var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
    await seeder.SeedAsync();

    // TODO: Move refresh token cleanup to a scheduled background task or cron job in production
    var tokenService = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>() as RefreshTokenService;
    if (tokenService != null)
    {

        await tokenService.RunStartupCleanupAsync();
        await tokenService.RunDevTokenCapCleanupAsync();
    }
}

// -----------------------------
// Configure Middleware Pipeline
// -----------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global exception handler to catch unhandled exceptions and return JSON response
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var errorFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var exception = errorFeature?.Error;

        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        if (exception != null)
            logger.LogError(exception, "Unhandled exception");

        var response = new { error = "An unexpected error occurred." };
        await context.Response.WriteAsJsonAsync(response);
    });
});

// Security
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Content Security Policy for Blazor WebAssembly + SignalR compatibility
app.Use(async (context, next) =>
{
    var csp = string.Join(" ",
        "default-src 'self';",                // Only allow resources from same origin
        "script-src 'self' 'unsafe-inline';", // Required by Blazor WebAssembly
        "style-src 'self' 'unsafe-inline';",  // Required by Blazor WebAssembly
        "connect-src 'self' wss: https:;",    // Allow SignalR / API over HTTPS/WSS
        "object-src 'none';",                 // Disallow plugin resources
        "frame-ancestors 'none';"             // Disallow iframe embedding
    );

    context.Response.Headers["Content-Security-Policy"] = csp;

    await next();
});

// Routing
app.MapControllers();

app.Run();

// Required by WebApplicationFactory<Program> in integration tests
public partial class Program { }
