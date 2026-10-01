using Kst.Domain.OpenOrders;

namespace Kst.Domain.Tests.OpenOrders;

public sealed class OpenOrderPlanningTests
{
    private static readonly OpenOrderEditableValues Original = new(null, null, null, null, 5m, 0.0125m);
    private static OpenOrderProposal Proposal(OpenOrderEditableValues? proposed = null, string? reason = "Quality") =>
        new(new("D", "SO-1", 1), "SW", "P", Original, proposed ?? Original with { OrderQty = 2m }, reason);
    private static OpenOrderLine Line() => new(new("D", "SO-1", 1), "P", "SW", null, null, 2m,
        Original, null, null, null, null, null, null, "", null, null, null, null, null,
        null, null, null, null, null, null, null, null, null, null, null);

    [Fact]
    public void EqualShippedAllowedRawPriceStillExtendsAndNoopIsValueBased()
    {
        var p = Proposal();
        Assert.Empty(OpenOrderPlanning.Issues(p, Line()));
        Assert.Equal(0m, OpenOrderPlanning.ProposedOpen(p, Line().ShippedQty));
        Assert.Equal(0m, OpenOrderPlanning.ProposedExtPrice(p, Line().ShippedQty));
        var price = Proposal(Original with { OrderQty = 5m, Price = 0.0125001m });
        Assert.Equal(0.0375003m, OpenOrderPlanning.ProposedExtPrice(price, Line().ShippedQty));
        Assert.False(OpenOrderPlanning.Changed(Proposal(Original with { OrderQty = 5.000m })));
    }

    [Fact]
    public void DetectsMissingChangedAndBelowShippedAndAbsentReason()
    {
        var p = Proposal(Original with { OrderQty = 1.9m }, null);
        Assert.Contains(OpenOrderPlanning.Issues(p, Line()), i => i.Contains("Shipped Qty"));
        Assert.Contains(OpenOrderPlanning.Issues(p, null), i => i.Contains("missing"));
        Assert.Contains(OpenOrderPlanning.Issues(p, Line() with { SourceValues = Original with { Price = 4m } }), i => i.Contains("original"));
        Assert.Contains(OpenOrderPlanning.Issues(p, Line()), i => i.Contains("Reason Code"));
    }
}
