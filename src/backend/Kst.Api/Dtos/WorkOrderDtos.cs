namespace Kst.Api.Dtos;

public sealed record KittingSummaryDto(
    int ApplicableLineCount,
    int FullyIssuedLineCount,
    decimal? KittingPercent
);

public sealed record WorkOrderSummaryDto(
    string PartNumber,
    string Woid,
    string Status,
    decimal OrderedQuantity,
    decimal CompletedQuantity,
    decimal OpenQuantity,
    DateOnly? ReleaseDate,
    DateOnly? DueDate,
    string? SalesOrder,
    KittingSummaryDto Kitting
);

public sealed record WorkOrderMaterialLineDto(
    string ComponentPart,
    string? ComponentDescription,
    decimal RequiredQuantity,
    decimal IssuedQuantity,
    decimal VarianceQuantity,
    decimal? IssuedPercent,
    string? IssueStatus,
    bool IsManufactured,
    bool IsFullyIssued
);

public sealed record WorkOrderPlanningWindowResponseDto(
    string SnapshotId,
    IReadOnlyList<WorkOrderSummaryDto> WorkOrders
);

public sealed record WorkOrderMaterialResponseDto(
    string SnapshotId,
    string Woid,
    KittingSummaryDto Kitting,
    IReadOnlyList<WorkOrderMaterialLineDto> Lines
);

public sealed record WorkOrderCandidateResponseDto(
    string SnapshotId,
    IReadOnlyList<WorkOrderSummaryDto> Candidates
);
