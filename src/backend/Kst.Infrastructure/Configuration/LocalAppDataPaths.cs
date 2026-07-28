namespace Kst.Infrastructure.Configuration;

/// <summary>
/// Resolves paths under the application's local data directory.
/// On Windows this is %LOCALAPPDATA%\KST\.
/// </summary>
public sealed class LocalAppDataPaths
{
    private readonly string _root;

    public LocalAppDataPaths(string? overrideRoot = null)
    {
        _root = overrideRoot
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KST");
    }

    public string Root => _root;
    public string LogsDirectory => Path.Combine(_root, "logs");
    public string ConfigDirectory => Path.Combine(_root, "config");
    public string DataDirectory => Path.Combine(_root, "data");

    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(DataDirectory);
    }
}
