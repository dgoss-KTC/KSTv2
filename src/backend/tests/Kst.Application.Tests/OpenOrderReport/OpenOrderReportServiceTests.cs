using Kst.Application.OpenOrderReport;
using Kst.Domain.OpenOrderReport;

namespace Kst.Application.Tests.OpenOrderReport;

public class OpenOrderReportServiceTests
{
    [Fact]
    public async Task Fresh_empty_failure_cancellation_and_duplicate_rows_are_distinct()
    {
        var filter = OpenOrderReportRules.Validate(new(null, null, null, null, null, null, null, null, "SW", []), new(2026, 10, 12));
        var cells = Enumerable.Repeat(new OpenOrderReportCell(), 34).ToArray(); cells[6] = new(Number: 0);
        var calls = 0;
        var service = new OpenOrderReportService(new DelegateOpenOrderReportReader((_, _) =>
        {
            calls++;
            return Task.FromResult<IReadOnlyList<OpenOrderReportRow>>(calls == 3 ? [] : [new(cells), new(cells)]);
        }));
        var first = await service.GenerateAsync(filter, new(2026, 10, 12), default);
        Assert.Equal(2, first!.Rows.Count); cells[6] = new(Number: 999);
        Assert.Equal(0, first.Rows[0].Cells[6].Number);
        await service.GenerateAsync(filter, new(2026, 10, 12), default);
        Assert.Null(await service.GenerateAsync(filter, new(2026, 10, 12), default)); Assert.Equal(3, calls);
        var failed = new OpenOrderReportService(new DelegateOpenOrderReportReader((_, _) => throw new InvalidOperationException("schema")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => failed.GenerateAsync(filter, new(2026, 10, 12), default));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GenerateAsync(filter, new(2026, 10, 12), cancel.Token));
        Assert.Equal(3, calls);
    }
}
