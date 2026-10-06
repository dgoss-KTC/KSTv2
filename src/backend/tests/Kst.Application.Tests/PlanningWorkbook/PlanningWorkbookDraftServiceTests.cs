using System.Text.Json;
using Kst.Application.Mps;
using Kst.Application.PlanningWorkbook;
using Kst.Application.Tests.PartDetail;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.Tests.PlanningWorkbook;
using Kst.Domain.Workspaces;
using Kst.Infrastructure.Configuration;
using Kst.Infrastructure.Mps;
using Kst.Infrastructure.PlanningWorkbook;

namespace Kst.Application.Tests.PlanningWorkbook;

/// <summary>
/// Stage 14.5 saved-draft service tests with the real JSON store in a temporary directory. Expected
/// restoration values (D01–D06) are read from the accepted Stage 14 fixture; no QAD access exists.
/// </summary>
public sealed class PlanningWorkbookDraftServiceTests : IDisposable
{
    private static readonly Stage14Fixture Fixture = Stage14Fixture.Load();
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"kst-planning-draft-test-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private static JsonElement Case(string id) =>
        Fixture.Root.GetProperty("draftRestoreCases").GetProperty("cases").EnumerateArray()
            .Single(c => c.GetProperty("id").GetString() == id);

    [Fact]
    public async Task Restoration_Reproduces_Fixture_D01_To_D05_Through_The_Service_And_Json_Store()
    {
        var h = new Harness(_root);
        var ids = new[] { "D01", "D02", "D03", "D04", "D05" };
        var entries = ids.Select(id =>
        {
            var saved = Case(id).GetProperty("saved");
            return new PlanningWorkbookDraftEntry(saved.GetProperty("part").GetString()!, Stage14Fixture.Date(saved.GetProperty("weekLabel")),
                saved.GetProperty("adjustment").GetString()!, Stage14Fixture.Dec(saved, "originalMpsQty"));
        }).ToList();
        await h.Store.SaveAsync(new PlanningWorkbookDraft(h.Id, h.Site, Guid.NewGuid().ToString(), h.Clock.UtcNow, entries), default);

        var result = await h.Service.LoadAsync(h.Id, h.MpsId, default);

        Assert.Equal(PlanningDraftOutcomeKind.Loaded, result.Kind);
        Assert.Null(result.Warning);
        Assert.Equal(5, result.Rows!.Count);
        for (var i = 0; i < ids.Length; i++)
        {
            var expected = Case(ids[i]);
            var row = result.Rows[i];
            Assert.Equal(expected.GetProperty("saved").GetProperty("adjustment").GetString(), row.Saved.Adjustment); // signed adjustment retained
            Assert.Equal(expected.GetProperty("exportEligible").GetBoolean(), row.IssueCode is null);
            if (expected.TryGetProperty("issueCode", out var code)) Assert.Equal(code.GetString(), row.IssueCode);
            if (expected.TryGetProperty("currentMps", out _))
            {
                Assert.Equal(Stage14Fixture.Dec(expected, "currentMps"), row.CurrentMps);
                Assert.Equal(expected.GetProperty("baselineChanged").GetBoolean(), row.BaselineChanged);
                Assert.Equal(Stage14Fixture.Dec(expected, "proposedMps"), row.ProposedMps);
            }
            else
            {
                // D04/D05: unavailable baseline, distinct from zero; retained and flagged.
                Assert.Null(row.CurrentMps);
                Assert.Null(row.ProposedMps);
                Assert.False(row.BaselineChanged);
            }
        }
    }

    [Fact]
    public async Task D06_Signed_Adjustment_Is_Reapplied_To_A_Replaced_Mps_Snapshot_Without_Conversion()
    {
        var d06 = Case("D06");
        var before = d06.GetProperty("before");
        var after = d06.GetProperty("afterMpsRefresh");
        var part = before.GetProperty("part").GetString()!;
        var week = Stage14Fixture.Date(before.GetProperty("weekLabel"));
        var h = new Harness(_root);

        var saved = await h.Service.SaveAsync(h.Id, h.MpsId, [new(part, week, before.GetProperty("adjustment").GetString())], default);
        Assert.Equal(PlanningDraftOutcomeKind.Loaded, saved.Kind);
        Assert.Equal(Stage14Fixture.Dec(before, "mps"), saved.Draft!.Entries.Single().SavedMpsQuantity);
        Assert.Equal(Stage14Fixture.Dec(before, "proposedMps"), saved.Rows!.Single().ProposedMps);

        // MPS refresh: a new snapshot whose baseline for the week is the fixture's refreshed value.
        var extra = Stage14Fixture.Dec(after, "mps") - Stage14Fixture.Dec(before, "mps");
        var template = Fixture.MpsRows.First(r => r.ParentPart == part);
        var newMps = SnapshotId.New();
        h.LoadMps(newMps, [template with { DueDate = week.AddDays(1), ReleaseDate = week.AddDays(1), Quantity = extra, WorkOrderId = "D06-REFRESH" }]);

        var restored = await h.Service.LoadAsync(h.Id, newMps, default);
        var row = restored.Rows!.Single();
        Assert.Equal(Stage14Fixture.Dec(after, "mps"), row.CurrentMps);
        Assert.True(row.BaselineChanged);
        Assert.Equal(after.GetProperty("adjustment").GetString(), row.Saved.Adjustment);
        Assert.Equal(Stage14Fixture.Dec(after, "proposedMps"), row.ProposedMps);
        Assert.Null(row.IssueCode);
        Assert.False(d06.GetProperty("acknowledgmentRequired").GetBoolean());
    }

