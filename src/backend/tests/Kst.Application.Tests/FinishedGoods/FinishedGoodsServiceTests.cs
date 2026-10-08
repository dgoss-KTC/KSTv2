using Kst.Application.FinishedGoods;
using Kst.Application.Mps;
using Kst.Application.Tests.PartDetail;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.FinishedGoods;
using Kst.Domain.Mps;
using Kst.Domain.Workspaces;
using Kst.Infrastructure.FinishedGoods;
using Kst.Infrastructure.Mps;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Application.Tests.FinishedGoods;

public sealed class FinishedGoodsServiceTests
{
    private readonly Guid _id = Guid.NewGuid();
    private readonly InMemoryMpsSnapshotStore _mps = new();
    private readonly InMemoryFinishedGoodsSnapshotStore _store = new();
    private readonly FakeClock _clock = new();
    private readonly MutableWorkspaces _workspaces = new();

    private FinishedGoodsService Service(Func<string, IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<FinishedGoodsSourceRow>>> read)
    {
        _workspaces.Current ??= new(_id, "Test", "SW", null, null, ["CONFIGURED-ONLY"], false, null, true, 0);
        return new(_workspaces, _mps, new DelegateFinishedGoodsSourceReader(read), _store, _clock, NullLogger<FinishedGoodsService>.Instance);
    }

    private SnapshotId Load(params string[] parts)
    {
        var snapshot = new MpsSnapshot(SnapshotId.New(), _clock.UtcNow, "SW", parts.Select(p => new MpsResolvedPart(p, null)).ToArray(), []);
        _mps.SetLoaded(_id, snapshot); return snapshot.Id;
    }

    [Fact]
    public async Task UsesResolvedScopeAndKeepsEverySourceRowWithUniqueSnapshotLocalId()
    {
        var expected = Load("P", "EXPLICIT-NO-MRP"); var calls = 0;
        var service = Service((site, parts, _) => {
            calls++; Assert.Equal("SW", site); Assert.Equal(["P", "EXPLICIT-NO-MRP"], parts);
            return Task.FromResult<IReadOnlyList<FinishedGoodsSourceRow>>([Row(), Row(), Row() with { Reference = "R2" }]); });
        var report = (await service.GetAsync(_id, expected, default)).Report!;
        Assert.Equal(3, report.Snapshot.Rows.Count);
        Assert.Equal(3, report.Snapshot.Rows.Select(r => r.RowId).Distinct().Count());
        Assert.Equal(report.Snapshot.Id, (await service.GetAsync(_id, expected, default)).Report!.Snapshot.Id);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task EmptyScopeSucceedsWithoutReaderAndInitialFailureIsNotEmpty()
    {
        var service = Service((_, _, _) => throw new InvalidOperationException("source secret"));
        Assert.Equal(FinishedGoodsOutcomeKind.MpsNotLoaded, (await service.GetAsync(_id, SnapshotId.New(), default)).Kind);
        Assert.Equal(FinishedGoodsOutcomeKind.UnknownWorkspace, (await service.GetAsync(Guid.NewGuid(), SnapshotId.New(), default)).Kind);
        Assert.Empty((await service.GetAsync(_id, Load(), default)).Report!.Snapshot.Rows);
        Assert.Equal(FinishedGoodsOutcomeKind.Unavailable, (await service.GetAsync(_id, Load("P"), default)).Kind);
    }

    [Fact]
    public async Task FailureKeepsOriginalTimeAndIdOnlyUntilSuccessfulRetry()
    {
        var expected = Load("P"); var calls = 0;
        var service = Service((_, _, _) => ++calls == 2 ? throw new Exception("private source error") : Task.FromResult<IReadOnlyList<FinishedGoodsSourceRow>>([Row()]));
        var first = (await service.GetAsync(_id, expected, default)).Report!;
        _clock.UtcNow = _clock.UtcNow.AddHours(1);
        var stale = (await service.RefreshAsync(_id, expected, default)).Report!;
        Assert.True(stale.IsStale); Assert.Equal(first.Snapshot.Id, stale.Snapshot.Id); Assert.Equal(first.Snapshot.AcquiredAtUtc, stale.Snapshot.AcquiredAtUtc);
        Assert.DoesNotContain("private", stale.Warning);
        var retry = (await service.RefreshAsync(_id, expected, default)).Report!;
        Assert.False(retry.IsStale); Assert.NotEqual(first.Snapshot.Id, retry.Snapshot.Id); Assert.Equal(_clock.UtcNow, retry.Snapshot.AcquiredAtUtc);
    }

    [Theory]
    [InlineData("snapshot", false)]
    [InlineData("snapshot", true)]
    [InlineData("site", false)]
    [InlineData("site", true)]
    [InlineData("delete", false)]
    [InlineData("delete", true)]
    public async Task ScopeChangesBlockPublicationAndStaleFallback(string change, bool failure)
    {
        var expected = Load("P"); var calls = 0;
        var service = Service((_, _, _) => {
            if (++calls == 1) return Task.FromResult<IReadOnlyList<FinishedGoodsSourceRow>>([Row()]);
            if (change == "snapshot") Load("OTHER");
            if (change == "site") _workspaces.Current = _workspaces.Current! with { Site = "KV" };
            if (change == "delete") _workspaces.Current = null;
            if (failure) throw new Exception("source failure");
            return Task.FromResult<IReadOnlyList<FinishedGoodsSourceRow>>([Row()]); });
        var first = (await service.GetAsync(_id, expected, default)).Report!;
        var result = await service.RefreshAsync(_id, expected, default);
        Assert.Equal(change == "delete" ? FinishedGoodsOutcomeKind.UnknownWorkspace : FinishedGoodsOutcomeKind.MpsSnapshotChanged, result.Kind);
        Assert.Equal(first.Snapshot.Id, _store.Get(_id)!.Snapshot.Id); Assert.False(_store.Get(_id)!.IsStale);
    }

    [Fact]
    public async Task CancellationAndMalformedPartialResultsNeverReplaceLastGood()
    {
        var expected = Load("P"); var calls = 0; using var cts = new CancellationTokenSource();
        var service = Service((_, _, ct) => {
            if (++calls == 1) return Task.FromResult<IReadOnlyList<FinishedGoodsSourceRow>>([Row()]);
            cts.Cancel(); return Task.FromResult<IReadOnlyList<FinishedGoodsSourceRow>>([Row()]); });
        var first = (await service.GetAsync(_id, expected, default)).Report!;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.RefreshAsync(_id, expected, cts.Token));
        Assert.Equal(first, _store.Get(_id));
        var malformed = Service((_, _, _) => Task.FromResult<IReadOnlyList<FinishedGoodsSourceRow>>([Row(), Row() with { Product = "OTHER" }]));
        var stale = (await malformed.RefreshAsync(_id, expected, default)).Report!;
        Assert.True(stale.IsStale); Assert.Single(stale.Snapshot.Rows);
    }

