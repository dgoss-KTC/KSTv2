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

    // --- Valid cases ---

    [Fact]
    public async Task Valid_Site_And_CustomerNumber_Succeeds()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Workspace);
    }

    [Fact]
    public async Task Valid_Site_And_SingleProductLine_Succeeds()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "SW", null, "0040", null, false, null));
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Valid_Site_And_ProductLineRange_Succeeds()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "AR", null, "0040", "0045", false, null));
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Valid_Site_Customer_And_ProductLineRange_Succeeds()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "VT", "12345678", "0040", "0045", false, null));
        Assert.True(result.IsSuccess);
    }

    // --- Site normalization ---

    [Fact]
    public async Task Site_Is_Normalized_To_Uppercase()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "nw", "12345678", null, null, false, null));
        Assert.True(result.IsSuccess);
        Assert.Equal("NW", result.Workspace!.Site);
    }

    // --- Site validation ---

    [Fact]
    public async Task Invalid_Site_TooShort_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "N", "12345678", null, null, false, null));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "site");
    }

    [Fact]
    public async Task Invalid_Site_TooLong_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NWW", "12345678", null, null, false, null));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "site");
    }

    [Fact]
    public async Task Invalid_Site_NonLetters_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "1W", "12345678", null, null, false, null));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "site");
    }

    [Fact]
    public async Task Missing_Site_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, null, "12345678", null, null, false, null));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "site");
    }

    // --- CustomerNumber validation ---

    [Fact]
    public async Task CustomerNumber_TooShort_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "1234567", null, null, false, null));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "customerNumber");
    }

    [Fact]
    public async Task CustomerNumber_TooLong_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "123456789", null, null, false, null));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "customerNumber");
    }

    [Fact]
    public async Task CustomerNumber_NonDigits_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "1234567A", null, null, false, null));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "customerNumber");
    }

    // --- ProductLine validation ---

    [Fact]
    public async Task ProductLineFrom_TooShort_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", null, "004", null, false, null));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "productLineFrom");
    }

    [Fact]
    public async Task ProductLineTo_Without_ProductLineFrom_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", null, null, "0045", false, null));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "productLineTo");
    }

    [Fact]
    public async Task ProductLineTo_Below_ProductLineFrom_Fails()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", null, "0045", "0040", false, null));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "productLineTo");
    }

    // --- Scope requirement ---

    [Fact]
    public async Task Site_Only_Workspace_Is_Rejected()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", null, null, null, false, null));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "scope");
    }

    // --- Single product-line normalization ---

    [Fact]
    public async Task ProductLineTo_Is_Set_To_ProductLineFrom_When_Blank()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", null, "0040", null, false, null));
        Assert.True(result.IsSuccess);
        Assert.Equal("0040", result.Workspace!.ProductLineFrom);
        Assert.Equal("0040", result.Workspace.ProductLineTo);
    }

    // --- Display name derivation ---

    [Fact]
    public async Task DisplayName_Derived_From_CustomerNumber()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));
        Assert.Equal("Customer 12345678", result.Workspace!.DisplayName);
    }

    [Fact]
    public async Task DisplayName_Derived_From_SingleProductLine()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", null, "0040", null, false, null));
        Assert.Equal("PL 0040", result.Workspace!.DisplayName);
    }

    [Fact]
    public async Task DisplayName_Derived_From_ProductLineRange()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", null, "0040", "0045", false, null));
        Assert.Equal("PL 0040\u20130045", result.Workspace!.DisplayName);
    }

    [Fact]
    public async Task Explicit_DisplayName_Is_Preserved()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            "My Workspace", "NW", "12345678", null, null, false, null));
        Assert.Equal("My Workspace", result.Workspace!.DisplayName);
    }

    // --- Sort order ---

    [Fact]
    public async Task First_Workspace_Gets_SortOrder_Zero()
    {
        var svc = BuildService();
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));
        Assert.Equal(0, result.Workspace!.SortOrder);
    }

    [Fact]
    public async Task Second_Workspace_Gets_Next_SortOrder()
    {
        var svc = BuildService();
        await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));
        var result = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "SW", "87654321", null, null, false, null));
        Assert.Equal(1, result.Workspace!.SortOrder);
    }

    // --- Update ---

    [Fact]
    public async Task Update_Changes_Workspace_Fields()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));

        var result = await svc.UpdateWorkspaceAsync(
            created.Workspace!.AssignmentId,
            new CreateWorkspaceCommand(null, "SW", "87654321", null, null, false, null));

        Assert.True(result.IsSuccess);
        Assert.Equal("SW", result.Workspace!.Site);
        Assert.Equal("87654321", result.Workspace.CustomerNumber);
    }

    [Fact]
    public async Task Update_Preserves_AssignmentId()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));

        var result = await svc.UpdateWorkspaceAsync(
            created.Workspace!.AssignmentId,
            new CreateWorkspaceCommand(null, "SW", "87654321", null, null, false, null));

        Assert.Equal(created.Workspace.AssignmentId, result.Workspace!.AssignmentId);
    }

    [Fact]
    public async Task Update_Preserves_SortOrder()
    {
        var svc = BuildService();
        await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));
        var second = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "SW", "87654321", null, null, false, null));

        var result = await svc.UpdateWorkspaceAsync(
            second.Workspace!.AssignmentId,
            new CreateWorkspaceCommand(null, "AR", "11112222", null, null, false, null));

        Assert.Equal(1, result.Workspace!.SortOrder);
    }

    [Fact]
    public async Task Update_Preserves_IsEnabled()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));
        await svc.ArchiveWorkspaceAsync(created.Workspace!.AssignmentId);

        var result = await svc.UpdateWorkspaceAsync(
            created.Workspace.AssignmentId,
            new CreateWorkspaceCommand(null, "SW", "87654321", null, null, false, null));

        Assert.False(result.Workspace!.IsEnabled);
    }

    [Fact]
    public async Task Update_Rejects_Invalid_Site()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));

        var result = await svc.UpdateWorkspaceAsync(
            created.Workspace!.AssignmentId,
            new CreateWorkspaceCommand(null, "N", "12345678", null, null, false, null));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == "site");
    }

    [Fact]
    public async Task Update_Returns_NotFound_For_Unknown_AssignmentId()
    {
        var svc = BuildService();

        var result = await svc.UpdateWorkspaceAsync(
            Guid.NewGuid(),
            new CreateWorkspaceCommand(null, "NW", "12345678", null, null, false, null));

        Assert.True(result.NotFound);
        Assert.False(result.IsSuccess);
    }

    // --- Archive / Restore ---

    [Fact]
    public async Task Archive_Sets_IsEnabled_False()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));

        var result = await svc.ArchiveWorkspaceAsync(created.Workspace!.AssignmentId);

        Assert.True(result.IsSuccess);
        Assert.False(result.Workspace!.IsEnabled);
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
        var created = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));
        await svc.ArchiveWorkspaceAsync(created.Workspace!.AssignmentId);

        var result = await svc.RestoreWorkspaceAsync(created.Workspace.AssignmentId);

        Assert.True(result.IsSuccess);
        Assert.True(result.Workspace!.IsEnabled);
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
        await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));
        var second = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "SW", "87654321", null, null, false, null));
        await svc.ArchiveWorkspaceAsync(second.Workspace!.AssignmentId);

        var result = await svc.RestoreWorkspaceAsync(second.Workspace.AssignmentId);

        Assert.Equal(1, result.Workspace!.SortOrder);
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_Removes_Assignment()
    {
        var svc = BuildService();
        var created = await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));

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
        await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "NW", "12345678", null, null, false, null));
        await svc.CreateWorkspaceAsync(new CreateWorkspaceCommand(
            null, "SW", "87654321", null, null, false, null));

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
