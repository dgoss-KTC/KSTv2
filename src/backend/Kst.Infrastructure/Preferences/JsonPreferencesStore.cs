using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Kst.Application.Preferences;
using Kst.Domain.Preferences;
using Kst.Infrastructure.Configuration;

namespace Kst.Infrastructure.Preferences;

/// <summary>
/// Persists user preferences as JSON under the KST local application data directory, separate
/// from workspace configuration.
/// </summary>
public sealed class JsonPreferencesStore : IPreferencesStore
{
    private readonly LocalAppDataPaths _paths;
    private readonly ILogger<JsonPreferencesStore> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public JsonPreferencesStore(LocalAppDataPaths paths, ILogger<JsonPreferencesStore> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public async Task<PreferencesLoadResult> LoadAsync()
    {
        var path = _paths.PreferencesFilePath;

        if (!File.Exists(path))
            return new PreferencesLoadResult(UserPreferences.Default, null);

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var preferences = JsonSerializer.Deserialize<UserPreferences>(json, JsonOptions)
                ?? UserPreferences.Default;
            return new PreferencesLoadResult(preferences, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to read preferences from {Path}. Renaming corrupt file and reverting to defaults.",
                path);

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var invalidPath = Path.Combine(
                Path.GetDirectoryName(path)!,
                $"preferences.{timestamp}.invalid.json");

            try
            {
                File.Move(path, invalidPath);
                _logger.LogWarning("Corrupt preferences file renamed to {InvalidPath}.", invalidPath);
            }
            catch (Exception renameEx)
            {
                _logger.LogError(renameEx, "Could not rename corrupt preferences file {Path}.", path);
            }

            return new PreferencesLoadResult(
                UserPreferences.Default,
                "Preferences could not be read and were reset to defaults.");
        }
    }

    public async Task SaveAsync(UserPreferences preferences)
    {
        _paths.EnsureDirectoriesExist();
        var path = _paths.PreferencesFilePath;
        var tempPath = path + ".tmp";

        var json = JsonSerializer.Serialize(preferences, JsonOptions);
        await File.WriteAllTextAsync(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }
}
