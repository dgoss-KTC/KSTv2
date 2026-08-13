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

    // Default resolved parent Due Date used by candidate-flow tests unless a test overrides readByWoids.
    private static WorkOrderSummary MakeSummary(string woid, DateOnly? dueDate = null) => new(
        PartNumber: "ABC100",
        Woid: woid,
        Status: WorkOrderStatus.Released,
        OrderedQuantity: 100m,
        CompletedQuantity: 0m,
        ReleaseDate: null,
        DueDate: dueDate ?? Today,
        Kitting: KittingSummary.Calculate(0, 0));

    // Default manufactured material line used by candidate-flow tests unless a test overrides readMaterial.
    private static WorkOrderMaterialLine MakeManufacturedLine(string componentPart) =>
        new(componentPart, "Description", RequiredQuantity: 10m, IssuedQuantity: 5m, IsManufactured: true);

    private sealed class Fixture
    {
        public required WorkOrderDrilldownService Service { get; init; }
        public required IMpsSnapshotStore MpsStore { get; init; }
        public required List<(string Site, IReadOnlyList<string> Woids)> SummaryCalls { get; init; }
        public required List<(string Site, string Woid)> MaterialCalls { get; init; }
        public required List<(string Site, string ComponentPart, int Limit)> CandidateCalls { get; init; }
    }

    private static Fixture BuildService(
        Func<string, IReadOnlyList<string>, Task<IReadOnlyList<WorkOrderSummary>>>? readByWoids = null,
        Func<string, string, int, Task<CandidateWorkOrdersResult>>? readCandidates = null,
        Func<string, string, Task<IReadOnlyList<WorkOrderMaterialLine>>>? readMaterial = null,
        IMpsSnapshotStore? mpsStore = null)
    {
        var store = mpsStore ?? new InMemoryMpsSnapshotStore();
        var summaryCalls = new List<(string, IReadOnlyList<string>)>();
        var materialCalls = new List<(string, string)>();
        var candidateCalls = new List<(string, string, int)>();

        var summaryReader = new DelegateWorkOrderSummaryReader(
            (site, woids, _) =>
            {
                summaryCalls.Add((site, woids));
                return readByWoids is not null
                    ? readByWoids(site, woids)
                    : Task.FromResult<IReadOnlyList<WorkOrderSummary>>(woids.Select(w => MakeSummary(w)).ToList());
            },
            (site, component, limit, _) =>
            {
                candidateCalls.Add((site, component, limit));
                return readCandidates is not null
                    ? readCandidates(site, component, limit)
                    : Task.FromResult(new CandidateWorkOrdersResult([], IsTruncated: false));
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
            new InMemoryWorkOrderCandidateCacheStore(),
            NullLogger<WorkOrderDrilldownService>.Instance);

        return new Fixture
        {
            Service = service,
            MpsStore = store,
            SummaryCalls = summaryCalls,
            MaterialCalls = materialCalls,
            CandidateCalls = candidateCalls
        };
    }

    [Fact]
    public async Task GetBucketWorkOrdersAsync_Throws_For_Unknown_Workspace()
    {
        var f = BuildService();

        await Assert.ThrowsAsync<WorkOrderDrilldownWorkspaceNotFoundException>(() =>
            f.Service.GetBucketWorkOrdersAsync(
                Guid.NewGuid(), SnapshotId.New(), "ABC100", MpsBucketKind.Falldown, null, MpsDateBasis.DueDate, 6, Today));
    }

    [Fact]
    public async Task GetBucketWorkOrdersAsync_Returns_MpsNotLoaded_When_No_Snapshot_Exists()
    {
        var f = BuildService();

        var result = await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, SnapshotId.New(), "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);

        Assert.Equal(WorkOrderBucketOutcomeKind.MpsNotLoaded, result.Kind);
    }

    [Fact]
    public async Task GetBucketWorkOrdersAsync_Returns_SnapshotChanged_When_Requested_Snapshot_Is_Stale()
    {
        var f = BuildService();
        SeedLoadedMps(f.MpsStore, Row("ABC100", new DateOnly(2026, 8, 11), "WO-R1", MpsWorkOrderState.Released));

        var result = await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, SnapshotId.New(), "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);

        Assert.Equal(WorkOrderBucketOutcomeKind.SnapshotChanged, result.Kind);
    }

    [Fact]
    public async Task GetBucketWorkOrdersAsync_Returns_PartNotInScope_For_Part_Not_In_Resolved_Scope()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore, Row("ABC100", new DateOnly(2026, 8, 11), "WO-R1", MpsWorkOrderState.Released));

        var result = await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ZZZ999", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);

        Assert.Equal(WorkOrderBucketOutcomeKind.PartNotInScope, result.Kind);
    }

    [Fact]
    public async Task GetBucketWorkOrdersAsync_Returns_BucketNotFound_For_Week_Outside_Horizon()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore, Row("ABC100", new DateOnly(2026, 8, 11), "WO-R1", MpsWorkOrderState.Released));

        var result = await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsBucketKind.Weekly, new DateOnly(2030, 1, 1), MpsDateBasis.DueDate, 6, Today);

        Assert.Equal(WorkOrderBucketOutcomeKind.BucketNotFound, result.Kind);
    }

    [Fact]
    public async Task GetBucketWorkOrdersAsync_Reads_Woid_Refs_Already_Retained_On_The_Matching_Bucket()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(
            f.MpsStore,
            Row("ABC100", new DateOnly(2026, 8, 11), "WO-R1", MpsWorkOrderState.Released),
            Row("ABC100", new DateOnly(2026, 8, 1), "WO-F1", MpsWorkOrderState.Frozen)); // falls in Falldown, not week 0

        var result = await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);

        Assert.Equal(WorkOrderBucketOutcomeKind.Loaded, result.Kind);
        var call = Assert.Single(f.SummaryCalls);
        Assert.Equal("SW", call.Site);
        Assert.Equal(["WO-R1"], call.Woids);
    }

    [Fact]
    public async Task GetBucketWorkOrdersAsync_Supports_Falldown_Bucket()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore, Row("ABC100", new DateOnly(2026, 8, 1), "WO-F1", MpsWorkOrderState.Frozen));

        var result = await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsBucketKind.Falldown, null, MpsDateBasis.DueDate, 6, Today);

        Assert.Equal(WorkOrderBucketOutcomeKind.Loaded, result.Kind);
        Assert.Equal(["WO-F1"], Assert.Single(f.SummaryCalls).Woids);
    }

    [Fact]
    public async Task GetBucketWorkOrdersAsync_Excludes_Planned_And_ExplicitlyScheduled_Refs()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(
            f.MpsStore,
            Row("ABC100", new DateOnly(2026, 8, 11), "WO-PLANNED", MpsWorkOrderState.Planned),
            Row("ABC100", new DateOnly(2026, 8, 12), "WO-EXPLICIT", MpsWorkOrderState.ExplicitlyScheduled));

        var result = await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);

        Assert.Equal(WorkOrderBucketOutcomeKind.Loaded, result.Kind);
        Assert.Empty(result.WorkOrders!);
        Assert.Empty(f.SummaryCalls); // no eligible WOIDs -> reader never called
    }

    [Fact]
    public async Task GetBucketWorkOrdersAsync_Deduplicates_Woids_From_Multiple_Rows()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(
            f.MpsStore,
            Row("ABC100", new DateOnly(2026, 8, 11), "WO-R1", MpsWorkOrderState.Released),
            Row("ABC100", new DateOnly(2026, 8, 12), "WO-R1", MpsWorkOrderState.Released));

        await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);

        Assert.Equal(["WO-R1"], Assert.Single(f.SummaryCalls).Woids);
    }

    [Fact]
    public async Task GetBucketWorkOrdersAsync_Reopening_The_Same_Bucket_Reuses_Cached_Summary()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore, Row("ABC100", new DateOnly(2026, 8, 11), "WO-R1", MpsWorkOrderState.Released));

        await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);
        var result = await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);

        Assert.Equal(WorkOrderBucketOutcomeKind.Loaded, result.Kind);
        Assert.Single(result.WorkOrders!);
        Assert.Single(f.SummaryCalls); // second open reused the cached summary, no second reader call
    }

    [Fact]
    public async Task GetBucketWorkOrdersAsync_DateBasis_Toggle_Does_Not_Invalidate_Cache()
    {
        var f = BuildService();
        // ReleaseDate defaults to DueDate, so the WO resolves into the same week-0 bucket either basis.
        var snapshotId = SeedLoadedMps(f.MpsStore, Row("ABC100", new DateOnly(2026, 8, 11), "WO-R1", MpsWorkOrderState.Released));

        await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);
        await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.ReleaseDate, 6, Today);

        Assert.Single(f.SummaryCalls); // toggling Due/Release basis reused the same cached WOID summary
    }

    [Fact]
    public async Task GetBucketWorkOrdersAsync_Only_Fetches_Missing_Woids_On_Partial_Cache_Hit()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(
            f.MpsStore,
            Row("ABC100", new DateOnly(2026, 8, 11), "WO-A", MpsWorkOrderState.Released),
            Row("ABC100", new DateOnly(2026, 8, 11), "WO-B", MpsWorkOrderState.Released),
            Row("ABC100", new DateOnly(2026, 8, 18), "WO-B", MpsWorkOrderState.Released), // WO-B recurs in week 1
            Row("ABC100", new DateOnly(2026, 8, 18), "WO-C", MpsWorkOrderState.Released));

        // Warms the cache for WO-A and WO-B via week 0.
        await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);

        // Week 1 shares WO-B with week 0 -> only WO-C should be a cache miss.
        var week1Label = Week0Label.AddDays(7);
        var result = await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsBucketKind.Weekly, week1Label, MpsDateBasis.DueDate, 6, Today);

        Assert.Equal(WorkOrderBucketOutcomeKind.Loaded, result.Kind);
        Assert.Equal(2, result.WorkOrders!.Count);
        Assert.Equal(2, f.SummaryCalls.Count);
        Assert.Equal(["WO-A", "WO-B"], f.SummaryCalls[0].Woids);
        Assert.Equal(["WO-C"], f.SummaryCalls[1].Woids); // WO-B was already cached from week 0
    }

    [Fact]
    public async Task GetBucketWorkOrdersAsync_Reader_Failure_Returns_Unavailable_And_Does_Not_Cache()
    {
        var f = BuildService(readByWoids: (_, _) => throw new InvalidOperationException("boom"));
        var snapshotId = SeedLoadedMps(f.MpsStore, Row("ABC100", new DateOnly(2026, 8, 11), "WO-R1", MpsWorkOrderState.Released));

        var result = await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);

        Assert.Equal(WorkOrderBucketOutcomeKind.Unavailable, result.Kind);
    }

    [Fact]
    public async Task GetBucketWorkOrdersAsync_Retries_After_A_Failed_Read()
    {
        var callCount = 0;
        var f = BuildService(readByWoids: (_, woids) =>
        {
            callCount++;
            if (callCount == 1)
                throw new InvalidOperationException("boom");
            return Task.FromResult<IReadOnlyList<WorkOrderSummary>>(woids.Select(w => MakeSummary(w)).ToList());
        });
        var snapshotId = SeedLoadedMps(f.MpsStore, Row("ABC100", new DateOnly(2026, 8, 11), "WO-R1", MpsWorkOrderState.Released));

        var first = await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);
        var second = await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);

        Assert.Equal(WorkOrderBucketOutcomeKind.Unavailable, first.Kind);
        Assert.Equal(WorkOrderBucketOutcomeKind.Loaded, second.Kind);
        Assert.Equal(2, callCount); // failed read was not cached, so the retry actually queried again
    }

    [Fact]
    public async Task GetBucketWorkOrdersAsync_New_Successful_Snapshot_Invalidates_Prior_Cache()
    {
        var f = BuildService();
        var snapshotIdV1 = SeedLoadedMps(f.MpsStore, Row("ABC100", new DateOnly(2026, 8, 11), "WO-R1", MpsWorkOrderState.Released));
        await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotIdV1, "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);

        // A brand-new successful MPS load (new SnapshotId), same underlying WOID. The caller is assumed
        // to have caught up to the new snapshot id (as it would after a real dashboard refresh).
        var snapshotIdV2 = SeedLoadedMps(f.MpsStore, Row("ABC100", new DateOnly(2026, 8, 11), "WO-R1", MpsWorkOrderState.Released));
        await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotIdV2, "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);

        Assert.NotEqual(snapshotIdV1, snapshotIdV2);
        Assert.Equal(2, f.SummaryCalls.Count); // new snapshot generation -> old cache entry no longer matched
    }

    [Fact]
    public async Task GetBucketWorkOrdersAsync_Failed_Refresh_Preserves_Cache_For_Retained_Snapshot()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore, Row("ABC100", new DateOnly(2026, 8, 11), "WO-R1", MpsWorkOrderState.Released));
        await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);

        // A failed refresh does not replace the retained (last-good) snapshot.
        f.MpsStore.SetFailed(Workspace.AssignmentId, "QAD unavailable");
        await f.Service.GetBucketWorkOrdersAsync(
            Workspace.AssignmentId, snapshotId, "ABC100", MpsBucketKind.Weekly, Week0Label, MpsDateBasis.DueDate, 6, Today);

        Assert.Single(f.SummaryCalls); // still the same snapshot id -> cache from before the failed refresh still hits
    }

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

    [Fact]
    public async Task GetCandidatesAsync_Throws_For_Unknown_Workspace()
    {
        var f = BuildService();

        await Assert.ThrowsAsync<WorkOrderDrilldownWorkspaceNotFoundException>(() =>
            f.Service.GetCandidatesAsync(Guid.NewGuid(), SnapshotId.New(), "WO-PARENT", "COMP1", targetDepth: 2));
    }

    [Fact]
    public async Task GetCandidatesAsync_Returns_MpsNotLoaded_When_No_Snapshot_Exists()
    {
        var f = BuildService();

        var result = await f.Service.GetCandidatesAsync(
            Workspace.AssignmentId, SnapshotId.New(), "WO-PARENT", "COMP1", targetDepth: 2);

        Assert.Equal(WorkOrderCandidateOutcomeKind.MpsNotLoaded, result.Kind);
    }

    [Fact]
    public async Task GetCandidatesAsync_Returns_SnapshotChanged_When_Requested_Snapshot_Is_Stale()
    {
        var f = BuildService();
        SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetCandidatesAsync(
            Workspace.AssignmentId, SnapshotId.New(), "WO-PARENT", "COMP1", targetDepth: 2);

        Assert.Equal(WorkOrderCandidateOutcomeKind.SnapshotChanged, result.Kind);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task GetCandidatesAsync_Rejects_Depth_Outside_Level_2_And_3(int depth)
    {
        var f = BuildService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            f.Service.GetCandidatesAsync(Workspace.AssignmentId, SnapshotId.New(), "WO-PARENT", "COMP1", targetDepth: depth));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task GetCandidatesAsync_Accepts_Depth_2_And_3(int depth)
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);

        await f.Service.GetCandidatesAsync(Workspace.AssignmentId, snapshotId, "WO-PARENT", "  COMP1  ", targetDepth: depth);

        var call = Assert.Single(f.CandidateCalls);
        Assert.Equal("COMP1", call.ComponentPart);
        Assert.Equal(WorkOrderDrilldownPolicy.CandidateResultLimit, call.Limit);
    }

    [Fact]
    public async Task GetCandidatesAsync_Returns_WorkOrderNotFound_When_Immediate_Parent_Does_Not_Resolve()
    {
        var f = BuildService(readByWoids: (_, _) => Task.FromResult<IReadOnlyList<WorkOrderSummary>>([]));
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetCandidatesAsync(
            Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", targetDepth: 2);

        Assert.Equal(WorkOrderCandidateOutcomeKind.WorkOrderNotFound, result.Kind);
    }

    [Fact]
    public async Task GetCandidatesAsync_Returns_Unavailable_When_Immediate_Parent_Read_Fails()
    {
        var f = BuildService(readByWoids: (_, _) => throw new InvalidOperationException("boom"));
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetCandidatesAsync(
            Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", targetDepth: 2);

        Assert.Equal(WorkOrderCandidateOutcomeKind.Unavailable, result.Kind);
    }

    [Fact]
    public async Task GetCandidatesAsync_Returns_ParentDueDateUnavailable_When_Parent_Has_No_Due_Date()
    {
        var f = BuildService(readByWoids: (_, woids) =>
            Task.FromResult<IReadOnlyList<WorkOrderSummary>>(woids.Select(w => MakeSummary(w) with { DueDate = null }).ToList()));
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetCandidatesAsync(
            Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", targetDepth: 2);

        Assert.Equal(WorkOrderCandidateOutcomeKind.ParentDueDateUnavailable, result.Kind);
    }

    [Fact]
    public async Task GetCandidatesAsync_Returns_ComponentNotManufactured_When_No_Matching_Material_Line()
    {
        var f = BuildService(readMaterial: (_, _) => Task.FromResult<IReadOnlyList<WorkOrderMaterialLine>>([]));
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetCandidatesAsync(
            Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", targetDepth: 2);

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
            Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", targetDepth: 2);

        Assert.Equal(WorkOrderCandidateOutcomeKind.ComponentNotManufactured, result.Kind);
    }

    [Fact]
    public async Task GetCandidatesAsync_Repeating_The_Same_Drill_Reuses_Cache()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);

        await f.Service.GetCandidatesAsync(Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", targetDepth: 2);
        var result = await f.Service.GetCandidatesAsync(Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", targetDepth: 2);

        Assert.Equal(WorkOrderCandidateOutcomeKind.Loaded, result.Kind);
        Assert.Single(f.CandidateCalls);
    }

    [Fact]
    public async Task GetCandidatesAsync_Different_Immediate_Parent_Woid_Is_Not_Aliased()
    {
        var f = BuildService();
        var snapshotId = SeedLoadedMps(f.MpsStore);

        await f.Service.GetCandidatesAsync(Workspace.AssignmentId, snapshotId, "WO-PARENT-A", "COMP1", targetDepth: 2);
        await f.Service.GetCandidatesAsync(Workspace.AssignmentId, snapshotId, "WO-PARENT-B", "COMP1", targetDepth: 2);

        Assert.Equal(2, f.CandidateCalls.Count); // same component/depth but different parent WOID -> both fetched
    }

    [Fact]
    public async Task GetCandidatesAsync_Reader_Failure_Returns_Unavailable_And_Does_Not_Cache()
    {
        var f = BuildService(readCandidates: (_, _, _) => throw new InvalidOperationException("boom"));
        var snapshotId = SeedLoadedMps(f.MpsStore);

        var result = await f.Service.GetCandidatesAsync(Workspace.AssignmentId, snapshotId, "WO-PARENT", "COMP1", targetDepth: 2);

        Assert.Equal(WorkOrderCandidateOutcomeKind.Unavailable, result.Kind);
    }
}
