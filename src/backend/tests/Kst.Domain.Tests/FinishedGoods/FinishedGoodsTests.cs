using Kst.Domain.FinishedGoods;

namespace Kst.Domain.Tests.FinishedGoods;

public sealed class FinishedGoodsTests
{
    [Theory]
    [InlineData("RA1", true)]
    [InlineData("ra2", true)]
    [InlineData("Ra3", true)]
    [InlineData("rA4", true)]
    [InlineData(" RA5", false)]
    [InlineData("XRA", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void InformationalRmaNeverRemovesOrChangesValue(string? lot, bool expected)
    {
        var row = new FinishedGoodsPosition("snapshot:1", new("KTC", "SW", "P", "ship", lot, "ref", 3.00001m, 1.23456m, false));
        Assert.Equal(expected, row.IsRma);
        Assert.Equal("SHIP", row.Location);
        Assert.Equal("ship", row.Source.Location);
        Assert.Equal(3.7036923456m, row.Value);
    }

    [Fact]
    public void ExactProductRejectsBothOverflowAndSilentRounding()
    {
        Assert.Throws<OverflowException>(() => FinishedGoodsDecimal.MultiplyExact(decimal.MaxValue, 2m));
        Assert.Throws<OverflowException>(() => FinishedGoodsDecimal.MultiplyExact(0.0000000000000000000000000001m, 0.1m));
        Assert.Equal(-2m, FinishedGoodsDecimal.MultiplyExact(2m, -1m));
        Assert.Equal(0.02m, FinishedGoodsDecimal.MultiplyExact(0.10000m, 0.2m));
    }

    [Theory]
    [InlineData("1e2")]
    [InlineData(" 1")]
    [InlineData("NaN")]
    [InlineData(".")]
    public void MalformedDecimalNeverBecomesZero(string text) => Assert.Throws<FormatException>(() => FinishedGoodsDecimal.ParseExact(text));

    [Fact]
    public void SourceDecimalCannotSilentlyLosePrecision()
    {
        Assert.Throws<OverflowException>(() => FinishedGoodsDecimal.ParseExact("0.00000000000000000000000000001"));
        Assert.Equal(1.23456m, FinishedGoodsDecimal.ParseExact("1.23456000"));
    }
}
