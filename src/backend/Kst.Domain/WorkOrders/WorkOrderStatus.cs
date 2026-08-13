namespace Kst.Domain.WorkOrders;

/// <summary>
/// Normalized Stage 7 work-order status. Only Allocating/Frozen/Released work orders are eligible
/// for a Stage 7 work-order card (accepted contract §5); Planned, explicitly-scheduled, closed, and
/// any other raw QAD status never reach this model. Raw-code normalization/filtering happens at the
/// <c>Kst.Integrations.Qad</c> boundary, not here.
/// </summary>
public enum WorkOrderStatus
{
    Allocating,
    Frozen,
    Released
}
