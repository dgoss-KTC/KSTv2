using Kst.Domain.Snapshots;

namespace Kst.Application.Mps;

/// <summary>
/// Per-workspace MPS load state. Reuses <see cref="SnapshotStatus"/> rather than inventing a new
/// lifecycle enum, per the accepted Stage 5A design. <see cref="Snapshot"/> is the last GOOD load and
/// is retained across a failed refresh (stale-but-available), matching the "keep last good data
/// visible" UX requirement. <see cref="IsRefreshInProgress"/> is a separate, transient signal so a
/// refresh-in-progress state can be shown even while a prior good snapshot is still Current/Stale.
/// </summary>
public sealed record MpsWorkspaceState(
    SnapshotStatus Status,
    MpsSnapshot? Snapshot,
    string? ErrorMessage,
    DateTimeOffset? LastAttemptAt,
    bool IsRefreshInProgress)
{
    public static MpsWorkspaceState Initial { get; } = new(SnapshotStatus.NotLoaded, null, null, null, false);
}

