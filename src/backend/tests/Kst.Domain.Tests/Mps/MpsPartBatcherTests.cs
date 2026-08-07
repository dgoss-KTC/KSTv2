using Kst.Domain.Mps;

namespace Kst.Domain.Tests.Mps;

public sealed class MpsPartBatcherTests
{
    [Fact]
    public void Batch_Empty_List_Returns_No_Batches()
    {
        var result = MpsPartBatcher.Batch([]);
        Assert.Empty(result);
    }

    [Fact]
    public void Batch_Under_Limit_Returns_Single_Batch()
    {
        var parts = Enumerable.Range(1, 10).Select(i => $"P{i}").ToList();
        var result = MpsPartBatcher.Batch(parts, maxBatchSize: 500);

        Assert.Single(result);
        Assert.Equal(10, result[0].Count);
    }

    [Fact]
    public void Batch_Exactly_At_Limit_Returns_Single_Batch()
    {
        var parts = Enumerable.Range(1, 500).Select(i => $"P{i}").ToList();
        var result = MpsPartBatcher.Batch(parts, maxBatchSize: 500);

        Assert.Single(result);
        Assert.Equal(500, result[0].Count);
    }

    [Fact]
    public void Batch_Over_Limit_Splits_Into_Two_Batches()
    {
        var parts = Enumerable.Range(1, 501).Select(i => $"P{i}").ToList();
        var result = MpsPartBatcher.Batch(parts, maxBatchSize: 500);

        Assert.Equal(2, result.Count);
        Assert.Equal(500, result[0].Count);
        Assert.Single(result[1]);
    }

    [Fact]
    public void Batch_Preserves_Order_Across_Batches()
    {
        var parts = Enumerable.Range(1, 1200).Select(i => $"P{i}").ToList();
        var result = MpsPartBatcher.Batch(parts, maxBatchSize: 500);

        var flattened = result.SelectMany(b => b).ToList();
        Assert.Equal(parts, flattened);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Batch_Rejects_NonPositive_MaxBatchSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MpsPartBatcher.Batch(["A"], maxBatchSize: 0));
    }
}
