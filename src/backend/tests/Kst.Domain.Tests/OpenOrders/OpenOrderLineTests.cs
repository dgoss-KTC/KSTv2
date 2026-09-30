using Kst.Domain.OpenOrders;

namespace Kst.Domain.Tests.OpenOrders;

public sealed class OpenOrderLineTests
{
    [Fact]
    public void DerivedValuesUseExactRawDecimalEvenForConsignment()
    {
        var line = MakeLine(true);
        Assert.Equal(3m, line.Open);
        Assert.Equal(0m, line.UnitPrice);
        Assert.Equal(0.0375m, line.ExtPrice);
        Assert.Equal(0.0125m, line.SourceValues.Price);
        Assert.Equal(5m, line.SourceValues.OrderQty);
        Assert.Equal(2m, line.ShippedQty);
        Assert.Equal("SP", line.Salesperson);
    }

    [Fact]
    public void NullAndNegativeInventoryAreNotRecastAsZero()
    {
        Assert.Null(MakeLine(false).SiteQoh);
        Assert.Equal(-3m, (MakeLine(false) with { SiteQoh = -3m }).SiteQoh);
        Assert.Equal(0.0125m, MakeLine(false).UnitPrice);
    }

    private static OpenOrderLine MakeLine(bool consignment) => new(
        new("TEST", "SO-1", 1), "P-1", "S1", null, null, 2m,
        new(null, null, null, null, 5m, 0.0125m), null, null, null, "SP",
        null, null, "", null, null, null, null, null, null, null, null,
        null, null, null, null, null, null, null, consignment);
}
