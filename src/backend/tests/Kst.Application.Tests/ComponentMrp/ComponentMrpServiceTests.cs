using Kst.Application.ComponentMrp;
using Kst.Domain.ComponentMrp;
using Kst.Domain.LongTermShortages;

namespace Kst.Application.Tests.ComponentMrp;

public class ComponentMrpServiceTests
{
    private static ComponentMrpSource Source() => new(new("ABC", "EA", null, null, null, null, 10,
        SafetyStockState.Resolved, 0, [], []) { SitePlanningPresent = true }, new(null, null, null, null, null, null), DateTimeOffset.UtcNow);

    [Fact]
    public async Task Every_generate_is_fresh_and_scope_is_direct_part()
    {
        var calls = 0;
        var service = new ComponentMrpService(new DelegateComponentMrpReader((site, part, end, _) =>
        {
            Assert.Equal("KV", site); Assert.Equal("ABC", part); Assert.Equal(new DateOnly(2026, 12, 20), end);
            calls++; return Task.FromResult<ComponentMrpSource?>(Source());
        }));
        for (var i = 0; i < 2; i++)
        {
            var result = await service.GenerateAsync("KV", "ABC", 13, false, new(2026, 9, 24), default);
            Assert.Equal(13, result!.Projection.Weeks.Count); Assert.Empty(result.Projection.DemandParentParts);
            Assert.Equal(10, result.Projection.Weeks[0].ProjectedQoh);
        }
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Missing_master_is_null_but_reader_failure_propagates()
    {
        var missing = new ComponentMrpService(new DelegateComponentMrpReader((_, _, _, _) => Task.FromResult<ComponentMrpSource?>(null)));
        Assert.Null(await missing.GenerateAsync("SW", "X", 26, false, new(2026, 9, 24), default));
        var failing = new ComponentMrpService(new DelegateComponentMrpReader((_, _, _, _) => throw new InvalidOperationException("incomplete")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => failing.GenerateAsync("SW", "X", 26, false, new(2026, 9, 24), default));
        await Assert.ThrowsAsync<ArgumentException>(() => missing.GenerateAsync("KS", "X", 26, false, new(2026, 9, 24), default));
    }

    [Fact]
    public async Task Cancellation_after_acquisition_never_builds_a_report()
    {
        using var cancel = new CancellationTokenSource();
        var service = new ComponentMrpService(new DelegateComponentMrpReader((_, _, _, _) => { cancel.Cancel(); return Task.FromResult<ComponentMrpSource?>(Source()); }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GenerateAsync("SW", "ABC", 26, false, new(2026, 9, 24), cancel.Token));
    }
}