    [Fact]
    public async Task Save_Drops_No_Ops_Keeps_Flagged_Entries_With_Unavailable_Baselines_And_Rejects_Invalid_Input()
    {
        var h = new Harness(_root);
        var w0921 = new DateOnly(2026, 9, 21);
        var w0928 = new DateOnly(2026, 9, 28);
        var result = await h.Service.SaveAsync(h.Id, h.MpsId,
        [
            new("P-1", w0921, "-75"),              // V04: fractional source 75.5 -> 0.5
            new("P-1", w0928, "-150"),             // V01: negative result retained and flagged
            new("P-1", new DateOnly(2026, 9, 14), "0"),
            new("P-2", w0921, "-0"),
            new("P-3", w0921, ""),
            new("P-1", new DateOnly(2026, 9, 7), "5"),   // week passed: baseline unavailable
            new("X-9", w0921, "5")                        // out of scope: baseline unavailable
        ], default);

        Assert.Equal(PlanningDraftOutcomeKind.Loaded, result.Kind);
        var entries = result.Draft!.Entries;
        Assert.Equal(4, entries.Count);
        Assert.Equal(75.5m, entries[0].SavedMpsQuantity);
        Assert.Equal(100m, entries[1].SavedMpsQuantity);
        Assert.Null(entries[2].SavedMpsQuantity);
        Assert.Null(entries[3].SavedMpsQuantity);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<PlanningDraftEntryComparison>>(result.Rows);
        Assert.Equal([null, "negative-result", "week-passed", "part-out-of-scope"], rows.Select(r => r.IssueCode));
        Assert.Equal(0.5m, rows[0].ProposedMps);
        Assert.Equal(-50m, rows[1].ProposedMps);

        var json = await File.ReadAllTextAsync(h.Store.FilePath(h.Id));
        Assert.DoesNotContain("\"0\"", json.Replace(" ", ""));

        foreach (var (text, code) in new[] { ("+5", "invalid-number"), ("10.5", "not-whole-number"), ("10.0", "not-whole-number"), ("1e3", "invalid-number"), ("1,000", "invalid-number"), (" 5", "invalid-number") })
        {
            var invalid = await h.Service.SaveAsync(h.Id, h.MpsId, [new("P-1", w0921, text)], default);
            Assert.Equal(PlanningDraftOutcomeKind.Invalid, invalid.Kind);
            Assert.Equal(code, invalid.InvalidReason);
        }
        var duplicate = await h.Service.SaveAsync(h.Id, h.MpsId, [new("P-1", w0921, "5"), new("p-1", w0921, "6")], default);
        Assert.Equal("duplicate-identity", duplicate.InvalidReason);
        var notWeek = await h.Service.SaveAsync(h.Id, h.MpsId, [new("P-1", new DateOnly(2026, 9, 22), "6")], default);
        Assert.Equal("not-a-week", notWeek.InvalidReason);
        Assert.Equal(json, await File.ReadAllTextAsync(h.Store.FilePath(h.Id))); // rejected saves never touch the file
    }

