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
    public async Task GetAsync_FailureAfterSuccessfulRefresh_ReturnsCompatibleStaleResult()
    {
        var sourceCalls = 0;
        var fixture = Build(ClassifiedOccurrences(), (_, _, _, _, _, _) =>
        {
            sourceCalls++;
            return sourceCalls == 1 ? Task.FromResult<IReadOnlyList<LongTermShortageInput>>([Input("COMP")]) : throw new InvalidOperationException("QAD unavailable");
        });

        var first = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        fixture.Clock.LocalNow = fixture.Clock.LocalNow.AddDays(1);
        var stale = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);

        Assert.False(first.IsStale);
        Assert.True(stale.IsStale);
        Assert.Equal(first.RefreshDate, stale.RefreshDate);
        Assert.Single(stale.Rows!);
    }

    [Fact]
    public async Task GetCachedForExportAsync_RejectsSupersededSnapshot()
    {
        var fixture = Build(ClassifiedOccurrences(), (_, _, _, _, _, _) => Task.FromResult<IReadOnlyList<LongTermShortageInput>>([Input("COMP")]));
        await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        fixture.Store.SetLoaded(fixture.WorkspaceId, new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, "SW", [new MpsResolvedPart("PARENT", "")], []));

        var result = await fixture.Service.GetCachedForExportAsync(fixture.WorkspaceId, fixture.SnapshotId, ["COMP"], LongTermShortagePopulationOptions.Default);

        Assert.Equal(LongTermShortagesOutcomeKind.SnapshotChanged, result.Kind);
    }

    [Fact]
    public async Task GetCachedForExportAsync_RejectsComponentOutsideCachedProjection()
    {
        var fixture = Build(ClassifiedOccurrences(), (_, _, _, _, _, _) => Task.FromResult<IReadOnlyList<LongTermShortageInput>>([Input("COMP")]));
        await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);

        var result = await fixture.Service.GetCachedForExportAsync(fixture.WorkspaceId, fixture.SnapshotId, ["NOT-CACHED"], LongTermShortagePopulationOptions.Default);

        Assert.Equal(LongTermShortagesOutcomeKind.Unavailable, result.Kind);
    }

    [Theory]
    [MemberData(nameof(PopulationOptionCases))]
    public async Task GetAsync_AppliesPopulationOptionsAfterBomSelection_BeforeSourceRead(bool includeManufacturedParts, bool includePhantoms, string[] expectedRequestedParts)
    {
        var requested = new List<string>();
        var fixture = Build(ClassifiedOccurrences(), (site, componentParents, _, _, _, _) =>
        {
            requested.AddRange(componentParents.Keys);
            return Task.FromResult<IReadOnlyList<LongTermShortageInput>>(componentParents.Keys.Select(Input).ToList());
        });

        var result = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, new LongTermShortagePopulationOptions(includeManufacturedParts, includePhantoms));

        Assert.Equal(LongTermShortagesOutcomeKind.Loaded, result.Kind);
        Assert.Equal(expectedRequestedParts.OrderBy(value => value), requested.OrderBy(value => value));
        Assert.Equal(expectedRequestedParts.OrderBy(value => value), result.Rows!.Select(row => row.ComponentPart).OrderBy(value => value));
    }

    [Fact]
    public async Task GetAsync_DistinctOptionValues_LoadDistinctResults_AndNeverReuseAnotherOptionsProjection()
    {
        var sourceCalls = 0;
        var fixture = Build(ClassifiedOccurrences(), (site, componentParents, _, _, _, _) =>
        {
            sourceCalls++;
            return Task.FromResult<IReadOnlyList<LongTermShortageInput>>(componentParents.Keys.Select(Input).ToList());
        });

        var defaultResult = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        var manufacturedResult = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, new LongTermShortagePopulationOptions(IncludeManufacturedParts: true));
        var cachedDefaultAgain = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);

        Assert.Equal(2, sourceCalls); // second option set is a cache miss; the third call reuses the default projection
        Assert.DoesNotContain(defaultResult.Rows!, row => row.ComponentPart == "MFG");
        Assert.Contains(manufacturedResult.Rows!, row => row.ComponentPart == "MFG");
        Assert.Equal(defaultResult.Rows!.Count, cachedDefaultAgain.Rows!.Count);
    }

    [Fact]
    public async Task GetAsync_StaleFallback_IsScopedToTheRequestedOptionValues()
    {
        var sourceCalls = 0;
        var fixture = Build(ClassifiedOccurrences(), (site, componentParents, _, _, _, _) =>
        {
            sourceCalls++;
            if (sourceCalls > 2) throw new InvalidOperationException("QAD unavailable");
            return Task.FromResult<IReadOnlyList<LongTermShortageInput>>(componentParents.Keys.Select(Input).ToList());
        });

        await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, new LongTermShortagePopulationOptions(IncludeManufacturedParts: true));
        fixture.Clock.LocalNow = fixture.Clock.LocalNow.AddDays(1);

        // Both refreshes now fail; each option set must fall back to its own prior projection only.
        var staleDefault = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);
        var staleManufactured = await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, new LongTermShortagePopulationOptions(IncludeManufacturedParts: true));

        Assert.True(staleDefault.IsStale);
        Assert.DoesNotContain(staleDefault.Rows!, row => row.ComponentPart == "MFG");
        Assert.True(staleManufactured.IsStale);
        Assert.Contains(staleManufactured.Rows!, row => row.ComponentPart == "MFG");
    }

    [Fact]
    public async Task GetCachedForExportAsync_OnlyServesTheProjectionProducedUnderTheSameOptionValues()
    {
        var fixture = Build(ClassifiedOccurrences(), (site, componentParents, _, _, _, _) =>
            Task.FromResult<IReadOnlyList<LongTermShortageInput>>(componentParents.Keys.Select(Input).ToList()));

        await fixture.Service.GetAsync(fixture.WorkspaceId, fixture.SnapshotId, LongTermShortagePopulationOptions.Default);

        // The default projection contains only NORMAL; exporting under the manufactured option set must not reuse it.
        var mismatched = await fixture.Service.GetCachedForExportAsync(fixture.WorkspaceId, fixture.SnapshotId, ["NORMAL"], new LongTermShortagePopulationOptions(IncludeManufacturedParts: true));
        Assert.Equal(LongTermShortagesOutcomeKind.Unavailable, mismatched.Kind);

        // A part that only exists under another option set cannot be exported from the default projection.
        var outside = await fixture.Service.GetCachedForExportAsync(fixture.WorkspaceId, fixture.SnapshotId, ["MFG"], LongTermShortagePopulationOptions.Default);
        Assert.Equal(LongTermShortagesOutcomeKind.Unavailable, outside.Kind);

        // The matching option set serves exactly its displayed rows.
        var matched = await fixture.Service.GetCachedForExportAsync(fixture.WorkspaceId, fixture.SnapshotId, ["NORMAL"], LongTermShortagePopulationOptions.Default);
        Assert.Equal(LongTermShortagesOutcomeKind.Loaded, matched.Kind);
        Assert.Single(matched.Rows!);
    }

    public static TheoryData<bool, bool, string[]> PopulationOptionCases() => new()
    {
        { false, false, ["NORMAL"] },
        { true, false, ["NORMAL", "MFG", "MFG-TRIMMED"] },
        { false, true, ["NORMAL", "PHANTOM"] },
        { true, true, ["NORMAL", "MFG", "MFG-TRIMMED", "PHANTOM", "BOTH"] },
    };

    /// <summary>One occurrence per classification: normal, manufactured (incl. a trimmed/lowercase 'm' code), phantom, and both.</summary>
    private static IReadOnlyList<BomOccurrence> ClassifiedOccurrences() =>
    [
        new("1", 1, "NORMAL", "P", false, null, 1m, null, null, "P"),
        new("2", 1, "MFG", "M", false, null, 1m, null, null, "M"),
        new("3", 1, "MFG-TRIMMED", "M", false, null, 1m, null, null, " m "),
        new("4", 1, "PHANTOM", "P", true, null, 1m, null, null, "P"),
        new("5", 1, "BOTH", "M", true, null, 1m, null, null, "M"),
    ];

    private static LongTermShortageInput Input(string componentPart) => new(componentPart, null, null, null, false, null, null, null, 10m, SafetyStockState.Resolved, 0m, ["PARENT"], [], [], []);

    private static Fixture Build(IReadOnlyList<BomOccurrence> occurrences, Func<string, IReadOnlyDictionary<string, IReadOnlyList<string>>, DateOnly, DateOnly, IReadOnlySet<string>, CancellationToken, Task<IReadOnlyList<LongTermShortageInput>>> source)
    {
        var workspaceId = Guid.NewGuid(); var snapshotId = SnapshotId.New(); var store = new InMemoryMpsSnapshotStore();
        store.SetLoaded(workspaceId, new MpsSnapshot(snapshotId, DateTimeOffset.UtcNow, "SW", [new MpsResolvedPart("PARENT", "")], []));
        var clock = new TestClock { LocalNow = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero) };
        var service = new LongTermShortagesService(new FakeWorkspaceConfigurationService(new WorkspaceAssignment(workspaceId, "Test Workspace", "SW", null, null, [], false, null, true, 0)), store,
            new DelegateBomSourceReader((_, _, _, _) => Task.FromResult(occurrences)),
            new DelegateLongTermShortageSourceReader(source), new InMemoryLongTermShortagesCacheStore(), clock, NullLogger<LongTermShortagesService>.Instance);
        return new(service, store, clock, workspaceId, snapshotId);
    }

    private sealed record Fixture(LongTermShortagesService Service, InMemoryMpsSnapshotStore Store, TestClock Clock, Guid WorkspaceId, SnapshotId SnapshotId);
    private sealed class TestClock : IClock { public DateTimeOffset UtcNow => LocalNow.ToUniversalTime(); public DateTimeOffset LocalNow { get; set; } }
}
