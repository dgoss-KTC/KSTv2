using Kst.Application.Sop;
using Kst.Domain.Sop;

namespace Kst.Application.Tests.Sop;

public class SopServiceTests
{
    [Fact]
    public async Task Source_observations_frozen_and_fresh_without_workspace_population()
    {
        var fixedCells = Enumerable.Repeat(new SopCell(Text: "source"), 8).ToArray();
        SopRow[] rows = [new(fixedCells, [new(Number: 7)]), new(fixedCells, [new(Number: 7)])];
        var calls = 0;
        var service = new SopService(new DelegateSopReader((site, ranges, ct) =>
        {
            calls++; Assert.Equal("SW", site); Assert.Equal(ranges[0], ranges[1]);
            return Task.FromResult(new SopSource(["Customer", "Part", "St", "Planr", "Prod", "type", "Time Std", " Falldown"], [new(2026, 10, 12)], rows));
        }));
        SopRange[] ranges = [new("2142", "2140"), new("2142", "2140")];
        var acquired = await service.AcquireAsync("SW", "customer", ranges, default);
        fixedCells[0] = new(Text: "modified");
        Assert.Equal("source", acquired.Rows[0].Fixed[0].Text); Assert.Equal(2, acquired.Rows.Count);
        await service.AcquireAsync("SW", "planr", ranges, default); Assert.Equal(2, calls);
        await Assert.ThrowsAsync<ArgumentException>(() => service.AcquireAsync("KS", "planr", ranges, default));
        Assert.Equal(2, calls);
    }
    [Fact]
    public async Task Cancellation_after_read_is_not_success()
    {
        using var cts = new CancellationTokenSource();
        var service = new SopService(new DelegateSopReader((_, _, _) => { cts.Cancel(); return Task.FromResult(new SopSource([], [], [])); }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.AcquireAsync("SW", "planr", [new("2140", "2142")], cts.Token));
    }
}
