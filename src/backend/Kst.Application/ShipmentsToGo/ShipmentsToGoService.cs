using Kst.Domain.ShipmentsToGo;

namespace Kst.Application.ShipmentsToGo;

public interface IShipmentsToGoReader
{
    Task<IReadOnlyList<ShipmentToGoLine>> ReadOrdersAsync(IReadOnlyList<string> sites, bool includeNextWeek, CancellationToken ct);
    Task<IReadOnlyDictionary<string, double>> ReadShippedTodayAsync(IReadOnlyList<string> sites, CancellationToken ct);
}

public sealed class DelegateShipmentsToGoReader(
    Func<IReadOnlyList<string>, bool, CancellationToken, Task<IReadOnlyList<ShipmentToGoLine>>> orders,
    Func<IReadOnlyList<string>, CancellationToken, Task<IReadOnlyDictionary<string, double>>> shipped) : IShipmentsToGoReader
{
    public Task<IReadOnlyList<ShipmentToGoLine>> ReadOrdersAsync(IReadOnlyList<string> sites, bool includeNextWeek, CancellationToken ct) => orders(sites, includeNextWeek, ct);
    public Task<IReadOnlyDictionary<string, double>> ReadShippedTodayAsync(IReadOnlyList<string> sites, CancellationToken ct) => shipped(sites, ct);
}

/// <summary>No workspace, MPS population, cache or stale fallback. Orders must complete; shipped reader preserves Python partial totals.</summary>
public sealed class ShipmentsToGoService(IShipmentsToGoReader reader)
{
    public async Task<ShipmentsToGoReport?> GenerateAsync(IReadOnlyList<string> sites, bool includeNextWeek,
        Func<DateOnly> localDate, CancellationToken ct)
    {
        if (!ShipmentsToGoRules.ValidSites(sites)) throw new ArgumentException("Select supported sites.");
        var frozenSites = sites.ToArray();
        var orders = await reader.ReadOrdersAsync(frozenSites, includeNextWeek, ct);
        ct.ThrowIfCancellationRequested();
        if (orders.Count == 0) return null;
        var shipped = await reader.ReadShippedTodayAsync(frozenSites, ct);
        ct.ThrowIfCancellationRequested();
        return ShipmentsToGoRules.Build(frozenSites, orders, shipped, localDate(), DateTimeOffset.UtcNow);
    }
}
