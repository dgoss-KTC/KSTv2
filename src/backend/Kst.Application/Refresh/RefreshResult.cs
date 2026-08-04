using Kst.Application.Snapshots;
using Kst.Application.SystemStatus;

namespace Kst.Application.Refresh;

/// <summary>
/// Result of a single refresh cycle: the resulting snapshot status, the resulting per-source
/// status list, and the updated refresh history.
/// </summary>
public sealed record RefreshResult(
    SnapshotInfo Snapshot,
    IReadOnlyList<DataSourceSummary> DataSources,
    RefreshHistory History
);
