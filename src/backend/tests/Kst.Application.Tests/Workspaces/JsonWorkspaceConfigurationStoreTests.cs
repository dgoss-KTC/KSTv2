using System.Text.Json;
using Kst.Application.Workspaces;
using Kst.Domain.Workspaces;
using Kst.Infrastructure.Configuration;
using Kst.Infrastructure.Workspaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Application.Tests.Workspaces;

public sealed class JsonWorkspaceConfigurationStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LocalAppDataPaths _paths;
    private readonly JsonWorkspaceConfigurationStore _store;

    public JsonWorkspaceConfigurationStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"kst-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _paths = new LocalAppDataPaths(_tempDir);
        _store = new JsonWorkspaceConfigurationStore(_paths, NullLogger<JsonWorkspaceConfigurationStore>.Instance);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public async Task Load_Returns_Empty_When_File_Missing()
    {
        var result = await _store.LoadAsync();
        Assert.Empty(result.Workspaces);
        Assert.Null(result.ConfigurationWarning);
    }

    [Fact]
    public async Task Save_And_Reload_Persists_Workspaces()
    {
        var workspace = new WorkspaceAssignment(
            Guid.NewGuid(), "NW Test", "NW",
            null, null, ["ABC100"], false, null, true, 0);

        await _store.SaveAsync([workspace]);
        var result = await _store.LoadAsync();

        Assert.Single(result.Workspaces);
        Assert.Equal(workspace.AssignmentId, result.Workspaces[0].AssignmentId);
        Assert.Equal("NW", result.Workspaces[0].Site);
        Assert.Equal(["ABC100"], result.Workspaces[0].ParentParts);
        Assert.Null(result.ConfigurationWarning);
    }

    [Fact]
    public async Task Save_Writes_CamelCase_Json()
    {
        var workspace = new WorkspaceAssignment(
            Guid.NewGuid(), "Test", "NW",
            null, null, ["ABC100"], false, null, true, 0);

        await _store.SaveAsync([workspace]);

        var json = await File.ReadAllTextAsync(_paths.WorkspacesFilePath);
        var doc = JsonDocument.Parse(json);
        var first = doc.RootElement.EnumerateArray().First();

        Assert.True(first.TryGetProperty("assignmentId", out _));
        Assert.True(first.TryGetProperty("displayName", out _));
        Assert.True(first.TryGetProperty("site", out _));
        Assert.True(first.TryGetProperty("parentParts", out _));
        Assert.True(first.TryGetProperty("isEnabled", out _));
        Assert.True(first.TryGetProperty("sortOrder", out _));
        Assert.False(first.TryGetProperty("customerNumber", out _));
    }

    [Fact]
    public async Task Corrupt_File_Returns_Empty_List_With_Warning()
    {
        _paths.EnsureDirectoriesExist();
        await File.WriteAllTextAsync(_paths.WorkspacesFilePath, "{ NOT VALID JSON !!!");

        var result = await _store.LoadAsync();

        Assert.Empty(result.Workspaces);
        Assert.NotNull(result.ConfigurationWarning);
        Assert.False(File.Exists(_paths.WorkspacesFilePath));
    }

    [Fact]
    public async Task Corrupt_File_Is_Renamed_With_Invalid_Suffix()
    {
        _paths.EnsureDirectoriesExist();
        await File.WriteAllTextAsync(_paths.WorkspacesFilePath, "NOT JSON");

        await _store.LoadAsync();

        var invalidFiles = Directory.GetFiles(_paths.ConfigDirectory, "workspaces.*.invalid.json");
        Assert.Single(invalidFiles);
    }

    [Fact]
    public async Task Save_Is_Idempotent_For_Multiple_Calls()
    {
        var w1 = new WorkspaceAssignment(Guid.NewGuid(), "A", "NW", null, null, ["ABC100"], false, null, true, 0);
        var w2 = new WorkspaceAssignment(Guid.NewGuid(), "B", "SW", null, null, ["ABC200"], false, null, true, 1);

        await _store.SaveAsync([w1]);
        await _store.SaveAsync([w1, w2]);

        var result = await _store.LoadAsync();
        Assert.Equal(2, result.Workspaces.Count);
    }

    // --- Backward compatibility ---

    [Fact]
    public async Task Legacy_File_With_CustomerNumber_Loads_Successfully()
    {
        _paths.EnsureDirectoriesExist();
        var id = Guid.NewGuid();
        var legacyJson = $$"""
        [
          {
            "assignmentId": "{{id}}",
            "displayName": "Customer 12345678",
            "site": "NW",
            "customerNumber": "12345678",
            "productLineFrom": null,
            "productLineTo": null,
            "isTemporary": false,
            "coverageEndsOn": null,
            "isEnabled": true,
            "sortOrder": 0
          }
        ]
        """;
        await File.WriteAllTextAsync(_paths.WorkspacesFilePath, legacyJson);

        var result = await _store.LoadAsync();

        Assert.Null(result.ConfigurationWarning);
        Assert.Single(result.Workspaces);
        Assert.Equal(id, result.Workspaces[0].AssignmentId);
        Assert.Equal("NW", result.Workspaces[0].Site);
        Assert.Empty(result.Workspaces[0].ParentParts);
    }

    [Fact]
    public async Task Legacy_File_Without_ParentParts_Loads_With_Empty_Collection()
    {
        _paths.EnsureDirectoriesExist();
        var id = Guid.NewGuid();
        var legacyJson = $$"""
        [
          {
            "assignmentId": "{{id}}",
            "displayName": "PL 0040",
            "site": "SW",
            "productLineFrom": "0040",
            "productLineTo": "0040",
            "isTemporary": false,
            "coverageEndsOn": null,
            "isEnabled": true,
            "sortOrder": 0
          }
        ]
        """;
        await File.WriteAllTextAsync(_paths.WorkspacesFilePath, legacyJson);

        var result = await _store.LoadAsync();

        Assert.Single(result.Workspaces);
        Assert.NotNull(result.Workspaces[0].ParentParts);
        Assert.Empty(result.Workspaces[0].ParentParts);
    }

    // --- Service operations persist across a simulated app restart (new store instance, same directory) ---

    private WorkspaceConfigurationService BuildService() =>
        new(_store, NullLogger<WorkspaceConfigurationService>.Instance);

    private WorkspaceConfigurationService BuildReloadedService()
    {
        var reloadedStore = new JsonWorkspaceConfigurationStore(_paths, NullLogger<JsonWorkspaceConfigurationStore>.Instance);
        return new WorkspaceConfigurationService(reloadedStore, NullLogger<WorkspaceConfigurationService>.Instance);
    }

    private static CreateWorkspaceCommand Command(
        string? displayName = null,
        string? site = "NW",
        string? productLineFrom = null,
        string? productLineTo = null,
        IReadOnlyList<string>? parentParts = null,
        bool isTemporary = false,
        DateOnly? coverageEndsOn = null) =>
        new(displayName, site, productLineFrom, productLineTo, parentParts, isTemporary, coverageEndsOn);

    [Fact]
    public async Task Archived_Workspace_Persists_Across_Reload()
    {
        var service = BuildService();
        var created = await service.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100"]));
        await service.ArchiveWorkspaceAsync(created.Workspace!.AssignmentId);

        var reloaded = await BuildReloadedService().GetWorkspacesAsync();

        Assert.Single(reloaded.Workspaces);
        Assert.False(reloaded.Workspaces[0].IsEnabled);
        Assert.Equal(["ABC100"], reloaded.Workspaces[0].ParentParts);
    }

    [Fact]
    public async Task Restored_Workspace_Persists_Across_Reload()
    {
        var service = BuildService();
        var created = await service.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100"]));
        await service.ArchiveWorkspaceAsync(created.Workspace!.AssignmentId);
        await service.RestoreWorkspaceAsync(created.Workspace!.AssignmentId);

        var reloaded = await BuildReloadedService().GetWorkspacesAsync();

        Assert.Single(reloaded.Workspaces);
        Assert.True(reloaded.Workspaces[0].IsEnabled);
    }

    [Fact]
    public async Task Delete_Persists_Across_Reload()
    {
        var service = BuildService();
        var created = await service.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100"]));
        await service.DeleteWorkspaceAsync(created.Workspace!.AssignmentId);

        var reloaded = await BuildReloadedService().GetWorkspacesAsync();

        Assert.Empty(reloaded.Workspaces);
    }

    [Fact]
    public async Task Reset_Persists_Across_Reload()
    {
        var service = BuildService();
        await service.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100"]));
        await service.CreateWorkspaceAsync(Command(site: "SW", parentParts: ["ABC200"]));
        await service.ResetWorkspacesAsync();

        var reloaded = await BuildReloadedService().GetWorkspacesAsync();

        Assert.Empty(reloaded.Workspaces);
    }
}
