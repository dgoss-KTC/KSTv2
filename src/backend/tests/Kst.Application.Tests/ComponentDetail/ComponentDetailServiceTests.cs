using Kst.Application.ComponentDetail;
using Kst.Application.Inventory;
using Kst.Application.Mps;
using Kst.Application.Tests.Mps;
using Kst.Application.Tests.PartDetail;
using Kst.Domain.ComponentDetail;
using Kst.Domain.Common;
using Kst.Domain.Inventory;
using Kst.Domain.Mps;
using Kst.Domain.Workspaces;
using Kst.Infrastructure.ComponentDetail;
using Kst.Infrastructure.Mps;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Application.Tests.ComponentDetail;

// The test namespace itself is named "ComponentDetail", which would shadow the composed
// Kst.Application.ComponentDetail.ComponentDetail record in simple-name lookup; alias it for the
// factory helpers (matching the established BomServiceTests convention).
using ComponentDetailModel = Kst.Application.ComponentDetail.ComponentDetail;

/// <summary>
/// Stage 8D.5 composition tests: workspace/MPS-loaded gating (no OutOfScope/no effective date),
/// single source-reader + shared-inventory-reader composition, not-found semantics, and
/// Site/snapshot-id cache compatibility. Reuses the established test fakes (FakeClock,
/// FakeWorkspaceConfigurationService, InMemoryMpsSnapshotStore) — no new shared test
/// infrastructure beyond the component-specific reader fakes below.
/// </summary>
public sealed class ComponentDetailServiceTests
{
    private static readonly WorkspaceAssignment Workspace = new(
        AssignmentId: Guid.NewGuid(),
        DisplayName: "Test Workspace",
        Site: "SW",
        ProductLineFrom: null,
        ProductLineTo: null,
        ParentParts: ["ABC100"],
        IsTemporary: false,
        CoverageEndsOn: null,
        IsEnabled: true,
        SortOrder: 0);

    private static ComponentSourceFacts Facts(
        string part = "COMP1",
        string? description = "WIDGET",
        string? partStatusCode = "C",
        string? iosCode = "1234",
        decimal? standardCost = 1.5m,
        decimal? qctc = 2.5m) =>
        new(
            ComponentPart: part,
            Description: description,
            PartStatusCode: partStatusCode,
            IosCode: iosCode,
            StandardCost: standardCost,
            Qctc: qctc,
            TimeFence: 14,
            SafetyTime: 3m,
            SafetyStock: 100m,
            BuyerPlanner: "JSMITH",
            PurchaseLeadTimeDays: 21,
            InspectionLeadTimeDays: 2,
            CumulativeLeadTimeDays: 30,
            MinimumOrderQuantity: 500m,
            OrderMultiple: 100m);

    private static PartInventorySummary Inv(string part, decimal net, decimal nonNet, decimal rma = 0m) =>
        new(Site: "SW", part, net, nonNet, rma);

