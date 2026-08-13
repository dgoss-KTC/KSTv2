using Kst.Domain.WorkOrders;

namespace Kst.Domain.Tests.WorkOrders;

public sealed class WorkOrderMaterialLineTests
{
    private static WorkOrderMaterialLine Line(decimal required, decimal issued, bool isManufactured = false) =>
        new(ComponentPart: "COMP1", ComponentDescription: "Widget", RequiredQuantity: required, IssuedQuantity: issued, IsManufactured: isManufactured);

    [Theory]
    [InlineData(10, 10, 0)]
    [InlineData(10, 6, -4)]
    [InlineData(10, 15, 5)]
    public void VarianceQuantity_Is_Issued_Minus_Required(decimal required, decimal issued, decimal expectedVariance)
    {
        Assert.Equal(expectedVariance, Line(required, issued).VarianceQuantity);
    }

    [Theory]
    [InlineData(10, 10, 100)]
    [InlineData(10, 5, 50)]
    [InlineData(10, 15, 150)]
    public void IssuedPercent_Is_Issued_Divided_By_Required(decimal required, decimal issued, decimal expectedPercent)
    {
        Assert.Equal(expectedPercent, Line(required, issued).IssuedPercent);
    }

    [Fact]
    public void IssuedPercent_Is_Null_When_Required_Is_Zero()
    {
        Assert.Null(Line(required: 0, issued: 5).IssuedPercent);
    }

    [Theory]
    [InlineData(10, 10, true)]   // exact 100%
    [InlineData(10, 15, true)]   // over-issued
    [InlineData(10, 9, false)]   // under-issued
    [InlineData(10, 0, false)]
    public void IsFullyIssued_Counts_Exact_And_Over_Issued_As_Fully_Issued(decimal required, decimal issued, bool expected)
    {
        Assert.Equal(expected, Line(required, issued).IsFullyIssued);
    }

    [Fact]
    public void IsFullyIssued_Is_False_When_Required_Is_Zero()
    {
        Assert.False(Line(required: 0, issued: 0).IsFullyIssued);
    }

    [Theory]
    [InlineData(10, 9.5, WorkOrderIssueStatus.UnderIssuedException)]   // 95% boundary -> exception
    [InlineData(10, 9.0, WorkOrderIssueStatus.UnderIssuedException)]   // 90% -> exception
    [InlineData(10, 9.501, WorkOrderIssueStatus.WithinExpectedRange)]  // just above 95% -> within range
    [InlineData(10, 10.0, WorkOrderIssueStatus.WithinExpectedRange)]   // 100% -> within range
    [InlineData(10, 10.499, WorkOrderIssueStatus.WithinExpectedRange)] // just below 105% -> within range
    [InlineData(10, 10.5, WorkOrderIssueStatus.OverIssuedException)]  // 105% boundary -> exception
    [InlineData(10, 11.0, WorkOrderIssueStatus.OverIssuedException)]  // 110% -> exception
    public void IssueStatus_Applies_95_105_Thresholds(decimal required, decimal issued, WorkOrderIssueStatus expected)
    {
        Assert.Equal(expected, Line(required, issued).IssueStatus);
    }

    [Fact]
    public void IssueStatus_Is_Null_When_Required_Is_Zero()
    {
        Assert.Null(Line(required: 0, issued: 5).IssueStatus);
    }

    [Fact]
    public void IsManufactured_Flag_Passes_Through()
    {
        Assert.True(Line(10, 10, isManufactured: true).IsManufactured);
        Assert.False(Line(10, 10, isManufactured: false).IsManufactured);
    }
}
