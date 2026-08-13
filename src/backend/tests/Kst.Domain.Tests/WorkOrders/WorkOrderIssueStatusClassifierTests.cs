using Kst.Domain.WorkOrders;

namespace Kst.Domain.Tests.WorkOrders;

public sealed class WorkOrderIssueStatusClassifierTests
{
    [Theory]
    [InlineData(0, WorkOrderIssueStatus.UnderIssuedException)]
    [InlineData(94.99, WorkOrderIssueStatus.UnderIssuedException)]
    [InlineData(95, WorkOrderIssueStatus.UnderIssuedException)]
    [InlineData(95.01, WorkOrderIssueStatus.WithinExpectedRange)]
    [InlineData(100, WorkOrderIssueStatus.WithinExpectedRange)]
    [InlineData(104.99, WorkOrderIssueStatus.WithinExpectedRange)]
    [InlineData(105, WorkOrderIssueStatus.OverIssuedException)]
    [InlineData(150, WorkOrderIssueStatus.OverIssuedException)]
    public void Classify_Applies_Accepted_Thresholds(decimal issuedPercent, WorkOrderIssueStatus expected)
    {
        Assert.Equal(expected, WorkOrderIssueStatusClassifier.Classify(issuedPercent));
    }
}
