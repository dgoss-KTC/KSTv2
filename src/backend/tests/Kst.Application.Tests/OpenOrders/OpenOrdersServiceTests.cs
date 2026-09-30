using Kst.Application.Mps;
using Kst.Application.OpenOrders;
using Kst.Application.Tests.Mps;
using Kst.Application.Tests.PartDetail;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.OpenOrders;
using Kst.Domain.Workspaces;
using Kst.Infrastructure.Mps;
using Kst.Infrastructure.OpenOrders;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Application.Tests.OpenOrders;

public sealed class OpenOrdersServiceTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly InMemoryMpsSnapshotStore _mps = new();
    private readonly InMemoryOpenOrdersSnapshotStore _cache = new();
    private readonly FakeClock _clock = new();

    private OpenOrdersService Service(Func<string, IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<OpenOrderLine>>> read) =>
        new(new FakeWorkspaceConfigurationService(new WorkspaceAssignment(_workspaceId, "Test", "SW", null, null,
                ["ONLY-CONFIGURED"], false, null, true, 0)), _mps,
            new DelegateOpenOrdersSourceReader(read), _cache, _clock, NullLogger<OpenOrdersService>.Instance);

    private SnapshotId Load(params string[] parents)
    {
        var snapshot = new MpsSnapshot(SnapshotId.New(), _clock.UtcNow, "SW",
            parents.Select(p => new MpsResolvedPart(p, null)).ToArray(), []);
        _mps.SetLoaded(_workspaceId, snapshot);
        return snapshot.Id;
    }

    [Fact]
    public async Task UnknownWorkspaceAndMpsNotLoadedNeverInvokeReader()
    {
        var service = Service((_, _, _) => throw new Exception("Unexpected read"));
        Assert.Equal(OpenOrdersOutcomeKind.UnknownWorkspace, (await service.GetAsync(Guid.NewGuid(), SnapshotId.New(), default)).Kind);
        Assert.Equal(OpenOrdersOutcomeKind.MpsNotLoaded, (await service.GetAsync(_workspaceId, SnapshotId.New(), default)).Kind);
    }

    [Fact]
    public async Task LoadedEmptyPopulationDoesNotReadQadAndGetsOwnSnapshot()
    {
        var id = Load();
        var result = await Service((_, _, _) => throw new Exception("Unexpected read"))
            .GetAsync(_workspaceId, id, default);
        Assert.Equal(OpenOrdersOutcomeKind.Loaded, result.Kind);
        Assert.Empty(result.Report!.Snapshot.Lines);
        Assert.NotEqual(id, result.Report.Snapshot.Id);
        Assert.Equal(id, result.Report.Snapshot.MpsSnapshotId);
    }

    [Fact]
    public async Task ReadsExactlyResolvedSnapshotParentsAndRejectsChangedSnapshot()
    {
        var id = Load("P-1", "P-2");
        var reads = 0;
        var service = Service((site, parents, _) =>
        {
            reads++;
            Assert.Equal("SW", site);
            Assert.Equal(["P-1", "P-2"], parents);
            return Task.FromResult<IReadOnlyList<OpenOrderLine>>([]);
        });
        Assert.Equal(OpenOrdersOutcomeKind.MpsSnapshotChanged, (await service.GetAsync(_workspaceId, SnapshotId.New(), default)).Kind);
        var first = await service.GetAsync(_workspaceId, id, default);
        var hit = await service.GetAsync(_workspaceId, id, default);
        Assert.Equal(1, reads);
        Assert.Equal(first.Report!.Snapshot.Id, hit.Report!.Snapshot.Id);
        var newId = Load("OTHER");
        Assert.Equal(OpenOrdersOutcomeKind.MpsSnapshotChanged, (await service.RefreshAsync(_workspaceId, id, default)).Kind);
        Assert.Equal(OpenOrdersOutcomeKind.Unavailable, (await Service((_, _, _) => throw new InvalidOperationException())
            .GetAsync(_workspaceId, newId, default)).Kind);
    }

    [Fact]
    public async Task FailureKeepsOnlyCompatibleLastGoodAsStaleAndSuccessfulRetryReplacesIt()
    {
        var id = Load("P-1");
        var attempt = 0;
        var service = Service((_, _, _) => ++attempt == 2
            ? throw new InvalidOperationException("Sensitive source exception")
            : Task.FromResult<IReadOnlyList<OpenOrderLine>>([]));
        var initial = await service.GetAsync(_workspaceId, id, default);
        var stale = await service.RefreshAsync(_workspaceId, id, default);
        Assert.True(stale.Report!.IsStale);
        Assert.NotNull(stale.Report.Warning);
        Assert.Equal(initial.Report!.Snapshot.Id, stale.Report.Snapshot.Id);
        Assert.DoesNotContain("Sensitive", stale.Report.Warning);
        Assert.True((await service.GetAsync(_workspaceId, id, default)).Report!.IsStale);
        var retry = await service.RefreshAsync(_workspaceId, id, default);
        Assert.False(retry.Report!.IsStale);
        Assert.NotEqual(initial.Report.Snapshot.Id, retry.Report.Snapshot.Id);
        Assert.Equal(3, attempt);
    }

    [Fact]
    public async Task CancellationPropagatesWithoutConvertingToUnavailableOrStale()
    {
        var id = Load("P-1");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Service((_, _, _) => throw new Exception("Unexpected read"))
                .GetAsync(_workspaceId, id, cts.Token));
        var active = Service((_, _, ct) => throw new OperationCanceledException(ct));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => active.RefreshAsync(_workspaceId, id, cts.Token));
    }

    [Fact]
    public async Task MpsChangeDuringReadRejectsResultAndNeverCachesDifferentPopulation()
    {
        var id = Load("P-1");
        var reads = 0;
        var service = Service((_, _, _) =>
        {
            reads++;
            Load("P-2");
            return Task.FromResult<IReadOnlyList<OpenOrderLine>>([]);
        });
        var result = await service.GetAsync(_workspaceId, id, default);
        Assert.Equal(OpenOrdersOutcomeKind.MpsSnapshotChanged, result.Kind);
        Assert.Null(_cache.Get(_workspaceId));
        Assert.Equal(1, reads);
    }

    [Fact]
    public async Task FullReturnedPopulationIsNotTruncatedByService()
    {
        var id = Load("P-1");
        var sourceLines = Enumerable.Range(1, 501).Select(i => new OpenOrderLine(
            new("TEST", "SO", i), "P-1", "SW", null, null, 0m,
            new(null, null, null, null, 1m, 1m), null, null, null, null,
            null, null, "", null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, false)).ToArray();
        var service = Service((_, _, _) => Task.FromResult<IReadOnlyList<OpenOrderLine>>(sourceLines));
        var result = await service.GetAsync(_workspaceId, id, default);
        Assert.Equal(501, result.Report!.Snapshot.Lines.Count);
        Assert.Equal(501, result.Report.Snapshot.Lines.Select(x => x.Key.Line).Distinct().Count());
    }

    [Fact]
    public async Task CancellationDuringReadDoesNotReplaceLastGood()
    {
        var id = Load("P-1");
        using var cts = new CancellationTokenSource();
        var calls = 0;
        var service = Service((_, _, ct) =>
        {
            if (++calls == 1) return Task.FromResult<IReadOnlyList<OpenOrderLine>>([]);
            cts.Cancel();
            throw new OperationCanceledException(ct);
        });
        var first = await service.GetAsync(_workspaceId, id, default);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.RefreshAsync(_workspaceId, id, cts.Token));
        var cached = _cache.Get(_workspaceId);
        Assert.Equal(first.Report!.Snapshot.Id, cached!.Snapshot.Id);
        Assert.False(cached.IsStale);
    }
}
