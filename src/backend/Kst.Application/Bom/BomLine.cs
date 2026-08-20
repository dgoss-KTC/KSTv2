namespace Kst.Application.Bom;

/// <summary>
/// One scheduler-visible BOM presentation line: an accepted 8D.2 structural
/// <see cref="Kst.Domain.Bom.BomOccurrence"/> composed with the accepted 8D.1 shared
/// <see cref="Kst.Domain.Inventory.PartInventorySummary"/> (Site + Part grain) for that
/// occurrence's component. Grain is the structural occurrence — repeated occurrences of the
/// same component are separate lines that deliberately repeat the same Site + Part inventory
/// values (one shared inventory pool, never independent pools).
///
/// Deliberately absent: RMA QOH, Extended Requirement, Incoming Supply, Coverage %, Material
/// Status, Short Qty, Projected QOH, PO/MRP quantities — Stage 8 is informational and no
/// requirement math belongs in 8D.3.
///
/// <see cref="Level"/> is the actual structural level from the traversal, preserved through
/// hidden (non-P/M) intermediates — gaps are intentional and are never cosmetically
/// renumbered. <see cref="OccurrenceKey"/> is the opaque expanded-occurrence identity; line
/// order is the structural traversal order restricted to scheduler-visible rows and is never
/// re-sorted.
/// </summary>
public sealed record BomLine(
    string OccurrenceKey,
    int Level,
    string ComponentPart,
    string? PmCode,
    bool IsPhantom,
    string? Description,
    decimal? QuantityPer,
    decimal? ScrapPercentage,
    decimal NetQuantityOnHand,
    decimal NonNetQuantityOnHand);