    [Fact]
    public async Task Load_And_Save_Require_The_Currently_Loaded_Mps_Snapshot_And_A_Known_Workspace()
    {
        var h = new Harness(_root, loadMps: false);
        var week = new DateOnly(2026, 9, 21);
        Assert.Equal(PlanningDraftOutcomeKind.MpsNotLoaded, (await h.Service.SaveAsync(h.Id, h.MpsId, [new("P-1", week, "5")], default)).Kind);
        h.LoadMps(h.MpsId);
        Assert.Equal(PlanningDraftOutcomeKind.MpsSnapshotChanged, (await h.Service.SaveAsync(h.Id, SnapshotId.New(), [new("P-1", week, "5")], default)).Kind);
        Assert.Equal(PlanningDraftOutcomeKind.UnknownWorkspace, (await h.Service.SaveAsync(Guid.NewGuid(), h.MpsId, [new("P-1", week, "5")], default)).Kind);
        Assert.False(await h.Store.ExistsAsync(h.Id, default));

        Assert.Equal(PlanningDraftOutcomeKind.NoDraft, (await h.Service.LoadAsync(h.Id, h.MpsId, default)).Kind);
        await h.Service.SaveAsync(h.Id, h.MpsId, [new("P-1", week, "5")], default);
        Assert.Equal(PlanningDraftOutcomeKind.MpsSnapshotChanged, (await h.Service.LoadAsync(h.Id, SnapshotId.New(), default)).Kind);
        Assert.Equal(PlanningDraftOutcomeKind.UnknownWorkspace, (await h.Service.LoadAsync(Guid.NewGuid(), h.MpsId, default)).Kind);
        Assert.False(await h.Service.ExistsAsync(Guid.NewGuid(), default));

        // Site change: the draft is still returned (retained) with an explanatory warning.
        h.Workspaces.Items[0] = h.Workspaces.Items[0] with { Site = "S2" };
        h.Mps.SetLoaded(h.Id, h.Mps.GetState(h.Id).Snapshot! with { Site = "S2" });
        var moved = await h.Service.LoadAsync(h.Id, h.MpsId, default);
        Assert.Equal(PlanningDraftOutcomeKind.Loaded, moved.Kind);
        Assert.Contains("site S1", moved.Warning);
    }

    [Fact]
    public async Task Corrupt_Draft_Is_Reported_Retained_Byte_For_Byte_And_Never_Overwritten()
    {
        var h = new Harness(_root);
        Directory.CreateDirectory(Path.GetDirectoryName(h.Store.FilePath(h.Id))!);
        var bytes = "{ \"workspaceId\": broken"u8.ToArray();
        await File.WriteAllBytesAsync(h.Store.FilePath(h.Id), bytes);

        Assert.True(await h.Service.ExistsAsync(h.Id, default)); // presence = existence only
        var load = await h.Service.LoadAsync(h.Id, h.MpsId, default);
        Assert.Equal(PlanningDraftOutcomeKind.Corrupt, load.Kind);
        Assert.Equal(PlanningWorkbookDraftService.CorruptWarning, load.Warning);
        var save = await h.Service.SaveAsync(h.Id, h.MpsId, [new("P-1", new DateOnly(2026, 9, 21), "5")], default);
        Assert.Equal(PlanningDraftOutcomeKind.ExistingDraftCorrupt, save.Kind);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(h.Store.FilePath(h.Id)));

        // Structurally valid JSON with an invalid entry (zero adjustment) is also corrupt, not empty.
        var invalid = JsonSerializer.Serialize(new PlanningWorkbookDraft(h.Id, h.Site, h.MpsId.ToString(), h.Clock.UtcNow,
            [new("P-1", new DateOnly(2026, 9, 21), "0", 75.5m)]), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await File.WriteAllTextAsync(h.Store.FilePath(h.Id), invalid);
        Assert.Equal(PlanningDraftOutcomeKind.Corrupt, (await h.Service.LoadAsync(h.Id, h.MpsId, default)).Kind);
        Assert.Equal(invalid, await File.ReadAllTextAsync(h.Store.FilePath(h.Id)));

        await h.Service.DeleteAsync(h.Id, default);
        Assert.False(await h.Service.ExistsAsync(h.Id, default));
        Assert.Equal(PlanningDraftOutcomeKind.Loaded,
            (await h.Service.SaveAsync(h.Id, h.MpsId, [new("P-1", new DateOnly(2026, 9, 21), "5")], default)).Kind);
    }

    [Fact]
    public async Task Writes_Are_Atomic_And_A_Failed_Write_Keeps_The_Previous_Draft()
    {
        var h = new Harness(_root);
        var week = new DateOnly(2026, 9, 21);
        await h.Service.SaveAsync(h.Id, h.MpsId, [new("P-1", week, "5")], default);
        var original = await File.ReadAllTextAsync(h.Store.FilePath(h.Id));

        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var draft = new PlanningWorkbookDraft(h.Id, h.Site, h.MpsId.ToString(), h.Clock.UtcNow, [new("P-1", week, "9", 75.5m)]);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Store.SaveAsync(draft, cancelled.Token));

        Assert.Equal(original, await File.ReadAllTextAsync(h.Store.FilePath(h.Id)));
        var directory = Path.GetDirectoryName(h.Store.FilePath(h.Id))!;
        Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        Assert.Equal(Path.Combine(_root, "config", $"planning-workbook-draft-{h.Id:D}.json"), h.Store.FilePath(h.Id));

