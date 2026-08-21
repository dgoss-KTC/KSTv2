namespace Kst.Application.ComponentDetail;

/// <summary>
/// Composed Stage 8D.5 Component Detail: <see cref="Kst.Domain.ComponentDetail.ComponentSourceFacts"/>
/// plus the shared Site + Part inventory summary (accepted 8D.1) and cache/freshness metadata,
/// mirroring how <c>Kst.Application.PartDetail.PartDetail</c> extends
/// <c>PartDetailSourceFacts</c>. Business grain is Site + ComponentPart. Deliberately excludes
/// P/M code, BOM occurrence identity, AVL, supplier/PO data, RMA, and requirement/coverage
/// fields — Component Detail is a standalone component reference/inventory/cost card, not a BOM
/// line or a component-MRP view.
/// </summary>
public sealed record ComponentDetail(
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
