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

    private static CreateWorkspaceCommand Command(
        string? displayName = null,
        string? site = "NW",
        string? productLineFrom = null,
        string? productLineTo = null,
        IReadOnlyList<string>? parentParts = null,
        bool isTemporary = false,
        DateOnly? coverageEndsOn = null) =>
        new(displayName, site, productLineFrom, productLineTo, parentParts, isTemporary, coverageEndsOn);

    // --- Duplicate scope validation ---

    [Fact]
    public async Task Create_Duplicate_ProductLine_Only_Scope_Among_Enabled_Workspaces_Fails()
    {
        var svc = BuildService();
        await svc.CreateWorkspaceAsync(Command(site: "NW", productLineFrom: "2380"));

        var result = await svc.CreateWorkspaceAsync(Command(site: "nw", productLineFrom: "2380"));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "scope");
    }

    [Fact]
    public async Task Create_Duplicate_ParentParts_Only_Scope_Fails()
    {
        var svc = BuildService();
        await svc.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100", "ABC200"]));

        var result = await svc.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC200", "ABC100"]));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "scope");
    }

    [Fact]
    public async Task Create_Duplicate_ProductLine_And_ParentParts_Scope_Regardless_Of_Input_Order_Fails()
    {
        var svc = BuildService();
        await svc.CreateWorkspaceAsync(Command(site: "NW", productLineFrom: "2380", parentParts: ["ABC100", "ABC200"]));

        var result = await svc.CreateWorkspaceAsync(Command(site: "NW", productLineFrom: "2380", parentParts: ["ABC200", "ABC100"]));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "scope");
    }

    [Fact]
    public async Task ProductLine_Only_And_Narrowed_ProductLine_Are_Distinct_Scopes()
    {
        var svc = BuildService();
        await svc.CreateWorkspaceAsync(Command(site: "NW", productLineFrom: "2380"));

        var result = await svc.CreateWorkspaceAsync(Command(site: "NW", productLineFrom: "2380", parentParts: ["ABC100"]));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Different_ParentPart_Subsets_Are_Distinct_Scopes()
    {
        var svc = BuildService();
        await svc.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100", "ABC200"]));

        var result = await svc.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100"]));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Create_Same_Scope_As_Archived_Workspace_Succeeds()
    {
        var svc = BuildService();
        var first = await svc.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100"]));
        await svc.ArchiveWorkspaceAsync(first.Workspace!.AssignmentId);

        var result = await svc.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100"]));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Update_Workspace_To_Its_Own_Scope_Succeeds()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(Command(displayName: "Original", site: "NW", parentParts: ["ABC100"]));

        var result = await svc.UpdateWorkspaceAsync(created.Workspace!.AssignmentId,
            Command(displayName: "Renamed", site: "NW", parentParts: ["ABC100"]));

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed", result.Workspace!.DisplayName);
    }

    [Fact]
    public async Task Update_Workspace_To_Another_Enabled_Workspaces_Scope_Fails()
    {
        var svc = BuildService();
        await svc.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100"]));
        var second = await svc.CreateWorkspaceAsync(Command(site: "SW", parentParts: ["ABC200"]));

        var result = await svc.UpdateWorkspaceAsync(second.Workspace!.AssignmentId,
            Command(site: "NW", parentParts: ["ABC100"]));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "scope");
    }

    // --- Reorder ---

    [Fact]
    public async Task Reorder_Persists_New_SortOrder_For_Enabled_Workspaces()
    {
        var svc = BuildService();
        var a = await svc.CreateWorkspaceAsync(Command(site: "AA", parentParts: ["ABC100"]));
        var b = await svc.CreateWorkspaceAsync(Command(site: "BB", parentParts: ["ABC200"]));
        var c = await svc.CreateWorkspaceAsync(Command(site: "CC", parentParts: ["ABC300"]));

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
    public async Task Reorder_Preserves_ParentParts()
    {
        var svc = BuildService();
        var a = await svc.CreateWorkspaceAsync(Command(site: "AA", parentParts: ["ABC100", "ABC200"]));
        var b = await svc.CreateWorkspaceAsync(Command(site: "BB", parentParts: ["XYZ900"]));

        var result = await svc.ReorderWorkspacesAsync(new ReorderWorkspacesCommand([
            b.Workspace!.AssignmentId, a.Workspace!.AssignmentId
        ]));

        Assert.True(result.IsSuccess);
        var reorderedA = result.Workspaces!.Single(w => w.AssignmentId == a.Workspace!.AssignmentId);
        var reorderedB = result.Workspaces!.Single(w => w.AssignmentId == b.Workspace!.AssignmentId);
        Assert.Equal(["ABC100", "ABC200"], reorderedA.ParentParts);
        Assert.Equal(["XYZ900"], reorderedB.ParentParts);
    }

    [Fact]
    public async Task Reorder_With_Duplicate_Ids_Fails()
    {
        var svc = BuildService();
        var a = await svc.CreateWorkspaceAsync(Command(site: "AA", parentParts: ["ABC100"]));
        var b = await svc.CreateWorkspaceAsync(Command(site: "BB", parentParts: ["ABC200"]));

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
        var a = await svc.CreateWorkspaceAsync(Command(site: "AA", parentParts: ["ABC100"]));
        await svc.CreateWorkspaceAsync(Command(site: "BB", parentParts: ["ABC200"]));

        var result = await svc.ReorderWorkspacesAsync(new ReorderWorkspacesCommand([a.Workspace!.AssignmentId]));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "assignmentIds");
    }

    [Fact]
    public async Task Reorder_With_Unknown_Id_Fails()
    {
        var svc = BuildService();
        var a = await svc.CreateWorkspaceAsync(Command(site: "AA", parentParts: ["ABC100"]));

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
        var a = await svc.CreateWorkspaceAsync(Command(site: "AA", parentParts: ["ABC100"]));
        var b = await svc.CreateWorkspaceAsync(Command(site: "BB", parentParts: ["ABC200"]));
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
        var a = await svc.CreateWorkspaceAsync(Command(site: "AA", parentParts: ["ABC100"]));
        var b = await svc.CreateWorkspaceAsync(Command(site: "BB", parentParts: ["ABC200"]));
        var c = await svc.CreateWorkspaceAsync(Command(site: "CC", parentParts: ["ABC300"]));

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
        var a = await svc.CreateWorkspaceAsync(Command(site: "AA", parentParts: ["ABC100"]));
        var b = await svc.CreateWorkspaceAsync(Command(site: "BB", parentParts: ["ABC200"]));

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
