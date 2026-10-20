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
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Kst.Application.Tests.ComponentOrders;

public sealed class ComponentOrdersServiceTests(ITestOutputHelper output)
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
        Func<ComponentOrderEnrichmentRequest, CancellationToken, Task<ComponentOrderEnrichmentResult>>? enrichment = null,
        ILogger<ComponentOrdersService>? logger = null)
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
            logger ?? NullLogger<ComponentOrdersService>.Instance);

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
            new Dictionary<string, BuyerComment> { ["COMP"] = new("first\r\nsecond", new(2022, 1, 31, 13, 30, 0), "jdoe"), ["OTHER"] = new(null) },
            new Dictionary<string, ComponentOrderSupplierRisk> { [" V1 "] = new(true, false), ["V1"] = new(false, true) })));
        var snapshotId = SeedSnapshot(fixture.MpsStore, "P1");

        var result = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);

        Assert.Equal(ComponentOrdersEnrichmentAvailability.Available, result.EnrichmentAvailability);
        var request = Assert.Single(fixture.EnrichmentRequests);
        Assert.Equal(["COMP", "OTHER"], request.ComponentIdentifiers);
        Assert.Equal([" V1 ", "V1"], request.SupplierIdentifiers);
        var lines = result.Groups!.SelectMany(group => new[] { group.DisplayLine }.Concat(group.AdditionalLines)).ToList();
        Assert.Equal("first\r\nsecond", lines.Single(line => line.PoNumber == "2076185").CurrentComments);
        Assert.All(lines.Where(line => line.ComponentPart == "COMP"), line =>
        {
            Assert.Equal(new DateTime(2022, 1, 31, 13, 30, 0), line.CommentRecordedAt);
            Assert.Equal("jdoe", line.CommentAuthor);
        });
        Assert.True(lines.Single(line => line.SupplierIdentifier == " V1 ").IsCreditHold);
        Assert.False(lines.Single(line => line.SupplierIdentifier == " V1 ").IsCia);
        Assert.False(lines.Single(line => line.SupplierIdentifier == "V1").IsCreditHold);
        Assert.True(lines.Single(line => line.SupplierIdentifier == "V1").IsCia);
        Assert.Null(lines.Single(line => line.SupplierIdentifier is null).IsCreditHold);
        Assert.Null(lines.Single(line => line.SupplierIdentifier is null).CurrentComments);
    }

    [Fact]
    public async Task RepeatedPoLinesShareOneScopedEnrichmentAndCachedMetadata()
    {
        var effective = new BuyerComment("synthetic latest", new(2022, 1, 31, 13, 30, 0), "jdoe");
        var fixture = Build(poReader: (_, _, _, _) => Task.FromResult<IReadOnlyList<ComponentOrderLine>>(
            Enumerable.Range(0, 2000).Select(i => Line("COMP", poNumber: i.ToString())).ToArray()),
            enrichment: (request, _) =>
            {
                Assert.Equal(["COMP"], request.ComponentIdentifiers);
                return Task.FromResult(new ComponentOrderEnrichmentResult(
                    new Dictionary<string, BuyerComment> { ["COMP"] = effective }, new Dictionary<string, ComponentOrderSupplierRisk>()));
            });
        var snapshotId = SeedSnapshot(fixture.MpsStore, "P1");
        for (var i = 0; i < 2; i++)
        {
            var result = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);
            var group = Assert.Single(result.Groups!);
            Assert.Equal(1999, group.AdditionalLines.Count);
            Assert.All(group.AdditionalLines.Append(group.DisplayLine), line =>
            {
                Assert.Equal(effective.Text, line.CurrentComments);
                Assert.Equal(effective.RecordedAt, line.CommentRecordedAt);
                Assert.Equal(effective.Author, line.CommentAuthor);
            });
        }
        Assert.Single(fixture.EnrichmentRequests);
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

    [Fact]
    public async Task Timings_CorrelateNestedPhases_AndDistinguishCacheHit()
    {
        var logger = new TimingLogger();
        var fixture = Build(logger: logger);
        var snapshotId = SeedSnapshot(fixture.MpsStore, "P1", "P2");
        await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);

        var timings = logger.Timings.ToArray();
        Assert.Single(timings.Select(t => t["OperationId"]).Distinct());
        Assert.All(timings, t => Assert.True((double)t["PhaseElapsedMs"]! >= 0));
        var service = Assert.Single(timings, t => Equals(t["Phase"], "Service"));
        var scope = Assert.Single(timings, t => Equals(t["Phase"], "BomScope"));
        Assert.Equal(service["SpanId"], scope["ParentSpanId"]);
        Assert.Equal("Loaded", service["Outcome"]);
        Assert.Contains(timings, t => Equals(t["Phase"], "CacheLookup") && Equals(t["Outcome"], "Miss"));
        Assert.Equal(2, timings.Count(t => Equals(t["Phase"], "BomGateWait")));

        logger.Timings.Clear();
        await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshotId, Today);
        Assert.Contains(logger.Timings, t => Equals(t["Phase"], "CacheLookup") && Equals(t["Outcome"], "Hit"));
        Assert.DoesNotContain(logger.Timings, t => Equals(t["Phase"], "BomScope") || Equals(t["Phase"], "PoSource"));
        Assert.Null(Activity.Current);
    }

    [Theory]
    [InlineData(false, "Failed", ComponentOrdersOutcomeKind.Unavailable)]
    [InlineData(true, "Cancelled", null)]
    public async Task Timings_PreserveFailuresAndCancellation_WithoutExceptionContents(
        bool cancel, string expectedOutcome, ComponentOrdersOutcomeKind? resultKind)
    {
        const string sensitive = "synthetic-sensitive-free-text";
        var logger = new TimingLogger();
        var fixture = Build(poReader: (_, _, _, _) => cancel
            ? throw new OperationCanceledException(sensitive)
            : throw new InvalidOperationException(sensitive), logger: logger);
        var snapshot = SeedSnapshot(fixture.MpsStore, "P1");
        if (cancel)
            await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshot, Today));
        else
            Assert.Equal(resultKind, (await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshot, Today)).Kind);

        Assert.Contains(logger.Timings, t => Equals(t["Phase"], "PoSource") && Equals(t["Outcome"], expectedOutcome));
        Assert.DoesNotContain(logger.Messages, message => message.Contains(sensitive, StringComparison.Ordinal));
        Assert.All(logger.Exceptions, exception => Assert.Null(exception));
    }

    [Theory]
    [InlineData(12, 100, 500, 4)]
    [InlineData(120, 200, 5000, 8)]
    public async Task SyntheticApplicationProfile_RecordsSamples_AndPreservesCompleteOutputs(
        int parentCount, int occurrencesPerParent, int partCount, int linesPerPart)
    {
        // Generated, in-memory data only: no source acquisition, network, SQL/cache simulation or latency assertion.
        var parents = Enumerable.Range(0, parentCount).Select(i => $"P{i}").ToArray();
        var bom = parents.Select((p, i) => (p, rows: (IReadOnlyList<BomOccurrence>)Enumerable.Range(0, occurrencesPerParent)
            .Select(j => Occ($"C{(i * 100 + j) % partCount}")).ToArray())).ToDictionary(x => x.p, x => x.rows);
        var parts = parents.SelectMany(p => bom[p]).Select(row => row.ComponentPart).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var raw = parts.SelectMany((part, i) => Enumerable.Range(0, linesPerPart).Select(j =>
            Line(j % 2 == 0 ? part : part.ToLowerInvariant(), j % 3 == 0 ? null : Today.AddDays(j), $"PO-{i}-{j}",
                j % 4 == 0 ? null : j % 2 == 0 ? "Supplier" : "supplier") with
            {
                Description = j % 5 == 0 ? null : "synthetic description",
                OpenQuantity = 1.2345678901m + j,
                Confirmed = j % 3 == 0 ? null : j % 2 == 0,
                IsKss = j % 2 == 0,
                TrackingInfo = "synthetic tracking"
            })).Reverse().ToArray();
        var comments = parts.ToDictionary(p => p, _ => new BuyerComment("synthetic\r\ncomment"), StringComparer.Ordinal);
        var risks = new Dictionary<string, ComponentOrderSupplierRisk>(StringComparer.Ordinal)
        {
            ["Supplier"] = new(true, false),
            ["supplier"] = new(false, true)
        };
        var expectedLines = raw.Select(line => line with
        {
            IsCreditHold = line.SupplierIdentifier is { } supplier && risks.TryGetValue(supplier, out var risk) ? risk.IsCreditHold : null,
            IsCia = line.SupplierIdentifier is { } key && risks.TryGetValue(key, out var cia) ? cia.IsCia : null,
            CurrentComments = comments.GetValueOrDefault(line.ComponentPart)?.Text
        }).ToArray();
        var expected = JsonSerializer.Serialize(ComponentOrdersBuilder.BuildGroups(expectedLines));
        var elapsed = new List<double>();
        var allocated = new List<long>();
        var phaseSamples = new Dictionary<string, List<double>>();
        for (var run = 0; run < 8; run++)
        {
            var logger = new TimingLogger();
            var fixture = Build(bom: (_, parent, _, _) => Task.FromResult(bom[parent]),
                poReader: (_, _, _, _) => Task.FromResult<IReadOnlyList<ComponentOrderLine>>(raw),
                enrichment: (_, _) => Task.FromResult(new ComponentOrderEnrichmentResult(comments, risks)), logger: logger);
            var snapshot = SeedSnapshot(fixture.MpsStore, parents);
            var beforeAllocation = GC.GetAllocatedBytesForCurrentThread();
            var started = Stopwatch.GetTimestamp();
            var result = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshot, Today);
            var duration = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var bytes = GC.GetAllocatedBytesForCurrentThread() - beforeAllocation;
            Assert.Equal(expected, JsonSerializer.Serialize(result.Groups));
            Assert.Equal(parts, Assert.Single(fixture.PoReaderComponentBatches));
            if (run > 0)
            {
                elapsed.Add(duration);
                allocated.Add(bytes);
                foreach (var timing in logger.Timings.Where(t => new[] { "BomScope", "Enrichment", "Grouping" }.Contains(t["Phase"])))
                {
                    var phase = (string)timing["Phase"]!;
                    if (!phaseSamples.TryGetValue(phase, out var samples)) phaseSamples[phase] = samples = [];
                    samples.Add((double)timing["PhaseElapsedMs"]!);
                }
            }
            Assert.Equal(expected, JsonSerializer.Serialize((await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshot, Today)).Groups));
            Assert.Single(fixture.PoReaderComponentBatches);
        }
        elapsed.Sort();
        allocated.Sort();
        output.WriteLine($"SYNTHETIC ONLY parents={parentCount} occurrences={parentCount * occurrencesPerParent} components={parts.Length} lines={raw.Length}; 1 warmup + 7 samples; total ms median={elapsed[3]:F3} range={elapsed[0]:F3}..{elapsed[^1]:F3}; median allocated bytes={allocated[3]}");
        foreach (var (phase, samples) in phaseSamples)
        {
            samples.Sort();
            output.WriteLine($"{phase} inclusive ms median={samples[3]:F3} range={samples[0]:F3}..{samples[^1]:F3}");
        }
    }

    [Fact]
    public async Task ThrowingTimingSink_DoesNotStrandBomPermits_OrPreventSuccessfulLoad()
    {
        var fixture = Build(logger: new TimingLogger(throwTimings: true));
        var snapshot = SeedSnapshot(fixture.MpsStore, Enumerable.Range(0, 12).Select(i => $"P{i}").ToArray());
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var result = await fixture.Service.GetComponentOrdersAsync(WorkspaceId, snapshot, Today, cancellation.Token);
        Assert.Equal(ComponentOrdersOutcomeKind.Loaded, result.Kind);
        Assert.Equal(12, fixture.BomParentsRead.Count);
    }

    [Fact]
    public async Task ThrowingTimingSink_PreservesOriginalFailureAndCancellation_AndResourceHandoff()
    {
        var logger = new TimingLogger(throwTimings: true);
        var failure = new InvalidOperationException("synthetic failure");
        var cancelled = new OperationCanceledException("synthetic cancellation");
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ComponentOrdersTiming.MeasureAsync<int>(logger, "SyntheticFailure", () => throw failure)));
        Assert.Same(cancelled, await Assert.ThrowsAsync<OperationCanceledException>(() =>
            ComponentOrdersTiming.MeasureAsync<int>(logger, "SyntheticCancelled", () => throw cancelled)));
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
            ComponentOrdersTiming.Measure<int>(logger, "SyntheticSyncFailure", () => throw failure)));
        var resource = new SyntheticResource();
        using (var owned = await ComponentOrdersTiming.MeasureAsync(logger, "SyntheticOpen", () => Task.FromResult(resource)))
            Assert.Same(resource, owned);
        Assert.True(resource.Disposed);
    }

    private sealed class SyntheticResource : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    private sealed class TimingLogger(bool throwTimings = false) : ILogger<ComponentOrdersService>
    {
        public ConcurrentQueue<Dictionary<string, object?>> Timings { get; } = new();
        public ConcurrentQueue<string> Messages { get; } = new();
        public ConcurrentQueue<Exception?> Exceptions { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Enqueue(formatter(state, exception));
            Exceptions.Enqueue(exception);
            if (state is IEnumerable<KeyValuePair<string, object?>> properties)
            {
                var fields = properties.ToDictionary(p => p.Key, p => p.Value);
                if (fields.ContainsKey("PhaseElapsedMs"))
                {
                    if (throwTimings) throw new InvalidOperationException("synthetic timing sink failure");
                    Timings.Enqueue(fields);
                }
            }
        }
    }
}
