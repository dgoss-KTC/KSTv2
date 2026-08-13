namespace Kst.Domain.WorkOrders;

/// <summary>
/// One Stage 7 work-order card's normalized business data (accepted contract §7/§15). <see cref="Woid"/>
/// (<c>wo_mstr.wo_lot</c>) is the scheduler-facing work-order identity; Work Order Number is
/// deliberately not part of this model (not unique, not user-facing for this workflow).
/// </summary>
public sealed record WorkOrderSummary(
    string PartNumber,
    string Woid,
    WorkOrderStatus Status,
    decimal OrderedQuantity,
    decimal CompletedQuantity,
    DateOnly? ReleaseDate,
    DateOnly? DueDate,
    KittingSummary Kitting,
    string? SalesOrder = null
)
{
    public decimal OpenQuantity => OrderedQuantity - CompletedQuantity;
}
