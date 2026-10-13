namespace Kst.Api.Dtos;

public sealed record WorkspaceShipmentsRequestDto(string? PartNumber, DateOnly? ShipDateFrom, DateOnly? ShipDateTo);
public sealed record WorkspaceShipmentsSpecDto(IReadOnlyList<HistoricalShipmentsColumnDto> Columns, int MaximumPageSize);
public sealed record WorkspaceShipmentsOperationDto(Guid OperationId, string State,
    long RowCount, IReadOnlyList<string> Columns, DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcquiredAtUtc, long UnavailableEnrichmentRows,
    long? OnTimeNumerator, long? OnTimeDenominator);
