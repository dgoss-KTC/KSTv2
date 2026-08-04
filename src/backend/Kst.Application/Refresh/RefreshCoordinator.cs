using Kst.Application.Snapshots;
using Kst.Application.SystemStatus;
using Kst.Domain.Common;
using Kst.Domain.Snapshots;

namespace Kst.Application.Refresh;

/// <summary>
/// Orchestrates the Stage 4 no-business-data refresh lifecycle: transitions the snapshot to Loading,
/// invokes the registered providers, and truthfully derives the resulting snapshot/source status and
/// refresh history. Never fabricates a successful business-data load.
/// </summary>
public sealed class RefreshCoordinator
{
    private readonly IClock _clock;
    private readonly ISnapshotStore _snapshotStore;
    private readonly IDataSourceStatusStore _dataSourceStatusStore;
    private readonly IRefreshHistoryStore _historyStore;
    private readonly IReadOnlyList<IRefreshProvider> _providers;

    public RefreshCoordinator(
        IClock clock,
        ISnapshotStore snapshotStore,
        IDataSourceStatusStore dataSourceStatusStore,
        IRefreshHistoryStore historyStore,
        IReadOnlyList<IRefreshProvider> providers)
    {
        _clock = clock;
        _snapshotStore = snapshotStore;
        _dataSourceStatusStore = dataSourceStatusStore;
        _historyStore = historyStore;
        _providers = providers;
    }

    public async Task<RefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var attemptedAt = _clock.LocalNow;
        var previousStatus = _snapshotStore.GetCurrentSnapshot().Status;

        _snapshotStore.ReplaceSnapshot(new SnapshotInfo(SnapshotId.New(), attemptedAt, SnapshotStatus.Loading));
        _historyStore.RecordAttempt(attemptedAt);

        var outcomes = new List<(string Name, RefreshProviderOutcome Outcome)>();
        foreach (var provider in _providers)
        {
            RefreshProviderOutcome outcome;
            try
            {
                outcome = await provider.RefreshAsync(cancellationToken);
            }
            catch
            {
                outcome = RefreshProviderOutcome.Failed;
            }

            outcomes.Add((provider.SourceName, outcome));
        }

        var dataSources = outcomes
            .Select(o => new DataSourceSummary(o.Name, ToDataSourceStatus(o.Outcome)))
            .ToList();
        _dataSourceStatusStore.ReplaceAll(dataSources);

        var newStatus = DeriveSnapshotStatus(outcomes, previousStatus);
        var snapshot = new SnapshotInfo(SnapshotId.New(), attemptedAt, newStatus);
        _snapshotStore.ReplaceSnapshot(snapshot);

        if (newStatus is SnapshotStatus.Current or SnapshotStatus.Partial)
            _historyStore.RecordSuccess(attemptedAt);

        return new RefreshResult(snapshot, dataSources, _historyStore.GetHistory());
    }

    private static SnapshotStatus DeriveSnapshotStatus(
        List<(string Name, RefreshProviderOutcome Outcome)> outcomes,
        SnapshotStatus previousStatus)
    {
        if (outcomes.Count == 0)
            return SnapshotStatus.NotLoaded;

        var succeededCount = outcomes.Count(o => o.Outcome == RefreshProviderOutcome.Succeeded);
        var failedCount = outcomes.Count(o => o.Outcome == RefreshProviderOutcome.Failed);

        if (succeededCount == outcomes.Count)
            return SnapshotStatus.Current;

        if (succeededCount > 0)
            return SnapshotStatus.Partial;

        if (failedCount > 0)
        {
            var hadData = previousStatus is SnapshotStatus.Current or SnapshotStatus.Stale or SnapshotStatus.Partial;
            return hadData ? SnapshotStatus.Stale : SnapshotStatus.Failed;
        }

        // Every provider reported NotConfigured/Unavailable — truthfully nothing was loaded.
        return SnapshotStatus.NotLoaded;
    }

    private static DataSourceStatus ToDataSourceStatus(RefreshProviderOutcome outcome) => outcome switch
    {
        RefreshProviderOutcome.NotConfigured => DataSourceStatus.NotConfigured,
        RefreshProviderOutcome.Succeeded => DataSourceStatus.Current,
        RefreshProviderOutcome.Failed => DataSourceStatus.Failed,
        RefreshProviderOutcome.Unavailable => DataSourceStatus.Unavailable,
        _ => DataSourceStatus.Unavailable
    };
}
