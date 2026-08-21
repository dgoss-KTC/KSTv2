namespace Kst.Api.Dtos;

/// <summary>
/// Stage 8D.5 Component Detail for one workspace-selected component part. <see cref="IsStale"/>/
/// <see cref="Warning"/> report same-site stale last-good service after a failed reload. All
/// values are semantic/typed (no formatted strings) — frontend formatting belongs to Stage 8D.6.
/// Deliberately excludes P/M code, BOM occurrence identity, AVL, supplier/PO data, RMA, and
/// requirement/coverage fields.
/// </summary>
public sealed record ComponentDetailResponseDto(
    string Site,
    string ComponentPart,
    string? Description,
    string? PartStatusCode,
    string? PartStatusDescription,
    string? IosCode,
    decimal NetQuantityOnHand,
    decimal NonNetQuantityOnHand,
    decimal? StandardCost,
    decimal? Qctc,
    int? TimeFence,
    decimal? SafetyTime,
    decimal? SafetyStock,
    string? BuyerPlanner,
    int? PurchaseLeadTimeDays,
    int? InspectionLeadTimeDays,
    int? CumulativeLeadTimeDays,
    decimal? MinimumOrderQuantity,
    decimal? OrderMultiple,
    DateTimeOffset LoadedAtUtc,
    bool IsStale,
    string? Warning);
