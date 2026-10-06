using System.Text.Json;
using Kst.Application.Inventory;
using Kst.Application.Mps;
using Kst.Application.OpenOrders;
using Kst.Application.PlanningWorkbook;
using Kst.Application.Tests.PartDetail;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.Inventory;
using Kst.Domain.Mps;
using Kst.Domain.OpenOrders;
using Kst.Domain.PlanningWorkbook;
using Kst.Domain.Tests.PlanningWorkbook;
using Kst.Domain.Workspaces;
using Kst.Infrastructure.Mps;
using Kst.Infrastructure.OpenOrders;
using Kst.Infrastructure.PlanningWorkbook;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Application.Tests.PlanningWorkbook;

/// <summary>
/// Offline Stage 14.3 composition tests. All readers are fakes; no QAD connection is possible.
/// Weekly, Falldown and Undated expectations are read from the accepted Stage 14 fixture.
/// </summary>
public sealed class PlanningWorkbookServiceTests
{
    private static readonly Stage14Fixture Fixture = Stage14Fixture.Load();

    [Fact]
    public async Task Composition_Reproduces_Accepted_Fixture_Weekly_Falldown_And_Undated_Results()
    {
        var h = new Harness();
        var result = await h.Service.GetAsync(h.Id, h.MpsId, Fixture.HorizonWeeks, default);

        Assert.Equal(PlanningWorkbookOutcomeKind.Loaded, result.Kind);
        var entry = result.Entry!;
        Assert.False(entry.IsStale);
        Assert.Null(entry.Warning);
        Assert.Equal(h.MpsId, entry.Snapshot.MpsSnapshotId);
        Assert.Equal(h.OpenOrdersStore.Get(h.Id)!.Snapshot.Id, entry.Snapshot.OpenOrdersSnapshotId);
        Assert.Equal(Fixture.Today, entry.Snapshot.PriceEffectiveDate);
        Assert.Equal(h.Clock.UtcNow, entry.Snapshot.AcquiredAtUtc);
        Assert.Equal(Fixture.WeekLabels, result.Workbook!.WeekLabels);
        Assert.Equal(Fixture.ResolvedParts.Select(p => p.ParentPart), result.Workbook.Parts.Select(p => p.Part));

        foreach (var expected in Fixture.Root.GetProperty("expected").GetProperty("parts").EnumerateArray())
        {
            var block = result.Workbook.Parts.Single(p => p.Part == expected.GetProperty("part").GetString());
            Assert.Equal(Stage14Fixture.NullableDec(expected, "selectedPrice"), block.SelectedPrice);
            Assert.Equal(expected.GetProperty("priced").GetBoolean(), block.IsPriced);
            AssertBucket(expected.GetProperty("falldown"), block.Falldown);
            var weeks = expected.GetProperty("weeks").EnumerateArray().ToList();
            Assert.Equal(weeks.Count, block.Weeks.Count);
            foreach (var week in weeks)
                AssertBucket(week, block.Weeks.Single(w => w.WeekLabel == Stage14Fixture.Date(week.GetProperty("weekLabel"))));
            var undated = expected.GetProperty("undated");
            Assert.Equal(Stage14Fixture.Dec(undated, "so"), block.Undated.SalesOrderQuantity);
            Assert.Equal(Stage14Fixture.Dec(undated, "soDollars"), block.Undated.SalesOrderDollars);
            Assert.Equal(undated.GetProperty("lineCount").GetInt32(), block.Undated.LineCount);
        }
    }

    private static void AssertBucket(JsonElement expected, PlanningBucket actual)
    {
        // The workbook API has no adjustments yet, so only adjustment-independent values are compared.
        Assert.Equal(Stage14Fixture.Dec(expected, "so"), actual.SalesOrderQuantity);
        Assert.Equal(Stage14Fixture.Dec(expected, "soDollars"), actual.SalesOrderDollars);
        Assert.Equal(Stage14Fixture.Dec(expected, "mps"), actual.Mps);
        Assert.Equal(Stage14Fixture.NullableDec(expected, "mpsDollars"), actual.MpsDollars);
        Assert.Equal(Stage14Fixture.Dec(expected, "forecast"), actual.Forecast);
        Assert.Equal(Stage14Fixture.Dec(expected, "estOnHand"), actual.EstimatedOnHand);
        Assert.Equal(0m, actual.Adjustment);
    }

