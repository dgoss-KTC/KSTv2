using System.Threading;
using Kst.Application.Snapshots;

namespace Kst.Infrastructure.Snapshots;

/// <summary>
/// Thread-safe in-memory snapshot store.
/// Holds the most recently loaded snapshot; starts with SnapshotInfo.None.
/// </summary>
public sealed class InMemorySnapshotStore : ISnapshotStore
{
    private SnapshotInfo _current = SnapshotInfo.None;
    private readonly Lock _lock = new();

    public SnapshotInfo GetCurrentSnapshot()
    {
        lock (_lock)
        {
            return _current;
        }
    }

    public void ReplaceSnapshot(SnapshotInfo snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_lock)
        {
            _current = snapshot;
        }
    }
}
