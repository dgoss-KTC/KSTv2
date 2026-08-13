namespace Kst.Domain.WorkOrders;

/// <summary>
/// Stage 7 drill-down policy constants (accepted contract §13/§14), kept as named values so they can
/// change later without restructuring reader/orchestration/UI code. Shared by
/// <c>Kst.Integrations.Qad</c> (candidate query fetch size) and <c>Kst.Application</c> (depth
/// enforcement).
/// </summary>
public static class WorkOrderDrilldownPolicy
{
    /// <summary>Initial candidate subassembly work-order display limit.</summary>
    public const int CandidateResultLimit = 10;

    /// <summary>Maximum work-order investigation depth: scheduled parent, then two candidate levels.</summary>
    public const int MaxDrillDepth = 3;
}
