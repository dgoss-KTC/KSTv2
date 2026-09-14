using Kst.Application.Bom;
using Kst.Application.ComponentOrders;
using Kst.Application.Mps;
using Kst.Application.Tests.Mps;
using Kst.Domain.Bom;
using Kst.Domain.Common;
using Kst.Domain.ComponentOrders;
using Kst.Domain.Mps;
using Kst.Domain.Workspaces;
using Kst.Infrastructure;
using Kst.Infrastructure.ComponentOrders;
using Kst.Infrastructure.Mps;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Threading;

namespace Kst.Application.Tests.ComponentOrders;

public sealed class ComponentOrdersServiceTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 9, 10);

    private static WorkspaceAssignment Workspace() => new(
        AssignmentId: WorkspaceId,
        DisplayName: "Test",
        Site: "SW",
        ProductLineFrom: null,
        ProductLineTo: null,
        ParentParts: [],
        IsTemporary: false,
        CoverageEndsOn: null,
        IsEnabled: true,
        SortOrder: 0);

    private static BomOccurrence Occ(string componentPart) => new(
        OccurrenceKey: $"K-{componentPart}",
        Level: 1,
        ComponentPart: componentPart,
        PmCode: "P",
        IsPhantom: false,
        Description: null,
        QuantityPer: 1m,
        ScrapPercentage: null);

    private static ComponentOrderLine Line(string part, DateOnly? dueDate = null, string poNumber = "2076185", string? supplierIdentifier = "SUP-1") => new(
        ComponentPart: part,
        Description: "Desc",
        LeadTimeDays: 14,
        PoNumber: poNumber,
        PoLine: 1,
        DueDate: dueDate,
        OpenQuantity: 5m,
        Confirmed: true,
        SupplierDisplay: "ACME",
        BuyerDisplay: null,
        ManufacturerItem: null,
        IsKss: false,
        TrackingInfo: null,
        SupplierIdentifier: supplierIdentifier);

    private sealed class Fixture
    {
        public required ComponentOrdersService Service { get; init; }
        public required InMemoryMpsSnapshotStore MpsStore { get; init; }
        public required ConcurrentQueue<string> BomParentsRead { get; init; }
        public required List<IReadOnlyList<string>> PoReaderComponentBatches { get; init; }
        public required List<ComponentOrderEnrichmentRequest> EnrichmentRequests { get; init; }
    }

    private static Fixture Build(
        Func<string, string, DateOnly, CancellationToken, Task<IReadOnlyList<BomOccurrence>>>? bom = null,
        Func<string, IReadOnlyList<string>, DateOnly, CancellationToken, Task<IReadOnlyList<ComponentOrderLine>>>? poReader = null,
        Func<ComponentOrderEnrichmentRequest, CancellationToken, Task<ComponentOrderEnrichmentResult>>? enrichment = null)
    {
        var mpsStore = new InMemoryMpsSnapshotStore();
        var cacheStore = new InMemoryComponentOrdersCacheStore();
        var bomParentsRead = new ConcurrentQueue<string>();
        var poReaderBatches = new List<IReadOnlyList<string>>();
        var enrichmentRequests = new List<ComponentOrderEnrichmentRequest>();

        var bomReader = new DelegateBomSourceReader((site, parentPart, effectiveDate, ct) =>
        {
            bomParentsRead.Enqueue(parentPart);
            return (bom ?? ((s, p, d, c) => Task.FromResult<IReadOnlyList<BomOccurrence>>([Occ("COMP")])))(site, parentPart, effectiveDate, ct);
        });

        var poReaderDelegate = new DelegateComponentOrderSourceReader((site, components, today, ct) =>
        {
            poReaderBatches.Add(components);
            return (poReader ?? ((s, c, d, x) => Task.FromResult<IReadOnlyList<ComponentOrderLine>>(c.Select(p => Line(p)).ToList())))(site, components, today, ct);
        });
        var enrichmentReader = new DelegateComponentOrderEnrichmentReader((request, ct) =>
        {
            enrichmentRequests.Add(request);
            return (enrichment ?? ((r, c) => Task.FromResult(ComponentOrderEnrichmentResult.Empty)))(request, ct);
        });

        var service = new ComponentOrdersService(
            new FakeWorkspaceConfigurationService(Workspace()),
            mpsStore,
            bomReader,
            poReaderDelegate,
            enrichmentReader,
            cacheStore,
            NullLogger<ComponentOrdersService>.Instance);

        return new Fixture { Service = service, MpsStore = mpsStore, BomParentsRead = bomParentsRead, PoReaderComponentBatches = poReaderBatches, EnrichmentRequests = enrichmentRequests };
    }

    private static SnapshotId SeedSnapshot(InMemoryMpsSnapshotStore store, params string[] parents)
    {
        var resolved = parents.Select(p => new MpsResolvedPart(p, "Desc")).ToList();
        var snapshot = new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, "SW", resolved, []);
        store.SetLoaded(WorkspaceId, snapshot);
        return snapshot.Id;
    }

    [Fact]
    public async Task UnknownWorkspace_Throws_WorkspaceNotFound()
    {
        var fixture = Build();
        await Assert.ThrowsAsync<ComponentOrdersWorkspaceNotFoundException>(
            () => fixture.Service.GetComponentOrdersAsync(Guid.NewGuid(), SnapshotId.New(), Today));
    }

    [Fact]
    public async Task NoMpsSnapshot_Returns_MpsNotLoaded_WithoutReadingBomOrPo()
    {
        var fixture = Build();

        var result = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, SnapshotId.New(), Today);

        Assert.Equal(ComponentOrdersOutcomeKind.MpsNotLoaded, result.Kind);
        Assert.Empty(fixture.BomParentsRead);
        Assert.Empty(fixture.PoReaderComponentBatches);
    }

    [Fact]
    public async Task SupersededSnapshotId_Returns_SnapshotChanged_WithoutReading()
    {
        var fixture = Build();
        SeedSnapshot(fixture.MpsStore, "P1"); // current snapshot differs from the requested id

        var result = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, SnapshotId.New(), Today);

        Assert.Equal(ComponentOrdersOutcomeKind.SnapshotChanged, result.Kind);
        Assert.Empty(fixture.BomParentsRead);
    }

    [Fact]
    public async Task AllResolvedParents_AreExploded_AndComponentsDeduplicatedCaseInsensitively()
    {
        var fixture = Build(bom: (site, parentPart, effectiveDate, ct) =>
            Task.FromResult<IReadOnlyList<BomOccurrence>>(parentPart switch
            {
                "P1" => new[] { Occ("COMP-A"), Occ("comp-b") },
                "P2" => new[] { Occ("COMP-B"), Occ("C3") }, // COMP-B overlaps P1's comp-b case-insensitively
                _ => throw new InvalidOperationException($"Unexpected parent {parentPart}"),
            }));
        var snapshotId = SeedSnapshot(fixture.MpsStore, "P1", "P2");

        var result = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);

        Assert.Equal(ComponentOrdersOutcomeKind.Loaded, result.Kind);
        // Every resolved parent is read at the business date — not a single lazy selected parent. Reads run
        // with bounded concurrency, so invocation order is not guaranteed; assert the set instead.
        Assert.Equal(new HashSet<string> { "P1", "P2" }, fixture.BomParentsRead.ToHashSet());
        var components = Assert.Single(fixture.PoReaderComponentBatches);
        // Case-insensitive dedup, first occurrence kept; site passed through from the workspace.
        Assert.Equal(["COMP-A", "comp-b", "C3"], components);
    }

    [Fact]
    public async Task BomFailure_Returns_Unavailable_AndIsNotCached()
    {
        var fixture = Build(bom: (_, _, _, _) => throw new InvalidOperationException("QAD down"));
        var snapshotId = SeedSnapshot(fixture.MpsStore, "P1");

        var first = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);
        Assert.Equal(ComponentOrdersOutcomeKind.Unavailable, first.Kind);

        // Not cached: the next attempt re-reads (BOM attempted again) instead of serving a stale/empty result.
        var second = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);
        Assert.Equal(ComponentOrdersOutcomeKind.Unavailable, second.Kind);
        Assert.Equal(2, fixture.BomParentsRead.Count(p => p == "P1"));

    }

    [Fact]
    public async Task PoReaderFailure_Returns_Unavailable_NotAnEmptyList()
    {
        var fixture = Build(poReader: (_, _, _, _) => throw new InvalidOperationException("QAD down"));
        var snapshotId = SeedSnapshot(fixture.MpsStore, "P1");

        var result = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);

        Assert.Equal(ComponentOrdersOutcomeKind.Unavailable, result.Kind);
        Assert.Null(result.Groups);
    }

    [Fact]
    public async Task NoComponents_Returns_LoadedEmpty_WithoutReadingPo_AndIsCached()
    {
        var fixture = Build(bom: (_, _, _, _) => Task.FromResult<IReadOnlyList<BomOccurrence>>([]));
        var snapshotId = SeedSnapshot(fixture.MpsStore, "P1");

        var first = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);
        Assert.Equal(ComponentOrdersOutcomeKind.Loaded, first.Kind);
        Assert.Empty(first.Groups!);
        Assert.Empty(fixture.PoReaderComponentBatches); // no components → no PO read at all

        await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);
        Assert.Single(fixture.BomParentsRead); // second call served from cache — BOM not re-read
    }

    [Fact]
    public async Task SuccessfulLoad_IsCached_ForSameSnapshotAndBusinessDate()
    {
        var fixture = Build();
        var snapshotId = SeedSnapshot(fixture.MpsStore, "P1");

        var first = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);
        Assert.Equal(ComponentOrdersOutcomeKind.Loaded, first.Kind);
        Assert.NotNull(first.Groups);

        var second = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);
        Assert.Same(first.Groups, second.Groups); // cache hit — identical composed result
        Assert.Single(fixture.PoReaderComponentBatches);
    }

    [Fact]
    public async Task DifferentBusinessDate_ReReads_SourceFacts()
    {
        var fixture = Build();
        var snapshotId = SeedSnapshot(fixture.MpsStore, "P1");

        await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);
        await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today.AddDays(1));

        Assert.Equal(2, fixture.PoReaderComponentBatches.Count); // business date is part of the cache key
    }

    [Fact]
    public async Task SnapshotRefresh_NeverServesPriorSnapshotResult_AsFresh()
    {
        var fixture = Build();
        var firstId = SeedSnapshot(fixture.MpsStore, "P1");
        await fixture.Service.GetComponentOrdersAsync(WorkspaceId, firstId, Today);

        // A new successful MPS refresh supersedes the snapshot generation.
        var secondId = SeedSnapshot(fixture.MpsStore, "P1", "P2");

        // The prior snapshot id is now stale — never answered from the old cache entry.
        var stale = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, firstId, Today);
        Assert.Equal(ComponentOrdersOutcomeKind.SnapshotChanged, stale.Kind);

        // The new generation reads fresh (both parents) and caches under its own id.
        var fresh = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, secondId, Today);
        Assert.Equal(ComponentOrdersOutcomeKind.Loaded, fresh.Kind);
        // Bounded-concurrency reads don't guarantee invocation order — assert the set read for the new generation.
        Assert.Equal(new HashSet<string> { "P1", "P2" }, fixture.BomParentsRead.Skip(1).ToHashSet());
    }

    [Fact]
    public async Task RefreshDuringRead_Returns_SnapshotChanged_AndDoesNotCache()
    {
        SnapshotId? replacementId = null;
        var readCount = 0;
        Fixture? fixture = null;
        fixture = Build(poReader: (site, components, today, ct) =>
        {
            if (++readCount == 2)
                replacementId = SeedSnapshot(fixture!.MpsStore, "P1");
            return Task.FromResult<IReadOnlyList<ComponentOrderLine>>([]);
        });
        var snapshotId = SeedSnapshot(fixture.MpsStore, "P1");

        var warm = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);
        Assert.Equal(ComponentOrdersOutcomeKind.Loaded, warm.Kind);

        var result = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today.AddDays(1));

        Assert.Equal(ComponentOrdersOutcomeKind.SnapshotChanged, result.Kind);

        // The superseded computation must not be cached: the new generation loads fresh from source.
        var fresh = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, replacementId!.Value, Today);
        Assert.Equal(ComponentOrdersOutcomeKind.Loaded, fresh.Kind);
    }

    [Fact]
    public async Task BoundedConcurrency_NeverExceedsCap_AndPreservesResolvedParentOrder()
    {
        const int Cap = 8;
        var parents = Enumerable.Range(1, 10).Select(i => $"P{i}").ToArray(); // more than the cap

        var active = 0;
        var peak = 0;
        var waveGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var fixture = Build(bom: async (site, parentPart, effectiveDate, ct) =>
        {
            var current = Interlocked.Increment(ref active);
            int observed;
            do
            {
                observed = peak;
                if (current <= observed)
                    break;
            } while (Interlocked.CompareExchange(ref peak, current, observed) != observed);

            if (current == Cap)
                waveGate.TrySetResult(); // the cap has been observed — release the held first wave

            await waveGate.Task; // holds every concurrent reader here until the cap is actually reached

            Interlocked.Decrement(ref active);
            return new[] { Occ(parentPart) };
        });
        var snapshotId = SeedSnapshot(fixture.MpsStore, parents);

        var result = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);

        Assert.Equal(ComponentOrdersOutcomeKind.Loaded, result.Kind);
        Assert.Equal(Cap, peak); // 10 parents against an 8-cap deterministically drives peak concurrency to exactly 8
        Assert.Equal(parents.ToHashSet(), fixture.BomParentsRead.ToHashSet()); // every parent was eventually read
        var components = Assert.Single(fixture.PoReaderComponentBatches);
        // Resolved-parent order is preserved regardless of completion order under bounded concurrency.
        Assert.Equal(parents, components);
    }

    [Fact]
    public async Task CancellationDuringBomFanOut_PropagatesOperationCanceledException_NeverLoadedOrUnavailable()
    {
        using var cts = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var fixture = Build(bom: async (site, parentPart, effectiveDate, ct) =>
        {
            started.TrySetResult(); // signals at least one BOM read is now pending inside the fan-out
            await Task.Delay(Timeout.Infinite, ct); // only ever completes (by throwing) when the caller cancels
            return (IReadOnlyList<BomOccurrence>)[];
        });
        var snapshotId = SeedSnapshot(fixture.MpsStore, "P1", "P2", "P3", "P4");

        var task = fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today, cts.Token);
        try
        {
            await started.Task;
            cts.Cancel();

            // The service must propagate cancellation directly — never converting it to Loaded or Unavailable.
            // Because the exception escapes GetComponentOrdersAsync entirely, the cache.Set call is never
            // reached, so no partial/superseded result can have been cached either.
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        }
        finally
        {
            cts.Cancel(); // idempotent safety net so no pending delegate is left awaiting Task.Delay(Timeout.Infinite)
            try { await task; } catch { /* already asserted above; just ensure nothing is left dangling */ }
        }
    }

    [Fact]
    public async Task LoadedResult_Uses_AcceptedGrouping_AndOrdering()
    {
        var fixture = Build(poReader: (_, components, _, _) => Task.FromResult<IReadOnlyList<ComponentOrderLine>>(new[]
        {
            Line("B-COMP", dueDate: new DateOnly(2026, 9, 1), poNumber: "300"),
            Line("A-COMP", dueDate: null, poNumber: "400"), // missing earliest due → yellow exception first
            Line("B-COMP", dueDate: new DateOnly(2026, 8, 15), poNumber: "200"),
        }));
        var snapshotId = SeedSnapshot(fixture.MpsStore, "P1");

        var result = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);

        Assert.Equal(ComponentOrdersOutcomeKind.Loaded, result.Kind);
        var groups = result.Groups!;
        Assert.Equal(["A-COMP", "B-COMP"], groups.Select(g => g.ComponentPart));
        Assert.Same(groups[0].DisplayLine, groups[0].DisplayLine); // A-COMP's only line is its display line
        Assert.Null(groups[0].DisplayLine.DueDate);
        var bGroup = groups[1];
        Assert.Equal("200", bGroup.DisplayLine.PoNumber); // earliest due date wins the collapsed row
        Assert.Single(bGroup.AdditionalLines);
    }

    [Fact]
    public async Task SuccessfulEnrichment_UsesExactDistinctSupplierDisplayKeys_AndComposesFacts()
    {
        var fixture = Build(poReader: (_, _, _, _) => Task.FromResult<IReadOnlyList<ComponentOrderLine>>([
            Line("COMP", supplierIdentifier: " V1 "),
            Line("COMP", poNumber: "2", supplierIdentifier: "V1"),
            Line("OTHER", poNumber: "3", supplierIdentifier: null)
        ]), enrichment: (request, _) => Task.FromResult(new ComponentOrderEnrichmentResult(
            new Dictionary<string, string?> { ["COMP"] = "first\r\nsecond", ["OTHER"] = null },
            new Dictionary<string, ComponentOrderSupplierRisk> { [" V1 "] = new(true, false), ["V1"] = new(false, true) })));
        var snapshotId = SeedSnapshot(fixture.MpsStore, "P1");

        var result = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);

        Assert.Equal(ComponentOrdersEnrichmentAvailability.Available, result.EnrichmentAvailability);
        var request = Assert.Single(fixture.EnrichmentRequests);
        Assert.Equal(["COMP", "OTHER"], request.ComponentIdentifiers);
        Assert.Equal([" V1 ", "V1"], request.SupplierIdentifiers);
        var lines = result.Groups!.SelectMany(group => new[] { group.DisplayLine }.Concat(group.AdditionalLines)).ToList();
        Assert.Equal("first\r\nsecond", lines.Single(line => line.PoNumber == "2076185").CurrentComments);
        Assert.True(lines.Single(line => line.SupplierIdentifier == " V1 ").IsCreditHold);
        Assert.False(lines.Single(line => line.SupplierIdentifier == " V1 ").IsCia);
        Assert.False(lines.Single(line => line.SupplierIdentifier == "V1").IsCreditHold);
        Assert.True(lines.Single(line => line.SupplierIdentifier == "V1").IsCia);
        Assert.Null(lines.Single(line => line.SupplierIdentifier is null).IsCreditHold);
        Assert.Null(lines.Single(line => line.SupplierIdentifier is null).CurrentComments);
    }

    [Fact]
    public async Task EnrichmentFailure_ReturnsQadGroupsUnavailable_WithAbsentFacts_AndIsNotCached()
    {
        var calls = 0;
        var fixture = Build(enrichment: (_, _) =>
        {
            calls++;
            throw new InvalidOperationException("Shortages unavailable");
        });
        var snapshotId = SeedSnapshot(fixture.MpsStore, "P1");

        var first = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);
        var second = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);

        Assert.Equal(ComponentOrdersOutcomeKind.Loaded, first.Kind);
        Assert.Equal(ComponentOrdersEnrichmentAvailability.Unavailable, first.EnrichmentAvailability);
        var firstLine = Assert.Single(first.Groups!).DisplayLine;
        Assert.Null(firstLine.IsCreditHold);
        Assert.Null(firstLine.IsCia);
        Assert.Null(firstLine.CurrentComments);
        Assert.Equal(2, calls); // unavailable data is never cached as if complete
        Assert.Equal(2, fixture.PoReaderComponentBatches.Count);
        Assert.Equal(ComponentOrdersEnrichmentAvailability.Unavailable, second.EnrichmentAvailability);
    }

    [Fact]
    public async Task CancellationDuringEnrichment_Propagates()
    {
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fixture = Build(enrichment: async (_, ct) =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.Infinite, ct);
            return ComponentOrderEnrichmentResult.Empty;
        });
        var snapshotId = SeedSnapshot(fixture.MpsStore, "P1");
        var task = fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today, cancellation.Token);
        await started.Task;
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }
}
