using Kst.Domain.Snapshots;

namespace Kst.Application.Snapshots;

/// <summary>
/// Contract for reading and writing the current in-memory snapshot.
/// Implementations live in Kst.Infrastructure.
/// </summary>
public interface ISnapshotStore
{
    SnapshotInfo GetCurrentSnapshot();
    void ReplaceSnapshot(SnapshotInfo snapshot);
}