        await h.Service.SaveAsync(h.Id, h.MpsId, [new("P-1", week, "7")], default);
        Assert.Equal("7", (await h.Store.LoadAsync(h.Id, default))!.Entries.Single().Adjustment);
        Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
    }

    [Fact]
    public async Task Drafts_Are_Isolated_Per_Workspace_Assignment_With_Overlapping_Parts()
    {
        var store = new JsonPlanningWorkbookDraftStore(new LocalAppDataPaths(_root));
        var a = new Harness(_root, store: store);
        var b = new Harness(_root, store: store, id: Guid.NewGuid());
        var week = new DateOnly(2026, 9, 21);

        await a.Service.SaveAsync(a.Id, a.MpsId, [new("P-1", week, "5")], default);
        Assert.True(await a.Service.ExistsAsync(a.Id, default));
        Assert.False(await b.Service.ExistsAsync(b.Id, default));
        Assert.Equal(PlanningDraftOutcomeKind.NoDraft, (await b.Service.LoadAsync(b.Id, b.MpsId, default)).Kind);

        await b.Service.SaveAsync(b.Id, b.MpsId, [new("P-1", week, "-3")], default);
        Assert.Equal("5", (await a.Service.LoadAsync(a.Id, a.MpsId, default)).Rows!.Single().Saved.Adjustment);
        Assert.Equal("-3", (await b.Service.LoadAsync(b.Id, b.MpsId, default)).Rows!.Single().Saved.Adjustment);

        await b.Service.DeleteAsync(b.Id, default);
        Assert.False(await b.Service.ExistsAsync(b.Id, default));
        Assert.True(await a.Service.ExistsAsync(a.Id, default));
        // The other workspace's snapshot cannot be used to load or save this workspace's draft.
        Assert.Equal(PlanningDraftOutcomeKind.MpsSnapshotChanged, (await a.Service.LoadAsync(a.Id, b.MpsId, default)).Kind);
    }

    private sealed class Harness
    {
        public Harness(string root, bool loadMps = true, JsonPlanningWorkbookDraftStore? store = null, Guid? id = null)
        {
            Id = id ?? Fixture.Root.GetProperty("scope").GetProperty("workspaceAssignmentId").GetGuid();
            Site = Fixture.Root.GetProperty("scope").GetProperty("site").GetString()!;
            MpsId = new SnapshotId(id is null ? Fixture.LoadedSnapshotId : Guid.NewGuid());
            Clock.LocalNow = new DateTimeOffset(Fixture.Today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.FromHours(-7));
            Clock.UtcNow = Clock.LocalNow.ToUniversalTime();
            Store = store ?? new JsonPlanningWorkbookDraftStore(new LocalAppDataPaths(root));
            Workspaces = new Workspaces([new WorkspaceAssignment(Id, "Test", Site, null, null, ["CONFIGURED"], false, null, true, 0)]);
            if (loadMps) LoadMps(MpsId);
            Service = new PlanningWorkbookDraftService(Store, Workspaces, Mps, Clock);
        }

        public Guid Id { get; }
        public string Site { get; }
        public SnapshotId MpsId { get; }
        public FakeClock Clock { get; } = new();
        public InMemoryMpsSnapshotStore Mps { get; } = new();
        public JsonPlanningWorkbookDraftStore Store { get; }
        public Workspaces Workspaces { get; }
        public PlanningWorkbookDraftService Service { get; }

        public void LoadMps(SnapshotId snapshotId, IEnumerable<MpsSourceRow>? extraRows = null) =>
            Mps.SetLoaded(Id, new MpsSnapshot(snapshotId, Clock.UtcNow, Site, Fixture.ResolvedParts,
                Fixture.MpsRows.Concat(extraRows ?? []).Select(r => r with { Site = Site }).ToList()));
    }

    private sealed class Workspaces(IReadOnlyList<WorkspaceAssignment> initial) : IWorkspaceConfigurationService
    {
        public List<WorkspaceAssignment> Items { get; } = [.. initial];
        public Task<WorkspaceListResult> GetWorkspacesAsync() => Task.FromResult(new WorkspaceListResult(Items.ToList(), null));
        public Task<WorkspaceCreateResult> CreateWorkspaceAsync(CreateWorkspaceCommand command) => throw new NotSupportedException();
        public Task<WorkspaceUpdateResult> UpdateWorkspaceAsync(Guid assignmentId, CreateWorkspaceCommand command) => throw new NotSupportedException();
        public Task<WorkspaceOperationResult> ArchiveWorkspaceAsync(Guid assignmentId) => throw new NotSupportedException();
        public Task<WorkspaceOperationResult> RestoreWorkspaceAsync(Guid assignmentId) => throw new NotSupportedException();
        public Task<WorkspaceOperationResult> DeleteWorkspaceAsync(Guid assignmentId) => throw new NotSupportedException();
        public Task<WorkspaceReorderResult> ReorderWorkspacesAsync(ReorderWorkspacesCommand command) => throw new NotSupportedException();
        public Task ResetWorkspacesAsync() => throw new NotSupportedException();
    }
}