    [Fact]
    public async Task Cached_Requests_Avoid_Reads_Horizon_Change_Reuses_Facts_And_Refresh_Reacquires_All_Sources()
    {
        var h = new Harness();
        var first = await h.Service.GetAsync(h.Id, h.MpsId, 4, default);
        Assert.Equal((1, 1, 1, 1), h.Counts);

        var again = await h.Service.GetAsync(h.Id, h.MpsId, 4, default);
        var shorter = await h.Service.GetAsync(h.Id, h.MpsId, 2, default);
        Assert.Equal((1, 1, 1, 1), h.Counts);
        Assert.Equal(first.Entry!.Snapshot.Id, again.Entry!.Snapshot.Id);
        Assert.Equal(first.Entry.Snapshot.Id, shorter.Entry!.Snapshot.Id);
        Assert.Equal(2, shorter.Workbook!.WeekLabels.Count);
        Assert.Equal(Fixture.WeekLabels.Take(2), shorter.Workbook.WeekLabels);
        Assert.All(shorter.Workbook.Parts, p => Assert.Equal(2, p.Weeks.Count));

        var refreshed = await h.Service.RefreshAsync(h.Id, h.MpsId, 4, default);
        Assert.Equal((2, 2, 2, 2), h.Counts);
        Assert.Equal(PlanningWorkbookOutcomeKind.Loaded, refreshed.Kind);
        Assert.NotEqual(first.Entry.Snapshot.Id, refreshed.Entry!.Snapshot.Id);
        Assert.NotEqual(first.Entry.Snapshot.OpenOrdersSnapshotId, refreshed.Entry.Snapshot.OpenOrdersSnapshotId);
        Assert.False(refreshed.Entry.IsStale);
    }

    [Fact]
    public async Task Business_Week_Change_Reprojects_And_Price_Date_Change_Reacquires()
    {
        var h = new Harness();
        await h.Service.GetAsync(h.Id, h.MpsId, 4, default);
        h.Clock.LocalNow = h.Clock.LocalNow.AddDays(7);
        var nextWeek = await h.Service.GetAsync(h.Id, h.MpsId, 4, default);

        // GET reuses the shared Stage 13 report; inventory, prices and forecast are reacquired.
        Assert.Equal((1, 2, 2, 2), h.Counts);
        Assert.Equal(Fixture.Today.AddDays(7), nextWeek.Entry!.Snapshot.PriceEffectiveDate);
        Assert.Equal(Fixture.WeekLabels[1], nextWeek.Workbook!.WeekLabels[0]);
    }

    [Theory]
    [InlineData("openOrders")]
    [InlineData("inventory")]
    [InlineData("prices")]
    [InlineData("forecast")]
    public async Task Source_Failure_Keeps_Compatible_Last_Good_As_Stale_And_Never_Substitutes_Values(string failing)
    {
        var h = new Harness();
        var good = await h.Service.GetAsync(h.Id, h.MpsId, 4, default);
        h.Fail = failing;

        var stale = await h.Service.RefreshAsync(h.Id, h.MpsId, 4, default);

        Assert.Equal(PlanningWorkbookOutcomeKind.Loaded, stale.Kind);
        Assert.True(stale.Entry!.IsStale);
        Assert.Equal(PlanningWorkbookService.StaleWarning, stale.Entry.Warning);
        Assert.Equal(good.Entry!.Snapshot, stale.Entry.Snapshot);
        Assert.True(h.WorkbookStore.Get(h.Id)!.IsStale);
        Assert.Equal(
            good.Workbook!.Parts.Select(p => (p.NetQuantityOnHand, p.SelectedPrice, p.Falldown, Weeks: string.Join("|", p.Weeks))),
            stale.Workbook!.Parts.Select(p => (p.NetQuantityOnHand, p.SelectedPrice, p.Falldown, Weeks: string.Join("|", p.Weeks))));
    }

    [Theory]
    [InlineData("openOrders")]
    [InlineData("inventory")]
    [InlineData("prices")]
    [InlineData("forecast")]
    public async Task Source_Failure_Without_Last_Good_Is_Unavailable_And_Publishes_Nothing(string failing)
    {
        var h = new Harness { Fail = failing };

        var result = await h.Service.GetAsync(h.Id, h.MpsId, 4, default);

        Assert.Equal(PlanningWorkbookOutcomeKind.Unavailable, result.Kind);
        Assert.Null(result.Entry);
        Assert.Null(h.WorkbookStore.Get(h.Id));
    }

