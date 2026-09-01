using Kst.Domain.WorkOrders;

namespace Kst.Domain.Tests.WorkOrders;

public sealed class WorkOrderDrilldownPolicyTests
{
    [Fact]
    public void MaxDrillDepth_Is_Three()
    {
        Assert.Equal(3, WorkOrderDrilldownPolicy.MaxDrillDepth);
    }
}
