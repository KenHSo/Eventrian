namespace Eventrian.Api.IntegrationTests.Helpers;

/// <summary>
/// Utility to fetch JWT settings from environment or fallback .env file.
/// </summary>
public static class JwtTestEnvHelper
{
    /// <summary>
    /// Resolves a single JWT setting by checking environment variables first,
    /// and falling back to a .env file if needed.
    /// </summary>
    /// <param name="key">Environment variable key</param>
    /// <param name="envPath">Full path to the .env file</param>
    /// <returns>The resolved value</returns>
    /// <exception cref="InvalidOperationException">If key is not found in either source</exception>
    public static string GetJwtSetting(string key, string envPath)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(value))
            return value;

        if (File.Exists(envPath))
            DotNetEnv.Env.Load(envPath);

        value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException($"Missing required JWT env variable: {key}");

        return value;
    }
}
