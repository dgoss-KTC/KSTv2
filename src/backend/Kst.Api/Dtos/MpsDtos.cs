namespace Kst.Api.Dtos;

public sealed record MpsSnapshotMetadataDto(
    string? SnapshotId,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? LastSuccessfulRefreshAtUtc,
    string Status,
    Guid WorkspaceId,
    string? Site,
    int ResolvedParentPartCount,
    int SourceRowCount,
    bool IsRefreshInProgress,
    string? LastRefreshError
);

public sealed record MpsBucketDto(
    string Kind,
    DateOnly? WeekLabel,
    decimal Quantity,
    string ExecutionStatus,
    bool ContainsPlannedWork,
    bool ContainsExplicitlyScheduledWork
);

public sealed record MpsPartScheduleDto(
    string ParentPart,
    string? Description,
    IReadOnlyList<MpsBucketDto> Buckets
);

public sealed record MpsDashboardResponseDto(
    MpsSnapshotMetadataDto Snapshot,
    string DateBasis,
    int HorizonWeeks,
    IReadOnlyList<MpsPartScheduleDto> Parts
);
