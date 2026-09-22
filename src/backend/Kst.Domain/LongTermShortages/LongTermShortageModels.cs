namespace Kst.Domain.LongTermShortages;

public enum LongTermShortageSeverity { None, SafetyStockShort, CriticalShort, SafetyStockUnavailable }
public enum SafetyStockState { Resolved, SelectedSiteValueMissing }
public enum MrpScheduleCategory { Unclassified, GrossRequirement, ScheduledReceipt, PlannedOrderDue, PlannedOrderRelease }

public sealed record LongTermMrpFact(
    int EvidenceOrdinal,
    string? Type,
    DateOnly? DueDate,
    DateOnly? ReleaseDate,
    decimal Quantity,
    MrpScheduleCategory Category)
{
    public bool IsPlannedOrderReleaseEvidence => string.Equals(Type?.Trim(), "SUPPLYP", StringComparison.OrdinalIgnoreCase) && ReleaseDate is not null;
}

public sealed record LongTermShortageBucket(
    int? WeekNumber,
    DateOnly? WeekStart,
    decimal GrossRequirements,
    decimal ScheduledReceipts,
    decimal PlannedOrdersDue,
    decimal PlannedOrdersRelease,
    decimal ProjectedQoh,
    LongTermShortageSeverity Severity);

/// <summary>
/// Informational purchasing context kept separate from the raw-MRP schedule. These values never
/// participate in projected-QOH arithmetic.
/// </summary>
public sealed record LongTermShortagePresentationContext(
    string? ManufacturerItem,
    string? PoNumber,
    int? PoLine,
    DateOnly? PoDueDate,
    decimal? PoOpenQuantity,
    bool? PoConfirmed,
    bool IsKss);

public sealed record LongTermShortageRow(
    string ComponentPart,
    string? UnitOfMeasure,
    string? QadStatus,
    string? Description,
    string? Planner,
    string? BuyerPlannerCode,
    decimal OpeningQoh,
    SafetyStockState SafetyStockState,
    decimal? SafetyStock,
    LongTermShortageSeverity Severity,
    DateOnly? FirstShortDate,
    IReadOnlyList<string> DemandParentParts,
    LongTermShortageBucket Past,
    IReadOnlyList<LongTermShortageBucket> Weeks,
    IReadOnlyList<LongTermMrpFact> Evidence,
    LongTermShortagePresentationContext? Presentation = null)
{
    public bool HasShortage => Severity is LongTermShortageSeverity.SafetyStockShort or LongTermShortageSeverity.CriticalShort;
}

public sealed record LongTermShortageInput(
    string ComponentPart,
    string? UnitOfMeasure,
    string? QadStatus,
    string? Description,
    string? Planner,
    string? BuyerPlannerCode,
    decimal OpeningQoh,
    SafetyStockState SafetyStockState,
    decimal? SafetyStock,
    IReadOnlyList<string> DemandParentParts,
    IReadOnlyList<LongTermMrpFact> Evidence,
    LongTermShortagePresentationContext? Presentation = null);
