using Kst.Domain.ApprovedVendors;
using Kst.Domain.ComponentDetail;
using Kst.Domain.ComponentOrders;
using Kst.Domain.Inventory;

namespace Kst.Application.PartInfo;

public interface IPartInfoContextReader
{
    Task<PartInfoContext> ReadAsync(string site, string part, CancellationToken ct);
}
public sealed class DelegatePartInfoContextReader(Func<string, string, CancellationToken, Task<PartInfoContext>> read) : IPartInfoContextReader
{
    public Task<PartInfoContext> ReadAsync(string site, string part, CancellationToken ct) => read(site, part, ct);
}
public sealed record PartInfoSection<T>(T? Value, DateTimeOffset? AcquiredAtUtc, string? Error);
public sealed record PartInfoSupplier(string Identifier, string? Display, bool? IsCreditHold, bool? IsCia);
public sealed record PartInfoReport(string Site, string Part, ComponentSourceFacts Master,
    PartInfoSection<PartInventorySummary> Inventory,
    PartInfoSection<IReadOnlyList<InventoryLocationRow>> Locations,
    PartInfoSection<IReadOnlyList<ApprovedVendor>> Alternates,
    PartInfoSection<PartInfoContext> Context,
    PartInfoSection<IReadOnlyList<ComponentOrderLine>> Orders,
    PartInfoSection<BuyerComment> Comment,
    PartInfoSection<IReadOnlyList<PartInfoSupplier>> Suppliers);
