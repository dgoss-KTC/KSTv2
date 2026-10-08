using Kst.Application.InventoryLocations;
using Kst.Application.Tests.Mps;
using Kst.Application.Tests.PartDetail;
using Kst.Domain.Common;
using Kst.Domain.Inventory;
using Kst.Domain.Workspaces;
using Kst.Infrastructure.InventoryLocations;
using Kst.Infrastructure.Mps;
using Kst.Application.Mps;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Application.Tests.InventoryLocations;

public sealed class InventoryLocationsServiceTests
{
    private readonly Guid _id = Guid.NewGuid();
    private readonly InMemoryMpsSnapshotStore _snapshots = new();
    private readonly InMemoryInventoryLocationsStore _store = new();
    private readonly FakeClock _clock = new();
    private MpsSnapshot Seed(string site = "SW")
    {
        var snapshot = new MpsSnapshot(SnapshotId.New(), _clock.UtcNow, site, [], []);
        _snapshots.SetLoaded(_id, snapshot);
        return snapshot;
    }
    private InventoryLocationsService Service(Func<string, string, CancellationToken, Task<IReadOnlyList<InventoryLocationRow>>> read) =>
        new(new FakeWorkspaceConfigurationService(new WorkspaceAssignment(_id, "Test", "SW", null, null, ["PARENT"], false, null, true, 0)),
            _snapshots, new DelegateInventoryLocationReader(read), _store, _clock, NullLogger<InventoryLocationsService>.Instance);

    [Fact]
    public async Task FreshReadsOnEveryRequestAndFailureRetainsCompatibleLastGoodTime()
    {
        var snapshot = Seed();
        var calls = 0;
        var service = Service((site, part, _) =>
        {
            Assert.Equal("SW", site); Assert.Equal("COMP", part, ignoreCase: true);
            if (++calls > 1) throw new InvalidOperationException("source failed");
            return Task.FromResult<IReadOnlyList<InventoryLocationRow>>([new("0", "MRB", "RA", "Hold", "-1.25")]);
        });
        var loaded = await service.GetAsync(_id, " COMP ", snapshot.Id.ToString(), default);
        _clock.UtcNow = _clock.UtcNow.AddHours(1);
        var stale = await service.GetAsync(_id, "comp", snapshot.Id.ToString(), default);
        Assert.Equal(2, calls);
        Assert.True(stale.Report!.IsStale);
        Assert.Equal(loaded.Report!.AcquiredAtUtc, stale.Report.AcquiredAtUtc);
        Assert.NotNull(stale.Report.Warning);
        Assert.Same(loaded.Report, _store.Get(_id, "COMP"));
    }

    [Fact]
    public async Task EmptyIsSuccessButFailureWithoutCompatibleDataIsUnavailable()
    {
        var snapshot = Seed();
        var loaded = await Service((_, _, _) => Task.FromResult<IReadOnlyList<InventoryLocationRow>>([])).GetAsync(_id, "COMP", snapshot.Id.ToString(), default);
        Assert.Empty(loaded.Report!.Rows);
        var next = Seed();
        var failed = await Service((_, _, _) => throw new Exception()).GetAsync(_id, "COMP", next.Id.ToString(), default);
        Assert.Equal(InventoryLocationsOutcome.Unavailable, failed.Outcome);
        Assert.Null(failed.Report);
    }

    [Fact]
    public async Task DifferentSiteCacheCannotBeReturned()
    {
        var snapshot = Seed();
        _store.Set(new(_id, "KV", "COMP", snapshot.Id.ToString(), _clock.UtcNow, false, null, []));
        var result = await Service((_, _, _) => throw new Exception()).GetAsync(_id, "COMP", snapshot.Id.ToString(), default);
        Assert.Equal(InventoryLocationsOutcome.Unavailable, result.Outcome);
    }

    [Fact]
    public async Task ChangedSnapshotDuringAcquisitionCannotPublishOrReturnStale()
    {
        var snapshot = Seed();
        var service = Service((_, _, _) => { Seed(); return Task.FromResult<IReadOnlyList<InventoryLocationRow>>([]); });
        var result = await service.GetAsync(_id, "COMP", snapshot.Id.ToString(), default);
        Assert.Equal(InventoryLocationsOutcome.SnapshotChanged, result.Outcome);
        Assert.Null(_store.Get(_id, "COMP"));
    }

    [Fact]
    public async Task CancellationNeverReturnsOrOverwritesLastGood()
    {
        var snapshot = Seed();
        var cached = new InventoryLocationsReport(_id, "SW", "COMP", snapshot.Id.ToString(), _clock.UtcNow, false, null, []);
        _store.Set(cached);
        await Assert.ThrowsAsync<OperationCanceledException>(() => Service((_, _, _) => throw new OperationCanceledException())
            .GetAsync(_id, "COMP", snapshot.Id.ToString(), default));
        Assert.Same(cached, _store.Get(_id, "COMP"));
    }

    [Fact]
    public async Task ConcurrentRequestsSerializeAndCancelledWaitDoesNotAcquire()
    {
        var snapshot = Seed();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var service = Service(async (_, _, ct) => { calls++; entered.TrySetResult(); await release.Task.WaitAsync(ct); return []; });
        var first = service.GetAsync(_id, "COMP", snapshot.Id.ToString(), default);
        await entered.Task;
        using var cancellation = new CancellationTokenSource();
        var waiting = service.GetAsync(_id, "comp", snapshot.Id.ToString(), cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        Assert.Equal(1, calls);
        release.SetResult();
        Assert.Equal(InventoryLocationsOutcome.Loaded, (await first).Outcome);
    }
}
