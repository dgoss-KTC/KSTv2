using Kst.Domain.Common;
using Kst.Domain.Snapshots;

namespace Kst.Application.Snapshots;

/// <summary>
/// Metadata describing a snapshot held in memory.
/// </summary>
public sealed record SnapshotInfo(
    SnapshotId Id,
    DateTimeOffset CreatedAt,
    SnapshotStatus Status
)
{
    public static readonly SnapshotInfo None = new(
        new SnapshotId(Guid.Empty),
        DateTimeOffset.MinValue,
        SnapshotStatus.NotLoaded
    );

    public bool IsAvailable => Status == SnapshotStatus.Loaded;
}
