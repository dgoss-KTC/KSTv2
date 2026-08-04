using Kst.Application.Refresh;

namespace Kst.Infrastructure.SystemStatus;

/// <summary>
/// Thread-safe in-memory store for refresh-attempt/refresh-success timestamps.
/// </summary>
public sealed class InMemoryRefreshHistoryStore : IRefreshHistoryStore
{
    private readonly Lock _lock = new();
    private RefreshHistory _current = RefreshHistory.None;

    public RefreshHistory GetHistory()
    {
        lock (_lock)
        {
            return _current;
        }
    }

    public void RecordAttempt(DateTimeOffset attemptedAt)
    {
        lock (_lock)
        {
            _current = _current with { LastAttemptAt = attemptedAt };
        }
    }

    public void RecordSuccess(DateTimeOffset succeededAt)
    {
        lock (_lock)
        {
            _current = _current with { LastSuccessfulAt = succeededAt };
        }
    }
}
