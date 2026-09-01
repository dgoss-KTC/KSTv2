using Kst.Application.Mps;
using Kst.Application.Tests.Mps;
using Kst.Application.WorkOrders;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.WorkOrders;
using Kst.Domain.Workspaces;
using Kst.Infrastructure.Mps;
using Kst.Infrastructure.WorkOrders;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Application.Tests.WorkOrders;

public sealed class WorkOrderDrilldownServiceTests
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

    // Monday. GetBusinessWeekStart -> Sunday 2026-08-09; week-0 label -> Monday 2026-08-10.
    private static readonly DateOnly Today = new(2026, 8, 10);
    private static readonly DateOnly Week0Label = new(2026, 8, 10);
    private static readonly DateOnly CurrentWeekStart = new(2026, 8, 9);
    private static readonly DateOnly WindowEnd = new(2026, 9, 6); // CurrentWeekStart + 28 days

    private static MpsSourceRow Row(
        string parentPart,
        DateOnly dueDate,
        string workOrderId,
        MpsWorkOrderState state,
        DateOnly? releaseDate = null) => new(
        Domain: "KTC",
        Site: "SW",
        ParentPart: parentPart,
        Description: "Description",
        DueDate: dueDate,
        ReleaseDate: releaseDate ?? dueDate,
        Quantity: 10m,
        SupplyType: MpsSupplyType.Supply,
        WorkOrderId: workOrderId,
        WorkOrderState: state);

    private static SnapshotId SeedLoadedMps(IMpsSnapshotStore store, params MpsSourceRow[] rows)
    {
        var resolved = new[] { new MpsResolvedPart("ABC100", "Description") };
        var snapshot = new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, "SW", resolved, rows);
        store.SetLoaded(Workspace.AssignmentId, snapshot);
        return snapshot.Id;
    }

    // Default resolved parent summary used by candidate-flow tests unless a test overrides readByWoid.
    private static WorkOrderSummary MakeSummary(string woid, DateOnly? dueDate = null) => new(
        PartNumber: "ABC100",
        Woid: woid,
        Status: "R",
        OrderedQuantity: 100m,
        CompletedQuantity: 0m,
        ReleaseDate: null,
        DueDate: dueDate ?? Today,
        Kitting: KittingSummary.Calculate(0, 0));

    // Default manufactured material line used by candidate-flow tests unless a test overrides readMaterial.
    private static WorkOrderMaterialLine MakeManufacturedLine(string componentPart) =>
        new(componentPart, "Description", RequiredQuantity: 10m, IssuedQuantity: 5m, IsManufactured: true);

    private sealed class PlanningWindowCall
    {
        public required string Site { get; init; }
        public required string ParentPart { get; init; }
        public required MpsDateBasis Basis { get; init; }
        public required DateOnly WeekStart { get; init; }
        public required DateOnly WindowEnd { get; init; }
        public required MpsBucketKind? BucketKind { get; init; }
        public required DateOnly? BucketWeekStart { get; init; }
    }

    private sealed class Fixture
    {
        public required WorkOrderDrilldownService Service { get; init; }
        public required IMpsSnapshotStore MpsStore { get; init; }
        public required List<PlanningWindowCall> PlanningWindowCalls { get; init; }
        public required List<(string Site, string Woid)> ByWoidCalls { get; init; }
        public required List<(string Site, string Woid)> MaterialCalls { get; init; }
    }

    private static Fixture BuildService(
        Func<string, string, MpsDateBasis, DateOnly, DateOnly, MpsBucketKind?, DateOnly?, Task<IReadOnlyList<WorkOrderSummary>>>? readPlanningWindow = null,
        Func<string, string, Task<WorkOrderSummary?>>? readByWoid = null,
        Func<string, string, Task<IReadOnlyList<WorkOrderMaterialLine>>>? readMaterial = null,
        IMpsSnapshotStore? mpsStore = null)
    {
        var store = mpsStore ?? new InMemoryMpsSnapshotStore();
        var planningWindowCalls = new List<PlanningWindowCall>();
        var byWoidCalls = new List<(string, string)>();
        var materialCalls = new List<(string, string)>();

        var summaryReader = new DelegateWorkOrderSummaryReader(
            (site, parentPart, dateBasis, weekStart, windowEnd, bucketKind, bucketWeekStart, _) =>
            {
                planningWindowCalls.Add(new PlanningWindowCall
                {
                    Site = site,
                    ParentPart = parentPart,
                    Basis = dateBasis,
                    WeekStart = weekStart,
                    WindowEnd = windowEnd,
                    BucketKind = bucketKind,
                    BucketWeekStart = bucketWeekStart
                });
                return readPlanningWindow is not null
                    ? readPlanningWindow(site, parentPart, dateBasis, weekStart, windowEnd, bucketKind, bucketWeekStart)
                    : Task.FromResult<IReadOnlyList<WorkOrderSummary>>([]);
            },
            (site, woid, _) =>
            {
                byWoidCalls.Add((site, woid));
                return readByWoid is not null
                    ? readByWoid(site, woid)
                    : Task.FromResult<WorkOrderSummary?>(MakeSummary(woid));
            });

        var materialReader = new DelegateWorkOrderMaterialReader((site, woid, _) =>
        {
            materialCalls.Add((site, woid));
            return readMaterial is not null
                ? readMaterial(site, woid)
                : Task.FromResult<IReadOnlyList<WorkOrderMaterialLine>>([MakeManufacturedLine("COMP1")]);
        });

        var service = new WorkOrderDrilldownService(
            new FakeWorkspaceConfigurationService(Workspace),
            store,
            summaryReader,
            materialReader,
            new InMemoryWorkOrderSummaryCacheStore(),
            new InMemoryWorkOrderMaterialCacheStore(),
            new InMemoryWorkOrderPlanningWindowCacheStore(),
            NullLogger<WorkOrderDrilldownService>.Instance);

        return new Fixture
        {
            Service = service,
            MpsStore = store,
            PlanningWindowCalls = planningWindowCalls,
            ByWoidCalls = byWoidCalls,
            MaterialCalls = materialCalls
        };
    }

    // -- Planning window: workspace / snapshot / scope ----------------------

    [Fact]
    public async Task GetPlanningWindowAsync_Throws_For_Unknown_Workspace()
    {
        var f = BuildService();

        await Assert.ThrowsAsync<WorkOrderDrilldownWorkspaceNotFoundException>(() =>
            f.Service.GetPlanningWindowAsync(
                Guid.NewGuid(), SnapshotId.New(), "ABC100", MpsDateBasis.DueDate, null, null, Today));
    }

    [Fact]
    public async Task GetPlanningWindowAsync_Returns_MpsNotLoaded_When_No_Snapshot_Exists()
    {
        var f = BuildService();

        var result = await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, SnapshotId.New(), "ABC100", MpsDateBasis.DueDate, null, null, Today);

        Assert.Equal(WorkOrderPlanningWindowOutcomeKind.MpsNotLoaded, result.Kind);
    }

    [Fact]
    public async Task GetPlanningWindowAsync_Returns_SnapshotChanged_When_Requested_Snapshot_Is_Stale()
    {
        var f = BuildService();
        SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, SnapshotId.New(), "ABC100", MpsDateBasis.DueDate, null, null, Today);

        Assert.Equal(WorkOrderPlanningWindowOutcomeKind.SnapshotChanged, result.Kind);
    }

    [Fact]
    public async Task GetPlanningWindowAsync_Returns_PartNotInScope_For_Part_Not_In_Resolved_Scope()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ZZZ999", MpsDateBasis.DueDate, null, null, Today);

        Assert.Equal(WorkOrderPlanningWindowOutcomeKind.PartNotInScope, result.Kind);
        Assert.Empty(f.PlanningWindowCalls); // scope check happens before any QAD read
    }

    [Fact]
    public async Task GetPlanningWindowAsync_Returns_BucketNotFound_For_Week_Outside_The_Four_Week_Horizon()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);

        // Week 5 (beyond Week 0..3) is outside the planning-window horizon.
        var week5Label = Week0Label.AddDays(35);
        var result = await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsDateBasis.DueDate, MpsBucketKind.Weekly, week5Label, Today);

        Assert.Equal(WorkOrderPlanningWindowOutcomeKind.BucketNotFound, result.Kind);
        Assert.Empty(f.PlanningWindowCalls);
    }

    [Fact]
    public async Task GetPlanningWindowAsync_Returns_BucketNotFound_For_Weekly_Bucket_Without_A_Week_Label()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsDateBasis.DueDate, MpsBucketKind.Weekly, null, Today);

        Assert.Equal(WorkOrderPlanningWindowOutcomeKind.BucketNotFound, result.Kind);
    }

    // -- Planning window: predicate parameters ------------------------------

    [Fact]
    public async Task GetPlanningWindowAsync_ParentLevel_Passes_Full_Window_Params_To_Reader()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsDateBasis.DueDate, null, null, Today);

        Assert.Equal(WorkOrderPlanningWindowOutcomeKind.Loaded, result.Kind);
        var call = Assert.Single(f.PlanningWindowCalls);
        Assert.Equal("SW", call.Site);
        Assert.Equal("ABC100", call.ParentPart);
        Assert.Equal(MpsDateBasis.DueDate, call.Basis);
        Assert.Equal(CurrentWeekStart, call.WeekStart);
        Assert.Equal(WindowEnd, call.WindowEnd);
        Assert.Null(call.BucketKind);
        Assert.Null(call.BucketWeekStart);
    }

    [Fact]
    public async Task GetPlanningWindowAsync_Falldown_Bucket_Passes_Falldown_Kind_Without_A_Bucket_Week()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);

        await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsDateBasis.ReleaseDate, MpsBucketKind.Falldown, null, Today);

        var call = Assert.Single(f.PlanningWindowCalls);
        Assert.Equal(MpsBucketKind.Falldown, call.BucketKind);
        Assert.Null(call.BucketWeekStart);
    }

    [Fact]
    public async Task GetPlanningWindowAsync_Weekly_Bucket_Passes_The_Bucket_Week_Start()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);

        await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsDateBasis.DueDate, MpsBucketKind.Weekly, Week0Label, Today);

        var call = Assert.Single(f.PlanningWindowCalls);
        Assert.Equal(MpsBucketKind.Weekly, call.BucketKind);
        Assert.Equal(CurrentWeekStart, call.BucketWeekStart); // Sunday of the requested week
    }

    [Fact]
    public async Task GetPlanningWindowAsync_Returns_The_Reader_Population()
    {
        var population = new[] { MakeSummary("WO-1"), MakeSummary("WO-2") };
        var f = BuildService(readPlanningWindow: (_, _, _, _, _, _, _) =>
            Task.FromResult<IReadOnlyList<WorkOrderSummary>>(population));
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsDateBasis.DueDate, null, null, Today);

        Assert.Equal(WorkOrderPlanningWindowOutcomeKind.Loaded, result.Kind);
        Assert.Equal(snapshotId, result.SnapshotId);
        Assert.Equal(["WO-1", "WO-2"], result.WorkOrders!.Select(w => w.Woid));
    }

    // -- Planning window: cache behavior ------------------------------------

    [Fact]
    public async Task GetPlanningWindowAsync_Reopening_The_Same_Window_Reuses_Cache()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);

        await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsDateBasis.DueDate, null, null, Today);
        var result = await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsDateBasis.DueDate, null, null, Today);

        Assert.Equal(WorkOrderPlanningWindowOutcomeKind.Loaded, result.Kind);
        Assert.Single(f.PlanningWindowCalls); // second open reused the cached population
    }

    [Fact]
    public async Task GetPlanningWindowAsync_DateBasis_Toggle_Is_A_Different_Population()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);

        await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsDateBasis.DueDate, null, null, Today);
        await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsDateBasis.ReleaseDate, null, null, Today);

        // The forward-week population depends on the active basis, so a basis toggle is a cache miss.
        Assert.Equal(2, f.PlanningWindowCalls.Count);
    }

    [Fact]
    public async Task GetPlanningWindowAsync_Reader_Failure_Returns_Unavailable_And_Does_Not_Cache()
    {
        var f = BuildService(readPlanningWindow: (_, _, _, _, _, _, _) => throw new InvalidOperationException("boom"));
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsDateBasis.DueDate, null, null, Today);

        Assert.Equal(WorkOrderPlanningWindowOutcomeKind.Unavailable, result.Kind);
    }

    [Fact]
    public async Task GetPlanningWindowAsync_Retries_After_A_Failed_Read()
    {
        var callCount = 0;
        var f = BuildService(readPlanningWindow: (_, _, _, _, _, _, _) =>
        {
            callCount++;
            if (callCount == 1)
                throw new InvalidOperationException("boom");
            return Task.FromResult<IReadOnlyList<WorkOrderSummary>>([MakeSummary("WO-1")]);
        });
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var first = await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsDateBasis.DueDate, null, null, Today);
        var second = await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsDateBasis.DueDate, null, null, Today);

        Assert.Equal(WorkOrderPlanningWindowOutcomeKind.Unavailable, first.Kind);
        Assert.Equal(WorkOrderPlanningWindowOutcomeKind.Loaded, second.Kind);
        Assert.Equal(2, callCount); // failed read was not cached, so the retry actually queried again
    }

    [Fact]
    public async Task GetPlanningWindowAsync_New_Successful_Snapshot_Invalidates_Prior_Cache()
    {
        var f = BuildService();
        var snapshotIdV1 = SeedLoadedMps(f.MpsStore);
        await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotIdV1, "ABC100", MpsDateBasis.DueDate, null, null, Today);

        // A brand-new successful MPS load (new SnapshotId). The caller is assumed to have caught up to
        // the new snapshot id (as it would after a real dashboard refresh).
        var snapshotIdV2 = SeedLoadedMps(f.MpsStore);
        await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotIdV2, "ABC100", MpsDateBasis.DueDate, null, null, Today);

        Assert.NotEqual(snapshotIdV1, snapshotIdV2);
        Assert.Equal(2, f.PlanningWindowCalls.Count); // new snapshot generation -> old cache entry no longer matched
    }

    [Fact]
    public async Task GetPlanningWindowAsync_Failed_Refresh_Preserves_Cache_For_Retained_Snapshot()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);
        await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsDateBasis.DueDate, null, null, Today);

        // A failed refresh does not replace the retained (last-good) snapshot.
        f.MpsStore.SetFailed(Workspace.AssignmentId, "QAD unavailable");
        await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsDateBasis.DueDate, null, null, Today);

        Assert.Single(f.PlanningWindowCalls); // still the same snapshot id -> cache from before the failed refresh still hits
    }

    // -- Material lines (unchanged) -----------------------------------------

    [Fact]
    public async Task GetMaterialLinesAsync_Throws_For_Unknown_Workspace()
    {
        var f = BuildService();

        await Assert.ThrowsAsync<WorkOrderDrilldownWorkspaceNotFoundException>(() =>
            f.Service.GetMaterialLinesAsync(Guid.NewGuid(), SnapshotId.New(), "1001"));
    }

    [Fact]
    public async Task GetMaterialLinesAsync_Returns_MpsNotLoaded_When_No_Snapshot_Exists()
    {
        var f = BuildService();

        var result = await f.Service.GetMaterialLinesAsync(Workspace.AssignmentId, SnapshotId.New(), "1001");

        Assert.Equal(WorkOrderMaterialOutcomeKind.MpsNotLoaded, result.Kind);
    }

    [Fact]
    public async Task GetMaterialLinesAsync_Returns_SnapshotChanged_When_Requested_Snapshot_Is_Stale()
    {
        var f = BuildService();
        SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetMaterialLinesAsync(Workspace.AssignmentId, SnapshotId.New(), "1001");

        Assert.Equal(WorkOrderMaterialOutcomeKind.SnapshotChanged, result.Kind);
    }

    [Fact]
    public async Task GetMaterialLinesAsync_Resolves_Site_And_Trims_Woid()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);

        await f.Service.GetMaterialLinesAsync(Workspace.AssignmentId, snapshotId, "  1001  ");

        var call = Assert.Single(f.MaterialCalls);
        Assert.Equal("SW", call.Site);
        Assert.Equal("1001", call.Woid);
    }

    [Fact]
    public async Task GetMaterialLinesAsync_Reopening_The_Same_Woid_Reuses_Cache()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);

        await f.Service.GetMaterialLinesAsync(Workspace.AssignmentId, snapshotId, "1001");
        var result = await f.Service.GetMaterialLinesAsync(Workspace.AssignmentId, snapshotId, "1001");

        Assert.Equal(WorkOrderMaterialOutcomeKind.Loaded, result.Kind);
        Assert.Single(f.MaterialCalls);
    }

    [Fact]
    public async Task GetMaterialLinesAsync_Reader_Failure_Returns_Unavailable_And_Does_Not_Cache()
    {
        var f = BuildService(readMaterial: (_, _) => throw new InvalidOperationException("boom"));
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var first = await f.Service.GetMaterialLinesAsync(Workspace.AssignmentId, snapshotId, "1001");

        Assert.Equal(WorkOrderMaterialOutcomeKind.Unavailable, first.Kind);
    }

    // -- Manufactured-subassembly planning window --------------------------------

    [Fact]
    public async Task GetCandidatesAsync_Throws_For_Unknown_Workspace()
    {
        var f = BuildService();

        await Assert.ThrowsAsync<WorkOrderDrilldownWorkspaceNotFoundException>(() =>
            f.Service.GetCandidatesAsync(Guid.NewGuid(), SnapshotId.New(), "WO-PARENT", "COMP1", 2, MpsDateBasis.DueDate, Today));
    }

    [Fact]
    public async Task GetCandidatesAsync_Returns_MpsNotLoaded_When_No_Snapshot_Exists()
    {
        var f = BuildService();

        var result = await f.Service.GetCandidatesAsync(
            Workspace.AssignmentId, SnapshotId.New(), "WO-PARENT", "COMP1", 2, MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderCandidateOutcomeKind.MpsNotLoaded, result.Kind);
    }

    [Fact]
    public async Task GetCandidatesAsync_Returns_SnapshotChanged_When_Requested_Snapshot_Is_Stale()
    {
        var f = BuildService();
        SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetCandidatesAsync(
            Workspace.AssignmentId, SnapshotId.New(), "WO-PARENT", "COMP1", 2, MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderCandidateOutcomeKind.SnapshotChanged, result.Kind);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task GetCandidatesAsync_Rejects_Depth_Outside_Level_2_And_3(int depth)
    {
        var f = BuildService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            f.Service.GetCandidatesAsync(Workspace.AssignmentId, SnapshotId.New(), "WO-PARENT", "COMP1", depth, MpsDateBasis.DueDate, Today));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task GetCandidatesAsync_Accepts_Depth_2_And_3_And_Uses_The_Shared_Planning_Window(int depth)
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);

        await f.Service.GetCandidatesAsync(Workspace.AssignmentId, snapshotId, "WO-PARENT", "  COMP1  ", depth, MpsDateBasis.ReleaseDate, Today);

        var call = Assert.Single(f.PlanningWindowCalls);
        Assert.Equal("COMP1", call.ParentPart);
        Assert.Equal(MpsDateBasis.ReleaseDate, call.Basis);
        Assert.Equal(CurrentWeekStart, call.WeekStart);
        Assert.Equal(WindowEnd, call.WindowEnd);
        Assert.Null(call.BucketKind);
    }

    [Fact]
    public async Task GetCandidatesAsync_Uses_The_Same_Due_Basis_Planning_Window_As_A_TopLevel_Parent()
    {
        var population = new[]
        {
            MakeSummary("FALLDOWN") with { PartNumber = "COMP1", DueDate = CurrentWeekStart.AddDays(-1), Status = "P" },
            MakeSummary("WEEK-3") with { PartNumber = "COMP1", DueDate = WindowEnd.AddDays(-1), Status = "e" }
        };
        var f = BuildService(readPlanningWindow: (_, _, _, _, _, _, _) =>
            Task.FromResult<IReadOnlyList<WorkOrderSummary>>(population));
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var topLevel = await f.Service.GetPlanningWindowAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsDateBasis.DueDate, null, null, Today);
        var nested = await f.Service.GetCandidatesAsync(
            Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", 2, MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderPlanningWindowOutcomeKind.Loaded, topLevel.Kind);
        Assert.Equal(WorkOrderCandidateOutcomeKind.Loaded, nested.Kind);
        Assert.Equal(population.Select(workOrder => workOrder.Woid), nested.Candidates!.Select(workOrder => workOrder.Woid));
        Assert.Equal(["ABC100", "COMP1"], f.PlanningWindowCalls.Select(call => call.ParentPart));
        Assert.All(f.PlanningWindowCalls, call => Assert.Equal(MpsDateBasis.DueDate, call.Basis));
    }

    [Fact]
    public async Task GetCandidatesAsync_Uses_The_Status_Agnostic_Single_Woid_Parent_Read()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);

        await f.Service.GetCandidatesAsync(Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", 2, MpsDateBasis.DueDate, Today);

        var call = Assert.Single(f.ByWoidCalls);
        Assert.Equal("SW", call.Site);
        Assert.Equal("WO-PARENT", call.Woid);
    }

    [Fact]
    public async Task GetCandidatesAsync_Returns_WorkOrderNotFound_When_Immediate_Parent_Does_Not_Resolve()
    {
        var f = BuildService(readByWoid: (_, _) => Task.FromResult<WorkOrderSummary?>(null));
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetCandidatesAsync(
            Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", 2, MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderCandidateOutcomeKind.WorkOrderNotFound, result.Kind);
    }

    [Fact]
    public async Task GetCandidatesAsync_Returns_Unavailable_When_Immediate_Parent_Read_Fails()
    {
        var f = BuildService(readByWoid: (_, _) => throw new InvalidOperationException("boom"));
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetCandidatesAsync(
            Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", 2, MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderCandidateOutcomeKind.Unavailable, result.Kind);
    }

    [Fact]
    public async Task GetCandidatesAsync_Returns_ComponentNotManufactured_When_No_Matching_Material_Line()
    {
        var f = BuildService(readMaterial: (_, _) => Task.FromResult<IReadOnlyList<WorkOrderMaterialLine>>([]));
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetCandidatesAsync(
            Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", 2, MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderCandidateOutcomeKind.ComponentNotManufactured, result.Kind);
    }

    [Fact]
    public async Task GetCandidatesAsync_Returns_ComponentNotManufactured_When_Matching_Line_Is_Not_Manufactured()
    {
        var f = BuildService(readMaterial: (_, _) =>
            Task.FromResult<IReadOnlyList<WorkOrderMaterialLine>>(
                [new WorkOrderMaterialLine("COMP1", "Description", RequiredQuantity: 10m, IssuedQuantity: 5m, IsManufactured: false)]));
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetCandidatesAsync(
            Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", 2, MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderCandidateOutcomeKind.ComponentNotManufactured, result.Kind);
        Assert.Empty(f.PlanningWindowCalls); // arbitrary parts cannot enter the planning reader without a manufactured material line
    }

    [Fact]
    public async Task GetCandidatesAsync_Repeating_The_Same_Drill_Reuses_The_Shared_Planning_Window_Cache()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);

        await f.Service.GetCandidatesAsync(Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", 2, MpsDateBasis.DueDate, Today);
        var result = await f.Service.GetCandidatesAsync(Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", 2, MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderCandidateOutcomeKind.Loaded, result.Kind);
        Assert.Single(f.PlanningWindowCalls);
    }

    [Fact]
    public async Task GetCandidatesAsync_More_Than_Ten_Valid_WorkOrders_Are_Not_Truncated()
    {
        var f = BuildService(readPlanningWindow: (_, part, _, _, _, _, _) =>
            Task.FromResult<IReadOnlyList<WorkOrderSummary>>(Enumerable.Range(1, 14)
                .Select(index => MakeSummary($"WO-{index}") with { PartNumber = part, Status = index == 14 ? "P" : "R" })
                .ToList()));
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetCandidatesAsync(Workspace.AssignmentId, snapshotId, "WO-PARENT-A", "COMP1", 2, MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderCandidateOutcomeKind.Loaded, result.Kind);
        Assert.Equal(14, result.Candidates!.Count);
        Assert.Contains(result.Candidates, workOrder => workOrder.Status == "P");
    }

    [Fact]
    public async Task GetCandidatesAsync_Planning_Window_Reader_Failure_Returns_Unavailable()
    {
        var f = BuildService(readPlanningWindow: (_, _, _, _, _, _, _) => throw new InvalidOperationException("boom"));
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetCandidatesAsync(Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", 2, MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderCandidateOutcomeKind.Unavailable, result.Kind);
    }
}
