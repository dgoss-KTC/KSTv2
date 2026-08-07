using Kst.Application.Mps;
using Kst.Domain.Mps;
using Kst.Domain.Snapshots;
using Kst.Domain.Workspaces;
using Kst.Infrastructure.Mps;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Application.Tests.Mps;

public sealed class MpsWorkspaceSnapshotServiceTests
{
    private static readonly WorkspaceAssignment Workspace = new(
        AssignmentId: Guid.NewGuid(),
        DisplayName: "Test Workspace",
        Site: "SW",
        ProductLineFrom: null,
        ProductLineTo: null,
        ParentParts: ["ABC100", "ABC200"],
        IsTemporary: false,
        CoverageEndsOn: null,
        IsEnabled: true,
        SortOrder: 0);

    private static readonly DateOnly Today = new(2026, 8, 5);

    private static (MpsWorkspaceSnapshotService Service, IMpsSnapshotStore Store) BuildService(
        Func<string, IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<MpsSourceRow>>>? read = null,
        IReadOnlyList<MpsResolvedPart>? resolvedParts = null)
    {
        var store = new InMemoryMpsSnapshotStore();
        var scopeResolver = new DelegateMpsScopeResolver((_, _) =>
            Task.FromResult(resolvedParts ?? (IReadOnlyList<MpsResolvedPart>)
                [new MpsResolvedPart("ABC100", "Widget"), new MpsResolvedPart("ABC200", "Gadget")]));

        var sourceReader = new DelegateMpsSourceReader(
            read ?? ((_, _, _) => Task.FromResult((IReadOnlyList<MpsSourceRow>)[])));

        var service = new MpsWorkspaceSnapshotService(
            new FakeWorkspaceConfigurationService(Workspace), scopeResolver, sourceReader, store,
            NullLogger<MpsWorkspaceSnapshotService>.Instance);

        return (service, store);
    }

    [Fact]
    public async Task GetDashboardAsync_Auto_Loads_On_First_Read()
    {
        var (service, _) = BuildService();

        var result = await service.GetDashboardAsync(Workspace.AssignmentId, MpsDateBasis.DueDate, 4, Today);

        Assert.Equal(SnapshotStatus.Current, result.Status);
        Assert.Equal(2, result.Schedules.Count);
        Assert.NotNull(result.Snapshot);
        Assert.False(result.IsRefreshInProgress);
    }

    [Fact]
    public async Task GetDashboardAsync_Does_Not_ReQuery_Once_Loaded()
    {
        var callCount = 0;
        var (service, _) = BuildService(read: (_, _, _) =>
        {
            callCount++;
            return Task.FromResult((IReadOnlyList<MpsSourceRow>)[]);
        });

        await service.GetDashboardAsync(Workspace.AssignmentId, MpsDateBasis.DueDate, 4, Today);
        await service.GetDashboardAsync(Workspace.AssignmentId, MpsDateBasis.ReleaseDate, 8, Today);
        await service.GetDashboardAsync(Workspace.AssignmentId, MpsDateBasis.DueDate, 12, Today);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetDashboardAsync_Retains_ZeroFact_Part_With_Empty_Buckets()
    {
        var (service, _) = BuildService(
            resolvedParts: [new MpsResolvedPart("ABC100", "Widget")],
            read: (_, _, _) => Task.FromResult((IReadOnlyList<MpsSourceRow>)[]));

        var result = await service.GetDashboardAsync(Workspace.AssignmentId, MpsDateBasis.DueDate, 4, Today);

        var schedule = Assert.Single(result.Schedules);
        Assert.Equal("ABC100", schedule.ParentPart);
        Assert.All(schedule.Buckets, b => Assert.Equal(0m, b.Quantity));
    }

    [Fact]
    public async Task Initial_Load_Failure_Yields_Failed_Status_With_No_Snapshot()
    {
        var (service, _) = BuildService(read: (_, _, _) =>
            throw new InvalidOperationException("QAD database connectivity failed."));

        var result = await service.GetDashboardAsync(Workspace.AssignmentId, MpsDateBasis.DueDate, 4, Today);

        Assert.Equal(SnapshotStatus.Failed, result.Status);
        Assert.Equal("QAD database connectivity failed.", result.ErrorMessage);
        Assert.Empty(result.Schedules);
        Assert.False(result.IsRefreshInProgress);
    }

    [Fact]
    public async Task RefreshAsync_Failure_After_Prior_Success_Yields_Stale_And_Retains_Old_Snapshot()
    {
        var shouldFail = false;
        var (service, _) = BuildService(read: (_, _, _) =>
        {
            if (shouldFail)
                throw new InvalidOperationException("QAD database connectivity failed.");

            return Task.FromResult((IReadOnlyList<MpsSourceRow>)[]);
        });

        var first = await service.GetDashboardAsync(Workspace.AssignmentId, MpsDateBasis.DueDate, 4, Today);
        Assert.Equal(SnapshotStatus.Current, first.Status);

        shouldFail = true;
        var second = await service.RefreshAsync(Workspace.AssignmentId, MpsDateBasis.DueDate, 4, Today);

        Assert.Equal(SnapshotStatus.Stale, second.Status);
        Assert.Equal("QAD database connectivity failed.", second.ErrorMessage);
        Assert.Equal(2, second.Schedules.Count); // prior good snapshot's parts still projected
    }

    [Fact]
    public async Task Concurrent_Refresh_For_Same_Workspace_Does_Not_Invoke_Reader_Twice_Simultaneously()
    {
        var gate = new TaskCompletionSource();
        var callCount = 0;
        var (service, _) = BuildService(read: async (_, _, _) =>
        {
            Interlocked.Increment(ref callCount);
            await gate.Task;
            return (IReadOnlyList<MpsSourceRow>)[];
        });

        var firstRefresh = service.RefreshAsync(Workspace.AssignmentId, MpsDateBasis.DueDate, 4, Today);
        var secondRefresh = service.RefreshAsync(Workspace.AssignmentId, MpsDateBasis.DueDate, 4, Today);

        gate.SetResult();
        await Task.WhenAll(firstRefresh, secondRefresh);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetDashboardAsync_Throws_MpsWorkspaceNotFoundException_For_Unknown_Workspace()
    {
        var (service, _) = BuildService();

        await Assert.ThrowsAsync<MpsWorkspaceNotFoundException>(() =>
            service.GetDashboardAsync(Guid.NewGuid(), MpsDateBasis.DueDate, 4, Today));
    }
}
