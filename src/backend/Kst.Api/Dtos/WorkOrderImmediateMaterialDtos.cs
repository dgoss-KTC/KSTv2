namespace Kst.Api.Dtos;

public sealed record WorkOrderImmediateMaterialAnalysisResponseDto(
    string SnapshotId,
    WorkOrderImmediateMaterialContextDto WorkOrder,
    IReadOnlyList<WorkOrderImmediateMaterialComponentDto> Components,
    string? Diagnostic);

public sealed record WorkOrderImmediateMaterialContextDto(
    string Woid,
    string BuildPart,
    string Status,
    string? WorkOrderType,
    decimal MaterialBuildQuantity,
    DateOnly? DueDate,
    DateOnly? ReleaseDate,
    string PlanningBucketContext);

public sealed record WorkOrderImmediateMaterialComponentDto(
    string ComponentPart,
    string? Description,
    bool IsManufactured,
    string? UnitOfMeasure,
    string RequirementSource,
    string MaterialStatus,
    string AllocationMode,
    decimal RequiredQuantity,
    decimal? IssuedQuantity,
    decimal? VarianceQuantity,
    decimal? IssuedPercent,
    decimal? RemainingRequirement,
    decimal UsableHardAllocationToThisWoComponent,
    decimal OwnHardCoverage,
    decimal? UncoveredRequirement,
    decimal? AvailableQuantityAtEvaluation,
    decimal? AllocatedQuantity,
    decimal UsableOnHand,
    decimal? ShortQuantity,
    bool IsFloorStockOrNonIssued,
    bool? IsOverIssued,
    WorkOrderImmediateMaterialInventoryActivityDto InventoryActivity,
    WorkOrderImmediateMaterialIncomingContextDto? Incoming,
    string? Diagnostic);

public sealed record WorkOrderImmediateMaterialSummaryResponseDto(
    string SnapshotId,
    IReadOnlyList<WorkOrderImmediateMaterialSummaryDto> WorkOrders);

public sealed record WorkOrderImmediateMaterialSummaryDto(
    string Woid,
    string BuildPart,
    string PlanningBucketContext,
    DateOnly? DueDate,
    DateOnly? ReleaseDate,
    bool HasShortage,
    bool HasDataIssue);

public sealed record WorkOrderImmediateMaterialInventoryActivityDto(
    decimal Transit,
    decimal Inspection,
    decimal NonNet,
    decimal Mrb,
    decimal NcmInspection,
    decimal ExpiredExpiring);

public sealed record WorkOrderImmediateMaterialIncomingContextDto(
    bool IsKss,
    string? PoState,
    string? PoNumber,
    DateOnly? PoDueDate,
    decimal? PoOpenQuantity,
    bool? PoConfirmed,
    string? TrackingInfo);
