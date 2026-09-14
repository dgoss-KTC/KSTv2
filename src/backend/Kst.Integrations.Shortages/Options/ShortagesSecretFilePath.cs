namespace Kst.Integrations.Shortages.Options;

/// <summary>
/// Resolves the path supplied by the Tauri host for the bundled Shortages SQL-authentication file.
/// </summary>
public static class ShortagesSecretFilePath
{
    public const string EnvironmentVariableName = "KST_SECRETS_FILE";

    public static string? Resolve() => Resolve(Environment.GetEnvironmentVariable(EnvironmentVariableName));

    public static string? Resolve(string? filePath)
    {
        return string.IsNullOrWhiteSpace(filePath) ? null : filePath;
    }
}