    [Fact]
    public async Task Malformed_Source_Results_Fail_Instead_Of_Becoming_Zero_Or_Unpriced()
    {
        var omitInventory = new Harness { OmitInventoryPart = "P-1" };
        Assert.Equal(PlanningWorkbookOutcomeKind.Unavailable, (await omitInventory.Service.GetAsync(omitInventory.Id, omitInventory.MpsId, 4, default)).Kind);
        var omitPrice = new Harness { OmitPricePart = "P-3" };
        Assert.Equal(PlanningWorkbookOutcomeKind.Unavailable, (await omitPrice.Service.GetAsync(omitPrice.Id, omitPrice.MpsId, 4, default)).Kind);
        Assert.Null(omitInventory.WorkbookStore.Get(omitInventory.Id));
        Assert.Null(omitPrice.WorkbookStore.Get(omitPrice.Id));
    }

    [Fact]
    public async Task Stale_Open_Orders_Report_Cannot_Produce_A_Current_Workbook()
    {
        var h = new Harness();
        var line = Fixture.SalesOrderLines[0];
        h.OpenOrdersStore.Set(h.Id, new OpenOrdersReport(
            new OpenOrdersSnapshot(SnapshotId.New(), h.MpsId, h.Clock.UtcNow, h.Id, h.Site, [line]), true, "stale"));

        var result = await h.Service.GetAsync(h.Id, h.MpsId, 4, default);

        Assert.Equal(PlanningWorkbookOutcomeKind.Unavailable, result.Kind);
        Assert.Null(h.WorkbookStore.Get(h.Id));
        Assert.Equal(0, h.Counts.Inventory);
    }

    [Fact]
    public async Task Mps_Change_During_Acquisition_Prevents_Publication()
    {
        var h = new Harness();
        h.DuringForecast = () => h.LoadMps(new SnapshotId(Guid.NewGuid()));

        var result = await h.Service.GetAsync(h.Id, h.MpsId, 4, default);

        Assert.Equal(PlanningWorkbookOutcomeKind.MpsSnapshotChanged, result.Kind);
        Assert.Null(h.WorkbookStore.Get(h.Id));
    }

    [Fact]
    public async Task Old_Mps_Workbook_Is_Never_Presented_For_A_New_Snapshot()
    {
        var h = new Harness();
        await h.Service.GetAsync(h.Id, h.MpsId, 4, default);
        var newMps = new SnapshotId(Guid.NewGuid());
        h.LoadMps(newMps);

        Assert.Equal(PlanningWorkbookOutcomeKind.MpsSnapshotChanged, (await h.Service.GetAsync(h.Id, h.MpsId, 4, default)).Kind);
        h.Fail = "inventory";
        // No compatible last-good for the new snapshot: unavailable, not the old-MPS workbook.
        Assert.Equal(PlanningWorkbookOutcomeKind.Unavailable, (await h.Service.GetAsync(h.Id, newMps, 4, default)).Kind);
        h.Fail = null;
        var reloaded = await h.Service.GetAsync(h.Id, newMps, 4, default);
        Assert.Equal(newMps, reloaded.Entry!.Snapshot.MpsSnapshotId);
    }

    [Fact]
    public async Task Workspace_And_Mps_Validation_Never_Invoke_Readers()
    {
        var h = new Harness();
        Assert.Equal(PlanningWorkbookOutcomeKind.UnknownWorkspace, (await h.Service.GetAsync(Guid.NewGuid(), h.MpsId, 4, default)).Kind);
        Assert.Equal(PlanningWorkbookOutcomeKind.MpsSnapshotChanged, (await h.Service.GetAsync(h.Id, SnapshotId.New(), 4, default)).Kind);
        var notLoaded = new Harness(loadMps: false);
        Assert.Equal(PlanningWorkbookOutcomeKind.MpsNotLoaded, (await notLoaded.Service.GetAsync(notLoaded.Id, notLoaded.MpsId, 4, default)).Kind);
        var wrongSite = new Harness(workspaceSite: "OTHER");
        Assert.Equal(PlanningWorkbookOutcomeKind.MpsSnapshotChanged, (await wrongSite.Service.GetAsync(wrongSite.Id, wrongSite.MpsId, 4, default)).Kind);
        Assert.Equal((0, 0, 0, 0), h.Counts);
        Assert.Equal((0, 0, 0, 0), notLoaded.Counts);
        Assert.Equal((0, 0, 0, 0), wrongSite.Counts);
    }

