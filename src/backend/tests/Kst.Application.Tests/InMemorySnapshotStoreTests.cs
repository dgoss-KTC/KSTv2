using Kst.Application.Snapshots;
using Kst.Domain.Common;
using Kst.Domain.Snapshots;
using Kst.Infrastructure.Snapshots;

namespace Kst.Application.Tests;

public sealed class InMemorySnapshotStoreTests
{
    [Fact]
    public void InitialState_IsNone()
    {
        var store = new InMemorySnapshotStore();
        var snapshot = store.GetCurrentSnapshot();

        Assert.Equal(SnapshotInfo.None, snapshot);
        Assert.Equal(SnapshotStatus.NotLoaded, snapshot.Status);
    }

    [Fact]
    public void ReplaceSnapshot_UpdatesCurrentSnapshot()
    {
        var store = new InMemorySnapshotStore();
        var id = SnapshotId.New();
        var createdAt = DateTimeOffset.Now;
        var newSnapshot = new SnapshotInfo(id, createdAt, SnapshotStatus.Current);

        store.ReplaceSnapshot(newSnapshot);

        var result = store.GetCurrentSnapshot();
        Assert.Equal(id, result.Id);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.Equal(SnapshotStatus.Current, result.Status);
    }

    [Fact]
    public void ReplaceSnapshot_WithNull_Throws()
    {
        var store = new InMemorySnapshotStore();
        Assert.Throws<ArgumentNullException>(() => store.ReplaceSnapshot(null!));
    }

    [Fact]
    public void SnapshotMetadata_IsPreserved()
    {
        var store = new InMemorySnapshotStore();
        var id = SnapshotId.New();
        var ts = new DateTimeOffset(2026, 7, 28, 12, 0, 0, TimeSpan.FromHours(-7));
        var snapshot = new SnapshotInfo(id, ts, SnapshotStatus.Current);

        store.ReplaceSnapshot(snapshot);

        var retrieved = store.GetCurrentSnapshot();
        Assert.Equal(id.Value, retrieved.Id.Value);
        Assert.Equal(ts, retrieved.CreatedAt);
    }

    [Fact]
    public async Task ThreadSafe_ConcurrentReads_NeverThrow()
    {
        var store = new InMemorySnapshotStore();

        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            var _ = store.GetCurrentSnapshot();
            store.ReplaceSnapshot(new SnapshotInfo(
                SnapshotId.New(),
                DateTimeOffset.Now,
                SnapshotStatus.Current));
        }));

        await Task.WhenAll(tasks); // Should not throw
    }
}
