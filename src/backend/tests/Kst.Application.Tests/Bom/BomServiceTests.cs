using Kst.Application.Bom;
using Kst.Application.Inventory;
using Kst.Application.Mps;
using Kst.Application.Tests.Mps;
using Kst.Application.Tests.PartDetail;
using Kst.Domain.Bom;
using Kst.Domain.Common;
using Kst.Domain.Inventory;
using Kst.Domain.Mps;
using Kst.Domain.Workspaces;
using Kst.Infrastructure.Bom;
using Kst.Infrastructure.Mps;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Application.Tests.Bom;

// The test namespace itself is named "Bom", which would shadow the composed
// Kst.Application.Bom.Bom record in simple-name lookup; alias it for the factory helpers.
using BomModel = Kst.Application.Bom.Bom;

/// <summary>
/// Stage 8D.3 composition tests: workspace/MPS scope, effective date from the injected clock,
/// P/M scheduler-visibility filtering, batch inventory composition by PartNumber (Amendment 2
/// completeness validation), and Site/effective-date/snapshot-id cache compatibility
/// (Amendment 1). Reuses the established test fakes (FakeClock, FakeWorkspaceConfigurationService,
/// InMemoryMpsSnapshotStore) — no new test infrastructure.
/// </summary>
public sealed class BomServiceTests
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

    /// <summary>Matches <see cref="FakeClock.LocalNow"/> (2026-08-10).</summary>
    private static readonly DateOnly DefaultEffectiveDate = new(2026, 8, 10);

    private static BomOccurrence Occ(
        string key,
        int level,
        string part,
        string? pm,
        bool phantom = false,
        string? description = null,
        decimal? qtyPer = 1m,
        decimal? scrap = 0m) =>
        new(key, level, part, pm, phantom, description, qtyPer, scrap);

    private static PartInventorySummary Inv(string part, decimal net, decimal nonNet, decimal rma = 0m) =>
        new(Site: "SW", part, net, nonNet, rma);

    private static BomLine Line(string key, string part, decimal net, decimal nonNet) =>
        new(key, 1, part, "P", false, null, 1m, 0m, net, nonNet);

    private static BomModel MakeBom(IReadOnlyList<BomLine> lines, DateOnly effectiveDate, string site = "SW") =>
        new(site, "ABC100", effectiveDate, lines,
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
        BomService Service,
        IMpsSnapshotStore MpsStore,
        InMemoryBomCacheStore Cache,
        FakeClock Clock,
        BomSourceFake Bom,
        InventoryReaderFake Inventory)
        BuildService(BomSourceFake? bomSource = null, InventoryReaderFake? inventory = null)
    {
        var mpsStore = new InMemoryMpsSnapshotStore();
        var cache = new InMemoryBomCacheStore();
        var clock = new FakeClock();
        var bom = bomSource ?? new BomSourceFake();
        var inv = inventory ?? new InventoryReaderFake();

        var service = new BomService(
            new FakeWorkspaceConfigurationService(Workspace),
            mpsStore,
            bom.Reader,
            inv.Reader,
            cache,
            clock,
            NullLogger<BomService>.Instance);

        return (service, mpsStore, cache, clock, bom, inv);
    }

    // ---------- Scope / workspace ----------

    [Fact]
    public async Task GetBomAsync_Throws_For_Unknown_Workspace()
    {
        var (service, _, _, _, _, _) = BuildService();

        await Assert.ThrowsAsync<BomWorkspaceNotFoundException>(() =>
            service.GetBomAsync(Guid.NewGuid(), "ABC100"));
    }

    [Fact]
    public async Task GetBomAsync_Returns_MpsNotLoaded_When_No_Snapshot_Exists()
    {
        var (service, _, _, _, bom, _) = BuildService();

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.MpsNotLoaded, result.Kind);
        Assert.Equal(0, bom.CallCount);
    }

    [Fact]
    public async Task GetBomAsync_Returns_OutOfScope_For_Part_Not_In_Resolved_Scope()
    {
        var (service, store, _, _, bom, inventory) = BuildService();
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ZZZ999");

        Assert.Equal(BomOutcomeKind.OutOfScope, result.Kind);
        Assert.Equal(0, bom.CallCount);
        Assert.Equal(0, inventory.CallCount);
    }

    [Fact]
    public async Task GetBomAsync_Matches_Parent_Case_Insensitively()
    {
        var (service, store, _, _, bom, _) = BuildService();
        SeedLoadedMps(store, Workspace.AssignmentId, parentParts: ["ABC100"]);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "abc100");

        Assert.Equal(BomOutcomeKind.Loaded, result.Kind);
        // Scope matched case-insensitively; the trimmed (case-preserved) parent is what the reader gets.
        Assert.Equal("abc100", bom.LastParentPart);
    }

    [Fact]
    public async Task GetBomAsync_Valid_InScope_Parent_Proceeds_To_Load()
    {
        var (service, store, _, _, _, _) = BuildService();
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Loaded, result.Kind);
        Assert.NotNull(result.Bom);
        Assert.Equal("SW", result.Bom!.Site);
        Assert.Equal("ABC100", result.Bom.ParentPart);
        Assert.False(result.Bom.IsStale);
        Assert.Null(result.Bom.Warning);
    }

    // ---------- Effective date ----------

    [Fact]
    public async Task GetBomAsync_EffectiveDate_Follows_Injected_Clock_And_Is_Reported()
    {
        var (service, store, _, clock, _, _) = BuildService();
        SeedLoadedMps(store, Workspace.AssignmentId);

        // The response reports the effective date derived from the injected clock...
        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");
        Assert.Equal(DefaultEffectiveDate, result.Bom!.EffectiveDate);

        // ...and it genuinely follows the clock, not a captured value.
        clock.LocalNow = new DateTimeOffset(2026, 9, 1, 3, 30, 0, TimeSpan.Zero);
        var advanced = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");
        Assert.Equal(new DateOnly(2026, 9, 1), advanced.Bom!.EffectiveDate);
    }

    [Fact]
    public async Task GetBomAsync_Passes_Exact_EffectiveDate_To_Structural_Reader()
    {
        var (service, store, _, clock, bom, _) = BuildService();
        SeedLoadedMps(store, Workspace.AssignmentId);

        await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(DateOnly.FromDateTime(clock.LocalNow.Date), bom.LastEffectiveDate);
        Assert.Equal("SW", bom.LastSite);
    }

    // ---------- P/M filtering / composition ----------

    [Fact]
    public async Task Composes_Only_Pm_Visible_Occurrences()
    {
        var (service, store, _, _, _, _) = BuildService(bomSource: new BomSourceFake(new[]
        {
            Occ("k1", 1, "A1", "P"),
            Occ("k2", 1, "A2", "M"),
            Occ("k3", 1, "A3", "N"),
            Occ("k4", 2, "A4", "S"),
            Occ("k5", 2, "A5", "2"),
            Occ("k6", 2, "A6", "3"),
            Occ("k7", 2, "A7", "4"),
            Occ("k8", 2, "A8", "C"),
            Occ("k9", 2, "A9", "D"),
            Occ("k10", 2, "A10", null),
            Occ("k11", 2, "A11", "   "),
        }));
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Loaded, result.Kind);
        Assert.Equal(new[] { "A1", "A2" }, result.Bom!.Lines.Select(l => l.ComponentPart).ToList());
    }

    [Fact]
    public async Task Pm_Visibility_Is_Trim_And_Case_Insensitive()
    {
        var (service, store, _, _, _, _) = BuildService(bomSource: new BomSourceFake(new[]
        {
            Occ("k1", 1, "A1", "p"),
            Occ("k2", 1, "A2", " M "),
            Occ("k3", 1, "A3", "P"),
            Occ("k4", 1, "A4", "m"),
            Occ("k5", 1, "A5", " n "),
            Occ("k6", 1, "A6", "S"),
        }));
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(new[] { "A1", "A2", "A3", "A4" }, result.Bom!.Lines.Select(l => l.ComponentPart).ToList());
    }

    [Fact]
    public async Task Phantom_Pm_Rows_Remain_Visible_With_Phantom_Flag()
    {
        var (service, store, _, _, _, _) = BuildService(bomSource: new BomSourceFake(new[]
        {
            Occ("k1", 1, "A1", "P", phantom: true),
            Occ("k2", 2, "A2", "N", phantom: true),
        }));
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        var line = Assert.Single(result.Bom!.Lines);
        Assert.Equal("A1", line.ComponentPart);
        Assert.True(line.IsPhantom);
    }

    [Fact]
    public async Task Hidden_Intermediate_Preserves_Level_Gap()
    {
        var (service, store, _, _, _, _) = BuildService(bomSource: new BomSourceFake(new[]
        {
            Occ("k1", 1, "A1", "P"),
            Occ("k2", 2, "A2", "N"),
            Occ("k3", 3, "A3", "M"),
        }));
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        // The hidden Level-2 row is omitted; the Level-3 descendant stays at Level 3 (gap kept).
        Assert.Equal(new[] { 1, 3 }, result.Bom!.Lines.Select(l => l.Level).ToList());
        Assert.Equal("A3", result.Bom.Lines[1].ComponentPart);
    }

    [Fact]
    public async Task Preserves_Structural_Order_After_Filtering()
    {
        var (service, store, _, _, _, _) = BuildService(bomSource: new BomSourceFake(new[]
        {
            Occ("k1", 1, "B", "P"),
            Occ("k2", 1, "A", "N"),
            Occ("k3", 2, "C", "M"),
            Occ("k4", 1, "D", "P"),
            Occ("k5", 3, "E", "M"),
            Occ("k6", 2, "F", "S"),
        }));
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(new[] { "B", "C", "D", "E" }, result.Bom!.Lines.Select(l => l.ComponentPart).ToList());
    }

    [Fact]
    public async Task Preserves_Repeated_Occurrences()
    {
        var (service, store, _, _, _, _) = BuildService(bomSource: new BomSourceFake(new[]
        {
            Occ("k1", 1, "A1", "P"),
            Occ("k2", 2, "A1", "P"), // same component reached through a different path
        }));
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(2, result.Bom!.Lines.Count);
        Assert.Equal(new[] { "k1", "k2" }, result.Bom.Lines.Select(l => l.OccurrenceKey).ToList());
    }

    [Fact]
    public async Task Repeated_Components_Share_Inventory_Values()
    {
        var inv = new InventoryReaderFake(handler: (_, parts) =>
            parts.Select(p => Inv(p, net: 42m, nonNet: 7m)).ToList());
        var (service, store, _, _, _, _) = BuildService(
            bomSource: new BomSourceFake(new[]
            {
                Occ("k1", 1, "A1", "P"),
                Occ("k2", 2, "A1", "P"),
            }),
            inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        var lines = result.Bom!.Lines;
        Assert.Equal(2, lines.Count);
        Assert.All(lines, l =>
        {
            Assert.Equal(42m, l.NetQuantityOnHand);
            Assert.Equal(7m, l.NonNetQuantityOnHand);
        });
    }

    [Fact]
    public async Task Requests_Inventory_Once_For_Distinct_Visible_Part_Keys()
    {
        var inv = new InventoryReaderFake();
        var (service, store, _, _, _, _) = BuildService(
            bomSource: new BomSourceFake(new[]
            {
                Occ("k1", 1, "A1", "P"),
                Occ("k2", 1, "A1", "M"), // repeated component
                Occ("k3", 2, "a1", "P"), // case variant of A1
                Occ("k4", 2, "A2", "M"),
                Occ("k5", 2, "A3", "N"), // hidden
            }),
            inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Loaded, result.Kind);
        Assert.Equal(1, inv.CallCount);
        Assert.Equal("SW", inv.LastSite);
        Assert.Equal(2, inv.LastPartNumbers!.Count);
        Assert.Contains("A1", inv.LastPartNumbers);
        Assert.Contains("A2", inv.LastPartNumbers);
        Assert.DoesNotContain("A3", inv.LastPartNumbers);
    }

    [Fact]
    public async Task Hidden_Part_Keys_Are_Not_Requested()
    {
        var inv = new InventoryReaderFake();
        var (service, store, _, _, _, _) = BuildService(
            bomSource: new BomSourceFake(new[]
            {
                Occ("k1", 1, "A1", "P"),
                Occ("k2", 1, "A2", "N"),
                Occ("k3", 2, "A3", "S"),
            }),
            inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(new[] { "A1" }, inv.LastPartNumbers!);
    }

    [Fact]
    public async Task Matches_Inventory_By_PartNumber_Not_List_Position()
    {
        var inv = new InventoryReaderFake(handler: (_, _) => new[]
        {
            // Returned in a different order than the requested keys.
            Inv("A2", net: 20m, nonNet: 2m),
            Inv("A1", net: 10m, nonNet: 1m),
        });
        var (service, store, _, _, _, _) = BuildService(
            bomSource: new BomSourceFake(new[]
            {
                Occ("k1", 1, "A1", "P"),
                Occ("k2", 1, "A2", "P"),
            }),
            inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        var lineA1 = result.Bom!.Lines.Single(l => l.ComponentPart == "A1");
        var lineA2 = result.Bom.Lines.Single(l => l.ComponentPart == "A2");
        Assert.Equal(10m, lineA1.NetQuantityOnHand);
        Assert.Equal(1m, lineA1.NonNetQuantityOnHand);
        Assert.Equal(20m, lineA2.NetQuantityOnHand);
        Assert.Equal(2m, lineA2.NonNetQuantityOnHand);
    }

    [Fact]
    public async Task Maps_Net_And_NonNet_Quantity_On_Hand()
    {
        var inv = new InventoryReaderFake(handler: (_, _) => new[] { Inv("A1", net: 123.45m, nonNet: 67.8m) });
        var (service, store, _, _, _, _) = BuildService(
            bomSource: new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") }),
            inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        var line = Assert.Single(result.Bom!.Lines);
        Assert.Equal(123.45m, line.NetQuantityOnHand);
        Assert.Equal(67.8m, line.NonNetQuantityOnHand);
    }

    [Fact]
    public async Task Rma_Values_Do_Not_Reach_BomLine()
    {
        var inv = new InventoryReaderFake(handler: (_, _) =>
            new[] { Inv("A1", net: 5m, nonNet: 3m, rma: 999m) });
        var (service, store, _, _, _, _) = BuildService(
            bomSource: new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") }),
            inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        var line = Assert.Single(result.Bom!.Lines);
        Assert.Equal(5m, line.NetQuantityOnHand);
        Assert.Equal(3m, line.NonNetQuantityOnHand);
        // The presentation model has no RMA member at all.
        var propertyNames = typeof(BomLine).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain(propertyNames, name => name.Contains("Rma", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task No_Visible_Rows_Returns_Empty_Lines_And_Skips_Inventory()
    {
        var inv = new InventoryReaderFake();
        var (service, store, _, _, _, _) = BuildService(
            bomSource: new BomSourceFake(new[]
            {
                Occ("k1", 1, "A1", "N"),
                Occ("k2", 2, "A2", "S"),
            }),
            inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Loaded, result.Kind);
        Assert.Empty(result.Bom!.Lines);
        Assert.Equal(0, inv.CallCount);
    }

    [Fact]
    public async Task Structural_Empty_Returns_Empty_Lines()
    {
        var inv = new InventoryReaderFake();
        var (service, store, _, _, _, _) = BuildService(inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Loaded, result.Kind);
        Assert.Empty(result.Bom!.Lines);
        Assert.Equal(0, inv.CallCount);
    }

    [Fact]
    public async Task Structural_Failure_Returns_Unavailable_Never_Empty_Loading()
    {
        var (service, store, _, _, bom, _) = BuildService();
        SeedLoadedMps(store, Workspace.AssignmentId);
        bom.Error = new InvalidOperationException("QAD database connectivity failed.");

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Unavailable, result.Kind);
        Assert.Null(result.Bom);
    }

    [Fact]
    public async Task Inventory_Failure_Returns_Unavailable_And_Does_Not_Cache()
    {
        var inv = new InventoryReaderFake();
        inv.Error = new InvalidOperationException("QAD database connectivity failed.");
        var (service, store, cache, _, _, _) = BuildService(
            bomSource: new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") }),
            inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Unavailable, result.Kind);
        Assert.Null(cache.Get(Workspace.AssignmentId, "ABC100"));
    }

    [Fact]
    public async Task Explicit_Zero_Summary_Is_Valid_Zero_Inventory()
    {
        var inv = new InventoryReaderFake(handler: (_, _) => new[] { Inv("A1", net: 0m, nonNet: 0m) });
        var (service, store, _, _, _, _) = BuildService(
            bomSource: new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") }),
            inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Loaded, result.Kind);
        var line = Assert.Single(result.Bom!.Lines);
        Assert.Equal(0m, line.NetQuantityOnHand);
        Assert.Equal(0m, line.NonNetQuantityOnHand);
    }

    // ---------- Amendment 2: inventory result completeness ----------

    [Fact]
    public async Task Missing_Inventory_Summary_Is_Load_Failure()
    {
        var inv = new InventoryReaderFake(handler: (_, _) =>
            new[] { Inv("A1", net: 1m, nonNet: 1m) }); // A2 missing
        var (service, store, cache, _, _, _) = BuildService(
            bomSource: new BomSourceFake(new[]
            {
                Occ("k1", 1, "A1", "P"),
                Occ("k2", 1, "A2", "P"),
            }),
            inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Unavailable, result.Kind);
        Assert.Null(cache.Get(Workspace.AssignmentId, "ABC100"));
    }

    [Fact]
    public async Task Duplicate_Inventory_Summary_Is_Load_Failure()
    {
        var inv = new InventoryReaderFake(handler: (_, _) => new[]
        {
            Inv("A1", net: 1m, nonNet: 1m),
            Inv("A1", net: 2m, nonNet: 2m),
        });
        var (service, store, cache, _, _, _) = BuildService(
            bomSource: new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") }),
            inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Unavailable, result.Kind);
        Assert.Null(cache.Get(Workspace.AssignmentId, "ABC100"));
    }

    [Fact]
    public async Task Missing_Inventory_Summary_With_Same_Date_Cache_Returns_Stale()
    {
        var bom = new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") });
        var inv = new InventoryReaderFake(handler: (_, parts) =>
            parts.Select(p => Inv(p, net: 11m, nonNet: 1m)).ToList());
        var (service, store, cache, _, _, _) = BuildService(bomSource: bom, inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var first = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");
        Assert.Equal(11m, first.Bom!.Lines[0].NetQuantityOnHand);

        // Next generation + a reader that now drops a required summary.
        SeedLoadedMps(store, Workspace.AssignmentId);
        inv.Handler = (_, _) => new[] { Inv("A2", net: 9m, nonNet: 9m) }; // A1 missing

        var second = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Loaded, second.Kind);
        Assert.True(second.Bom!.IsStale);
        Assert.NotNull(second.Bom.Warning);
        Assert.Equal(11m, second.Bom.Lines[0].NetQuantityOnHand);
        // The failed partial load did not overwrite the last-good entry.
        Assert.Equal(first.Bom.LoadedAtUtc, cache.Get(Workspace.AssignmentId, "ABC100")!.Bom.LoadedAtUtc);
    }

    [Fact]
    public async Task Duplicate_Inventory_Summary_With_Same_Date_Cache_Returns_Stale()
    {
        var inv = new InventoryReaderFake(handler: (_, parts) =>
            parts.Select(p => Inv(p, net: 11m, nonNet: 1m)).ToList());
        var (service, store, _, _, _, _) = BuildService(
            bomSource: new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") }),
            inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var first = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");
        Assert.Equal(BomOutcomeKind.Loaded, first.Kind);

        SeedLoadedMps(store, Workspace.AssignmentId);
        inv.Handler = (_, _) => new[]
        {
            Inv("A1", net: 1m, nonNet: 1m),
            Inv("A1", net: 2m, nonNet: 2m),
        };

        var second = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Loaded, second.Kind);
        Assert.True(second.Bom!.IsStale);
        Assert.NotNull(second.Bom.Warning);
        Assert.Equal(11m, second.Bom.Lines[0].NetQuantityOnHand);
    }

    // ---------- Cancellation ----------

    [Fact]
    public async Task GetBomAsync_Structural_Reader_Cancellation_Propagates()
    {
        var bom = new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") });
        bom.Error = new OperationCanceledException();
        var (service, store, _, _, _, _) = BuildService(bomSource: bom);
        SeedLoadedMps(store, Workspace.AssignmentId);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.GetBomAsync(Workspace.AssignmentId, "ABC100"));
    }

    [Fact]
    public async Task GetBomAsync_Inventory_Reader_Cancellation_Propagates()
    {
        var inv = new InventoryReaderFake();
        inv.Error = new OperationCanceledException();
        var (service, store, _, _, _, _) = BuildService(
            bomSource: new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") }),
            inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.GetBomAsync(Workspace.AssignmentId, "ABC100"));
    }

    [Fact]
    public async Task GetBomAsync_Structural_Cancellation_Does_Not_Serve_Stale_Cache()
    {
        var bom = new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") });
        var (service, store, cache, _, _, _) = BuildService(bomSource: bom);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var first = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");
        Assert.Equal(BomOutcomeKind.Loaded, first.Kind);
        Assert.False(first.Bom!.IsStale);

        // A new generation makes a compatible stale fallback available if the reload fails...
        SeedLoadedMps(store, Workspace.AssignmentId);
        // ...but cancellation must propagate instead of serving it.
        bom.Error = new OperationCanceledException();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.GetBomAsync(Workspace.AssignmentId, "ABC100"));

        var entry = cache.Get(Workspace.AssignmentId, "ABC100");
        Assert.NotNull(entry);
        Assert.False(entry!.Bom.IsStale);
        Assert.Equal(first.Bom.LoadedAtUtc, entry.Bom.LoadedAtUtc);
    }

    [Fact]
    public async Task GetBomAsync_Inventory_Cancellation_Does_Not_Serve_Stale_Cache()
    {
        var inv = new InventoryReaderFake();
        var (service, store, cache, _, _, _) = BuildService(
            bomSource: new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") }),
            inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var first = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");
        Assert.Equal(BomOutcomeKind.Loaded, first.Kind);
        Assert.False(first.Bom!.IsStale);

        SeedLoadedMps(store, Workspace.AssignmentId);
        inv.Error = new OperationCanceledException();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.GetBomAsync(Workspace.AssignmentId, "ABC100"));

        var entry = cache.Get(Workspace.AssignmentId, "ABC100");
        Assert.NotNull(entry);
        Assert.False(entry!.Bom.IsStale);
        Assert.Equal(first.Bom.LoadedAtUtc, entry.Bom.LoadedAtUtc);
    }

    [Fact]
    public async Task GetBomAsync_Cancellation_Leaves_LastGood_Cache_Entry_Intact()
    {
        var bom = new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") });
        var (service, store, cache, _, _, _) = BuildService(bomSource: bom);
        SeedLoadedMps(store, Workspace.AssignmentId);

        await service.GetBomAsync(Workspace.AssignmentId, "ABC100");
        var before = cache.Get(Workspace.AssignmentId, "ABC100");
        Assert.NotNull(before);

        SeedLoadedMps(store, Workspace.AssignmentId);
        bom.Error = new OperationCanceledException();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.GetBomAsync(Workspace.AssignmentId, "ABC100"));

        // Reference-identical: the cancelled reload neither replaced nor mutated the entry.
        Assert.Same(before, cache.Get(Workspace.AssignmentId, "ABC100"));
        Assert.False(before!.Bom.IsStale);
        Assert.Null(before.Bom.Warning);
    }

    // ---------- Cache / freshness ----------

    [Fact]
    public async Task Fresh_Cache_Hit_Skips_Both_Readers()
    {
        var bom = new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") });
        var inv = new InventoryReaderFake();
        var (service, store, _, _, _, _) = BuildService(bomSource: bom, inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        for (var i = 0; i < 3; i++)
        {
            var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");
            Assert.Equal(BomOutcomeKind.Loaded, result.Kind);
        }

        Assert.Equal(1, bom.CallCount);
        Assert.Equal(1, inv.CallCount);
    }

    [Fact]
    public async Task Successful_Mps_Refresh_Forces_New_Load()
    {
        var bom = new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") });
        var (service, store, _, _, _, _) = BuildService(bomSource: bom);
        SeedLoadedMps(store, Workspace.AssignmentId);

        await service.GetBomAsync(Workspace.AssignmentId, "ABC100");
        SeedLoadedMps(store, Workspace.AssignmentId); // successful refresh → new snapshot generation
        await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(2, bom.CallCount);
    }

    [Fact]
    public async Task Failed_Mps_Refresh_Does_Not_Invalidate_Fresh_Cache()
    {
        var bom = new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") });
        var (service, store, _, _, _, _) = BuildService(bomSource: bom);
        SeedLoadedMps(store, Workspace.AssignmentId);

        await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        // A failed refresh retains the prior good snapshot id — no spurious reload.
        store.SetFailed(Workspace.AssignmentId, "QAD database connectivity failed.");
        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Loaded, result.Kind);
        Assert.False(result.Bom!.IsStale);
        Assert.Equal(1, bom.CallCount);
    }

    [Fact]
    public async Task Stale_LastGood_Served_On_Reload_Failure()
    {
        var bom = new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") });
        var (service, store, _, _, _, _) = BuildService(bomSource: bom);
        SeedLoadedMps(store, Workspace.AssignmentId);

        var first = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");
        Assert.False(first.Bom!.IsStale);

        SeedLoadedMps(store, Workspace.AssignmentId); // new generation forces a reload...
        bom.Error = new InvalidOperationException("QAD database connectivity failed."); // ...which fails.

        var second = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Loaded, second.Kind);
        Assert.True(second.Bom!.IsStale);
        Assert.NotNull(second.Bom.Warning);
        // Same-site/same-effective-date payload intact.
        Assert.Equal(first.Bom.Lines.Select(l => l.OccurrenceKey).ToList(),
            second.Bom.Lines.Select(l => l.OccurrenceKey).ToList());
    }

    [Fact]
    public async Task Different_EffectiveDate_Never_Stale_Compatible()
    {
        var bom = new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") });
        var (service, store, _, clock, _, _) = BuildService(bomSource: bom);
        SeedLoadedMps(store, Workspace.AssignmentId);

        await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        // The business date advances: yesterday's entry can be neither fresh nor stale.
        clock.LocalNow = new DateTimeOffset(2026, 8, 11, 5, 0, 0, TimeSpan.FromHours(-7));
        bom.Error = new InvalidOperationException("QAD database connectivity failed.");

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Unavailable, result.Kind);
    }

    [Fact]
    public async Task Advanced_EffectiveDate_Forces_New_Load()
    {
        var bom = new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") });
        var inv = new InventoryReaderFake(handler: (_, parts) =>
            parts.Select(p => Inv(p, net: 11m, nonNet: 1m)).ToList());
        var (service, store, cache, clock, _, _) = BuildService(bomSource: bom, inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        clock.LocalNow = new DateTimeOffset(2026, 8, 11, 5, 0, 0, TimeSpan.FromHours(-7));
        inv.Handler = (_, parts) => parts.Select(p => Inv(p, net: 22m, nonNet: 2m)).ToList();

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Loaded, result.Kind);
        Assert.Equal(new DateOnly(2026, 8, 11), result.Bom!.EffectiveDate);
        Assert.Equal(new DateOnly(2026, 8, 11), bom.LastEffectiveDate);
        Assert.Equal(22m, result.Bom.Lines[0].NetQuantityOnHand);
        Assert.Equal(new DateOnly(2026, 8, 11), cache.Get(Workspace.AssignmentId, "ABC100")!.EffectiveDate);
    }

    [Fact]
    public async Task Failed_Reload_Leaves_Prior_Cache_Entry_Touched()
    {
        var bom = new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") });
        var (service, store, cache, _, _, _) = BuildService(bomSource: bom);
        SeedLoadedMps(store, Workspace.AssignmentId);

        await service.GetBomAsync(Workspace.AssignmentId, "ABC100");
        var before = cache.Get(Workspace.AssignmentId, "ABC100");
        Assert.NotNull(before);

        SeedLoadedMps(store, Workspace.AssignmentId);
        bom.Error = new InvalidOperationException("QAD database connectivity failed.");
        await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        // The failed partial reload must not replace the last-good complete entry.
        var after = cache.Get(Workspace.AssignmentId, "ABC100");
        Assert.Same(before, after);
        Assert.False(after!.Bom.IsStale);
        Assert.Equal(before.Bom.LoadedAtUtc, after.Bom.LoadedAtUtc);
    }

    [Fact]
    public async Task Successful_Later_Reload_Replaces_Cache()
    {
        var bom = new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") });
        var inv = new InventoryReaderFake(handler: (_, parts) =>
            parts.Select(p => Inv(p, net: 11m, nonNet: 1m)).ToList());
        var (service, store, cache, _, _, _) = BuildService(bomSource: bom, inventory: inv);
        SeedLoadedMps(store, Workspace.AssignmentId);

        await service.GetBomAsync(Workspace.AssignmentId, "ABC100");
        var snapshot2 = SeedLoadedMps(store, Workspace.AssignmentId);

        bom.Error = new InvalidOperationException("QAD database connectivity failed.");
        var stale = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");
        Assert.True(stale.Bom!.IsStale);

        bom.Error = null;
        inv.Handler = (_, parts) => parts.Select(p => Inv(p, net: 33m, nonNet: 3m)).ToList();
        var fresh = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Loaded, fresh.Kind);
        Assert.False(fresh.Bom!.IsStale);
        Assert.Equal(33m, fresh.Bom.Lines[0].NetQuantityOnHand);
        var entry = cache.Get(Workspace.AssignmentId, "ABC100")!;
        Assert.Equal(snapshot2.Id, entry.LoadedAgainstMpsSnapshotId);
        Assert.Equal(33m, entry.Bom.Lines[0].NetQuantityOnHand);
    }

    [Fact]
    public async Task Different_Site_Cache_Entry_Is_Not_Fresh_Hit()
    {
        var bom = new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") });
        var (service, store, cache, clock, _, _) = BuildService(bomSource: bom);
        var snapshot = SeedLoadedMps(store, Workspace.AssignmentId);

        // Pre-seed the physical key with an entry from another site (e.g. a workspace site edit
        // whose snapshot generation did not advance). Same date, same snapshot id — only Site differs.
        cache.Set(Workspace.AssignmentId, "ABC100", new BomCacheEntry(
            Workspace.AssignmentId,
            Site: "OTHER",
            "ABC100",
            DateOnly.FromDateTime(clock.LocalNow.Date),
            snapshot.Id,
            MakeBom(new[] { Line("old", "OLD", 999m, 999m) }, DateOnly.FromDateTime(clock.LocalNow.Date))));

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        // The different-site entry was not served; a fresh load ran and replaced it.
        Assert.Equal(BomOutcomeKind.Loaded, result.Kind);
        Assert.Equal(1, bom.CallCount);
        Assert.NotEqual("OLD", result.Bom!.Lines[0].ComponentPart);
        Assert.Equal("SW", cache.Get(Workspace.AssignmentId, "ABC100")!.Site);
    }

    [Fact]
    public async Task Different_Site_Cache_Entry_Is_Not_Stale_Eligible()
    {
        var bom = new BomSourceFake(new[] { Occ("k1", 1, "A1", "P") });
        bom.Error = new InvalidOperationException("QAD database connectivity failed.");
        var (service, store, cache, clock, _, _) = BuildService(bomSource: bom);
        var snapshot = SeedLoadedMps(store, Workspace.AssignmentId);

        cache.Set(Workspace.AssignmentId, "ABC100", new BomCacheEntry(
            Workspace.AssignmentId,
            Site: "OTHER",
            "ABC100",
            DateOnly.FromDateTime(clock.LocalNow.Date),
            snapshot.Id,
            MakeBom(new[] { Line("old", "OLD", 999m, 999m) }, DateOnly.FromDateTime(clock.LocalNow.Date))));

        var result = await service.GetBomAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(BomOutcomeKind.Unavailable, result.Kind);
    }

    // ---------- Fakes ----------

    /// <summary>
    /// Deterministic <see cref="IBomSourceReader"/> fake recording calls; default returns the
    /// fixed occurrence list (empty by default).
    /// </summary>
    private sealed class BomSourceFake
    {
        private readonly Func<string, string, DateOnly, IReadOnlyList<BomOccurrence>> _defaultHandler;

        public int CallCount { get; private set; }
        public string? LastSite { get; private set; }
        public string? LastParentPart { get; private set; }
        public DateOnly? LastEffectiveDate { get; private set; }
        public Func<string, string, DateOnly, IReadOnlyList<BomOccurrence>> Handler { get; set; }
        public Exception? Error { get; set; }
        public IBomSourceReader Reader { get; }

        public BomSourceFake(IReadOnlyList<BomOccurrence>? defaultOccurrences = null)
        {
            _defaultHandler = (_, _, _) => defaultOccurrences ?? [];
            Handler = _defaultHandler;
            Reader = new DelegateBomSourceReader((site, parentPart, effectiveDate, _) =>
            {
                CallCount++;
                LastSite = site;
                LastParentPart = parentPart;
                LastEffectiveDate = effectiveDate;
                if (Error is not null)
                    throw Error;
                return Task.FromResult(Handler(site, parentPart, effectiveDate));
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
