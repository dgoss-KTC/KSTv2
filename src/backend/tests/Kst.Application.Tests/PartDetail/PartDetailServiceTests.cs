using Kst.Application.Mps;
using Kst.Application.PartDetail;
using Kst.Application.Tests.Mps;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.PartDetail;
using Kst.Domain.Workspaces;
using Kst.Infrastructure.Mps;
using Kst.Infrastructure.PartDetail;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Application.Tests.PartDetail;

public sealed class PartDetailServiceTests
{
    private static readonly WorkspaceAssignment Workspace = new(
        AssignmentId: Guid.NewGuid(),
        DisplayName: "Test Workspace",
        Site: "SW",
        ProductLineFrom: null,
        ProductLineTo: null,
        ParentParts: ["ABC100", "ABC200"],
        IsTemporary: false,
        CoverageEndsOn: null,
        IsEnabled: true,
        SortOrder: 0);

    private static PartDetailSourceFacts MakeFacts(string partNumber = "ABC100") => new(
        PartNumber: partNumber,
        PlannerCode: "JSMITH",
        ManufacturingLeadTimeDays: 10m,
        SafetyTimeDays: 2m,
        PartStatusCode: "C",
        CurrentRevision: "B",
        Description: "WIDGET CONTROL ASSEMBLY",
        IosCode: "1234",
        SafetyStockQuantity: 250m,
        QuantityOnHand: 1325m,
        QuantityNonNet: 75m,
        QuantityRmaOnHand: 25m,
        PriceBreaks: [new PartPriceBreak(100m, 12.45m)]);

