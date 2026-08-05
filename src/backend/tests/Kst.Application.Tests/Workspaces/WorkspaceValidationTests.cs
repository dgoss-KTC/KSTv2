using Kst.Application.Workspaces;
using Kst.Domain.Workspaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Application.Tests.Workspaces;

/// <summary>
/// Validates workspace creation rules via WorkspaceConfigurationService.
/// </summary>
public sealed class WorkspaceValidationTests
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

    // --- Valid cases ---

    [Fact]
    public async Task Valid_Site_And_SingleProductLine_Succeeds()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(site: "SW", productLineFrom: "0040"));
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Workspace);
    }

    [Fact]
    public async Task Valid_Site_And_ProductLineRange_Succeeds()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(site: "AR", productLineFrom: "0040", productLineTo: "0045"));
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Valid_Site_And_ExplicitParentParts_Succeeds()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(site: "VT", parentParts: ["ABC100"]));
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Valid_Site_ProductLine_And_ExplicitParentParts_Succeeds()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(
            site: "VT", productLineFrom: "0040", productLineTo: "0045", parentParts: ["ABC100"]));
        Assert.True(result.IsSuccess);
    }

    // --- Site normalization ---

    [Fact]
    public async Task Site_Is_Normalized_To_Uppercase()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(site: "nw", parentParts: ["ABC100"]));
        Assert.True(result.IsSuccess);
        Assert.Equal("NW", result.Workspace!.Site);
    }

    // --- Site validation ---

    [Fact]
    public async Task Invalid_Site_TooShort_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(site: "N", parentParts: ["ABC100"]));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "site");
    }

    [Fact]
    public async Task Invalid_Site_TooLong_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(site: "NWW", parentParts: ["ABC100"]));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "site");
    }

    [Fact]
    public async Task Invalid_Site_NonLetters_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(site: "1W", parentParts: ["ABC100"]));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "site");
    }

    [Fact]
    public async Task Missing_Site_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(site: null, parentParts: ["ABC100"]));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "site");
    }

    // --- ProductLine validation ---

    [Fact]
    public async Task ProductLineFrom_TooShort_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(productLineFrom: "004"));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "productLineFrom");
    }

    [Fact]
    public async Task ProductLineTo_Without_ProductLineFrom_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(productLineTo: "0045"));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "productLineTo");
    }

    [Fact]
    public async Task ProductLineTo_Below_ProductLineFrom_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(productLineFrom: "0045", productLineTo: "0040"));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "productLineTo");
    }

    // --- Scope requirement ---

    [Fact]
    public async Task Site_Only_Workspace_Is_Rejected()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command());
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "scope");
    }

    [Fact]
    public async Task Site_And_Only_Blank_Parent_Part_Rows_Is_Rejected()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(parentParts: ["   ", "", "  "]));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "scope");
    }

    // --- Single product-line normalization ---

    [Fact]
    public async Task ProductLineTo_Is_Set_To_ProductLineFrom_When_Blank()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(productLineFrom: "0040"));
        Assert.True(result.IsSuccess);
        Assert.Equal("0040", result.Workspace!.ProductLineFrom);
        Assert.Equal("0040", result.Workspace.ProductLineTo);
    }

    // --- Parent-part normalization ---

    [Fact]
    public async Task ParentParts_Are_Trimmed_And_Blanks_Removed()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(parentParts: ["  ABC100  ", "", "   "]));
        Assert.True(result.IsSuccess);
        Assert.Equal(["ABC100"], result.Workspace!.ParentParts);
    }

    [Fact]
    public async Task ParentParts_Duplicate_Identical_Entries_Are_Deduplicated()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(parentParts: ["ABC100", "ABC100", " ABC100 "]));
        Assert.True(result.IsSuccess);
        Assert.Equal(["ABC100"], result.Workspace!.ParentParts);
    }

    [Fact]
    public async Task ParentParts_Empty_Collection_Means_No_Narrowing()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(productLineFrom: "0040", parentParts: []));
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Workspace!.ParentParts);
    }

    // --- Display name derivation ---

    [Fact]
    public async Task DisplayName_Derived_From_SingleProductLine()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(productLineFrom: "0040"));
        Assert.Equal("PL 0040", result.Workspace!.DisplayName);
    }

    [Fact]
    public async Task DisplayName_Derived_From_ProductLineRange()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(productLineFrom: "0040", productLineTo: "0045"));
        Assert.Equal("PL 0040\u20130045", result.Workspace!.DisplayName);
    }

    [Fact]
    public async Task DisplayName_Derived_From_ParentParts_Only()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(parentParts: ["ABC100", "ABC200", "ABC300"]));
        Assert.Equal("3 parent parts", result.Workspace!.DisplayName);
    }

    [Fact]
    public async Task DisplayName_Derived_From_Single_ParentPart_Uses_Singular()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(parentParts: ["ABC100"]));
        Assert.Equal("1 parent part", result.Workspace!.DisplayName);
    }

    [Fact]
    public async Task DisplayName_Derived_From_ProductLine_And_ParentParts()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(productLineFrom: "2380", parentParts: ["A", "B", "C"]));
        Assert.Equal("PL 2380 \u00b7 3 parts", result.Workspace!.DisplayName);
    }

    [Fact]
    public async Task Explicit_DisplayName_Is_Preserved()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(displayName: "My Workspace", parentParts: ["ABC100"]));
        Assert.Equal("My Workspace", result.Workspace!.DisplayName);
    }

    // --- Sort order ---

    [Fact]
    public async Task First_Workspace_Gets_SortOrder_Zero()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(Command(parentParts: ["ABC100"]));
        Assert.Equal(0, result.Workspace!.SortOrder);
    }

    [Fact]
    public async Task Second_Workspace_Gets_Next_SortOrder()
    {
        var svc = BuildService();
        await svc.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100"]));
        var result = await svc.CreateWorkspaceAsync(Command(site: "SW", parentParts: ["ABC200"]));
        Assert.Equal(1, result.Workspace!.SortOrder);
    }

    // --- Update ---

    [Fact]
    public async Task Update_Changes_Workspace_Fields()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100"]));

        var result = await svc.UpdateWorkspaceAsync(
            created.Workspace!.AssignmentId,
            Command(site: "SW", parentParts: ["ABC200"]));

        Assert.True(result.IsSuccess);
        Assert.Equal("SW", result.Workspace!.Site);
        Assert.Equal(["ABC200"], result.Workspace.ParentParts);
    }

    [Fact]
    public async Task Update_Preserves_AssignmentId()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100"]));

        var result = await svc.UpdateWorkspaceAsync(
            created.Workspace!.AssignmentId,
            Command(site: "SW", parentParts: ["ABC200"]));

        Assert.Equal(created.Workspace.AssignmentId, result.Workspace!.AssignmentId);
    }

    [Fact]
    public async Task Update_Preserves_SortOrder()
    {
        var svc = BuildService();
        await svc.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100"]));
        var second = await svc.CreateWorkspaceAsync(Command(site: "SW", parentParts: ["ABC200"]));

        var result = await svc.UpdateWorkspaceAsync(
            second.Workspace!.AssignmentId,
            Command(site: "AR", parentParts: ["ABC300"]));

        Assert.Equal(1, result.Workspace!.SortOrder);
    }

    [Fact]
    public async Task Update_Preserves_IsEnabled()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100"]));
        await svc.ArchiveWorkspaceAsync(created.Workspace!.AssignmentId);

        var result = await svc.UpdateWorkspaceAsync(
            created.Workspace.AssignmentId,
            Command(site: "SW", parentParts: ["ABC200"]));

        Assert.False(result.Workspace!.IsEnabled);
    }

    [Fact]
    public async Task Update_Rejects_Invalid_Site()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100"]));

        var result = await svc.UpdateWorkspaceAsync(
            created.Workspace!.AssignmentId,
            Command(site: "N", parentParts: ["ABC100"]));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "site");
    }

    [Fact]
    public async Task Update_Adds_ParentParts()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(Command(productLineFrom: "0040"));

        var result = await svc.UpdateWorkspaceAsync(
            created.Workspace!.AssignmentId,
            Command(productLineFrom: "0040", parentParts: ["ABC100", "ABC200"]));

        Assert.True(result.IsSuccess);
        Assert.Equal(["ABC100", "ABC200"], result.Workspace!.ParentParts);
    }

    [Fact]
    public async Task Update_Removes_Some_ParentParts()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(Command(productLineFrom: "0040", parentParts: ["ABC100", "ABC200"]));

        var result = await svc.UpdateWorkspaceAsync(
            created.Workspace!.AssignmentId,
            Command(productLineFrom: "0040", parentParts: ["ABC100"]));

        Assert.True(result.IsSuccess);
        Assert.Equal(["ABC100"], result.Workspace!.ParentParts);
    }

    [Fact]
    public async Task Update_Clears_ParentParts_When_ProductLine_Remains()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(Command(productLineFrom: "0040", parentParts: ["ABC100"]));

        var result = await svc.UpdateWorkspaceAsync(
            created.Workspace!.AssignmentId,
            Command(productLineFrom: "0040", parentParts: []));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Workspace!.ParentParts);
    }

    [Fact]
    public async Task Update_Rejects_Removing_Final_Scope_Mechanism()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(Command(parentParts: ["ABC100"]));

        var result = await svc.UpdateWorkspaceAsync(
            created.Workspace!.AssignmentId,
            Command(parentParts: []));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "scope");
    }

    [Fact]
    public async Task Update_Returns_NotFound_For_Unknown_AssignmentId()
    {
        var svc = BuildService();

        var result = await svc.UpdateWorkspaceAsync(
            Guid.NewGuid(),
            Command(parentParts: ["ABC100"]));

        Assert.True(result.NotFound);
        Assert.False(result.IsSuccess);
    }

    // --- Archive / Restore ---

    [Fact]
    public async Task Archive_Sets_IsEnabled_False()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(Command(parentParts: ["ABC100"]));

        var result = await svc.ArchiveWorkspaceAsync(created.Workspace!.AssignmentId);

        Assert.True(result.IsSuccess);
        Assert.False(result.Workspace!.IsEnabled);
    }

    [Fact]
    public async Task Archive_Preserves_ParentParts()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(Command(parentParts: ["ABC100", "ABC200"]));

        var result = await svc.ArchiveWorkspaceAsync(created.Workspace!.AssignmentId);

        Assert.Equal(["ABC100", "ABC200"], result.Workspace!.ParentParts);
    }

    [Fact]
    public async Task Archive_Returns_NotFound_For_Unknown_AssignmentId()
    {
        var svc = BuildService();
        var result = await svc.ArchiveWorkspaceAsync(Guid.NewGuid());
        Assert.True(result.NotFound);
    }

    [Fact]
    public async Task Restore_Sets_IsEnabled_True()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(Command(parentParts: ["ABC100"]));
        await svc.ArchiveWorkspaceAsync(created.Workspace!.AssignmentId);

        var result = await svc.RestoreWorkspaceAsync(created.Workspace.AssignmentId);

        Assert.True(result.IsSuccess);
        Assert.True(result.Workspace!.IsEnabled);
    }

    [Fact]
    public async Task Restore_Preserves_ParentParts()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(Command(parentParts: ["ABC100", "ABC200"]));
        await svc.ArchiveWorkspaceAsync(created.Workspace!.AssignmentId);

        var result = await svc.RestoreWorkspaceAsync(created.Workspace.AssignmentId);

        Assert.Equal(["ABC100", "ABC200"], result.Workspace!.ParentParts);
    }

    [Fact]
    public async Task Restore_Returns_NotFound_For_Unknown_AssignmentId()
    {
        var svc = BuildService();
        var result = await svc.RestoreWorkspaceAsync(Guid.NewGuid());
        Assert.True(result.NotFound);
    }

    [Fact]
    public async Task Restore_Preserves_SortOrder()
    {
        var svc = BuildService();
        await svc.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100"]));
        var second = await svc.CreateWorkspaceAsync(Command(site: "SW", parentParts: ["ABC200"]));
        await svc.ArchiveWorkspaceAsync(second.Workspace!.AssignmentId);

        var result = await svc.RestoreWorkspaceAsync(second.Workspace.AssignmentId);

        Assert.Equal(1, result.Workspace!.SortOrder);
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_Removes_Assignment()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(Command(parentParts: ["ABC100"]));

        var result = await svc.DeleteWorkspaceAsync(created.Workspace!.AssignmentId);
        Assert.True(result.IsSuccess);

        var list = await svc.GetWorkspacesAsync();
        Assert.Empty(list.Workspaces);
    }

    [Fact]
    public async Task Delete_Returns_NotFound_For_Unknown_AssignmentId()
    {
        var svc = BuildService();
        var result = await svc.DeleteWorkspaceAsync(Guid.NewGuid());
        Assert.True(result.NotFound);
    }

    // --- Reset ---

    [Fact]
    public async Task Reset_Removes_All_Assignments()
    {
        var svc = BuildService();
        await svc.CreateWorkspaceAsync(Command(site: "NW", parentParts: ["ABC100"]));
        await svc.CreateWorkspaceAsync(Command(site: "SW", parentParts: ["ABC200"]));

        await svc.ResetWorkspacesAsync();

        var list = await svc.GetWorkspacesAsync();
        Assert.Empty(list.Workspaces);
    }

    [Fact]
    public async Task Reset_On_Empty_Configuration_Succeeds()
    {
        var svc = BuildService();
        await svc.ResetWorkspacesAsync();

        var list = await svc.GetWorkspacesAsync();
        Assert.Empty(list.Workspaces);
    }
}

/// <summary>
/// In-memory workspace store for testing.
/// </summary>
internal sealed class InMemoryTestWorkspaceStore : IWorkspaceConfigurationStore
{
    private List<WorkspaceAssignment> _workspaces;

    public InMemoryTestWorkspaceStore(IReadOnlyList<WorkspaceAssignment> seed)
    {
        _workspaces = [.. seed];
    }

    public Task<WorkspaceLoadResult> LoadAsync() =>
        Task.FromResult(new WorkspaceLoadResult(_workspaces, null));

    public Task SaveAsync(IReadOnlyList<WorkspaceAssignment> workspaces)
    {
        _workspaces = [.. workspaces];
        return Task.CompletedTask;
    }
}
