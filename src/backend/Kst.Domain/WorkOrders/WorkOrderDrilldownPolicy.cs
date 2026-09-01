namespace Kst.Domain.WorkOrders;

/// <summary>
/// Stage 7 drill-down policy constants (accepted contract §13/§14), kept as named values so they can
/// change later without restructuring reader/orchestration/UI code.
/// </summary>
public static class WorkOrderDrilldownPolicy
{
    /// <summary>Maximum work-order investigation depth: scheduled parent, then two candidate levels.</summary>
    public const int MaxDrillDepth = 3;
}
