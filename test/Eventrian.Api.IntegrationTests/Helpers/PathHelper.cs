namespace Eventrian.Api.IntegrationTests.Helpers;

public class PathHelper
{
    /// <summary>
    /// Helper method to find the solution root directory
    /// </summary>
    /// <param name="startDir"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static string FindSolutionRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);

        while (dir != null && !dir.GetFiles("*.sln").Any())
            dir = dir.Parent;

        return dir?.FullName ?? throw new InvalidOperationException("Solution root not found");
    }
}
