using Kst.Domain.Common;

namespace Kst.Domain.Tests;

public sealed class SnapshotIdTests
{
    [Fact]
    public void New_Returns_Different_Values_Each_Call()
    {
        var id1 = SnapshotId.New();
        var id2 = SnapshotId.New();

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void ToString_Returns_Guid_String()
    {
        var id = SnapshotId.New();
        var str = id.ToString();

        Assert.True(Guid.TryParse(str, out _), "SnapshotId.ToString() should return a valid GUID string.");
    }

    [Fact]
    public void Equality_Is_Value_Based()
    {
        var guid = Guid.NewGuid();
        var id1 = new SnapshotId(guid);
        var id2 = new SnapshotId(guid);

        Assert.Equal(id1, id2);
    }
}
