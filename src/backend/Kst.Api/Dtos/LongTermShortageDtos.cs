namespace Kst.Api.Dtos;

public sealed record LongTermShortagesResponseDto(string SnapshotId, DateOnly RefreshDate, bool IsStale, string? Warning, IReadOnlyList<LongTermShortageRowDto> Rows);
public sealed record LongTermShortageRowDto(string ComponentPart, string? UnitOfMeasure, string? QadStatus, string? Description, bool IsKss, int? LeadTimeWeeks, string? Planner, decimal OpeningQoh, decimal DisplayOpeningQoh, string SafetyStockState, decimal? SafetyStock, decimal? DisplaySafetyStock, string Severity, int? FirstSafetyStockShortWeek, int? FirstCriticalShortWeek, IReadOnlyList<string> DemandParentParts, IReadOnlyList<string> OtherProgramParentParts, IReadOnlyList<LongTermShortageWeekDto> Weeks, IReadOnlyList<LongTermPurchaseOrderDto> PurchaseOrders, string? BuyerPlannerCode);
public sealed record LongTermShortageWeekDto(int WeekNumber, DateOnly WeekStart, decimal WorkOrderDemand, decimal DisplayWorkOrderDemand, decimal ForecastDemand, decimal DisplayForecastDemand, decimal DisplayDemand, decimal PurchaseOrderSupply, decimal DisplayPurchaseOrderSupply, decimal Balance, decimal DisplayBalance, string Severity);
public sealed record LongTermPurchaseOrderDto(string PoNumber, int PoLine, DateOnly? DueDate, decimal OpenQuantity, decimal DisplayOpenQuantity, bool? Confirmed, string? ManufacturerItem, bool IsScheduled);
/// <summary>
/// Stage 11-A export request. The population options must match the projection that produced the
/// displayed rows; they are part of the cached-projection identity and never trigger a refresh.
/// </summary>
public sealed record ExportLongTermShortagesRequestDto(string SnapshotId, IReadOnlyList<string> ComponentParts, bool IncludeManufacturedParts = false, bool IncludePhantoms = false);
