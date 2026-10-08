namespace Kst.Api.Dtos;

public sealed record InventoryLocationRowDto(string RowId, string? Location, string? LotSerial, string? Status, string? QuantityOnHand);
public sealed record InventoryLocationsResponseDto(Guid WorkspaceId, string Site, string ComponentPart,
    string MpsSnapshotId, DateTimeOffset AcquiredAtUtc, bool IsStale, string? Warning, IReadOnlyList<InventoryLocationRowDto> Rows);
