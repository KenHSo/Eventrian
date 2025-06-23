using Eventrian.Api.Data;
using Eventrian.Api.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Eventrian.Api.IntegrationTests.Setup;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Loads JWT settings from environment or .env file,
/// overrides configuration, and uses in-memory database.
/// </summary>
public class TestApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Resolve .env path from solution root (ensures reliability across test contexts)
        string root = PathHelper.FindSolutionRoot(AppContext.BaseDirectory);
        string envPath = Path.Combine(root, "src", "Eventrian.Api", ".env");

        // Load JWT settings from environment or fallback to .env
        var secretKey = JwtTestEnvHelper.GetJwtSetting("JwtSettings__SecretKey", envPath);
        var issuer = JwtTestEnvHelper.GetJwtSetting("JwtSettings__Issuer", envPath);
        var audience = JwtTestEnvHelper.GetJwtSetting("JwtSettings__Audience", envPath);

        // Inject settings into config (overrides appsettings)
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = secretKey,
                ["JwtSettings:Issuer"] = issuer,
                ["JwtSettings:Audience"] = audience
            };

            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Use in-memory DB
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("IntegrationTestsDb");
            });

            // Reset DB before tests
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Development"); // Enable detailed errors
    }
}


