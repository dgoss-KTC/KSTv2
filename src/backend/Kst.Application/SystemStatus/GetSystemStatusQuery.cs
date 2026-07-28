using Kst.Domain.Common;
using Kst.Application.Snapshots;

namespace Kst.Application.SystemStatus;

/// <summary>
/// Returns the current system status for the technical-foundation API endpoint.
/// </summary>
public sealed class GetSystemStatusQuery
{
    private readonly IClock _clock;
    private readonly ISnapshotStore _snapshotStore;
    private readonly ApplicationInfo _appInfo;
    private readonly IReadOnlyList<DataSourceSummary> _dataSources;

    public GetSystemStatusQuery(
        IClock clock,
        ISnapshotStore snapshotStore,
        ApplicationInfo appInfo,
        IReadOnlyList<DataSourceSummary> dataSources)
    {
        _clock = clock;
        _snapshotStore = snapshotStore;
        _appInfo = appInfo;
        _dataSources = dataSources;
    }

    public SystemStatusResult Execute()
    {
        var snapshot = _snapshotStore.GetCurrentSnapshot();

        return new SystemStatusResult(
            ApplicationName: _appInfo.Name,
            ApplicationVersion: _appInfo.Version,
            BackendFramework: ".NET 10",
            BackendInstanceId: _appInfo.InstanceId,
            StartedAt: _appInfo.StartedAt,
            CurrentTime: _clock.LocalNow,
            Snapshot: snapshot,
            DataSources: _dataSources
        );
    }
}
