using Kst.Domain.Common;

namespace Kst.Application.Tests.PartDetail;

/// <summary>Deterministic <see cref="IClock"/> fake for PartDetail cache/freshness tests.</summary>
internal sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
    public DateTimeOffset LocalNow { get; set; } = new(2026, 8, 10, 5, 0, 0, TimeSpan.FromHours(-7));
}
