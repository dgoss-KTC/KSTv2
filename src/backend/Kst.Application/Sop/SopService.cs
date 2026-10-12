using Kst.Domain.Sop;

namespace Kst.Application.Sop;

public interface ISopReader
{
    Task<SopSource> ReadAsync(string site, IReadOnlyList<SopRange> ranges, CancellationToken ct);
}
public sealed class DelegateSopReader(Func<string, IReadOnlyList<SopRange>, CancellationToken, Task<SopSource>> read) : ISopReader
{
    public Task<SopSource> ReadAsync(string site, IReadOnlyList<SopRange> ranges, CancellationToken ct) => read(site, ranges, ct);
}
public sealed class SopService(ISopReader reader)
{
    public async Task<SopSource> AcquireAsync(string site, string groupBy, IReadOnlyList<SopRange> ranges, CancellationToken ct)
    {
        if (!SopRules.ValidRequest(site, groupBy, ranges)) throw new ArgumentException("Invalid S&OP inputs.");
        var result = await reader.ReadAsync(site, ranges.ToArray(), ct);
        ct.ThrowIfCancellationRequested();
        // Freeze the reader's collections; report facts never cross the page boundary.
        return new(result.Headers.ToArray(), result.WeekLabels.ToArray(),
            result.Rows.Select(r => new SopRow(r.Fixed.ToArray(), r.Weeks.ToArray())).ToArray());
    }
}
