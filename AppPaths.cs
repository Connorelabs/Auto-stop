namespace CounterStrafe;

internal static class AppPaths
{
    public const string ConfigDirectoryName = "config";

    public static string GetPreferredConfigDirectory()
    {
        var currentConfigDirectory = Path.Combine(Environment.CurrentDirectory, ConfigDirectoryName);
        if (Directory.Exists(currentConfigDirectory))
        {
            return currentConfigDirectory;
        }

        return Path.Combine(AppContext.BaseDirectory, ConfigDirectoryName);
    }

    public static IEnumerable<string> EnumerateCandidateConfigDirectories()
    {
        yield return Path.Combine(Environment.CurrentDirectory, ConfigDirectoryName);

        var currentDirectory = Path.GetFullPath(Environment.CurrentDirectory);
        var baseDirectory = Path.GetFullPath(AppContext.BaseDirectory);
        if (!string.Equals(currentDirectory, baseDirectory, StringComparison.OrdinalIgnoreCase))
        {
            yield return Path.Combine(baseDirectory, ConfigDirectoryName);
        }
    }

    public static string EnsurePreferredConfigDirectory()
    {
        var directory = GetPreferredConfigDirectory();
        Directory.CreateDirectory(directory);
        return directory;
    }
}
