namespace Kst.Integrations.Qad.Connectivity;

/// <summary>
/// Result of a QAD connectivity check.
/// </summary>
public enum ConnectivityStatus
{
    NotConfigured,
    Succeeded,
    Failed,
    TimedOut
}

/// <summary>
/// Contract for verifying QAD database connectivity without running business queries.
/// </summary>
public interface IQadConnectivityCheck
{
    Task<ConnectivityResult> CheckAsync(CancellationToken cancellationToken = default);
}

public sealed record ConnectivityResult(
    ConnectivityStatus Status,
    string? ErrorMessage = null
);
