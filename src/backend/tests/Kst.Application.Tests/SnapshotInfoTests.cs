using Kst.Application.Snapshots;
using Kst.Domain.Common;
using Kst.Domain.Snapshots;

namespace Kst.Application.Tests;

public sealed class SnapshotInfoTests
{
    [Fact]
    public void None_Has_NotLoaded_Status()
    {
        Assert.Equal(SnapshotStatus.NotLoaded, SnapshotInfo.None.Status);
        Assert.False(SnapshotInfo.None.IsAvailable);
    }

    [Fact]
    public void Loaded_Snapshot_IsAvailable()
    {
        var snapshot = new SnapshotInfo(
            SnapshotId.New(),
            DateTimeOffset.Now,
            SnapshotStatus.Loaded
        );

        Assert.True(snapshot.IsAvailable);
    }

    [Fact]
    public void Failed_Snapshot_IsNotAvailable()
    {
        var snapshot = new SnapshotInfo(
            SnapshotId.New(),
            DateTimeOffset.Now,
            SnapshotStatus.Failed
        );

        Assert.False(snapshot.IsAvailable);
    }
}
