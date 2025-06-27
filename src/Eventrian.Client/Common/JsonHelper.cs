using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

public static class JsonHelper
{
    /// <summary>
    /// Attempts to deserialize JSON content to the specified type <typeparamref name="T"/>.
    /// Returns <c>default</c> if deserialization fails.
    /// </summary>
    /// <param name="content">The HTTP content to read.</param>
    /// <typeparam name="T">The target type to deserialize.</typeparam>
    /// <param name="caller">The name of the calling member (automatically populated).</param>
    /// <returns>A deserialized object or <c>default</c> on failure.</returns>
    public static async Task<T?> TryReadJsonAsync<T>(HttpContent content, [CallerMemberName] string? caller = null)
    {
        try
        {
            return await content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JsonHelper] Failed to parse JSON for {typeof(T).Name}. Caller: {caller}. Exception: {ex.Message}");
            return default;
        }
    }
}
