namespace Kst.Domain.Common;

/// <summary>
/// Represents a point in time with timezone offset.
/// Abstracts DateTimeOffset to allow controlled time in tests.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
    DateTimeOffset LocalNow { get; }
}
