using Kst.Application.Bom;
using Kst.Application.ComponentOrders;
using Kst.Application.LongTermShortages;
using Kst.Application.Mps;
using Kst.Application.Tests.Mps;
using Kst.Domain.Bom;
using Kst.Domain.ComponentOrders;
using Kst.Domain.Common;
using Kst.Domain.LongTermShortages;
using Kst.Domain.Mps;
using Kst.Domain.Workspaces;
using Kst.Infrastructure.LongTermShortages;
using Kst.Infrastructure.Mps;
using Microsoft.Extensions.Logging.Abstractions;
namespace Kst.Application.Tests.LongTermShortages;
public sealed class LongTermShortagesServiceTests
{
    [Fact]
    public async Task GetAsync_CachesCompatibleProjectionButFailsClosedOnRefreshError()
    {
        var calls = 0; var fixture = Build((_, _, _, _, _) => ++calls == 1 ? Task.FromResult(new LongTermShortageAcquisition(new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero), [Input() with { OpeningQoh = -1m }])) : throw new InvalidOperationException());
        var first = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        var hit = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        fixture.Clock.LocalNow = fixture.Clock.LocalNow.AddDays(1);
        var stale = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        Assert.Equal(2, calls); Assert.False(first.IsStale); Assert.False(hit.IsStale); Assert.Equal(first.AcquiredAtUtc, hit.AcquiredAtUtc); Assert.Equal(LongTermShortageAcquisition.ConsistencyMode, first.ConsistencyMode); Assert.Equal(LongTermShortagesOutcomeKind.Unavailable, stale.Kind); Assert.Null(stale.Rows);
    }
    [Fact]
    public async Task GetCachedForExportAsync_DoesNotReadAgainAndRejectsWrongPart()
    {
        var calls = 0; var fixture = Build((_, _, _, _, _) => { calls++; return Task.FromResult(new LongTermShortageAcquisition(new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero), [Input() with { OpeningQoh = -1m }])); });
        await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        var exported = await fixture.Service.GetCachedForExportAsync(fixture.WorkspaceId, fixture.SnapshotId, ["COMP"], LongTermShortagePopulationOptions.Default);
        var rejected = await fixture.Service.GetCachedForExportAsync(fixture.WorkspaceId, fixture.SnapshotId, ["OTHER"], LongTermShortagePopulationOptions.Default);
        Assert.Equal(1, calls); Assert.Equal(LongTermShortagesOutcomeKind.Loaded, exported.Kind); Assert.Equal(LongTermShortagesOutcomeKind.Unavailable, rejected.Kind);
    }
    [Fact]
    public async Task GetAsync_InitialQueryFailureIsUnavailable_NotAnEmptyHealthyReport()
    {
        var fixture = Build((_, _, _, _, _) => throw new InvalidOperationException("Source unavailable"));
        var result = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        Assert.Equal(LongTermShortagesOutcomeKind.Unavailable, result.Kind);
        Assert.Null(result.Rows);
        Assert.Equal("Component MRP source acquisition failed. Check the QADPro2 connection and retry.", result.FailureDetail);
        var export = await fixture.Service.GetCachedForExportAsync(fixture.WorkspaceId, fixture.SnapshotId, [], LongTermShortagePopulationOptions.Default);
        Assert.Equal(LongTermShortagesOutcomeKind.Unavailable, export.Kind);
    }
    [Fact]
    public async Task GetAsync_PartialSuccessfulReadIsUnavailable_NotZeroInventory()
    {
        var fixture = Build((_, _, _, _, _) => Task.FromResult(new LongTermShortageAcquisition(DateTimeOffset.UtcNow, [])));
        var result = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        Assert.Equal(LongTermShortagesOutcomeKind.Unavailable, result.Kind);
        Assert.Null(result.Rows);
        Assert.Equal("QADPro2 returned incomplete component results. Retry the report.", result.FailureDetail);
    }
    [Fact]
    public async Task GetAsync_ReturnsCompletePopulationAndBothModesWithoutAnotherAcquisition()
    {
        var calls = 0;
        var fixture = Build((_, _, _, _, _) => { calls++; return Task.FromResult(new LongTermShortageAcquisition(DateTimeOffset.UtcNow,
        [
            Input() with { OpeningQoh = -1m, Evidence = [new(1, "SUPPLY", new(2026, 9, 16), null, 10m, MrpScheduleCategory.Unclassified)] },
            Input() with { ComponentPart = "FUTURE", OpeningQoh = 1m, Evidence = [new(2, "DEMAND", new(2026, 9, 22), null, 2m, MrpScheduleCategory.Unclassified)] },
            Input() with { ComponentPart = "SAFE", OpeningQoh = 5m, SafetyStock = 6m },
            Input() with { ComponentPart = "HEALTHY" }
        ])); }, ["COMP", "FUTURE", "SAFE", "HEALTHY"]);
        var defaults = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        var all = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default with { ShowAll = true });
        var unconfirmed = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default with { IncludeUnconfirmed = true });
        var shorter = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default with { HorizonWeeks = 13 });
        Assert.Equal(LongTermShortagesOutcomeKind.Loaded, defaults.Kind);
        Assert.Equal(4, defaults.Rows!.Count);
        Assert.Contains(defaults.Rows, row => row.ComponentPart == "COMP" && row.Severity == LongTermShortageSeverity.CriticalShort);
        Assert.Contains(defaults.Rows, row => row.ComponentPart == "FUTURE" && row.Severity == LongTermShortageSeverity.FutureShort);
        Assert.Contains(defaults.Rows, row => row.ComponentPart == "SAFE" && row.Severity == LongTermShortageSeverity.SafetyStockShort);
        Assert.Equal(4, all.Rows!.Count);
        Assert.Contains(all.Rows, row => row.ComponentPart == "HEALTHY" && row.Severity == LongTermShortageSeverity.Healthy);
        Assert.Equal(4, unconfirmed.AllReceiptsRows!.Count);
        Assert.Equal(13, shorter.Rows![0].Weeks.Count);
        Assert.Equal(defaults.AcquiredAtUtc, shorter.AcquiredAtUtc);
        Assert.Equal(1, calls);
    }
    [Fact]
    public async Task GetAsync_ReceiptModeRetainsIndependentShortageStatusAndExportUsesSelectedProjection()
    {
        var calls = 0;
        var fixture = Build((_, _, _, _, _) => { calls++; return Task.FromResult(new LongTermShortageAcquisition(DateTimeOffset.UtcNow,
            [Input() with { OpeningQoh = 1m, Evidence = [new(1, "DEMAND", new(2026, 9, 16), null, 3m, MrpScheduleCategory.Unclassified),
                new(2, "SUPPLY", new(2026, 9, 15), null, 4m, MrpScheduleCategory.Unclassified) { IsPoReceipt = true, PoConfirmed = false }] }])); });
        var result = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        var confirmed = Assert.Single(result.Rows!);
        var allReceipts = Assert.Single(result.AllReceiptsRows!);
        Assert.Equal(LongTermShortageSeverity.FutureShort, confirmed.Severity);
        Assert.Equal(LongTermShortageSeverity.Healthy, allReceipts.Severity);
        var exported = await fixture.Service.GetCachedForExportAsync(fixture.WorkspaceId, fixture.SnapshotId, ["COMP"], LongTermShortagePopulationOptions.Default with { IncludeUnconfirmed = true, ShowAll = true });
        Assert.Equal(LongTermShortageSeverity.Healthy, Assert.Single(exported.Rows!).Severity);
        Assert.Equal(result.AcquiredAtUtc, exported.AcquiredAtUtc);
        Assert.Equal(1, calls);
    }
    [Fact]
    public async Task GetPurchasingAsync_UsesSelectedCachedComponentWithoutReacquiringProjection_AndKeepsCommentsIndependentOfPos()
    {
        var reads = 0;
        var fixture = Build((_, _, _, _, _) => { reads++; return Task.FromResult(new LongTermShortageAcquisition(DateTimeOffset.UtcNow, [Input()])); });
        var poReads = 0;
        var comments = 0;
        var service = fixture.CreateService(new DelegateComponentOrderSourceReader((site, parts, _, _) =>
        {
            poReads++;
            Assert.Equal("SW", site);
            Assert.Equal(["COMP"], parts);
            return Task.FromResult<IReadOnlyList<ComponentOrderLine>>([]);
        }), new DelegateComponentOrderEnrichmentReader((request, _) =>
        {
            comments++;
            Assert.Equal(["COMP"], request.ComponentIdentifiers);
            Assert.Empty(request.SupplierIdentifiers);
            return Task.FromResult(new ComponentOrderEnrichmentResult(new Dictionary<string, string?> { ["COMP"] = "Current comment" },
                new Dictionary<string, ComponentOrderSupplierRisk>()));
        }));
        await service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        var first = await service.GetPurchasingAsync(fixture.WorkspaceId, fixture.SnapshotId, "COMP", LongTermShortagePopulationOptions.Default);
        var invalid = await service.GetPurchasingAsync(fixture.WorkspaceId, fixture.SnapshotId, "UNSCOPED", LongTermShortagePopulationOptions.Default);
        Assert.Equal(LongTermShortagesOutcomeKind.Loaded, first.Kind);
        Assert.Empty(first.Lines!);
        Assert.Equal("Current comment", first.CurrentComment);
        Assert.True(first.CommentAvailable);
        Assert.Equal(LongTermShortagesOutcomeKind.Unavailable, invalid.Kind);
        Assert.Equal(1, reads);
        Assert.Equal(1, poReads);
        Assert.Equal(1, comments);
    }
    [Fact]
    public async Task GetPurchasingAsync_KeepsPoLinesWhenOptionalCommentSourceFails()
    {
        var fixture = Build((_, _, _, _, _) => Task.FromResult(new LongTermShortageAcquisition(DateTimeOffset.UtcNow, [Input()])));
        var service = fixture.CreateService(new DelegateComponentOrderSourceReader((_, _, _, _) =>
            Task.FromResult<IReadOnlyList<ComponentOrderLine>>([new("COMP", null, null, "PO-1", 1, null, 4m, false, null, null, null, false, null)])),
            new DelegateComponentOrderEnrichmentReader((_, _) => throw new InvalidOperationException("Optional source down")));
        await service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);

        var result = await service.GetPurchasingAsync(fixture.WorkspaceId, fixture.SnapshotId, "COMP", LongTermShortagePopulationOptions.Default);

        Assert.Equal(LongTermShortagesOutcomeKind.Loaded, result.Kind);
        Assert.Equal("PO-1", Assert.Single(result.Lines!).PoNumber);
        Assert.False(result.CommentAvailable);
        Assert.Null(result.CurrentComment);
    }
    private static LongTermShortageInput Input() => new("COMP", "EA", null, null, null, null, 10m, SafetyStockState.Resolved, 0m, ["PARENT"], []);
    private static Fixture Build(Func<string, IReadOnlyDictionary<string, IReadOnlyList<string>>, DateOnly, DateOnly, CancellationToken, Task<LongTermShortageAcquisition>> source, IReadOnlyList<string>? components = null)
    {
        components ??= ["COMP"];
        var workspaceId = Guid.NewGuid(); var snapshotId = SnapshotId.New(); var store = new InMemoryMpsSnapshotStore(); store.SetLoaded(workspaceId, new MpsSnapshot(snapshotId, DateTimeOffset.UtcNow, "SW", [new MpsResolvedPart("PARENT", "")], [])); var clock = new TestClock { LocalNow = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero) };
        var workspaces = new FakeWorkspaceConfigurationService(new WorkspaceAssignment(workspaceId, "Test", "SW", null, null, [], false, null, true, 0));
        var bom = new DelegateBomSourceReader((_, _, _, _) => Task.FromResult<IReadOnlyList<BomOccurrence>>(components.Select((part, index) => new BomOccurrence(index.ToString(), 1, part, "P", false, null, 1m, null)).ToList()));
        var shortageSource = new DelegateLongTermShortageSourceReader(source);
        var cache = new InMemoryLongTermShortagesCacheStore();
        var fixture = new Fixture(clock, workspaceId, snapshotId, workspaces, store, bom, shortageSource, cache);
        return fixture;
    }
    private sealed record Fixture(TestClock Clock, Guid WorkspaceId, SnapshotId SnapshotId,
        FakeWorkspaceConfigurationService Workspaces, InMemoryMpsSnapshotStore Snapshots, IBomSourceReader Bom,
        ILongTermShortageSourceReader Source, InMemoryLongTermShortagesCacheStore Cache)
    {
        public LongTermShortagesService Service => CreateService();
        public LongTermShortagesService CreateService(IComponentOrderSourceReader? orders = null, IComponentOrderEnrichmentReader? enrichment = null) =>
            new(Workspaces, Snapshots, Bom, Source, Cache,
                orders ?? new DelegateComponentOrderSourceReader((_, _, _, _) => Task.FromResult<IReadOnlyList<ComponentOrderLine>>([])),
                enrichment ?? new DelegateComponentOrderEnrichmentReader((_, _) => Task.FromResult(ComponentOrderEnrichmentResult.Empty)),
                Clock, NullLogger<LongTermShortagesService>.Instance);
    }
    private sealed class TestClock : IClock { public DateTimeOffset UtcNow => LocalNow.ToUniversalTime(); public DateTimeOffset LocalNow { get; set; } }
}
