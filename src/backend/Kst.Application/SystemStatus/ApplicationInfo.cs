namespace Kst.Application.SystemStatus;

/// <summary>
/// Static application metadata provided at startup.
/// </summary>
public sealed record ApplicationInfo(
    string Name,
    string Version,
    string InstanceId,
    DateTimeOffset StartedAt
);
