using Kst.Application.ApprovedVendors;
using Kst.Application.ComponentDetail;
using Kst.Application.ComponentOrders;
using Kst.Application.Inventory;
using Kst.Application.InventoryLocations;
using Kst.Application.PartInfo;
using Kst.Domain.ApprovedVendors;
using Kst.Domain.Common;
using Kst.Domain.ComponentDetail;
using Kst.Domain.ComponentOrders;
using Kst.Domain.Inventory;

namespace Kst.Application.Tests.PartInfo;

public sealed class PartInfoServiceTests
{
    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-10-14T12:00:00Z");
        public DateTimeOffset LocalNow => UtcNow;
    }

    [Theory]
    [InlineData("inventory")]
    [InlineData("locations")]
    [InlineData("alternates")]
    [InlineData("context")]
    [InlineData("orders")]
    [InlineData("comment")]
    public async Task SectionFailuresNeverInventEmptyOrEraseOtherAcquisitions(string fail)
    {
        Task<T> Read<T>(string section, T value) => fail == section ? throw new Exception("private") : Task.FromResult(value);
        var service = Create(
            new DelegatePartInventoryReader((site, _, _) => Read<IReadOnlyList<PartInventorySummary>>("inventory", [new(site, "P", 0m, 1m, 0m)])),
            new DelegateInventoryLocationReader((_, _, _) => Read<IReadOnlyList<InventoryLocationRow>>("locations", [])),
            new DelegateApprovedVendorSourceReader((_, _, _) => Read<IReadOnlyList<ApprovedVendor>>("alternates", [])),
            new DelegatePartInfoContextReader((_, _, _) => Read("context", new PartInfoContext(null, null, null))),
            new DelegateComponentOrderSourceReader((_, _, _, _) => Read<IReadOnlyList<ComponentOrderLine>>("orders", [])),
            new DelegateComponentOrderEnrichmentReader((_, _) => Read("comment", ComponentOrderEnrichmentResult.Empty)));
        var report = Assert.IsType<PartInfoReport>(await service.LookupAsync("SW", "P", new(2026, 10, 14), default));
        Assert.Equal(fail == "inventory", report.Inventory.Error is not null);
        Assert.Equal(fail == "locations", report.Locations.Error is not null);
        Assert.Equal(fail == "alternates", report.Alternates.Error is not null);
        Assert.Equal(fail == "context", report.Context.Error is not null);
        Assert.Equal(fail == "orders", report.Orders.Error is not null);
        Assert.Equal(fail == "comment", report.Comment.Error is not null);
        Assert.Equal(fail == "orders", report.Suppliers.Error is not null);
    }

    [Fact]
    public async Task CancellationIsNotAnUnavailableOrSuccessfulEmptySection()
    {
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = Create(locations: new DelegateInventoryLocationReader(async (_, _, ct) =>
        {
            started.SetResult(); await Task.Delay(Timeout.Infinite, ct); return [];
        }));
        var task = service.LookupAsync("SW", "P", new(2026, 10, 14), cancellation.Token);
        await started.Task; cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    private static PartInfoService Create(IPartInventoryReader? inventory = null, IInventoryLocationReader? locations = null,
        IApprovedVendorSourceReader? alternates = null, IPartInfoContextReader? context = null,
        IComponentOrderSourceReader? orders = null, IComponentOrderEnrichmentReader? enrichment = null) => new(
            new DelegateComponentSourceReader((_, part, _) => Task.FromResult<ComponentSourceFacts?>(new(part, null, null, null, null, null, null, null, null, null, null, null, null, null, null))),
            inventory ?? new DelegatePartInventoryReader((site, _, _) => Task.FromResult<IReadOnlyList<PartInventorySummary>>([new(site, "P", 0m, 0m, 0m)])),
            locations ?? new DelegateInventoryLocationReader((_, _, _) => Task.FromResult<IReadOnlyList<InventoryLocationRow>>([])),
            alternates ?? new DelegateApprovedVendorSourceReader((_, _, _) => Task.FromResult<IReadOnlyList<ApprovedVendor>>([])),
            context ?? new DelegatePartInfoContextReader((_, _, _) => Task.FromResult(new PartInfoContext(null, null, null))),
            orders ?? new DelegateComponentOrderSourceReader((_, _, _, _) => Task.FromResult<IReadOnlyList<ComponentOrderLine>>([])),
            enrichment ?? new DelegateComponentOrderEnrichmentReader((_, _) => Task.FromResult(ComponentOrderEnrichmentResult.Empty)), new Clock());
}
