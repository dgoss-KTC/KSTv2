namespace Kst.Api.Dtos;

public sealed record HistoricalShipmentsRequestDto(
    string Site, string? CorpGroup, string? CustomerFrom, string? CustomerTo,
    string? ItemFrom, string? ItemTo, string? ProductLineFrom, string? ProductLineTo,
    string? SalespersonFrom, string? SalespersonTo, DateOnly? InvoiceDateFrom,
    DateOnly? InvoiceDateTo, IReadOnlyList<string> Columns);

public sealed record HistoricalShipmentsColumnDto(string Id, string Label, string Kind,
    string Provenance, bool DefaultSelected, bool Monetary);
public sealed record HistoricalShipmentsSpecDto(IReadOnlyList<string> Sites,
    IReadOnlyList<HistoricalShipmentsColumnDto> Columns, int MaximumPageSize, int MaximumXlsxDataRows);
public sealed record HistoricalShipmentsOperationDto(Guid OperationId, string State,
    long RowCount, IReadOnlyList<string> Columns, DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcquiredAtUtc, long UnavailableEnrichmentRows);
public sealed record HistoricalShipmentsRowDto(string PhysicalDetailId, IReadOnlyList<string?> Cells);
public sealed record HistoricalShipmentsPageDto(Guid OperationId, long Offset,
    long TotalRows, IReadOnlyList<string> Columns, IReadOnlyList<HistoricalShipmentsRowDto> Rows);
public sealed record HistoricalShipmentsPreparedDto(Guid OperationId, string PreparationId,
    string FileName, long ByteLength, DateTimeOffset ExpiresAtUtc);
