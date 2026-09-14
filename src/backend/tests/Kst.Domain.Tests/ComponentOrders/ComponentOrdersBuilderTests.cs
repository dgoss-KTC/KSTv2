using Kst.Domain.ComponentOrders;

namespace Kst.Domain.Tests.ComponentOrders;

public class ComponentOrdersBuilderTests
{
    private static ComponentOrderLine Line(
        string part,
        DateOnly? dueDate = null,
        string poNumber = "2076185",
        int poLine = 1) => new(
        ComponentPart: part,
        Description: "Desc",
        LeadTimeDays: 14,
        PoNumber: poNumber,
        PoLine: poLine,
        DueDate: dueDate,
        OpenQuantity: 10m,
        Confirmed: true,
        SupplierDisplay: "ACME",
        BuyerDisplay: null,
        ManufacturerItem: null,
        IsKss: false,
        TrackingInfo: null);

    [Fact]
    public void BuildGroups_EmptyInput_ReturnsNoGroups() =>
        Assert.Empty(ComponentOrdersBuilder.BuildGroups([]));

    [Fact]
    public void BuildGroups_SingleLine_IsDisplayLineWithNoAdditionalLines()
    {
        var line = Line("ABC");
        var groups = ComponentOrdersBuilder.BuildGroups([line]);

        var group = Assert.Single(groups);
        Assert.Equal("ABC", group.ComponentPart);
        Assert.Same(line, group.DisplayLine);
        Assert.Empty(group.AdditionalLines);
    }

    [Fact]
    public void BuildGroups_GroupsCaseInsensitively_AndKeepsDisplayLineCasing()
    {
        var upper = Line("ABC", dueDate: null, poNumber: "100");
        var lower = Line("abc", dueDate: new DateOnly(2026, 10, 1), poNumber: "200");

        var groups = ComponentOrdersBuilder.BuildGroups([lower, upper]);

        var group = Assert.Single(groups);
        // The display line is the earliest under the accepted ordering (missing due first), so its casing wins.
        Assert.Equal("ABC", group.ComponentPart);
        Assert.Same(upper, group.DisplayLine);
        Assert.Same(lower, Assert.Single(group.AdditionalLines));
    }

    [Fact]
    public void BuildGroups_DisplayLine_IsEarliestDueWithMissingDatesFirst()
    {
        var dated = Line("ABC", dueDate: new DateOnly(2026, 9, 15), poNumber: "100");
        var missing = Line("ABC", dueDate: null, poNumber: "200");

        var groups = ComponentOrdersBuilder.BuildGroups([dated, missing]);

        Assert.Same(missing, Assert.Single(groups).DisplayLine);
    }

    [Fact]
    public void BuildGroups_TiesBreakByPoNumberThenLine()
    {
        var due = new DateOnly(2026, 9, 15);
        var highPoLowLine = Line("ABC", dueDate: due, poNumber: "300", poLine: 1);
        var lowPoHighLine = Line("ABC", dueDate: due, poNumber: "200", poLine: 9);
        var lowPoLowLine = Line("ABC", dueDate: due, poNumber: "200", poLine: 2);

        var group = Assert.Single(ComponentOrdersBuilder.BuildGroups([highPoLowLine, lowPoHighLine, lowPoLowLine]));

        Assert.Same(lowPoLowLine, group.DisplayLine);
        Assert.Equal([lowPoHighLine, highPoLowLine], group.AdditionalLines);
    }

    [Fact]
    public void BuildGroups_AdditionalLines_AreOrderedAfterTheDisplayLine()
    {
        var first = Line("ABC", dueDate: new DateOnly(2026, 9, 1));
        var second = Line("ABC", dueDate: new DateOnly(2026, 9, 3), poNumber: "400");
        var third = Line("ABC", dueDate: null, poNumber: "500");

        var group = Assert.Single(ComponentOrdersBuilder.BuildGroups([first, second, third]));

        // Missing-due line is earliest under the accepted ordering.
        Assert.Same(third, group.DisplayLine);
        Assert.Equal([first, second], group.AdditionalLines);
    }

    [Fact]
    public void BuildGroups_SortsMissingDueDateGroupsFirst_AsYellowExceptions()
    {
        var missingA = Line("AAA", dueDate: null, poNumber: "100");
        var missingB = Line("BBB", dueDate: null, poNumber: "200");
        var datedEarly = Line("CCC", dueDate: new DateOnly(2026, 9, 1));
        var datedLate = Line("DDD", dueDate: new DateOnly(2026, 12, 31));

        var groups = ComponentOrdersBuilder.BuildGroups([datedLate, missingB, datedEarly, missingA]);

        Assert.Equal(["AAA", "BBB", "CCC", "DDD"], groups.Select(g => g.ComponentPart).ToList());
    }

    [Fact]
    public void BuildGroups_SortsDatedGroupsEarliestToLatest_WithPoLineTiebreak()
    {
        var late = Line("AAA", dueDate: new DateOnly(2026, 10, 1), poNumber: "900");
        var earlyB = Line("BBB", dueDate: new DateOnly(2026, 9, 5), poNumber: "300");
        var earlyA = Line("CCC", dueDate: new DateOnly(2026, 9, 5), poNumber: "200");

        var groups = ComponentOrdersBuilder.BuildGroups([late, earlyB, earlyA]);

        Assert.Equal(["CCC", "BBB", "AAA"], groups.Select(g => g.ComponentPart).ToList());
    }
}
