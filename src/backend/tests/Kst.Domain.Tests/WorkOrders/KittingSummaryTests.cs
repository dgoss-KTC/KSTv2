using Kst.Domain.WorkOrders;

namespace Kst.Domain.Tests.WorkOrders;

public sealed class KittingSummaryTests
{
    [Fact]
    public void Calculate_With_Zero_Applicable_Lines_Returns_Null_Percent()
    {
        var summary = KittingSummary.Calculate(0, 0);

        Assert.Equal(0, summary.ApplicableLineCount);
        Assert.Equal(0, summary.FullyIssuedLineCount);
        Assert.Null(summary.KittingPercent);
    }

    [Fact]
    public void Calculate_Partial_Kitting_Computes_Expected_Percent()
    {
        var summary = KittingSummary.Calculate(applicableLineCount: 4, fullyIssuedLineCount: 1);

        Assert.Equal(25m, summary.KittingPercent);
    }

    [Fact]
    public void Calculate_Full_Kitting_Returns_100_Percent()
    {
        var summary = KittingSummary.Calculate(applicableLineCount: 5, fullyIssuedLineCount: 5);

        Assert.Equal(100m, summary.KittingPercent);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(2, 3)]
    public void Calculate_Rejects_Invalid_Counts(int applicable, int fullyIssued)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => KittingSummary.Calculate(applicable, fullyIssued));
    }

    private static WorkOrderMaterialLine Line(decimal required, decimal issued) =>
        new(ComponentPart: "COMP1", ComponentDescription: "Widget", RequiredQuantity: required, IssuedQuantity: issued, IsManufactured: false);

    [Fact]
    public void FromMaterialLines_Counts_Exact_100_Percent_Line_As_Fully_Issued()
    {
        var summary = KittingSummary.FromMaterialLines([Line(required: 10, issued: 10)]);

        Assert.Equal(1, summary.ApplicableLineCount);
        Assert.Equal(1, summary.FullyIssuedLineCount);
        Assert.Equal(100m, summary.KittingPercent);
    }

    [Fact]
    public void FromMaterialLines_Counts_OverIssued_Line_As_Exactly_One_Fully_Issued_Line()
    {
        var summary = KittingSummary.FromMaterialLines([Line(required: 10, issued: 25)]);

        Assert.Equal(1, summary.ApplicableLineCount);
        Assert.Equal(1, summary.FullyIssuedLineCount);
        Assert.Equal(100m, summary.KittingPercent);
    }

    [Fact]
    public void FromMaterialLines_Mixed_Lines_Compute_LineBased_Not_QuantityWeighted_Percent()
    {
        // 3 applicable lines, 1 fully issued (33.33...%): quantity-weighted math would give a different result.
        var summary = KittingSummary.FromMaterialLines(
        [
            Line(required: 10, issued: 10),
            Line(required: 100, issued: 1),
            Line(required: 5, issued: 4)
        ]);

        Assert.Equal(3, summary.ApplicableLineCount);
        Assert.Equal(1, summary.FullyIssuedLineCount);
        Assert.Equal(1m / 3m * 100m, summary.KittingPercent);
    }
}
