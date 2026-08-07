namespace Kst.Domain.Mps;

/// <summary>
/// Minimal internal work-order reference retained on a bucket to explain its derived state and to
/// provide a stable handoff point for later drill-down work. Not necessarily exposed publicly.
/// </summary>
public sealed record MpsWorkOrderRef(
    string WorkOrderId,
    MpsWorkOrderState State
);
