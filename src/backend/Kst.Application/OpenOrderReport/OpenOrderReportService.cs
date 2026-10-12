using Kst.Domain.OpenOrderReport;

namespace Kst.Application.OpenOrderReport;

public interface IOpenOrderReportReader
{
    Task<IReadOnlyList<OpenOrderReportRow>> ReadAsync(OpenOrderReportFilter filter, CancellationToken ct);
}

public sealed class DelegateOpenOrderReportReader(Func<OpenOrderReportFilter, CancellationToken, Task<IReadOnlyList<OpenOrderReportRow>>> read) : IOpenOrderReportReader
{
    public Task<IReadOnlyList<OpenOrderReportRow>> ReadAsync(OpenOrderReportFilter filter, CancellationToken ct) => read(filter, ct);
}

/// <summary>Fresh independent acquisition, no workspace population/cache or stale fallback.</summary>
public sealed class OpenOrderReportService(IOpenOrderReportReader reader)
{
    public async Task<OpenOrderReportDocument?> GenerateAsync(OpenOrderReportFilter filter, DateOnly asOfDate, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var rows = await reader.ReadAsync(filter, ct);
        ct.ThrowIfCancellationRequested();
        var frozen = OpenOrderReportRules.Freeze(rows);
        return frozen.Count == 0 ? null : new(frozen,
            Array.AsReadOnly(OpenOrderReportRules.Mandatory.Concat(filter.OptionalFields).ToArray()), asOfDate, DateTimeOffset.UtcNow);
    }
}
