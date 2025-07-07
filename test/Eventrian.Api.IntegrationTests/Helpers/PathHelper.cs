namespace Eventrian.Api.IntegrationTests.Helpers;

/// <summary>
/// Helper for locating the solution root directory by traversing parent folders.
/// </summary>
public class PathHelper
{
    /// <summary>
    /// Finds the solution root directory by walking upward until a *.sln file is found.
    /// </summary>
    /// <param name="startDir">Starting path, usually AppContext.BaseDirectory</param>
    /// <returns>Full path to the solution root</returns>
    /// <exception cref="InvalidOperationException">Thrown if no .sln file is found</exception>
    public static string FindSolutionRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);

        while (dir != null && !dir.GetFiles("*.sln").Any())
            dir = dir.Parent;

        return dir?.FullName ?? throw new InvalidOperationException("Solution root not found");
    }
}
