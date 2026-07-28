namespace Kst.Integrations.Shortages.Connectivity;

public enum ShortagesConnectivityStatus
{
    NotConfigured,
    Succeeded,
    Failed,
    TimedOut
}

/// <summary>
/// Contract for verifying shortage database connectivity.
/// </summary>
public interface IShortagesConnectivityCheck
{
    Task<ShortagesConnectivityResult> CheckAsync(CancellationToken cancellationToken = default);
}

public sealed record ShortagesConnectivityResult(
    ShortagesConnectivityStatus Status,
    string? ErrorMessage = null
);
