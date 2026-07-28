using Kst.Domain.Common;
using Kst.Application.Snapshots;

namespace Kst.Application.SystemStatus;

/// <summary>
/// Describes the connectivity status of an external data source.
/// </summary>
public enum DataSourceStatus
{
    NotConfigured,
    Connecting,
    Connected,
    Unavailable
}

/// <summary>
/// Summary of an external data source as reported in the system-status response.
/// </summary>
public sealed record DataSourceSummary(string Name, DataSourceStatus Status);

/// <summary>
/// Result of the GetSystemStatus use case.
/// </summary>
public sealed record SystemStatusResult(
    string ApplicationName,
    string ApplicationVersion,
    string BackendFramework,
    string BackendInstanceId,
    DateTimeOffset StartedAt,
    DateTimeOffset CurrentTime,
    SnapshotInfo Snapshot,
    IReadOnlyList<DataSourceSummary> DataSources
);
