namespace Kst.Api.Dtos;

/// <summary>Selection of an already acquired projection; never a request to acquire source data.</summary>
public sealed record WorkspaceShortagesExportRequestDto(
    Guid WorkspaceId, Guid MpsSnapshotId, string Site, DateOnly BusinessDate,
    DateTimeOffset AcquiredAtUtc, ExportLongTermShortagesRequestDto Selection);