    private static SnapshotId SeedLoadedMps(
        IMpsSnapshotStore store,
        Guid workspaceId,
        string site = "SW",
        params string[] parentParts)
    {
        var resolved = (parentParts.Length > 0 ? parentParts : ["ABC100", "ABC200"])
            .Select(p => new MpsResolvedPart(p, "Description"))
            .ToList();
        var snapshot = new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, site, resolved, []);
        store.SetLoaded(workspaceId, snapshot);
        return snapshot.Id;
    }

    private static (PartDetailService Service, IMpsSnapshotStore MpsStore, IPartDetailCacheStore Cache, FakeClock Clock)
        BuildService(
            Func<string, string, DateOnly, CancellationToken, Task<PartDetailSourceFacts?>>? read = null,
            IMpsSnapshotStore? mpsStore = null,
            IPartDetailCacheStore? cache = null)
    {
        var store = mpsStore ?? new InMemoryMpsSnapshotStore();
        var cacheStore = cache ?? new InMemoryPartDetailCacheStore();
        var clock = new FakeClock();

        var reader = new DelegatePartDetailSourceReader(
            read ?? ((_, partNumber, _, _) => Task.FromResult<PartDetailSourceFacts?>(MakeFacts(partNumber))));

        var service = new PartDetailService(
            new FakeWorkspaceConfigurationService(Workspace),
            store,
            reader,
            cacheStore,
            clock,
            NullLogger<PartDetailService>.Instance);

        return (service, store, cacheStore, clock);
    }

    [Fact]
    public async Task GetPartDetailAsync_Throws_For_Unknown_Workspace()
    {
        var (service, _, _, _) = BuildService();

        await Assert.ThrowsAsync<PartDetailWorkspaceNotFoundException>(() =>
            service.GetPartDetailAsync(Guid.NewGuid(), "ABC100"));
    }

    [Fact]
    public async Task GetPartDetailAsync_Returns_MpsNotLoaded_When_No_Snapshot_Exists()
    {
        var (service, _, _, _) = BuildService();

        var result = await service.GetPartDetailAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(PartDetailOutcomeKind.MpsNotLoaded, result.Kind);
    }

    [Fact]
    public async Task GetPartDetailAsync_Returns_OutOfScope_For_Part_Not_In_Resolved_Scope()
    {
        var (service, store, _, _) = BuildService();
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetPartDetailAsync(Workspace.AssignmentId, "ZZZ999");

        Assert.Equal(PartDetailOutcomeKind.OutOfScope, result.Kind);
    }

    [Fact]
    public async Task GetPartDetailAsync_Returns_MissingPart_When_Reader_Returns_Null()
    {
        var (service, store, _, _) = BuildService(read: (_, _, _, _) => Task.FromResult<PartDetailSourceFacts?>(null));
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetPartDetailAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(PartDetailOutcomeKind.MissingPart, result.Kind);
    }

    [Fact]
    public async Task GetPartDetailAsync_Returns_Loaded_Fresh_Detail_On_Success()
    {
        var (service, store, _, clock) = BuildService();
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetPartDetailAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(PartDetailOutcomeKind.Loaded, result.Kind);
        Assert.NotNull(result.Detail);
        Assert.False(result.Detail!.IsStale);
        Assert.Null(result.Detail.Warning);
        Assert.Equal("C", result.Detail.PartStatusCode);
        Assert.Equal("CURRENT", result.Detail.PartStatusDescription);
        Assert.Equal(clock.UtcNow, result.Detail.LoadedAtUtc);
        Assert.Single(result.Detail.PriceBreaks);
    }

    [Fact]
    public async Task GetPartDetailAsync_Composes_QuantityRmaOnHand_From_Reader_Facts()
    {
        var (service, store, _, _) = BuildService(read: (_, partNumber, _, _) =>
            Task.FromResult<PartDetailSourceFacts?>(MakeFacts(partNumber) with { QuantityRmaOnHand = 25m }));
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetPartDetailAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(PartDetailOutcomeKind.Loaded, result.Kind);
        Assert.Equal(25m, result.Detail!.QuantityRmaOnHand);
    }

    [Fact]
    public async Task GetPartDetailAsync_Zero_QuantityRmaOnHand_Composes_As_Zero_Not_Missing()
    {
        var (service, store, _, _) = BuildService(read: (_, partNumber, _, _) =>
            Task.FromResult<PartDetailSourceFacts?>(MakeFacts(partNumber) with { QuantityRmaOnHand = 0m }));
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetPartDetailAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(PartDetailOutcomeKind.Loaded, result.Kind);
        Assert.Equal(0m, result.Detail!.QuantityRmaOnHand);
    }

    [Fact]
    public async Task GetPartDetailAsync_Reuses_Cache_For_Same_Mps_Snapshot()
    {
        var callCount = 0;
        var (service, store, _, _) = BuildService(read: (_, partNumber, _, _) =>
        {
            callCount++;
            return Task.FromResult<PartDetailSourceFacts?>(MakeFacts(partNumber));
        });
        SeedLoadedMps(store, Workspace.AssignmentId);

        await service.GetPartDetailAsync(Workspace.AssignmentId, "ABC100");
        await service.GetPartDetailAsync(Workspace.AssignmentId, "ABC100");
        await service.GetPartDetailAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetPartDetailAsync_Requeries_When_Mps_Snapshot_Changes()
    {
        var callCount = 0;
        var (service, store, _, _) = BuildService(read: (_, partNumber, _, _) =>
        {
            callCount++;
            return Task.FromResult<PartDetailSourceFacts?>(MakeFacts(partNumber));
        });
        SeedLoadedMps(store, Workspace.AssignmentId);

        await service.GetPartDetailAsync(Workspace.AssignmentId, "ABC100");

        // Simulate a successful MPS refresh producing a new snapshot generation.
        SeedLoadedMps(store, Workspace.AssignmentId);

        await service.GetPartDetailAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GetPartDetailAsync_Returns_Unavailable_When_Reader_Fails_And_No_Cache_Exists()
    {
        var (service, store, _, _) = BuildService(
            read: (_, _, _, _) => throw new InvalidOperationException("QAD database connectivity failed."));
        SeedLoadedMps(store, Workspace.AssignmentId);

        var result = await service.GetPartDetailAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(PartDetailOutcomeKind.Unavailable, result.Kind);
    }

    [Fact]
    public async Task GetPartDetailAsync_Returns_Stale_LastGood_When_Reader_Fails_But_Cache_Exists()
    {
        var shouldFail = false;
        var (service, store, _, _) = BuildService(read: (_, partNumber, _, _) =>
        {
            if (shouldFail)
                throw new InvalidOperationException("QAD database connectivity failed.");
            return Task.FromResult<PartDetailSourceFacts?>(MakeFacts(partNumber));
        });
        SeedLoadedMps(store, Workspace.AssignmentId);

        var first = await service.GetPartDetailAsync(Workspace.AssignmentId, "ABC100");
        Assert.Equal(PartDetailOutcomeKind.Loaded, first.Kind);
        Assert.False(first.Detail!.IsStale);

        // A subsequent successful MPS refresh makes the cache stale-eligible for re-query...
        SeedLoadedMps(store, Workspace.AssignmentId);
        shouldFail = true;

        var second = await service.GetPartDetailAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(PartDetailOutcomeKind.Loaded, second.Kind);
        Assert.True(second.Detail!.IsStale);
        Assert.NotNull(second.Detail.Warning);
        Assert.Equal(25m, second.Detail.QuantityRmaOnHand);
    }

    [Fact]
    public async Task GetPartDetailAsync_Failed_Mps_Refresh_Preserves_Compatible_PartDetail_Cache()
    {
        var callCount = 0;
        var (service, store, _, _) = BuildService(read: (_, partNumber, _, _) =>
        {
            callCount++;
            return Task.FromResult<PartDetailSourceFacts?>(MakeFacts(partNumber));
        });
        SeedLoadedMps(store, Workspace.AssignmentId);

        await service.GetPartDetailAsync(Workspace.AssignmentId, "ABC100");

        // A failed MPS refresh retains the prior good snapshot/id (see InMemoryMpsSnapshotStore.SetFailed).
        store.SetFailed(Workspace.AssignmentId, "QAD database connectivity failed.");

        var afterFailedRefresh = await service.GetPartDetailAsync(Workspace.AssignmentId, "ABC100");

        Assert.Equal(PartDetailOutcomeKind.Loaded, afterFailedRefresh.Kind);
        Assert.False(afterFailedRefresh.Detail!.IsStale);
        Assert.Equal(1, callCount);
    }
}