    [Fact]
    public async Task Workspace_Deleted_During_Acquisition_Returns_Unknown_And_Publishes_Nothing()
    {
        var h = new Harness();
        h.DuringForecast = () => h.Workspaces.Items.Clear();

        Assert.Equal(PlanningWorkbookOutcomeKind.UnknownWorkspace, (await h.Service.GetAsync(h.Id, h.MpsId, 4, default)).Kind);
        Assert.Null(h.WorkbookStore.Get(h.Id));
    }

    [Fact]
    public async Task Workspace_Site_Changed_During_Acquisition_Returns_Conflict_And_Publishes_Nothing()
    {
        var h = new Harness();
        h.DuringForecast = () => h.Workspaces.Items[0] = h.Workspace with { Site = "S9" };

        Assert.Equal(PlanningWorkbookOutcomeKind.MpsSnapshotChanged, (await h.Service.GetAsync(h.Id, h.MpsId, 4, default)).Kind);
        Assert.Null(h.WorkbookStore.Get(h.Id));
    }

    [Theory]
    [InlineData("deleted", PlanningWorkbookOutcomeKind.UnknownWorkspace)]
    [InlineData("resited", PlanningWorkbookOutcomeKind.MpsSnapshotChanged)]
    public async Task Workspace_Change_During_Failed_Refresh_Never_Returns_Or_Marks_The_Previous_Context_Workbook(
        string change, PlanningWorkbookOutcomeKind expected)
    {
        var h = new Harness();
        var good = await h.Service.GetAsync(h.Id, h.MpsId, 4, default);
        h.Fail = "forecast";
        h.DuringForecast = () =>
        {
            if (change == "deleted") h.Workspaces.Items.Clear();
            else h.Workspaces.Items[0] = h.Workspace with { Site = "S9" };
        };

        var result = await h.Service.RefreshAsync(h.Id, h.MpsId, 4, default);

        Assert.Equal(expected, result.Kind);
        Assert.Null(result.Entry);
        Assert.Null(result.Workbook);
        var stored = h.WorkbookStore.Get(h.Id)!;
        Assert.False(stored.IsStale);
        Assert.Equal(good.Entry!.Snapshot.Id, stored.Snapshot.Id);
    }

    [Fact]
    public async Task Workspace_Change_While_Waiting_For_The_Gate_Is_Detected_Before_Reuse_Or_Acquisition()
    {
        var h = new Harness();
        await h.Service.GetAsync(h.Id, h.MpsId, 4, default);
        var release = new TaskCompletionSource();
        var entered = new TaskCompletionSource();
        h.DuringForecast = () => { entered.TrySetResult(); release.Task.Wait(); };

        var first = Task.Run(() => h.Service.RefreshAsync(h.Id, h.MpsId, 4, default));
        await entered.Task;
        var second = h.Service.RefreshAsync(h.Id, h.MpsId, 4, default); // waits for the gate
        h.DuringForecast = null;
        h.Workspaces.Items.Clear();
        release.SetResult();

        Assert.Equal(PlanningWorkbookOutcomeKind.UnknownWorkspace, (await first).Kind);
        Assert.Equal(PlanningWorkbookOutcomeKind.UnknownWorkspace, (await second).Kind);
        Assert.Equal(2, h.Counts.Forecast); // initial GET + first refresh; the waiting request read nothing
    }

    [Fact]
    public async Task Empty_Resolved_Scope_Invokes_No_Source_Readers()
    {
        var h = new Harness(parts: []);

        var result = await h.Service.RefreshAsync(h.Id, h.MpsId, 4, default);

        Assert.Equal(PlanningWorkbookOutcomeKind.Loaded, result.Kind);
        Assert.Empty(result.Workbook!.Parts);
        Assert.Equal((0, 0, 0, 0), h.Counts);
    }

