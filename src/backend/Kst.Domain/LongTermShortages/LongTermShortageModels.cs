namespace Kst.Domain.LongTermShortages;

public enum LongTermShortageSeverity
{
    None,
    SafetyStockShort,
    CriticalShort,
    SafetyStockUnavailable
}

public enum SafetyStockState
{
    Resolved,
    SelectedSiteValueMissing
}

public sealed record LongTermShortageWeek(
    int WeekNumber,
    DateOnly WeekStart,
    decimal WorkOrderDemand,
    decimal ForecastDemand,
    decimal PurchaseOrderSupply,
    decimal Balance,
    LongTermShortageSeverity Severity);

public sealed record LongTermPurchaseOrder(
    string PoNumber,
    int PoLine,
    DateOnly? DueDate,
    decimal OpenQuantity,
    bool? Confirmed,
    string? ManufacturerItem,
    bool IsScheduled);

public sealed record LongTermShortageRow(
    string ComponentPart,
    string? UnitOfMeasure,
    string? QadStatus,
    string? Description,
    bool IsKss,
    int? LeadTimeWeeks,
    string? Planner,
    string? BuyerPlannerCode,
    decimal OpeningQoh,
    SafetyStockState SafetyStockState,
    decimal? SafetyStock,
    LongTermShortageSeverity Severity,
    int? FirstSafetyStockShortWeek,
    int? FirstCriticalShortWeek,
    IReadOnlyList<string> DemandParentParts,
    IReadOnlyList<string> OtherProgramParentParts,
    IReadOnlyList<LongTermPurchaseOrder> PurchaseOrders,
    IReadOnlyList<LongTermShortageWeek> Weeks)
{
    public bool HasShortage => Severity is LongTermShortageSeverity.SafetyStockShort or LongTermShortageSeverity.CriticalShort;
}

public sealed record LongTermShortageInput(
    string ComponentPart,
    string? UnitOfMeasure,
    string? QadStatus,
    string? Description,
    bool IsKss,
    int? LeadTimeDays,
    string? Planner,
    string? BuyerPlannerCode,
    decimal OpeningQoh,
    SafetyStockState SafetyStockState,
    decimal? SafetyStock,
    IReadOnlyList<string> DemandParentParts,
    IReadOnlyList<string> OtherProgramParentParts,
    IReadOnlyList<LongTermDemandEvent> DemandEvents,
    IReadOnlyList<LongTermPurchaseOrder> PurchaseOrders);

public sealed record LongTermDemandEvent(DateOnly DueDate, decimal Quantity, bool IsForecast);
