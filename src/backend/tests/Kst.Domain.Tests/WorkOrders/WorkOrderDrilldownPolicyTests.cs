using Kst.Domain.WorkOrders;

namespace Kst.Domain.Tests.WorkOrders;

public sealed class WorkOrderDrilldownPolicyTests
{
    [Fact]
    public void CandidateResultLimit_Is_Ten()
    {
        Assert.Equal(10, WorkOrderDrilldownPolicy.CandidateResultLimit);
    }

    [Fact]
    public void MaxDrillDepth_Is_Three()
    {
        Assert.Equal(3, WorkOrderDrilldownPolicy.MaxDrillDepth);
    }
}
