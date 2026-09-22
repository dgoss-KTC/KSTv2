using Kst.Application.Bom;
using Kst.Application.LongTermShortages;
using Kst.Application.Mps;
using Kst.Application.Tests.Mps;
using Kst.Domain.Bom;
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
    public async Task GetAsync_CachesCompatibleProjectionAndReturnsStaleLastGood()
    {
        var calls = 0; var fixture = Build((_, _, _, _) => ++calls == 1 ? Task.FromResult<IReadOnlyList<LongTermShortageInput>>([Input()]) : throw new InvalidOperationException());
        var first = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        var hit = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        fixture.Clock.LocalNow = fixture.Clock.LocalNow.AddDays(1);
        var stale = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        Assert.Equal(2, calls); Assert.False(first.IsStale); Assert.False(hit.IsStale); Assert.True(stale.IsStale); Assert.Equal(first.RefreshDate, stale.RefreshDate);
    }
    [Fact]
    public async Task GetCachedForExportAsync_DoesNotReadAgainAndRejectsWrongPart()
    {
        var calls = 0; var fixture = Build((_, _, _, _) => { calls++; return Task.FromResult<IReadOnlyList<LongTermShortageInput>>([Input()]); });
        await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        var exported = await fixture.Service.GetCachedForExportAsync(fixture.WorkspaceId, fixture.SnapshotId, ["COMP"], LongTermShortagePopulationOptions.Default);
        var rejected = await fixture.Service.GetCachedForExportAsync(fixture.WorkspaceId, fixture.SnapshotId, ["OTHER"], LongTermShortagePopulationOptions.Default);
        Assert.Equal(1, calls); Assert.Equal(LongTermShortagesOutcomeKind.Loaded, exported.Kind); Assert.Equal(LongTermShortagesOutcomeKind.Unavailable, rejected.Kind);
    }
    private static LongTermShortageInput Input() => new("COMP", "EA", null, null, null, null, 10m, SafetyStockState.Resolved, 0m, ["PARENT"], []);
    private static Fixture Build(Func<string, IReadOnlyDictionary<string, IReadOnlyList<string>>, DateOnly, CancellationToken, Task<IReadOnlyList<LongTermShortageInput>>> source)
    {
        var workspaceId = Guid.NewGuid(); var snapshotId = SnapshotId.New(); var store = new InMemoryMpsSnapshotStore(); store.SetLoaded(workspaceId, new MpsSnapshot(snapshotId, DateTimeOffset.UtcNow, "SW", [new MpsResolvedPart("PARENT", "")], [])); var clock = new TestClock { LocalNow = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero) };
        return new(new LongTermShortagesService(new FakeWorkspaceConfigurationService(new WorkspaceAssignment(workspaceId, "Test", "SW", null, null, [], false, null, true, 0)), store, new DelegateBomSourceReader((_, _, _, _) => Task.FromResult<IReadOnlyList<BomOccurrence>>([new("1", 1, "COMP", "P", false, null, 1m, null)])), new DelegateLongTermShortageSourceReader(source), new InMemoryLongTermShortagesCacheStore(), clock, NullLogger<LongTermShortagesService>.Instance), clock, workspaceId, snapshotId);
    }
    private sealed record Fixture(LongTermShortagesService Service, TestClock Clock, Guid WorkspaceId, SnapshotId SnapshotId);
    private sealed class TestClock : IClock { public DateTimeOffset UtcNow => LocalNow.ToUniversalTime(); public DateTimeOffset LocalNow { get; set; } }
}
