namespace Kst.Api.Dtos;

public sealed record LongTermShortagesResponseDto(string SnapshotId, DateOnly RefreshDate, bool IsStale, string? Warning, IReadOnlyList<LongTermShortageRowDto> Rows);
public sealed record LongTermShortageRowDto(string ComponentPart, string? UnitOfMeasure, string? QadStatus, string? Description, string? Planner, string? BuyerPlannerCode, decimal OpeningQoh, string SafetyStockState, decimal? SafetyStock, string Severity, DateOnly? FirstShortDate, IReadOnlyList<string> DemandParentParts, LongTermShortageBucketDto Past, IReadOnlyList<LongTermShortageBucketDto> Weeks, IReadOnlyList<LongTermMrpFactDto> Evidence, LongTermShortagePresentationContextDto? Presentation);
public sealed record LongTermShortageBucketDto(int? WeekNumber, DateOnly? WeekStart, decimal GrossRequirements, decimal ScheduledReceipts, decimal PlannedOrdersDue, decimal PlannedOrdersRelease, decimal ProjectedQoh, string Severity);
public sealed record LongTermMrpFactDto(int EvidenceOrdinal, string? Type, DateOnly? DueDate, DateOnly? ReleaseDate, decimal Quantity, string Category);
public sealed record LongTermShortagePresentationContextDto(string? ManufacturerItem, string? PoNumber, int? PoLine, DateOnly? PoDueDate, decimal? PoOpenQuantity, bool? PoConfirmed, bool IsKss);
public sealed record ExportLongTermShortagesRequestDto(string SnapshotId, IReadOnlyList<string> ComponentParts, bool IncludeManufacturedParts = false, bool IncludePhantoms = false);
