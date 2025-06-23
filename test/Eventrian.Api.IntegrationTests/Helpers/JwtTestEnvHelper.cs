namespace Eventrian.Api.IntegrationTests.Helpers;

public static class JwtTestEnvHelper
{
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
