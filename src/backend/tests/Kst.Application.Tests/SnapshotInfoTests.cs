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
    public void Current_Snapshot_IsAvailable()
    {
        var snapshot = new SnapshotInfo(
            SnapshotId.New(),
            DateTimeOffset.Now,
            SnapshotStatus.Current
        );

        Assert.True(snapshot.IsAvailable);
    }

    [Theory]
    [InlineData(SnapshotStatus.Current)]
    [InlineData(SnapshotStatus.Stale)]
    [InlineData(SnapshotStatus.Partial)]
    public void DataBearing_Statuses_AreAvailable(SnapshotStatus status)
    {
        var snapshot = new SnapshotInfo(SnapshotId.New(), DateTimeOffset.Now, status);

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
