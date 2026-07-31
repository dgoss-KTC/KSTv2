using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Kst.Application.Workspaces;
using Kst.Domain.Workspaces;
using Kst.Infrastructure.Configuration;

namespace Kst.Infrastructure.Workspaces;

/// <summary>
/// Persists workspace configuration as JSON under the KST local application data directory.
/// </summary>
public sealed class JsonWorkspaceConfigurationStore : IWorkspaceConfigurationStore
{
    private readonly LocalAppDataPaths _paths;
    private readonly ILogger<JsonWorkspaceConfigurationStore> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public JsonWorkspaceConfigurationStore(
        LocalAppDataPaths paths,
        ILogger<JsonWorkspaceConfigurationStore> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public async Task<WorkspaceLoadResult> LoadAsync()
    {
        var path = _paths.WorkspacesFilePath;

        if (!File.Exists(path))
            return new WorkspaceLoadResult([], null);

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var workspaces = JsonSerializer.Deserialize<List<WorkspaceAssignment>>(json, JsonOptions)
                ?? [];
            return new WorkspaceLoadResult(workspaces, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to read workspace configuration from {Path}. Renaming corrupt file and starting with empty list.",
                path);

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var invalidPath = Path.Combine(
                Path.GetDirectoryName(path)!,
                $"workspaces.{timestamp}.invalid.json");

            try
            {
                File.Move(path, invalidPath);
                _logger.LogWarning("Corrupt workspace file renamed to {InvalidPath}.", invalidPath);
            }
            catch (Exception renameEx)
            {
                _logger.LogError(renameEx, "Could not rename corrupt workspace file {Path}.", path);
            }

            return new WorkspaceLoadResult(
                [],
                "Workspace configuration could not be read and was reset. Your previous workspaces have been cleared.");
        }
    }

    public async Task SaveAsync(IReadOnlyList<WorkspaceAssignment> workspaces)
    {
        _paths.EnsureDirectoriesExist();
        var path = _paths.WorkspacesFilePath;
        var tempPath = path + ".tmp";

        var json = JsonSerializer.Serialize(workspaces, JsonOptions);
        await File.WriteAllTextAsync(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }
}
