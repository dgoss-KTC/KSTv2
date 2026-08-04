using Kst.Application.Workspaces;
using Kst.Domain.Workspaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Application.Tests.Workspaces;

/// <summary>
/// Covers duplicate-scope rejection and active-workspace reordering behavior.
/// </summary>
public sealed class WorkspaceReorderAndDuplicateTests
{
    private static WorkspaceConfigurationService BuildService(
        IReadOnlyList<WorkspaceAssignment>? seed = null)
    {
        var store = new InMemoryTestWorkspaceStore(seed ?? []);
        return new WorkspaceConfigurationService(store, NullLogger<WorkspaceConfigurationService>.Instance);
    }

    // --- Duplicate scope validation ---

    [Fact]
    public async Task Create_Duplicate_Scope_Among_Enabled_Workspaces_Fails()
    {
        var svc = BuildService();
        await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));

        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "nw", "12345678", null, null, false, null));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "scope");
    }

    [Fact]
    public async Task Create_Same_Scope_As_Archived_Workspace_Succeeds()
    {
        var svc = BuildService();
        var first = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));
        await svc.ArchiveWorkspaceAsync(first.Workspace!.AssignmentId);

        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Update_Workspace_To_Its_Own_Scope_Succeeds()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            "Original", "NW", "12345678", null, null, false, null));

        var result = await svc.UpdateWorkspaceAsync(created.Workspace!.AssignmentId, new CreateWorkspaceCommand(
            "Renamed", "NW", "12345678", null, null, false, null));

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed", result.Workspace!.DisplayName);
    }

    [Fact]
    public async Task Update_Workspace_To_Another_Enabled_Workspaces_Scope_Fails()
    {
        var svc = BuildService();
        await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));
        var second = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "SW", "87654321", null, null, false, null));

        var result = await svc.UpdateWorkspaceAsync(second.Workspace!.AssignmentId, new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "scope");
    }

    // --- Reorder ---

    [Fact]
    public async Task Reorder_Persists_New_SortOrder_For_Enabled_Workspaces()
    {
        var svc = BuildService();
        var a = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(null, "AA", "11111111", null, null, false, null));
        var b = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(null, "BB", "22222222", null, null, false, null));
        var c = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(null, "CC", "33333333", null, null, false, null));

        var result = await svc.ReorderWorkspacesAsync(new ReorderWorkspacesCommand([
            c.Workspace!.AssignmentId, a.Workspace!.AssignmentId, b.Workspace!.AssignmentId
        ]));

        Assert.True(result.IsSuccess);
        var ordered = result.Workspaces!.Where(w => w.IsEnabled).OrderBy(w => w.SortOrder).ToList();
        Assert.Equal(c.Workspace!.AssignmentId, ordered[0].AssignmentId);
        Assert.Equal(a.Workspace!.AssignmentId, ordered[1].AssignmentId);
        Assert.Equal(b.Workspace!.AssignmentId, ordered[2].AssignmentId);
        Assert.Equal(0, ordered[0].SortOrder);
        Assert.Equal(1, ordered[1].SortOrder);
        Assert.Equal(2, ordered[2].SortOrder);
    }

    [Fact]
    public async Task Reorder_With_Duplicate_Ids_Fails()
    {
        var svc = BuildService();
        var a = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(null, "AA", "11111111", null, null, false, null));
        var b = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(null, "BB", "22222222", null, null, false, null));

        var result = await svc.ReorderWorkspacesAsync(new ReorderWorkspacesCommand([
            a.Workspace!.AssignmentId, a.Workspace!.AssignmentId
        ]));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "assignmentIds");
        // Ensure nothing was persisted / order unaffected.
        var reloaded = await svc.GetWorkspacesAsync();
        Assert.Equal(0, reloaded.Workspaces.First(w => w.AssignmentId == a.Workspace!.AssignmentId).SortOrder);
        Assert.Equal(1, reloaded.Workspaces.First(w => w.AssignmentId == b.Workspace!.AssignmentId).SortOrder);
    }

    [Fact]
    public async Task Reorder_Missing_An_Enabled_Id_Fails()
    {
        var svc = BuildService();
        var a = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(null, "AA", "11111111", null, null, false, null));
        await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(null, "BB", "22222222", null, null, false, null));

        var result = await svc.ReorderWorkspacesAsync(new ReorderWorkspacesCommand([a.Workspace!.AssignmentId]));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "assignmentIds");
    }

    [Fact]
    public async Task Reorder_With_Unknown_Id_Fails()
    {
        var svc = BuildService();
        var a = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(null, "AA", "11111111", null, null, false, null));

        var result = await svc.ReorderWorkspacesAsync(new ReorderWorkspacesCommand([
            a.Workspace!.AssignmentId, Guid.NewGuid()
        ]));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "assignmentIds");
    }

    [Fact]
    public async Task Reorder_With_Archived_Id_Included_Fails()
    {
        var svc = BuildService();
        var a = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(null, "AA", "11111111", null, null, false, null));
        var b = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(null, "BB", "22222222", null, null, false, null));
        await svc.ArchiveWorkspaceAsync(b.Workspace!.AssignmentId);

        var result = await svc.ReorderWorkspacesAsync(new ReorderWorkspacesCommand([
            a.Workspace!.AssignmentId, b.Workspace!.AssignmentId
        ]));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Reorder_Preserves_Archived_Workspaces_Relative_Order_After_Enabled()
    {
        var svc = BuildService();
        var a = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(null, "AA", "11111111", null, null, false, null));
        var b = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(null, "BB", "22222222", null, null, false, null));
        var c = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(null, "CC", "33333333", null, null, false, null));

        await svc.ArchiveWorkspaceAsync(a.Workspace!.AssignmentId);
        await svc.ArchiveWorkspaceAsync(c.Workspace!.AssignmentId);

        // Only B remains enabled.
        var result = await svc.ReorderWorkspacesAsync(new ReorderWorkspacesCommand([b.Workspace!.AssignmentId]));

        Assert.True(result.IsSuccess);
        var archived = result.Workspaces!.Where(w => !w.IsEnabled).OrderBy(w => w.SortOrder).ToList();
        Assert.Equal(2, archived.Count);
        // A was archived before C, so A must still precede C after reorder.
        Assert.Equal(a.Workspace!.AssignmentId, archived[0].AssignmentId);
        Assert.Equal(c.Workspace!.AssignmentId, archived[1].AssignmentId);
    }

    [Fact]
    public async Task Reorder_Persists_Across_Reload()
    {
        var store = new InMemoryTestWorkspaceStore([]);
        var svc = new WorkspaceConfigurationService(store, NullLogger<WorkspaceConfigurationService>.Instance);
        var a = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(null, "AA", "11111111", null, null, false, null));
        var b = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(null, "BB", "22222222", null, null, false, null));

        await svc.ReorderWorkspacesAsync(new ReorderWorkspacesCommand([
            b.Workspace!.AssignmentId, a.Workspace!.AssignmentId
        ]));

        var reloaded = new WorkspaceConfigurationService(store, NullLogger<WorkspaceConfigurationService>.Instance);
        var list = await reloaded.GetWorkspacesAsync();
        var ordered = list.Workspaces.OrderBy(w => w.SortOrder).ToList();

        Assert.Equal(b.Workspace!.AssignmentId, ordered[0].AssignmentId);
        Assert.Equal(a.Workspace!.AssignmentId, ordered[1].AssignmentId);
    }
}
