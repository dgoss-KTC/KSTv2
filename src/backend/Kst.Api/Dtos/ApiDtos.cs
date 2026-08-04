namespace Kst.Api.Dtos;

// Health check response
public sealed record HealthResponse(
    string Status,
    string Application,
    string BackendVersion,
    int ProcessId,
    string InstanceId,
    DateTimeOffset Timestamp
);

// Readiness check response
public sealed record ReadyResponse(
    string Status,
    bool Initialized,
    bool SnapshotAvailable,
    DateTimeOffset Timestamp
);

// System status response
public sealed record SystemStatusResponse(
    string ApplicationName,
    string ApplicationVersion,
    string BackendFramework,
    string BackendInstanceId,
    DateTimeOffset StartedAt,
    DateTimeOffset CurrentTime,
    SnapshotStatusDto Snapshot,
    IReadOnlyList<DataSourceDto> DataSources,
    DateTimeOffset? LastRefreshAttemptAt,
    DateTimeOffset? LastSuccessfulRefreshAt
);

public sealed record SnapshotStatusDto(
    bool Available,
    string? SnapshotId,
    DateTimeOffset? CreatedAt,
    string Status
);

public sealed record DataSourceDto(
    string Name,
    string Status
);
