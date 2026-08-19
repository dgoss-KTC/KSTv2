namespace Kst.Domain.Inventory;

/// <summary>
/// Shared Site + Part inventory summary using the accepted Stage 6 classification:
/// <see cref="NetQuantityOnHand"/> is positive, non-RMA, nettable inventory;
/// <see cref="NonNetQuantityOnHand"/> is positive, non-RMA, non-nettable inventory;
/// <see cref="RmaQuantityOnHand"/> is positive RMA (<c>RA%</c> lot) inventory, isolated from the two
/// QOH totals (RMA classification takes precedence over net/non-net classification).
/// All-zero values are an authoritative numeric zero (no qualifying inventory rows), not missing
/// data. Grain is Site + Part — not BOM occurrence, work order, MPS bucket, customer, or requirement.
/// Crosses the Kst.Integrations.Qad → Kst.Application boundary (analogous to
/// <see cref="Kst.Domain.Mps.MpsSourceRow"/>); QAD-shaped raw rows never do.
/// </summary>
public sealed record PartInventorySummary(
    string Site,
    string PartNumber,
    decimal NetQuantityOnHand,
    decimal NonNetQuantityOnHand,
    decimal RmaQuantityOnHand);