    private static ComponentDetailModel MakeDetail(string part, decimal net, decimal nonNet, string site = "SW") =>
        new(site, part, "OLD DESC", "C", "CURRENT", "1234", net, nonNet,
            1m, 1m, 14, 3m, 100m, "JSMITH", 21, 2, 30, 500m, 100m,
            new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero), IsStale: false, Warning: null);

    private static MpsSnapshot SeedLoadedMps(
        IMpsSnapshotStore store,
        Guid workspaceId,
        string site = "SW",
        params string[] parentParts)
    {
        var parents = parentParts.Length > 0 ? parentParts : ["ABC100"];
        var resolved = parents.Select(p => new MpsResolvedPart(p, "Description")).ToList();
        var snapshot = new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, site, resolved, []);
        store.SetLoaded(workspaceId, snapshot);
        return snapshot;
    }

    private static (
        ComponentDetailService Service,
        IMpsSnapshotStore MpsStore,
        InMemoryComponentDetailCacheStore Cache,
        FakeClock Clock,
        ComponentSourceFake Source,
        InventoryReaderFake Inventory)
        BuildService(ComponentSourceFake? source = null, InventoryReaderFake? inventory = null)
    {
        var mpsStore = new InMemoryMpsSnapshotStore();
        var cache = new InMemoryComponentDetailCacheStore();
        var clock = new FakeClock();
        var src = source ?? new ComponentSourceFake();
        var inv = inventory ?? new InventoryReaderFake();

        var service = new ComponentDetailService(
            new FakeWorkspaceConfigurationService(Workspace),
            mpsStore,
            src.Reader,
            inv.Reader,
            cache,
            clock,
            NullLogger<ComponentDetailService>.Instance);

        return (service, mpsStore, cache, clock, src, inv);
    }

    // ---------- Scope / workspace ----------

    [Fact]
    public async Task GetComponentDetailAsync_Throws_For_Unknown_Workspace()
    {
        var (service, _, _, _, _, _) = BuildService();

        await Assert.ThrowsAsync<ComponentWorkspaceNotFoundException>(() =>
            service.GetComponentDetailAsync(Guid.NewGuid(), "COMP1"));
    }

    [Fact]
    public async Task GetComponentDetailAsync_Returns_MpsNotLoaded_When_No_Snapshot_Exists()
    {
        var (service, _, _, _, source, _) = BuildService();

        var result = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(ComponentDetailOutcomeKind.MpsNotLoaded, result.Kind);
        Assert.Equal(0, source.CallCount);
    }

    [Fact]
    public async Task GetComponentDetailAsync_Does_Not_Require_Part_In_Resolved_Mps_Scope()
    {
        // Deliberately unlike Bom/PartDetail: any component with a pt_mstr row is servable
        // regardless of BOM/MPS-parent membership.
        var (service, store, _, _, source, _) = BuildService(
            source: new ComponentSourceFake(Facts("ZZZ999")));
        SeedLoadedMps(store, Workspace.AssignmentId); // resolved parents = ["ABC100"]

        var result = await service.GetComponentDetailAsync(Workspace.AssignmentId, "ZZZ999");

        Assert.Equal(ComponentDetailOutcomeKind.Loaded, result.Kind);
        Assert.Equal(1, source.CallCount);
    }

    // ---------- Not found ----------

    [Fact]
    public async Task GetComponentDetailAsync_Returns_NotFound_When_Source_Reader_Returns_Null()
    {
        var source = new ComponentSourceFake(facts: null);
        var (service, store, cache, _, _, inventory) = BuildService(source: source);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(ComponentDetailOutcomeKind.NotFound, result.Kind);
        Assert.Null(cache.Get(Workspace.AssignmentId, "COMP1"));
        // A missing pt_mstr row must short-circuit before the inventory read.
        Assert.Equal(0, inventory.CallCount);
    }

    [Fact]
    public async Task NotFound_Never_Replaces_An_Existing_LastGood_Cache_Entry()
    {
        var source = new ComponentSourceFake(Facts("COMP1"));
        var (service, store, cache, _, _, _) = BuildService(source: source);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var first = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");
        Assert.Equal(ComponentDetailOutcomeKind.Loaded, first.Kind);

        SeedLoadedMps(store, Workspace.AssignmentId); // new generation forces a reload...
        source.Facts = null; // ...the component was removed from pt_mstr entirely.

        var second = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(ComponentDetailOutcomeKind.NotFound, second.Kind);
        // The prior loaded entry is untouched (NotFound is not a failure and is not stale-eligible).
        var entry = cache.Get(Workspace.AssignmentId, "COMP1");
        Assert.NotNull(entry);
        Assert.False(entry!.Detail.IsStale);
    }

    // ---------- Composition ----------

    [Fact]
    public async Task GetComponentDetailAsync_Composes_Detail_From_Source_And_Inventory()
    {
        var source = new ComponentSourceFake(Facts("COMP1", partStatusCode: "O"));
        var inventory = new InventoryReaderFake(handler: (_, parts) =>
            parts.Select(p => Inv(p, net: 42m, nonNet: 7m)).ToList());
        var (service, store, _, _, _, _) = BuildService(source: source, inventory: inventory);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(ComponentDetailOutcomeKind.Loaded, result.Kind);
        var detail = result.Detail!;
        Assert.Equal("SW", detail.Site);
        Assert.Equal("COMP1", detail.ComponentPart);
        Assert.Equal("WIDGET", detail.Description);
        Assert.Equal("O", detail.PartStatusCode);
        Assert.Equal("OBSOLETE", detail.PartStatusDescription);
        Assert.Equal(42m, detail.NetQuantityOnHand);
        Assert.Equal(7m, detail.NonNetQuantityOnHand);
        Assert.Equal(1.5m, detail.StandardCost);
        Assert.Equal(2.5m, detail.Qctc);
        Assert.False(detail.IsStale);
        Assert.Null(detail.Warning);
    }

    [Fact]
    public async Task GetComponentDetailAsync_Unrecognized_PartStatusCode_Yields_Null_Description()
    {
        var source = new ComponentSourceFake(Facts("COMP1", partStatusCode: "ZZ"));
        var (service, store, _, _, _, _) = BuildService(source: source);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal("ZZ", result.Detail!.PartStatusCode);
        Assert.Null(result.Detail.PartStatusDescription);
    }

    [Fact]
    public async Task GetComponentDetailAsync_Missing_StandardCost_And_Qctc_Are_Null_Not_Failures()
    {
        var source = new ComponentSourceFake(Facts("COMP1", standardCost: null, qctc: null));
        var (service, store, _, _, _, _) = BuildService(source: source);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(ComponentDetailOutcomeKind.Loaded, result.Kind);
        Assert.Null(result.Detail!.StandardCost);
        Assert.Null(result.Detail.Qctc);
    }

    // ---------- Cache / freshness ----------

    [Fact]
    public async Task Fresh_Cache_Hit_Skips_Both_Readers()
    {
        var source = new ComponentSourceFake(Facts("COMP1"));
        var inventory = new InventoryReaderFake();
        var (service, store, _, _, _, _) = BuildService(source: source, inventory: inventory);
        SeedLoadedMps(store, Workspace.AssignmentId);

        for (var i = 0; i < 3; i++)
        {
            var result = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");
            Assert.Equal(ComponentDetailOutcomeKind.Loaded, result.Kind);
        }

        Assert.Equal(1, source.CallCount);
        Assert.Equal(1, inventory.CallCount);
    }

    [Fact]
    public async Task Successful_Mps_Refresh_Forces_New_Load()
    {
        var source = new ComponentSourceFake(Facts("COMP1"));
        var (service, store, _, _, _, _) = BuildService(source: source);
        SeedLoadedMps(store, Workspace.AssignmentId);

        await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");
        SeedLoadedMps(store, Workspace.AssignmentId); // successful refresh -> new snapshot generation
        await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(2, source.CallCount);
    }

    [Fact]
    public async Task Failed_Mps_Refresh_Does_Not_Invalidate_Fresh_Cache()
    {
        var source = new ComponentSourceFake(Facts("COMP1"));
        var (service, store, _, _, _, _) = BuildService(source: source);
        SeedLoadedMps(store, Workspace.AssignmentId);

        await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");

        store.SetFailed(Workspace.AssignmentId, "QAD database connectivity failed.");
        var result = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(ComponentDetailOutcomeKind.Loaded, result.Kind);
        Assert.False(result.Detail!.IsStale);
        Assert.Equal(1, source.CallCount);
    }

    [Fact]
    public async Task Stale_LastGood_Served_On_Reload_Failure()
    {
        var source = new ComponentSourceFake(Facts("COMP1"));
        var (service, store, _, _, _, _) = BuildService(source: source);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var first = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");
        Assert.False(first.Detail!.IsStale);

        SeedLoadedMps(store, Workspace.AssignmentId); // new generation forces a reload...
        source.Error = new InvalidOperationException("QAD database connectivity failed."); // ...which fails.

        var second = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(ComponentDetailOutcomeKind.Loaded, second.Kind);
        Assert.True(second.Detail!.IsStale);
        Assert.NotNull(second.Detail.Warning);
        Assert.Equal(first.Detail.Description, second.Detail.Description);
    }

    [Fact]
    public async Task Unavailable_When_No_Compatible_Cache_Exists_On_Reload_Failure()
    {
        var source = new ComponentSourceFake(facts: null);
        source.Error = new InvalidOperationException("QAD database connectivity failed.");
        var (service, store, _, _, _, _) = BuildService(source: source);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(ComponentDetailOutcomeKind.Unavailable, result.Kind);
    }

    [Fact]
    public async Task Duplicate_Inventory_Summary_Falls_Back_To_Stale_Cache()
    {
        var source = new ComponentSourceFake(Facts("COMP1"));
        var inventory = new InventoryReaderFake(handler: (_, parts) =>
            parts.Select(p => Inv(p, net: 11m, nonNet: 1m)).ToList());
        var (service, store, _, _, _, _) = BuildService(source: source, inventory: inventory);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var first = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");
        Assert.Equal(ComponentDetailOutcomeKind.Loaded, first.Kind);

        SeedLoadedMps(store, Workspace.AssignmentId);
        inventory.Handler = (_, _) => new[]
        {
            Inv("COMP1", net: 1m, nonNet: 1m),
            Inv("COMP1", net: 2m, nonNet: 2m),
        };

        var second = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(ComponentDetailOutcomeKind.Loaded, second.Kind);
        Assert.True(second.Detail!.IsStale);
        Assert.Equal(11m, second.Detail.NetQuantityOnHand);
    }

    [Fact]
    public async Task Missing_Inventory_Summary_Falls_Back_To_Stale_Cache()
    {
        var source = new ComponentSourceFake(Facts("COMP1"));
        var inventory = new InventoryReaderFake(handler: (_, parts) =>
            parts.Select(p => Inv(p, net: 11m, nonNet: 1m)).ToList());
        var (service, store, _, _, _, _) = BuildService(source: source, inventory: inventory);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var first = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");
        Assert.Equal(ComponentDetailOutcomeKind.Loaded, first.Kind);

        SeedLoadedMps(store, Workspace.AssignmentId);
        inventory.Handler = (_, _) => [];

        var second = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(ComponentDetailOutcomeKind.Loaded, second.Kind);
        Assert.True(second.Detail!.IsStale);
        Assert.Equal(11m, second.Detail.NetQuantityOnHand);
    }

    [Fact]
    public async Task Different_Site_Cache_Entry_Is_Not_Fresh_Hit()
    {
        var source = new ComponentSourceFake(Facts("COMP1"));
        var (service, store, cache, _, _, _) = BuildService(source: source);
        var snapshot = SeedLoadedMps(store, Workspace.AssignmentId);

        cache.Set(Workspace.AssignmentId, "COMP1", new ComponentDetailCacheEntry(
            Workspace.AssignmentId,
            Site: "OTHER",
            "COMP1",
            snapshot.Id,
            MakeDetail("COMP1", net: 999m, nonNet: 999m, site: "OTHER")));

        var result = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(ComponentDetailOutcomeKind.Loaded, result.Kind);
        Assert.Equal(1, source.CallCount);
        Assert.Equal("SW", cache.Get(Workspace.AssignmentId, "COMP1")!.Site);
    }

    [Fact]
    public async Task Different_Site_Cache_Entry_Is_Not_Stale_Eligible()
    {
        var source = new ComponentSourceFake(Facts("COMP1"));
        source.Error = new InvalidOperationException("QAD database connectivity failed.");
        var (service, store, cache, _, _, _) = BuildService(source: source);
        var snapshot = SeedLoadedMps(store, Workspace.AssignmentId);

        cache.Set(Workspace.AssignmentId, "COMP1", new ComponentDetailCacheEntry(
            Workspace.AssignmentId,
            Site: "OTHER",
            "COMP1",
            snapshot.Id,
            MakeDetail("COMP1", net: 999m, nonNet: 999m, site: "OTHER")));

        var result = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(ComponentDetailOutcomeKind.Unavailable, result.Kind);
    }

    // ---------- Cancellation ----------

    [Fact]
    public async Task GetComponentDetailAsync_Source_Reader_Cancellation_Propagates()
    {
        var source = new ComponentSourceFake(Facts("COMP1"));
        source.Error = new OperationCanceledException();
        var (service, store, _, _, _, _) = BuildService(source: source);
        SeedLoadedMps(store, Workspace.AssignmentId);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1"));
    }

    [Fact]
    public async Task GetComponentDetailAsync_Inventory_Reader_Cancellation_Propagates()
    {
        var inventory = new InventoryReaderFake();
        inventory.Error = new OperationCanceledException();
        var (service, store, _, _, _, _) = BuildService(
            source: new ComponentSourceFake(Facts("COMP1")), inventory: inventory);
        SeedLoadedMps(store, Workspace.AssignmentId);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1"));
    }

    [Fact]
    public async Task Source_Reader_Cancellation_Does_Not_Serve_Stale_Cache()
    {
        var source = new ComponentSourceFake(Facts("COMP1"));
        var (service, store, cache, _, _, _) = BuildService(source: source);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var first = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");
        Assert.False(first.Detail!.IsStale);

        SeedLoadedMps(store, Workspace.AssignmentId);
        source.Error = new OperationCanceledException();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1"));

        var entry = cache.Get(Workspace.AssignmentId, "COMP1");
        Assert.NotNull(entry);
        Assert.False(entry!.Detail.IsStale);
        Assert.Equal(first.Detail.LoadedAtUtc, entry.Detail.LoadedAtUtc);
    }

    [Fact]
    public async Task Inventory_Reader_Cancellation_Does_Not_Serve_Stale_Cache()
    {
        var inventory = new InventoryReaderFake();
        var (service, store, cache, _, _, _) = BuildService(
            source: new ComponentSourceFake(Facts("COMP1")), inventory: inventory);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var first = await service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1");
        Assert.False(first.Detail!.IsStale);

        SeedLoadedMps(store, Workspace.AssignmentId);
        inventory.Error = new OperationCanceledException();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.GetComponentDetailAsync(Workspace.AssignmentId, "COMP1"));

        var entry = cache.Get(Workspace.AssignmentId, "COMP1");
        Assert.NotNull(entry);
        Assert.False(entry!.Detail.IsStale);
        Assert.Equal(first.Detail.LoadedAtUtc, entry.Detail.LoadedAtUtc);
    }

    // ---------- Fakes ----------

    /// <summary>
    /// Deterministic <see cref="IComponentSourceReader"/> fake recording calls; returns
    /// <see cref="Facts"/> (null represents "no pt_mstr row") or throws <see cref="Error"/>.
    /// </summary>
    private sealed class ComponentSourceFake
    {
        public int CallCount { get; private set; }
        public string? LastSite { get; private set; }
        public string? LastComponentPart { get; private set; }
        public ComponentSourceFacts? Facts { get; set; }
        public Exception? Error { get; set; }
        public IComponentSourceReader Reader { get; }

        public ComponentSourceFake(ComponentSourceFacts? facts = null)
        {
            Facts = facts;
            Reader = new DelegateComponentSourceReader((site, componentPart, _) =>
            {
                CallCount++;
                LastSite = site;
                LastComponentPart = componentPart;
                if (Error is not null)
                    throw Error;
                return Task.FromResult(Facts);
            });
        }
    }

    /// <summary>
    /// Deterministic <see cref="IPartInventoryReader"/> fake recording calls; default returns a
    /// valid one-summary-per-requested-part result (net 10 / non-net 5), satisfying the accepted
    /// reader contract so composition tests focus on composition.
    /// </summary>
    private sealed class InventoryReaderFake
    {
        public int CallCount { get; private set; }
        public string? LastSite { get; private set; }
        public IReadOnlyList<string>? LastPartNumbers { get; private set; }
        public Func<string, IReadOnlyList<string>, IReadOnlyList<PartInventorySummary>> Handler { get; set; }
        public Exception? Error { get; set; }
        public IPartInventoryReader Reader { get; }

        public InventoryReaderFake(
            Func<string, IReadOnlyList<string>, IReadOnlyList<PartInventorySummary>>? handler = null)
        {
            Handler = handler ?? ((_, parts) => parts.Select(p => Inv(p, net: 10m, nonNet: 5m)).ToList());
            Reader = new DelegatePartInventoryReader((site, partNumbers, _) =>
            {
                CallCount++;
                LastSite = site;
                LastPartNumbers = partNumbers;
                if (Error is not null)
                    throw Error;
                return Task.FromResult(Handler(site, partNumbers));
            });
        }
    }
}
