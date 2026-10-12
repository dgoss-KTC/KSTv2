using Kst.Application.ShipmentsToGo;
using Kst.Domain.ShipmentsToGo;

namespace Kst.Application.Tests.ShipmentsToGo;

public class ShipmentsToGoServiceTests
{
    [Fact]
    public async Task Empty_orders_skip_shipped_and_orders_failure_propagates_without_fallback()
    {
        var shipped = 0;
        var service = new ShipmentsToGoService(new DelegateShipmentsToGoReader((_, _, _) => Task.FromResult<IReadOnlyList<ShipmentToGoLine>>([]),
            (_, _) => { shipped++; return Task.FromResult<IReadOnlyDictionary<string, double>>(new Dictionary<string, double>()); }));
        Assert.Null(await service.GenerateAsync(["MS"], false, () => new(2026, 10, 9), default));
        Assert.Equal(0, shipped);
        var failed = new ShipmentsToGoService(new DelegateShipmentsToGoReader((_, _, _) => throw new InvalidOperationException("Synthetic order failure"),
            (_, _) => { shipped++; return Task.FromResult<IReadOnlyDictionary<string, double>>(new Dictionary<string, double>()); }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => failed.GenerateAsync(["MS"], false, () => new(2026, 10, 9), default));
        Assert.Equal(0, shipped);
    }

    [Fact]
    public async Task Every_generate_reacquires_and_uses_separate_local_label_clock_and_partial_shipments()
    {
        var calls = 0;
        var row = new ShipmentToGoLine(new(2026, 10, 9), "ORDER", null, "Standard", 1, null, "PART", null, "SW", "AR", 1, 0, 0, 1, "Name", 10, "CRC", 0, "");
        var service = new ShipmentsToGoService(new DelegateShipmentsToGoReader((sites, next, _) => {
            calls++; Assert.Equal(new[] { "SW", "KV" }, sites); Assert.True(next);
            return Task.FromResult<IReadOnlyList<ShipmentToGoLine>>([row]);
        }, (_, _) => Task.FromResult<IReadOnlyDictionary<string, double>>(new Dictionary<string, double> { ["SW"] = 20, ["KV"] = 0 })));
        for (var i = 0; i < 2; i++)
        {
            var report = await service.GenerateAsync(["SW", "KV"], true, () => new(2026, 10, 10), default);
            Assert.Equal(new DateOnly(2026, 10, 10), report!.LabelDate); Assert.Equal("KTC-AR S2G", report.Sheets.Single().Name);
            Assert.Equal(20, report.ShippedToday["SW"]); Assert.Equal(0, report.ShippedToday["KV"]);
        }
        Assert.Equal(2, calls);
    }
}
