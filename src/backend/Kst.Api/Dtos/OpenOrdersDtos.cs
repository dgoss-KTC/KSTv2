namespace Kst.Api.Dtos;

public sealed record OpenOrderLineKeyDto(string Domain, string SalesOrder, int Line);
public sealed record OpenOrderEditableValuesDto(
    DateOnly? DueDate, DateOnly? PerformDate, DateOnly? RequiredDate, DateOnly? DockDate,
    decimal OrderQty, decimal Price);

public sealed record OpenOrderLineDto(
    OpenOrderLineKeyDto Key, string ItemNumber, string Site, string? PurchaseOrder,
    string? Stat, decimal ShippedQty, OpenOrderEditableValuesDto SourceValues,
    decimal Open, decimal ExtPrice, decimal UnitPrice,
    decimal? Allocated, string? Customer, string? CustomerName, string? Salesperson,
    string? CustomerPart, string? Ios, string LineComments, string? LineHold,
    bool? Partials, decimal? Picked, string? Plnr, string? ProdStat,
    string? ProductLine, string? QaHold, string? Remarks, string? Revision,
    string? ShipAcct, string? ShipTo, string? ShipVia, decimal? SiteQoh,
    string? SoHoldStatus, string? SoType, bool? Consignment,
    OpenOrderDraftValuesDto PlanningValues, string ShippedQtyText);

public sealed record OpenOrdersResponseDto(
    Guid WorkspaceId, string Site, string MpsSnapshotId, string OpenOrdersSnapshotId,
    DateTimeOffset AcquiredAtUtc, bool IsStale, string? Warning,
    IReadOnlyList<OpenOrderLineDto> Lines);

public sealed record ExportOpenOrdersReportRequestDto(
    string MpsSnapshotId, string OpenOrdersSnapshotId,
    IReadOnlyList<OpenOrderLineKeyDto> LineKeys, IReadOnlyList<string> Columns);

// Decimal text is deliberate: JavaScript numbers cannot preserve all .NET decimal precision.
public sealed record OpenOrderDraftValuesDto(
    DateOnly? DueDate, DateOnly? PerformDate, DateOnly? RequiredDate, DateOnly? DockDate,
    string OrderQty, string Price);
public sealed record OpenOrderProposalDto(OpenOrderLineKeyDto Key, string Site, string ItemNumber,
    OpenOrderDraftValuesDto Original, OpenOrderDraftValuesDto Proposed, string? ReasonCode);
public sealed record SaveOpenOrdersDraftRequestDto(string MpsSnapshotId, string OpenOrdersSnapshotId,
    IReadOnlyList<OpenOrderProposalDto> Proposals);
public sealed record OpenOrderDraftRowDto(OpenOrderProposalDto Proposal, IReadOnlyList<string> Issues);
public sealed record OpenOrdersDraftResponseDto(bool Exists, bool Restored, string? Warning,
    OpenOrdersResponseDto? FreshReport, IReadOnlyList<OpenOrderDraftRowDto> Rows);
public sealed record OpenOrdersDraftPresenceDto(bool Exists);

public sealed record ExportOpenOrdersQxtendRequestDto(string MpsSnapshotId, string OpenOrdersSnapshotId,
    IReadOnlyList<OpenOrderProposalDto> Proposals);
public sealed record OpenOrdersQxtendFileDto(string Kind, string FileName, string ContentBase64);
public sealed record OpenOrdersQxtendResponseDto(IReadOnlyList<OpenOrdersQxtendFileDto> Files);
