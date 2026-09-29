namespace Kst.Api.Dtos;

/// <summary>Immutable matrix transport; complete evidence remains in the projection cache.</summary>
public sealed record LongTermShortagesScreenDto(string SnapshotId, DateOnly RefreshDate, bool IsStale,
    string? Warning, DateTimeOffset AcquiredAtUtc, string ConsistencyMode,
    IReadOnlyList<LongTermShortageWeekDto> Weeks, IReadOnlyList<LongTermShortageScreenComponentDto> Components);
public sealed record LongTermShortageWeekDto(int WeekNumber, DateOnly WeekStart, DateOnly LabelDate);
public sealed record LongTermShortageScreenComponentDto(string ComponentPart, string? UnitOfMeasure,
    string? QadStatus, string? Description, string? Planner, string? BuyerPlannerCode, decimal OpeningQoh,
    string OpeningDisplay, decimal? SafetyStock, string? DataQualityWarning, bool IsKss,
    LongTermShortageScreenModeDto Confirmed, LongTermShortageScreenModeDto All);
public sealed record LongTermShortageScreenModeDto(string Severity, DateOnly? FirstShortDate,
    DateOnly? FirstAtRiskDate, decimal MaximumShortage, DateOnly? FirstRecoveryDate,
    IReadOnlyList<decimal> Ending, IReadOnlyList<string> EndingDisplay, IReadOnlyList<string> WeeklySeverity);
public sealed record LongTermShortageProjectionDetailDto(string SnapshotId, string ComponentPart,
    DateOnly RefreshDate, DateTimeOffset AcquiredAtUtc, string ConsistencyMode,
    IReadOnlyList<string> DemandParentParts, decimal PastGrossRequirements, decimal OverdueReceipts,
    decimal AdjustedOpeningQoh, string? EffectivePmCode, int? ManufacturingLeadWorkingDays, string? ManufacturerItem);

public sealed record LongTermShortagesResponseDto(string SnapshotId, DateOnly RefreshDate, bool IsStale, string? Warning, IReadOnlyList<LongTermShortageRowDto> Rows, DateTimeOffset AcquiredAtUtc, string ConsistencyMode, IReadOnlyList<LongTermShortageRowDto> AllReceiptsRows)
{
    /// <summary>False when raw evidence was explicitly omitted for the screen; cached export remains complete.</summary>
    public bool EvidenceIncluded { get; init; } = true;
}
public sealed record LongTermShortageRowDto(string ComponentPart, string? UnitOfMeasure, string? QadStatus, string? Description, string? Planner, string? BuyerPlannerCode, decimal OpeningQoh, string SafetyStockState, decimal? SafetyStock, string Severity, DateOnly? FirstShortDate, IReadOnlyList<string> DemandParentParts, LongTermShortageBucketDto Past, IReadOnlyList<LongTermShortageBucketDto> Weeks, IReadOnlyList<LongTermMrpFactDto> Evidence, LongTermShortagePresentationContextDto? Presentation, IReadOnlyList<LongTermShortageEpisodeDto> Episodes, string? DataQualityWarning, DateOnly? FirstAtRiskDate, string? EffectivePmCode, string? PartStatusDescription, int? OrderPeriodDays, decimal? SafetyTimeWorkingDays, int? ManufacturingLeadWorkingDays, int? PurchasingLeadCalendarDays, int? CumulativeLeadCalendarDays, bool? SitePlanningPresent);
public sealed record LongTermShortageEpisodeDto(DateOnly StartDate, DateOnly DeepestDate, decimal MaximumShortage, DateOnly? FirstRecoveryDate, DateOnly? StableClearDate);
public sealed record LongTermShortageBucketDto(int? WeekNumber, DateOnly? WeekStart, decimal GrossRequirements, decimal ScheduledReceipts, decimal PlannedOrdersDue, decimal PlannedOrdersRelease, decimal ProjectedQoh, string Severity, decimal UnconfirmedReceipts, decimal ConfirmedEnding, decimal AllReceiptsEnding, decimal PlanningEnding, decimal AllReceiptsPlanningEnding, decimal LowestProjectedBalance, decimal OverdueReceipts, bool IncludesUnconfirmed, decimal LowestConfirmedBalance, decimal LowestAllReceiptsBalance);
public sealed record LongTermMrpFactDto(int EvidenceOrdinal, string? Type, DateOnly? DueDate, DateOnly? ReleaseDate, decimal Quantity, string Category, string? SourceNumber, string? SourceLine, string? SourceLine2, bool? IsPoReceipt, bool? PoConfirmed, string? SourceRowId);
public sealed record LongTermShortagePresentationContextDto(string? ManufacturerItem, string? PoNumber, int? PoLine, DateOnly? PoDueDate, decimal? PoOpenQuantity, bool? PoConfirmed, bool IsKss);
public sealed record ExportLongTermShortagesRequestDto(string SnapshotId, IReadOnlyList<string> ComponentParts, bool IncludeManufacturedParts = false, bool IncludePhantoms = false, bool IncludeUnconfirmed = false, int HorizonWeeks = 26, bool ShowAll = false);
public sealed record LongTermShortagePurchasingDto(bool CommentAvailable, string? CurrentComment, IReadOnlyList<ComponentOrderLineDto> OpenPurchaseOrders);
