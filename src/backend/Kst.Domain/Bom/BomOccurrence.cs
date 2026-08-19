namespace Kst.Domain.Bom;

/// <summary>
/// One expanded structural BOM occurrence in proven depth-first traversal order — a single
/// parent→child relationship at one position in the current-effective multi-level BOM of a
/// parent part. Grain is the <b>expanded occurrence</b>, not the physical relationship: the
/// same physical BOM relationship reached through different structural paths yields distinct
/// occurrences with distinct <see cref="OccurrenceKey"/> values, repeated components under one
/// parent are separate occurrences, and the same component at different levels is separate at
/// each level. Nothing is consolidated, aggregated, or DISTINCTed.
///
/// <see cref="OccurrenceKey"/> is an opaque, deterministic identity for the expanded
/// occurrence. Consumers must not parse it or derive ordering from it; sibling/traversal
/// order is the order of the collection, and Level is <see cref="Level"/>.
///
/// <see cref="Level"/> is the actual structural level (1-based) produced by the traversal and
/// is preserved through hidden (non-P/M) intermediate rows — never cosmetically renumbered.
///
/// <see cref="PmCode"/> carries the effective P/M classification (selected-site site-specific
/// value, fallback to the part-master value, P/M classification only). It is any source code
/// (P, M, or known non-P/M codes such as 2, 3, 4, C, D, N, S) — selecting scheduler-visible
/// P/M rows is Application-owned (Stage 8D.3), not done here.
///
/// <see cref="QuantityPer"/> and <see cref="ScrapPercentage"/> are relationship/occurrence
/// level values carried verbatim — never multiplied through the hierarchy, never turned into
/// requirement calculations.
///
/// Deliberately absent: Net/Non-Net/RMA QOH, Extended Requirement, Incoming Supply, Coverage,
/// Material Status, Short Quantity, Projected QOH — inventory grain is Site + Part
/// (<see cref="Kst.Domain.Inventory.PartInventorySummary"/>) and is composed later in Stage
/// 8D.3; repeated occurrences may legitimately show repeated inventory values then.
///
/// Crosses the Kst.Integrations.Qad → Kst.Application boundary (analogous to
/// <see cref="Kst.Domain.Inventory.PartInventorySummary"/>); QAD-shaped raw rows never do.
/// </summary>
public sealed record BomOccurrence(
    string OccurrenceKey,
    int Level,
    string ComponentPart,
    string? PmCode,
    bool IsPhantom,
    string? Description,
    decimal? QuantityPer,
    decimal? ScrapPercentage);
