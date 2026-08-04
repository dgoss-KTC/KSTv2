namespace Kst.Application.Refresh;

/// <summary>
/// Distinguishes the last refresh attempt from the last refresh that actually produced/confirmed
/// real data. Never fabricated: both remain null until a real attempt/success occurs.
/// </summary>
public sealed record RefreshHistory(
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? LastSuccessfulAt
)
{
    public static readonly RefreshHistory None = new(null, null);
}

/// <summary>
/// Persists refresh attempt/success timestamps across the application lifetime.
/// Implementations live in Kst.Infrastructure.
/// </summary>
public interface IRefreshHistoryStore
{
    RefreshHistory GetHistory();
    void RecordAttempt(DateTimeOffset attemptedAt);
    void RecordSuccess(DateTimeOffset succeededAt);
}
