namespace Kst.Domain.WorkOrders;

/// <summary>
/// One Stage 7 work-order card's normalized business data (accepted contract §7/§15). <see cref="Woid"/>
/// (<c>wo_mstr.wo_lot</c>) is the scheduler-facing work-order identity; Work Order Number is
/// deliberately not part of this model (not unique, not user-facing for this workflow).
/// </summary>
/// <remarks>
/// <see cref="Status"/> is the raw QAD <c>wo_mstr.wo_status</code> code (trimmed), not a closed
/// enum. Stage 7R widens the top-level planning population to every non-closed work order, so a
/// previously unseen non-closed code must render safely rather than fail normalization. Known
/// codes (A/F/R) receive friendly presentation labels at the API boundary; any other non-closed
/// code passes through as its raw value. Closed ('C') and RMABOM work orders are excluded at the
/// QAD query boundary and never reach this model.
/// </remarks>
public sealed record WorkOrderSummary(
    string PartNumber,
    string Woid,
    string Status,
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
