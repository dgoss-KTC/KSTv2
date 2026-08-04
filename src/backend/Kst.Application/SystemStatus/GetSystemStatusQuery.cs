using Kst.Domain.Common;
using Kst.Application.Snapshots;
using Kst.Application.Refresh;

namespace Kst.Application.SystemStatus;

/// <summary>
/// Returns the current system status for the technical-foundation API endpoint.
/// </summary>
public sealed class GetSystemStatusQuery
{
    private readonly IClock _clock;
    private readonly ISnapshotStore _snapshotStore;
    private readonly ApplicationInfo _appInfo;
    private readonly IDataSourceStatusStore _dataSourceStatusStore;
    private readonly IRefreshHistoryStore _refreshHistoryStore;

    public GetSystemStatusQuery(
        IClock clock,
        ISnapshotStore snapshotStore,
        ApplicationInfo appInfo,
        IDataSourceStatusStore dataSourceStatusStore,
        IRefreshHistoryStore refreshHistoryStore)
    {
        _clock = clock;
        _snapshotStore = snapshotStore;
        _appInfo = appInfo;
        _dataSourceStatusStore = dataSourceStatusStore;
        _refreshHistoryStore = refreshHistoryStore;
    }

    public SystemStatusResult Execute()
    {
        var snapshot = _snapshotStore.GetCurrentSnapshot();
        var history = _refreshHistoryStore.GetHistory();

        return new SystemStatusResult(
            ApplicationName: _appInfo.Name,
            ApplicationVersion: _appInfo.Version,
            BackendFramework: ".NET 10",
            BackendInstanceId: _appInfo.InstanceId,
            StartedAt: _appInfo.StartedAt,
            CurrentTime: _clock.LocalNow,
            Snapshot: snapshot,
            DataSources: _dataSourceStatusStore.GetAll(),
            LastRefreshAttemptAt: history.LastAttemptAt,
            LastSuccessfulRefreshAt: history.LastSuccessfulAt
        );
    }
}
