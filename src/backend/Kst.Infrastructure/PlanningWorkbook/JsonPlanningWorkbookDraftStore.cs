using System.Text.Json;
using Kst.Application.PlanningWorkbook;
using Kst.Domain.PlanningWorkbook;
using Kst.Infrastructure.Configuration;

namespace Kst.Infrastructure.PlanningWorkbook;

/// <summary>
/// Stage 14.5 workspace-assignment-scoped Planning drafts at
/// <c>%LOCALAPPDATA%\KST\config\planning-workbook-draft-{assignmentId}.json</c>. Writes go to a temp file
/// and are renamed into place (atomic replace). Invalid files are reported and retained for recovery;
/// they are never replaced by empty data.
/// </summary>
public sealed class JsonPlanningWorkbookDraftStore(LocalAppDataPaths paths) : IPlanningWorkbookDraftStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public string FilePath(Guid id) => Path.Combine(paths.ConfigDirectory, $"planning-workbook-draft-{id:D}.json");

    public Task<bool> ExistsAsync(Guid workspaceId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(FilePath(workspaceId)));
    }

    public async Task<PlanningWorkbookDraft?> LoadAsync(Guid workspaceId, CancellationToken ct)
    {
        var path = FilePath(workspaceId);
        if (!File.Exists(path)) return null;
        var draft = JsonSerializer.Deserialize<PlanningWorkbookDraft>(await File.ReadAllTextAsync(path, ct), Options);
        if (draft is null || draft.WorkspaceId != workspaceId || string.IsNullOrWhiteSpace(draft.Site) ||
            string.IsNullOrWhiteSpace(draft.MpsSnapshotId) || draft.Entries is null ||
            draft.Entries.Any(e => e is null || string.IsNullOrWhiteSpace(e.Part) || e.WeekLabel.DayOfWeek != DayOfWeek.Monday ||
                MpsAdjustmentInput.Parse(e.Adjustment).Kind != MpsAdjustmentInputKind.Value) ||
            draft.Entries.GroupBy(e => (e.Part.ToUpperInvariant(), e.WeekLabel)).Any(g => g.Count() > 1))
            throw new InvalidDataException("Invalid Planning Workbook draft.");
        return draft;
    }

    public async Task SaveAsync(PlanningWorkbookDraft draft, CancellationToken ct)
    {
        paths.EnsureDirectoriesExist();
        var path = FilePath(draft.WorkspaceId);
        var temp = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(draft, Options), ct);
            File.Move(temp, path, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    public Task DeleteAsync(Guid workspaceId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        File.Delete(FilePath(workspaceId));
        return Task.CompletedTask;
    }
}
