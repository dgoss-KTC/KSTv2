namespace Kst.Domain.Snapshots;

/// <summary>
/// Describes the current state of an in-memory snapshot.
/// </summary>
public enum SnapshotStatus
{
    NotLoaded,
    Loading,
    Loaded,
    Failed
}
