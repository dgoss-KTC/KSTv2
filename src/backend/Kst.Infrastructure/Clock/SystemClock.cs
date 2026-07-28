using Kst.Domain.Common;

namespace Kst.Infrastructure.Clock;

/// <summary>
/// Production clock backed by DateTimeOffset.UtcNow.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateTimeOffset LocalNow => DateTimeOffset.Now;
}
