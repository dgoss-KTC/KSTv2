namespace Kst.Domain.LongTermShortages;

public enum LongTermShortageSeverity { Healthy, SafetyStockShort, CriticalShort, FutureShort, SafetyStockUnavailable }
public enum SafetyStockState { Resolved, SelectedSiteValueMissing }
public enum MrpScheduleCategory { Unclassified, GrossRequirement, ScheduledReceipt, PlannedOrderDue, PlannedOrderRelease }

public sealed record LongTermShortageAcquisition(DateTimeOffset AcquiredAtUtc, IReadOnlyList<LongTermShortageInput> Inputs)
{
    public const string ConsistencyMode = "PRO2_READ_UNCOMMITTED";
}

public sealed record LongTermMrpFact(
    int EvidenceOrdinal,
    string? Type,
    DateOnly? DueDate,
    DateOnly? ReleaseDate,
    decimal Quantity,
    MrpScheduleCategory Category)
{
    public bool IsPlannedOrderReleaseEvidence => string.Equals(Type?.Trim(), "SUPPLYP", StringComparison.OrdinalIgnoreCase) && ReleaseDate is not null;
    public string? SourceNumber { get; init; }
    public string? SourceLine { get; init; }
    public string? SourceLine2 { get; init; }
    public string? SourceRowId { get; init; }
    public bool? IsPoReceipt { get; init; }
    public bool? PoConfirmed { get; init; }
}

public sealed record LongTermShortageEpisode(DateOnly StartDate, DateOnly DeepestDate, decimal MaximumShortage, DateOnly? FirstRecoveryDate, DateOnly? StableClearDate);

public static class LongTermQuantityPresentation
{
    public static int DecimalPlaces(string? unitOfMeasure) => unitOfMeasure?.Trim().ToUpperInvariant() is "BX" or "EA" or "PK" ? 0 : 2;
    public static decimal Round(decimal value, string? unitOfMeasure) => Math.Round(value, DecimalPlaces(unitOfMeasure), MidpointRounding.AwayFromZero);
}

public sealed record LongTermShortageBucket(
    int? WeekNumber,
    DateOnly? WeekStart,
    decimal GrossRequirements,
    decimal ScheduledReceipts,
    decimal PlannedOrdersDue,
    decimal PlannedOrdersRelease,
    decimal ProjectedQoh,
    LongTermShortageSeverity Severity)
{
    public decimal UnconfirmedReceipts { get; init; }
    public decimal OverdueReceipts { get; init; }
    public decimal ConfirmedEnding { get; init; }
    public decimal AllReceiptsEnding { get; init; }
    public decimal PlanningEnding { get; init; }
    public decimal AllReceiptsPlanningEnding { get; init; }
    public decimal LowestProjectedBalance { get; init; }
    public bool IncludesUnconfirmed { get; init; }
    public decimal LowestConfirmedBalance { get; init; }
    public decimal LowestAllReceiptsBalance { get; init; }
}

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
    public bool HasShortage => Severity is LongTermShortageSeverity.SafetyStockShort or LongTermShortageSeverity.CriticalShort or LongTermShortageSeverity.FutureShort;
    public IReadOnlyList<LongTermShortageEpisode> Episodes { get; init; } = [];
    public string? DataQualityWarning { get; init; }
    public DateOnly? FirstAtRiskDate { get; init; }
    public string? EffectivePmCode { get; init; }
    public string? PartStatusDescription { get; init; }
    public int? OrderPeriodDays { get; init; }
    public decimal? SafetyTimeWorkingDays { get; init; }
    public int? ManufacturingLeadWorkingDays { get; init; }
    public int? PurchasingLeadCalendarDays { get; init; }
    public int? CumulativeLeadCalendarDays { get; init; }
    public bool? SitePlanningPresent { get; init; }
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
    LongTermShortagePresentationContext? Presentation = null)
{
    public string? EffectivePmCode { get; init; }
    public int? OrderPeriodDays { get; init; }
    public decimal? SafetyTimeWorkingDays { get; init; }
    public int? ManufacturingLeadWorkingDays { get; init; }
    public int? PurchasingLeadCalendarDays { get; init; }
    public int? CumulativeLeadCalendarDays { get; init; }
    public bool? SitePlanningPresent { get; init; }
}
