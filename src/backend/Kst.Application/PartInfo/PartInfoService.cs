using Kst.Application.ApprovedVendors;
using Kst.Application.ComponentDetail;
using Kst.Application.ComponentMrp;
using Kst.Application.ComponentOrders;
using Kst.Application.Inventory;
using Kst.Application.InventoryLocations;
using Kst.Domain.Common;
using Kst.Domain.Inventory;

namespace Kst.Application.PartInfo;

/// <summary>Uncached exact-part lookup. No workspace, BOM, MPS or projection dependencies.</summary>
public sealed class PartInfoService(IComponentSourceReader master, IPartInventoryReader inventory,
    IInventoryLocationReader locations, IApprovedVendorSourceReader alternates, IPartInfoContextReader context,
    IComponentOrderSourceReader orders, IComponentOrderEnrichmentReader enrichment, IClock clock)
{
    public const int MaximumPartLength = 256;
    public static readonly string[] Sites = ["SW", "NW", "AR", "MN", "MS", "KV"];

    public static string Validate(string site, string part)
    {
        if (!ComponentMrpService.ValidSite(site) || string.IsNullOrWhiteSpace(part) || part.Length > MaximumPartLength || part.Any(char.IsControl))
            throw new ArgumentException("Enter one exact part number (up to 256 characters) and a supported site.");
        return part.Trim();
    }

    public async Task<PartInfoSection<IReadOnlyList<InventoryLocationRow>>> ReadLocationsAsync(string site, string part, CancellationToken ct)
    {
        part = Validate(site, part);
        ct.ThrowIfCancellationRequested();
        try
        {
            var rows = await locations.ReadAsync(site, part, ct);
            ct.ThrowIfCancellationRequested();
            return new(rows, clock.UtcNow, null);
        }
        catch (OperationCanceledException) { throw; }
        catch { return new(null, null, "Inventory locations could not be acquired. Retry inventory."); }
    }

    public async Task<PartInfoReport?> LookupAsync(string site, string part, DateOnly today, CancellationToken ct)
    {
        part = Validate(site, part);
        ct.ThrowIfCancellationRequested();
        var facts = await master.ReadAsync(site, part, ct);
        if (facts is null) return null;
        if (!string.Equals(facts.ComponentPart.Trim(), part, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Part master scope mismatch.");

        var inventoryTask = Capture(async () =>
        {
            var rows = await inventory.ReadSummariesAsync(site, [part], ct);
            return rows.Single(r => string.Equals(r.Site, site, StringComparison.OrdinalIgnoreCase)
                && string.Equals(r.PartNumber.Trim(), part, StringComparison.OrdinalIgnoreCase));
        });
        var locationsTask = Capture(() => locations.ReadAsync(site, part, ct));
        var alternatesTask = Capture(() => alternates.ReadAsync(site, part, ct));
        var contextTask = Capture(() => context.ReadAsync(site, part, ct));
        var commentTask = Capture(async () =>
        {
            var result = await enrichment.ReadAsync(new(site, [facts.ComponentPart], []), ct);
            return result.CommentsByComponent.FirstOrDefault(p => string.Equals(p.Key, facts.ComponentPart, StringComparison.Ordinal)).Value;
        });
        var orderTask = Capture(async () =>
        {
            var lines = await orders.ReadAsync(site, [part], today, ct);
            if (lines.Any(l => !string.Equals(l.ComponentPart.Trim(), part, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Purchase order scope mismatch.");
            return lines;
        });
        // Observe every independent task even when one acquisition is cancelled.
        await Task.WhenAll(inventoryTask, locationsTask, alternatesTask, contextTask, commentTask, orderTask);
        var orderSection = await orderTask;
        PartInfoSection<IReadOnlyList<PartInfoSupplier>> supplierSection;
        if (orderSection.Error is not null)
            supplierSection = new(null, null, "Supplier status unavailable because open purchase orders could not be acquired.");
        else
        {
            var suppliers = orderSection.Value!.Where(l => l.SupplierIdentifier is not null)
                .GroupBy(l => l.SupplierIdentifier!, StringComparer.Ordinal).ToArray();
            supplierSection = await Capture<IReadOnlyList<PartInfoSupplier>>(async () =>
            {
                if (suppliers.Length == 0) return [];
                var result = await enrichment.ReadAsync(new(site, [], suppliers.Select(g => g.Key).ToArray()), ct);
                return suppliers.Select(g =>
                {
                    // Do not inherit a caller-provided dictionary's potentially insensitive comparer.
                    var risk = result.RisksBySupplier.FirstOrDefault(p => string.Equals(p.Key, g.Key, StringComparison.Ordinal)).Value;
                    return new PartInfoSupplier(g.Key, g.First().SupplierDisplay, risk?.IsCreditHold, risk?.IsCia);
                }).ToArray();
            });
            if (supplierSection.Error is not null)
                supplierSection = supplierSection with { Value = suppliers.Select(g => new PartInfoSupplier(g.Key, g.First().SupplierDisplay, null, null)).ToArray() };
        }
        await Task.WhenAll(inventoryTask, locationsTask, alternatesTask, contextTask, commentTask);
        ct.ThrowIfCancellationRequested();
        return new(site, part, facts, await inventoryTask, await locationsTask, await alternatesTask,
            await contextTask, orderSection, await commentTask, supplierSection);

        async Task<PartInfoSection<T>> Capture<T>(Func<Task<T>> read)
        {
            try { var value = await read(); ct.ThrowIfCancellationRequested(); return new(value, clock.UtcNow, null); }
            catch (OperationCanceledException) { throw; }
            catch { return new(default, null, "This section could not be acquired. Retry the lookup."); }
        }
    }
}