    [Fact]
    public async Task Cancellation_Propagates_And_Publishes_Nothing()
    {
        var h = new Harness();
        using var cts = new CancellationTokenSource();
        h.DuringForecast = cts.Cancel;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Service.GetAsync(h.Id, h.MpsId, 4, cts.Token));
        Assert.Null(h.WorkbookStore.Get(h.Id));
    }

    [Fact]
    public async Task Two_Workspaces_With_Overlapping_Parts_Remain_Isolated()
    {
        var mps = new InMemoryMpsSnapshotStore();
        var openOrders = new InMemoryOpenOrdersSnapshotStore();
        var workbooks = new InMemoryPlanningWorkbookSnapshotStore();
        var a = new Harness(mps: mps, openOrdersStore: openOrders, workbookStore: workbooks, workspaceSite: "S1", siteOverride: "S1");
        var b = new Harness(mps: mps, openOrdersStore: openOrders, workbookStore: workbooks, workspaceSite: "S2", siteOverride: "S2",
            id: Guid.NewGuid(), netQohOffset: 1000m);
        var workspaces = new MultiWorkspaceService([a.Workspace, b.Workspace]);
        var serviceA = a.CreateService(workspaces);
        var serviceB = b.CreateService(workspaces);

        var resultA = await serviceA.GetAsync(a.Id, a.MpsId, 4, default);
        var resultB = await serviceB.GetAsync(b.Id, b.MpsId, 4, default);
        Assert.Equal(Fixture.NetQoh["P-1"], resultA.Workbook!.Parts.Single(p => p.Part == "P-1").NetQuantityOnHand);
        Assert.Equal(Fixture.NetQoh["P-1"] + 1000m, resultB.Workbook!.Parts.Single(p => p.Part == "P-1").NetQuantityOnHand);
        Assert.Equal("S1", workbooks.Get(a.Id)!.Snapshot.Site);
        Assert.Equal("S2", workbooks.Get(b.Id)!.Snapshot.Site);

        // Refreshing B never changes A; A's MPS identity is not valid for B.
        var aSnapshot = workbooks.Get(a.Id)!.Snapshot.Id;
        await serviceB.RefreshAsync(b.Id, b.MpsId, 4, default);
        Assert.Equal(aSnapshot, workbooks.Get(a.Id)!.Snapshot.Id);
        Assert.Equal(PlanningWorkbookOutcomeKind.MpsSnapshotChanged, (await serviceB.GetAsync(b.Id, a.MpsId, 4, default)).Kind);
        b.Fail = "inventory";
        await serviceB.RefreshAsync(b.Id, b.MpsId, 4, default);
        Assert.True(workbooks.Get(b.Id)!.IsStale);
        Assert.False(workbooks.Get(a.Id)!.IsStale);
    }

    private static MpsSourceRow FutureMps(int weekIndex, decimal quantity = 5m)
    {
        var template = Fixture.MpsRows[0];
        var due = MpsBusinessCalendar.GetBusinessWeekStart(Fixture.Today).AddDays(7 * weekIndex + 2);
        return template with { DueDate = due, ReleaseDate = due, Quantity = quantity, WorkOrderId = $"HZ-{weekIndex}" };
    }

    [Fact]
    public async Task Horizon_Range_Falls_Back_To_Twelve_Weeks_And_Rejects_Larger_Requests_Without_Reads()
    {
        var h = new Harness();
        var loaded = await h.Service.GetAsync(h.Id, h.MpsId, 12, default);
        Assert.Equal(PlanningWorkbookOutcomeKind.Loaded, loaded.Kind);
        Assert.Equal(12, loaded.MaxHorizonWeeks);
        Assert.Equal(12, loaded.Workbook!.WeekLabels.Count);

        var fresh = new Harness();
        var tooLong = await fresh.Service.GetAsync(fresh.Id, fresh.MpsId, 13, default);
        Assert.Equal(PlanningWorkbookOutcomeKind.HorizonOutOfRange, tooLong.Kind);
        Assert.Equal(12, tooLong.MaxHorizonWeeks);
        Assert.Equal((0, 0, 0, 0), fresh.Counts);
        Assert.Null(fresh.WorkbookStore.Get(fresh.Id));
    }

    [Fact]
    public async Task Horizon_Range_Follows_Loaded_Mps_Beyond_72_Weeks_And_Recalculates_On_Snapshot_And_Week_Change()
    {
        var h = new Harness(loadMps: false);
        h.LoadMps(h.MpsId, [FutureMps(80)]);

        // Final MPS week index 80 -> maximum 82 weeks; the former fixed 72-week cap does not truncate it.
        var full = await h.Service.GetAsync(h.Id, h.MpsId, 82, default);
        Assert.Equal(PlanningWorkbookOutcomeKind.Loaded, full.Kind);
        Assert.Equal(82, full.MaxHorizonWeeks);
        Assert.Equal(82, full.Workbook!.WeekLabels.Count);
        Assert.Equal(5m, full.Workbook.Parts.Single(p => p.Part == Fixture.MpsRows[0].ParentPart).Weeks[80].Mps);
        Assert.Equal(PlanningWorkbookOutcomeKind.HorizonOutOfRange, (await h.Service.GetAsync(h.Id, h.MpsId, 83, default)).Kind);

        // Next business week: the same MPS week is now index 79, so the range shrinks to 81.
        h.Clock.LocalNow = h.Clock.LocalNow.AddDays(7);
        var nextWeek = await h.Service.GetAsync(h.Id, h.MpsId, 12, default);
        Assert.Equal(81, nextWeek.MaxHorizonWeeks);
        Assert.Equal(PlanningWorkbookOutcomeKind.HorizonOutOfRange, (await h.Service.GetAsync(h.Id, h.MpsId, 82, default)).Kind);

        // A new MPS snapshot without the distant supply returns to the 12-week minimum range.
        var newMps = SnapshotId.New();
        h.LoadMps(newMps);
        var reloaded = await h.Service.GetAsync(h.Id, newMps, 12, default);
        Assert.Equal(PlanningWorkbookOutcomeKind.Loaded, reloaded.Kind);
        Assert.Equal(12, reloaded.MaxHorizonWeeks);
        var rejected = await h.Service.GetAsync(h.Id, newMps, 81, default);
        Assert.Equal(PlanningWorkbookOutcomeKind.HorizonOutOfRange, rejected.Kind);
        Assert.Equal(12, rejected.MaxHorizonWeeks);
    }

    /// <summary>Counting fake sources around the shared Stage 14 fixture.</summary>
    private sealed class Harness
    {
        private readonly IReadOnlyList<MpsResolvedPart> _parts;
        private readonly decimal _netQohOffset;
        private int _openOrders, _inventory, _prices, _forecast;

        public Harness(
            bool loadMps = true, IReadOnlyList<MpsResolvedPart>? parts = null, string workspaceSite = "S1",
            InMemoryMpsSnapshotStore? mps = null, InMemoryOpenOrdersSnapshotStore? openOrdersStore = null,
            InMemoryPlanningWorkbookSnapshotStore? workbookStore = null, string? siteOverride = null,
            Guid? id = null, decimal netQohOffset = 0m)
        {
            Id = id ?? Fixture.Root.GetProperty("scope").GetProperty("workspaceAssignmentId").GetGuid();
            Site = siteOverride ?? Fixture.Root.GetProperty("scope").GetProperty("site").GetString()!;
            MpsId = new SnapshotId(id is null ? Fixture.LoadedSnapshotId : Guid.NewGuid());
            _parts = parts ?? Fixture.ResolvedParts;
            _netQohOffset = netQohOffset;
            Mps = mps ?? new InMemoryMpsSnapshotStore();
            OpenOrdersStore = openOrdersStore ?? new InMemoryOpenOrdersSnapshotStore();
            WorkbookStore = workbookStore ?? new InMemoryPlanningWorkbookSnapshotStore();
            Clock.LocalNow = new DateTimeOffset(Fixture.Today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.FromHours(-7));
            Clock.UtcNow = Clock.LocalNow.ToUniversalTime();
            Workspace = new WorkspaceAssignment(Id, "Test", workspaceSite, null, null, ["CONFIGURED"], false, null, true, 0);
            if (loadMps) LoadMps(MpsId);
            Workspaces = new MultiWorkspaceService([Workspace]);
            Service = CreateService(Workspaces);
        }

        public Guid Id { get; }
        public string Site { get; }
        public SnapshotId MpsId { get; }
        public WorkspaceAssignment Workspace { get; }
        public FakeClock Clock { get; } = new();
        public InMemoryMpsSnapshotStore Mps { get; }
        public InMemoryOpenOrdersSnapshotStore OpenOrdersStore { get; }
        public InMemoryPlanningWorkbookSnapshotStore WorkbookStore { get; }
        public PlanningWorkbookService Service { get; }
        public MultiWorkspaceService Workspaces { get; }
        public string? Fail { get; set; }
        public string? OmitInventoryPart { get; init; }
        public string? OmitPricePart { get; init; }
        public Action? DuringForecast { get; set; }
        public (int OpenOrders, int Inventory, int Prices, int Forecast) Counts => (_openOrders, _inventory, _prices, _forecast);

        public void LoadMps(SnapshotId id, IEnumerable<MpsSourceRow>? extraRows = null) =>
            Mps.SetLoaded(Id, new MpsSnapshot(id, Clock.UtcNow, Site, _parts,
                Fixture.MpsRows.Concat(extraRows ?? []).Select(r => r with { Site = Site }).ToList()));

        public PlanningWorkbookService CreateService(IWorkspaceConfigurationService workspaces)
        {
            var openOrders = new OpenOrdersService(workspaces, Mps,
                new DelegateOpenOrdersSourceReader((site, parents, _) =>
                {
                    _openOrders++;
                    Assert.Equal(Site, site);
                    Assert.Equal(_parts.Select(p => p.ParentPart), parents);
                    if (Fail == "openOrders") throw new InvalidOperationException("source down");
                    return Task.FromResult(Fixture.SalesOrderLines);
                }),
                OpenOrdersStore, Clock, NullLogger<OpenOrdersService>.Instance);
            return new PlanningWorkbookService(workspaces, Mps, openOrders,
                new DelegatePartInventoryReader((site, parts, _) =>
                {
                    _inventory++;
                    Assert.Equal(Site, site);
                    if (Fail == "inventory") throw new InvalidOperationException("source down");
                    return Task.FromResult<IReadOnlyList<PartInventorySummary>>(parts
                        .Where(p => p != OmitInventoryPart)
                        .Select(p => new PartInventorySummary(site, p, Fixture.NetQoh[p] + _netQohOffset, 0m, 0m)).ToList());
                }),
                new DelegatePlanningPriceReader((site, parts, date, _) =>
                {
                    _prices++;
                    Assert.Equal(Site, site);
                    Assert.Equal(DateOnly.FromDateTime(Clock.LocalNow.Date), date);
                    if (Fail == "prices") throw new InvalidOperationException("source down");
                    return Task.FromResult<IReadOnlyList<PlanningPartPrice>>(parts
                        .Where(p => p != OmitPricePart)
                        .Select(p => new PlanningPartPrice(p, Fixture.PriceTiers[p])).ToList());
                }),
                new DelegatePlanningForecastReader((site, parts, _) =>
                {
                    _forecast++;
                    Assert.Equal(Site, site);
                    DuringForecast?.Invoke();
                    if (Fail == "forecast") throw new InvalidOperationException("source down");
                    return Task.FromResult(Fixture.ForecastRows);
                }),
                WorkbookStore, Clock, NullLogger<PlanningWorkbookService>.Instance);
        }
    }

    private sealed class MultiWorkspaceService(IReadOnlyList<WorkspaceAssignment> initial) : IWorkspaceConfigurationService
    {
        public List<WorkspaceAssignment> Items { get; } = [.. initial];
        public Task<WorkspaceListResult> GetWorkspacesAsync() => Task.FromResult(new WorkspaceListResult(Items.ToList(), null));
        public Task<WorkspaceCreateResult> CreateWorkspaceAsync(CreateWorkspaceCommand command) => throw new NotSupportedException();
        public Task<WorkspaceUpdateResult> UpdateWorkspaceAsync(Guid assignmentId, CreateWorkspaceCommand command) => throw new NotSupportedException();
        public Task<WorkspaceOperationResult> ArchiveWorkspaceAsync(Guid assignmentId) => throw new NotSupportedException();
        public Task<WorkspaceOperationResult> RestoreWorkspaceAsync(Guid assignmentId) => throw new NotSupportedException();
        public Task<WorkspaceOperationResult> DeleteWorkspaceAsync(Guid assignmentId) => throw new NotSupportedException();
        public Task<WorkspaceReorderResult> ReorderWorkspacesAsync(ReorderWorkspacesCommand command) => throw new NotSupportedException();
        public Task ResetWorkspacesAsync() => throw new NotSupportedException();
    }
}
