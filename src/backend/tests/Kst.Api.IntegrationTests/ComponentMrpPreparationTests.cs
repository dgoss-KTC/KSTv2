namespace Kst.Api.IntegrationTests;

public class ComponentMrpPreparationTests
{
    [Fact]
    public void Ownership_copy_latest_and_capacity_are_enforced()
    {
        var store = new ComponentMrpPreparations();
        var owner = Guid.NewGuid().ToString(); var original = new byte[] { 1, 2, 3 };
        var first = store.Add(owner, original); original[0] = 9;
        Assert.Equal(new byte[] { 1, 2, 3 }, store.Get(first.Id, owner));
        Assert.Null(store.Get(first.Id, Guid.NewGuid().ToString()));
        var returned = store.Get(first.Id, owner)!; returned[0] = 8;
        Assert.Equal(1, store.Get(first.Id, owner)![0]);
        var second = store.Add(owner, [4]); Assert.Null(store.Get(first.Id, owner));
        Assert.Equal(new byte[] { 4 }, store.Get(second.Id, owner));
        Assert.Throws<InvalidOperationException>(() => store.Add(owner, new byte[ComponentMrpPreparations.MaxBytes + 1]));
        for (var i = 0; i < 7; i++) store.Add(Guid.NewGuid().ToString(), [1]);
        Assert.Throws<InvalidOperationException>(() => store.Add(Guid.NewGuid().ToString(), [1]));
    }
}
