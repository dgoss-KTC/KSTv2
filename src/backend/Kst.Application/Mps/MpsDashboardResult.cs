using Kst.Domain.Mps;
using Kst.Domain.Snapshots;

namespace Kst.Application.Mps;

/// <summary>
/// Projected MPS dashboard for one workspace at a specific date basis and horizon.
/// <see cref="Snapshot"/> is null only when no successful load has ever occurred (NotLoaded/Failed
/// with no prior good data); the API layer maps that case to a Problem Details response rather than
/// a 200 with an empty snapshot, per the accepted Stage 5A contract.
/// </summary>
public sealed record MpsDashboardResult(
    SnapshotStatus Status,
    string? ErrorMessage,
    bool IsRefreshInProgress,
    MpsSnapshot? Snapshot,
    IReadOnlyList<MpsPartSchedule> Schedules
);
