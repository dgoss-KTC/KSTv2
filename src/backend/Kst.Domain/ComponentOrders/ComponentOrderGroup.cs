namespace Kst.Domain.ComponentOrders;

/// <summary>
/// One component's qualifying conventional open PO lines, composed for the Stage 10 Component
/// Orders presentation. <see cref="DisplayLine"/> is the group's earliest qualifying line under
/// the accepted ordering (missing due dates first, then due date ascending, then PO number, then
/// line — the same ordering Stage 9 uses to select the next PO); it backs the collapsed component
/// row. <see cref="AdditionalLines"/> holds the remaining qualifying lines in the same order; a
/// non-empty collection is what makes the expand arrow appear. Child rows repeat the group's
/// <see cref="ComponentPart"/> and show their own line values.
/// </summary>
public sealed record ComponentOrderGroup(
    string ComponentPart,
    ComponentOrderLine DisplayLine,
    IReadOnlyList<ComponentOrderLine> AdditionalLines);
