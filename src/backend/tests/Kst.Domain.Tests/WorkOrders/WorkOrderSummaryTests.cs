using Kst.Domain.WorkOrders;

namespace Kst.Domain.Tests.WorkOrders;

public sealed class WorkOrderSummaryTests
{
    private static WorkOrderSummary Summary(decimal ordered, decimal completed) => new(
        PartNumber: "ABC100",
        Woid: "12345",
        Status: "R",
        OrderedQuantity: ordered,
        CompletedQuantity: completed,
        ReleaseDate: new DateOnly(2026, 8, 1),
        DueDate: new DateOnly(2026, 8, 15),
        Kitting: KittingSummary.Calculate(0, 0));

    [Theory]
    [InlineData(100, 0, 100)]
    [InlineData(100, 40, 60)]
    [InlineData(100, 100, 0)]
    [InlineData(100, 120, -20)]
    public void OpenQuantity_Is_Ordered_Minus_Completed(decimal ordered, decimal completed, decimal expectedOpen)
    {
        Assert.Equal(expectedOpen, Summary(ordered, completed).OpenQuantity);
    }
}
