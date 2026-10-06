using Kst.Application.PlanningWorkbook;
using Kst.Infrastructure.Configuration;
using Kst.Infrastructure.PlanningWorkbook;

namespace Kst.Application.Tests.PlanningWorkbook;

/// <summary>
/// Stage 14.5 local Planning draft files: per-assignment isolation, atomic replacement (no temp file left,
/// a failed write leaves the previous draft intact), and corrupt or invalid files reported and retained.
/// Uses a temporary root, never the developer's real local app data.
/// </summary>
public sealed class JsonPlanningWorkbookDraftStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"kst-planning-draft-test-{Guid.NewGuid():N}");
    private readonly JsonPlanningWorkbookDraftStore _store;

    public JsonPlanningWorkbookDraftStoreTests() => _store = new JsonPlanningWorkbookDraftStore(new LocalAppDataPaths(_root));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private static PlanningWorkbookDraft Draft(Guid id, string adjustment = "-50", decimal? baseline = 100m) =>
        new(id, "SW", Guid.NewGuid().ToString(), DateTimeOffset.UtcNow,
            [new PlanningWorkbookDraftEntry("P-1", new DateOnly(2026, 9, 14), adjustment, baseline),
             new PlanningWorkbookDraftEntry("P-2", new DateOnly(2026, 9, 21), "7", null)]);

    [Fact]
    public async Task Uses_The_Assignment_Scoped_Path_And_Isolates_Workspaces()
    {
        var id = Guid.NewGuid(); var other = Guid.NewGuid();
        var draft = Draft(id);
        await _store.SaveAsync(draft, CancellationToken.None);

        Assert.Equal(Path.Combine(_root, "config", $"planning-workbook-draft-{id:D}.json"), _store.FilePath(id));
        Assert.True(await _store.ExistsAsync(id, CancellationToken.None));
        Assert.False(await _store.ExistsAsync(other, CancellationToken.None));
        var loaded = await _store.LoadAsync(id, CancellationToken.None);
        Assert.Equal(draft.Entries, loaded!.Entries); // signed adjustments and saved baselines (including unavailable) round-trip
        Assert.Null(await _store.LoadAsync(other, CancellationToken.None));
        await _store.DeleteAsync(other, CancellationToken.None);
        Assert.True(await _store.ExistsAsync(id, CancellationToken.None));
        await _store.DeleteAsync(id, CancellationToken.None);
        Assert.False(await _store.ExistsAsync(id, CancellationToken.None));
        await _store.DeleteAsync(id, CancellationToken.None); // deleting a missing draft is not an error
    }

    [Fact]
    public async Task Replaces_Atomically_Without_Leaving_Temp_Files_And_Keeps_The_Previous_Draft_When_A_Write_Fails()
    {
        var id = Guid.NewGuid();
        await _store.SaveAsync(Draft(id, "-50"), CancellationToken.None);
        await _store.SaveAsync(Draft(id, "25"), CancellationToken.None);
        Assert.Equal("25", (await _store.LoadAsync(id, CancellationToken.None))!.Entries[0].Adjustment);
        Assert.Single(Directory.GetFiles(Path.Combine(_root, "config"), "planning-workbook-draft-*"));

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _store.SaveAsync(Draft(id, "-1"), cts.Token));
        Assert.Equal("25", (await _store.LoadAsync(id, CancellationToken.None))!.Entries[0].Adjustment);
        Assert.Empty(Directory.GetFiles(Path.Combine(_root, "config"), "*.tmp"));
    }

    [Theory]
    [InlineData("not JSON")]
    [InlineData("null")]
    [InlineData("{\"workspaceId\":\"00000000-0000-0000-0000-000000000000\",\"site\":\"SW\",\"mpsSnapshotId\":\"s\",\"savedAtUtc\":\"2026-09-16T00:00:00Z\",\"entries\":[]}")]
    public async Task Reports_Corrupt_Or_Foreign_Files_And_Retains_Them(string content)
    {
        var id = Guid.NewGuid();
        await _store.SaveAsync(Draft(id), CancellationToken.None);
        var path = _store.FilePath(id);
        await File.WriteAllTextAsync(path, content);

        await Assert.ThrowsAnyAsync<Exception>(() => _store.LoadAsync(id, CancellationToken.None));
        Assert.True(File.Exists(path));
        Assert.Equal(content, await File.ReadAllTextAsync(path)); // never replaced with empty data
    }

    [Theory]
    [InlineData("0")]       // no-op is never persisted
    [InlineData("1.5")]     // not a whole number
    [InlineData("+5")]      // invalid syntax
    public async Task Rejects_Files_Containing_Non_Proposal_Adjustments(string adjustment)
    {
        var id = Guid.NewGuid();
        await _store.SaveAsync(Draft(id, adjustment), CancellationToken.None);
        await Assert.ThrowsAsync<InvalidDataException>(() => _store.LoadAsync(id, CancellationToken.None));
        Assert.True(File.Exists(_store.FilePath(id)));
    }
}