    [Fact]
    public async Task QueuedRefreshRechecksScopeBeforeAnySecondAcquisition()
    {
        var expected = Load("P");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<IReadOnlyList<FinishedGoodsSourceRow>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var service = Service((_, _, _) => { calls++; entered.SetResult(); return finish.Task; });
        var first = service.GetAsync(_id, expected, default);
        await entered.Task;
        var queued = service.RefreshAsync(_id, expected, default);
        Load("OTHER");
        finish.SetResult([Row()]);
        Assert.Equal(FinishedGoodsOutcomeKind.MpsSnapshotChanged, (await first).Kind);
        Assert.Equal(FinishedGoodsOutcomeKind.MpsSnapshotChanged, (await queued).Kind);
        Assert.Equal(1, calls);
        Assert.Null(_store.Get(_id));
    }

    [Fact]
    public async Task UnrepresentableValueCannotPublishPartialCurrentRows()
    {
        var expected = Load("P");
        var service = Service((_, _, _) => Task.FromResult<IReadOnlyList<FinishedGoodsSourceRow>>([
            Row(), Row() with { OnHand = decimal.MaxValue, UnitPrice = 2m }]));
        Assert.Equal(FinishedGoodsOutcomeKind.Unavailable, (await service.GetAsync(_id, expected, default)).Kind);
        Assert.Null(_store.Get(_id));
    }

    [Fact]
    public async Task FailureLoggingContainsStaticReasonAndTraceButNeverExceptionPayload()
    {
        var expected = Load("P");
        _workspaces.Current = new(_id, "Test", "SW", null, null, ["P"], false, null, true, 0);
        var logger = new CaptureLogger();
        var error = FinishedGoodsFailure.Create(FinishedGoodsFailureReason.InventoryScopeInvalid);
        error.Data["private"] = "secret source values";
        var service = new FinishedGoodsService(_workspaces, _mps,
            new DelegateFinishedGoodsSourceReader((_, _, _) => throw error), _store, _clock, logger);
        using var activity = new System.Diagnostics.Activity("test").SetIdFormat(System.Diagnostics.ActivityIdFormat.W3C).Start();
        Assert.Equal(FinishedGoodsOutcomeKind.Unavailable, (await service.GetAsync(_id, expected, default)).Kind);
        Assert.Contains("InventoryScopeInvalid", logger.Message);
        Assert.Contains(activity.TraceId.ToString(), logger.Message);
        Assert.DoesNotContain("secret", logger.Message);
        Assert.Null(logger.Exception);
    }

    private sealed class CaptureLogger : Microsoft.Extensions.Logging.ILogger<FinishedGoodsService>
    {
        public string Message { get; private set; } = "";
        public Exception? Exception { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel level) => true;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel level, Microsoft.Extensions.Logging.EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        { Message = formatter(state, exception); Exception = exception; }
    }

    private static FinishedGoodsSourceRow Row() => new("KTC", "SW", "P", "ship", "RA1", "R1", 3.00001m, 1.23456m, false);

    private sealed class MutableWorkspaces : IWorkspaceConfigurationService
    {
        public WorkspaceAssignment? Current { get; set; }
        public Task<WorkspaceListResult> GetWorkspacesAsync() => Task.FromResult(new WorkspaceListResult(Current is null ? [] : [Current], null));
        public Task<WorkspaceCreateResult> CreateWorkspaceAsync(CreateWorkspaceCommand command) => throw new NotImplementedException();
        public Task<WorkspaceUpdateResult> UpdateWorkspaceAsync(Guid id, CreateWorkspaceCommand command) => throw new NotImplementedException();
        public Task<WorkspaceOperationResult> ArchiveWorkspaceAsync(Guid id) => throw new NotImplementedException();
        public Task<WorkspaceOperationResult> RestoreWorkspaceAsync(Guid id) => throw new NotImplementedException();
        public Task<WorkspaceOperationResult> DeleteWorkspaceAsync(Guid id) => throw new NotImplementedException();
        public Task ResetWorkspacesAsync() => throw new NotImplementedException();
        public Task<WorkspaceReorderResult> ReorderWorkspacesAsync(ReorderWorkspacesCommand command) => throw new NotImplementedException();
    }
}
