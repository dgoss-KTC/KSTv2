namespace Kst.Api.Dtos;

// Exact invariant decimal strings; Value display rounding never changes these values.
public sealed record FinishedGoodsRowDto(
    string RowId, string Domain, string Product, string? SourceLocation, string? Location,
    string? Lot, string? Reference, string OnHand, string UnitPrice, string Value,
    bool IsMissingPrice, bool IsRma);

public sealed record FinishedGoodsResponseDto(
    Guid WorkspaceId, string Site, string MpsSnapshotId, string FinishedGoodsSnapshotId,
    DateTimeOffset AcquiredAtUtc, string Currency, bool IsStale, string? Warning,
    IReadOnlyList<FinishedGoodsRowDto> Rows);
