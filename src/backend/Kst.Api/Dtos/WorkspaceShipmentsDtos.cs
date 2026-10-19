namespace Kst.Api.Dtos;

public sealed record WorkspaceShipmentsRequestDto(string? PartNumber, DateOnly? ShipDateFrom, DateOnly? ShipDateTo);
public sealed record WorkspaceShipmentsColumnDto(string Id, string Label, string Kind, string Provenance, bool DefaultSelected, bool Monetary, string? FilterKind);
public sealed record WorkspaceShipmentsSpecDto(IReadOnlyList<WorkspaceShipmentsColumnDto> Columns, int MaximumPageSize,
    IReadOnlyList<int> PageSizes, int MaximumValues, int MaximumTextLength, int MaximumDecimalDigits, int MaximumDecimalScale);
public sealed record WorkspaceShipmentsOperationDto(Guid OperationId, string State,
    long RowCount, IReadOnlyList<string> Columns, DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcquiredAtUtc, long UnavailableEnrichmentRows,
    long? OnTimeNumerator, long? OnTimeDenominator);
public sealed record ShipmentColumnFilterDto(string Column, string Kind, string? Mode = null,
    IReadOnlyList<string>? Values = null, bool IncludeBlanks = false, string? Text = null,
    string? Operator = null, string? Value = null, string? Upper = null, DateOnly? From = null, DateOnly? To = null);
public sealed record WorkspaceShipmentsViewRequestDto(long ExpectedVersion, long Sequence, IReadOnlyList<ShipmentColumnFilterDto> Filters);
public sealed record WorkspaceShipmentsViewDto(Guid OperationId, long Version, long RowCount, long OnTimeNumerator, long OnTimeDenominator,
    IReadOnlyList<ShipmentColumnFilterDto> Filters);
public sealed record WorkspaceShipmentsCancelViewDto(long Sequence);
public sealed record WorkspaceShipmentsPageDto(Guid OperationId, long Offset, long RowCount, IReadOnlyList<string> Columns,
    IReadOnlyList<HistoricalShipmentsRowDto> Rows, long ViewVersion);
public sealed record WorkspaceShipmentsFacetRequestDto(long ViewVersion, string Column, string? Search, IReadOnlyList<string> RetainedValues);
public sealed record WorkspaceShipmentsFacetValueDto(string Value, long Count);
public sealed record WorkspaceShipmentsFacetsDto(Guid OperationId, long ViewVersion, string Column,
    IReadOnlyList<WorkspaceShipmentsFacetValueDto> Values, IReadOnlyList<WorkspaceShipmentsFacetValueDto> RetainedValues,
    long BlankCount, long DistinctTotal, long MatchingDistinctTotal, bool Truncated);
