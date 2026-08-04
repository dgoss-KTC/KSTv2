using Kst.Application.Refresh;
using Kst.Application.Snapshots;
using Kst.Application.SystemStatus;
using Kst.Domain.Common;
using Kst.Domain.Snapshots;
using Kst.Infrastructure.Clock;
using Kst.Infrastructure.Snapshots;
using Kst.Infrastructure.SystemStatus;

namespace Kst.Application.Tests.Refresh;

public sealed class RefreshCoordinatorTests
{
    private static IRefreshProvider Provider(string name, RefreshProviderOutcome outcome) =>
        new DelegateRefreshProvider(name, _ => Task.FromResult(outcome));

    private static RefreshCoordinator BuildCoordinator(
        IReadOnlyList<IRefreshProvider> providers,
        ISnapshotStore? snapshotStore = null,
        IDataSourceStatusStore? dataSourceStatusStore = null,
        IRefreshHistoryStore? historyStore = null) =>
        new(
            new SystemClock(),
            snapshotStore ?? new InMemorySnapshotStore(),
            dataSourceStatusStore ?? new InMemoryDataSourceStatusStore(),
            historyStore ?? new InMemoryRefreshHistoryStore(),
            providers);

    [Fact]
    public async Task All_Providers_Succeed_Yields_Current_Snapshot()
    {
        var coordinator = BuildCoordinator([
            Provider("QAD", RefreshProviderOutcome.Succeeded),
            Provider("Shortage Database", RefreshProviderOutcome.Succeeded)
        ]);

        var result = await coordinator.RefreshAsync();

        Assert.Equal(SnapshotStatus.Current, result.Snapshot.Status);
        Assert.All(result.DataSources, d => Assert.Equal(DataSourceStatus.Current, d.Status));
        Assert.NotNull(result.History.LastSuccessfulAt);
        Assert.NotNull(result.History.LastAttemptAt);
    }

    [Fact]
    public async Task Some_Providers_Succeed_Some_Fail_Yields_Partial()
    {
        var coordinator = BuildCoordinator([
            Provider("QAD", RefreshProviderOutcome.Succeeded),
            Provider("Shortage Database", RefreshProviderOutcome.Failed)
        ]);

        var result = await coordinator.RefreshAsync();

        Assert.Equal(SnapshotStatus.Partial, result.Snapshot.Status);
        Assert.NotNull(result.History.LastSuccessfulAt);
    }

    [Fact]
    public async Task All_Providers_Fail_With_No_Prior_Data_Yields_Failed()
    {
        var coordinator = BuildCoordinator([
            Provider("QAD", RefreshProviderOutcome.Failed),
            Provider("Shortage Database", RefreshProviderOutcome.Failed)
        ]);

        var result = await coordinator.RefreshAsync();

        Assert.Equal(SnapshotStatus.Failed, result.Snapshot.Status);
        Assert.Null(result.History.LastSuccessfulAt);
        Assert.NotNull(result.History.LastAttemptAt);
    }

    [Fact]
    public async Task All_Providers_Fail_After_Prior_Success_Yields_Stale_Not_Failed()
    {
        var snapshotStore = new InMemorySnapshotStore();
        snapshotStore.ReplaceSnapshot(new SnapshotInfo(
            SnapshotId.New(), DateTimeOffset.Now, SnapshotStatus.Current));

        var coordinator = BuildCoordinator(
            [
                Provider("QAD", RefreshProviderOutcome.Failed),
                Provider("Shortage Database", RefreshProviderOutcome.Failed)
            ],
            snapshotStore: snapshotStore);

        var result = await coordinator.RefreshAsync();

        Assert.Equal(SnapshotStatus.Stale, result.Snapshot.Status);
        Assert.Null(result.History.LastSuccessfulAt);
    }

    [Fact]
    public async Task All_Providers_NotConfigured_Yields_NotLoaded()
    {
        var coordinator = BuildCoordinator([
            Provider("QAD", RefreshProviderOutcome.NotConfigured),
            Provider("Shortage Database", RefreshProviderOutcome.NotConfigured)
        ]);

        var result = await coordinator.RefreshAsync();

        Assert.Equal(SnapshotStatus.NotLoaded, result.Snapshot.Status);
        Assert.Null(result.History.LastSuccessfulAt);
    }

    [Fact]
    public async Task No_Providers_Yields_NotLoaded()
    {
        var coordinator = BuildCoordinator([]);

        var result = await coordinator.RefreshAsync();

        Assert.Equal(SnapshotStatus.NotLoaded, result.Snapshot.Status);
        Assert.Empty(result.DataSources);
    }

    [Fact]
    public async Task Snapshot_Transitions_Through_Loading_During_Refresh()
    {
        var snapshotStore = new InMemorySnapshotStore();
        var gate = new TaskCompletionSource();
        var slowProvider = new DelegateRefreshProvider("QAD", async _ =>
        {
            await gate.Task;
            return RefreshProviderOutcome.Succeeded;
        });

        var coordinator = BuildCoordinator([slowProvider], snapshotStore: snapshotStore);

        var refreshTask = coordinator.RefreshAsync();

        Assert.Equal(SnapshotStatus.Loading, snapshotStore.GetCurrentSnapshot().Status);

        gate.SetResult();
        var result = await refreshTask;

        Assert.Equal(SnapshotStatus.Current, result.Snapshot.Status);
    }

    [Fact]
    public async Task Provider_Throwing_Is_Treated_As_Failed()
    {
        var throwing = new DelegateRefreshProvider("QAD", _ => throw new InvalidOperationException("boom"));
        var coordinator = BuildCoordinator([throwing]);

        var result = await coordinator.RefreshAsync();

        Assert.Equal(SnapshotStatus.Failed, result.Snapshot.Status);
        Assert.Equal(DataSourceStatus.Failed, result.DataSources[0].Status);
    }
}
