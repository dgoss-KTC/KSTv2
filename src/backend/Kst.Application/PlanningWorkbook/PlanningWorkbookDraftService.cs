using System.Collections.Concurrent;
using Kst.Application.Mps;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.PlanningWorkbook;

namespace Kst.Application.PlanningWorkbook;

/// <summary>
/// Stage 14.5 optional saved Planning drafts (ledger §6). Drafts hold signed whole-number adjustments
/// with the MPS quantity each was saved against. Restoration reconciles them against the workspace's
/// currently loaded MPS snapshot with the accepted Domain <see cref="PlanningDraftReconciler"/>; it never
/// loads or refreshes MPS and never reads QAD. Signed adjustments are retained (never converted into
/// target quantities) and entries that are now invalid are returned flagged rather than dropped.
/// Saving or deleting a draft never applies anything to QAD.
/// </summary>
public sealed class PlanningWorkbookDraftService(
    IPlanningWorkbookDraftStore store,
    IWorkspaceConfigurationService workspaces,
    IMpsSnapshotStore mps,
    IClock clock)
{
    public const int MaxEntries = 10_000;
    public const int MaxPartLength = 100;
    public const string CorruptWarning =
        "The saved Planning draft could not be read. It was kept on disk for recovery and was not restored.";

    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _gates = new();

    /// <summary>Presence is file existence only (no parsing, no MPS or QAD access).</summary>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct) =>
        await WorkspaceExistsAsync(id) && await store.ExistsAsync(id, ct);

    public async Task<PlanningDraftResult> LoadAsync(Guid id, SnapshotId mpsSnapshotId, CancellationToken ct)
    {
        var workspace = await WorkspaceAsync(id);
        if (workspace is null) return new(PlanningDraftOutcomeKind.UnknownWorkspace);

        var gate = Gate(id);
        await gate.WaitAsync(ct);
        try
        {
            PlanningWorkbookDraft? draft;
            try { draft = await store.LoadAsync(id, ct); }
            catch (Exception ex) when (IsCorruption(ex))
            {
                return new(PlanningDraftOutcomeKind.Corrupt, Warning: CorruptWarning);
            }
            if (draft is null) return new(PlanningDraftOutcomeKind.NoDraft);

            if (CurrentContext(id, mpsSnapshotId, workspace.Site) is not { } context)
                return new(MpsProblem(id));

            var warning = string.Equals(draft.Site, workspace.Site, StringComparison.Ordinal)
                ? null
                : $"This draft was saved for site {draft.Site}; the workspace now uses site {workspace.Site}.";
            return new(PlanningDraftOutcomeKind.Loaded, draft, Compare(draft.Entries, context), warning);
        }
        finally { gate.Release(); }
    }

    public async Task<PlanningDraftResult> SaveAsync(
        Guid id, SnapshotId mpsSnapshotId, IReadOnlyList<PlanningDraftSaveEntry> entries, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var workspace = await WorkspaceAsync(id);
        if (workspace is null) return new(PlanningDraftOutcomeKind.UnknownWorkspace);
        if (Validate(entries) is { } invalid) return new(PlanningDraftOutcomeKind.Invalid, InvalidReason: invalid);
        if (CurrentContext(id, mpsSnapshotId, workspace.Site) is not { } context)
            return new(MpsProblem(id));

        // No-op entries (blank, 0, -0) are not proposals and are not saved.
        var proposals = entries
            .Where(e => MpsAdjustmentInput.Parse(e.Adjustment).Kind == MpsAdjustmentInputKind.Value)
            .ToList();
        // The accepted reconciler supplies the loaded baseline for each entry (null when unavailable).
        var baselines = PlanningDraftReconciler.Compare(
            proposals.Select(e => new PlanningDraftEntry(e.Part, e.WeekLabel, 0m, e.Adjustment!)).ToList(), context);
        var saved = proposals.Select((e, i) => new PlanningWorkbookDraftEntry(
            e.Part, e.WeekLabel, Canonical(e.Adjustment!), baselines[i].CurrentMps)).ToList();
        var draft = new PlanningWorkbookDraft(id, workspace.Site, mpsSnapshotId.ToString(), clock.UtcNow, saved);

        var gate = Gate(id);
        await gate.WaitAsync(ct);
        try
        {
            // A corrupt existing file is retained for recovery; it is never silently overwritten.
            try { await store.LoadAsync(id, ct); }
            catch (Exception ex) when (IsCorruption(ex))
            {
                return new(PlanningDraftOutcomeKind.ExistingDraftCorrupt, Warning: CorruptWarning);
            }
            // The workspace may have been deleted or re-sited while waiting.
            var current = await WorkspaceAsync(id);
            if (current is null) return new(PlanningDraftOutcomeKind.UnknownWorkspace);
            if (current.Site != workspace.Site || CurrentContext(id, mpsSnapshotId, current.Site) is null)
                return new(PlanningDraftOutcomeKind.MpsSnapshotChanged);

            await store.SaveAsync(draft, ct);
            return new(PlanningDraftOutcomeKind.Loaded, draft, Compare(saved, context));
        }
        finally { gate.Release(); }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var gate = Gate(id);
        await gate.WaitAsync(ct);
        try { await store.DeleteAsync(id, ct); }
        finally { gate.Release(); }
    }

    private static IReadOnlyList<PlanningDraftEntryComparison> Compare(
        IReadOnlyList<PlanningWorkbookDraftEntry> entries, MpsAdjustmentContext context)
    {
        var compared = PlanningDraftReconciler.Compare(
            entries.Select(e => new PlanningDraftEntry(e.Part, e.WeekLabel, e.SavedMpsQuantity ?? 0m, e.Adjustment)).ToList(),
            context);
        return entries.Select((entry, i) =>
        {
            var c = compared[i];
            // A baseline that was unavailable when saved is not "unchanged" once it can be evaluated.
            var changed = entry.SavedMpsQuantity is null ? c.CurrentMps is not null : c.BaselineChanged;
            return new PlanningDraftEntryComparison(entry, c.CurrentMps, changed, c.ProposedMps, c.Issue?.ToCode());
        }).ToList();
    }

    private static string? Validate(IReadOnlyList<PlanningDraftSaveEntry> entries)
    {
        if (entries.Count > MaxEntries) return "too-many-entries";
        if (entries.Any(e => e is null || string.IsNullOrWhiteSpace(e.Part) || e.Part.Length > MaxPartLength))
            return "invalid-part";
        foreach (var entry in entries)
        {
            var kind = MpsAdjustmentInput.Parse(entry.Adjustment).Kind;
            if (kind == MpsAdjustmentInputKind.InvalidNumber) return MpsAdjustmentIssue.InvalidNumber.ToCode();
            if (kind == MpsAdjustmentInputKind.NotWholeNumber) return MpsAdjustmentIssue.NotWholeNumber.ToCode();
            if (entry.WeekLabel.DayOfWeek != DayOfWeek.Monday) return MpsAdjustmentIssue.NotAWeek.ToCode();
        }
        var duplicate = entries
            .GroupBy(e => (e.Part.ToUpperInvariant(), e.WeekLabel))
            .Any(g => g.Count() > 1);
        return duplicate ? MpsAdjustmentIssue.DuplicateIdentity.ToCode() : null;
    }

    private static string Canonical(string text) =>
        MpsAdjustmentInput.Parse(text).Value!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private MpsAdjustmentContext? CurrentContext(Guid id, SnapshotId expected, string site)
    {
        var snapshot = mps.GetState(id).Snapshot;
        if (snapshot is null || snapshot.Id != expected || snapshot.Site != site) return null;
        return new MpsAdjustmentContext(snapshot.Id.Value, snapshot.ResolvedParts, snapshot.SourceRows,
            DateOnly.FromDateTime(clock.LocalNow.Date));
    }

    private PlanningDraftOutcomeKind MpsProblem(Guid id) =>
        mps.GetState(id).Snapshot is null ? PlanningDraftOutcomeKind.MpsNotLoaded : PlanningDraftOutcomeKind.MpsSnapshotChanged;

    private static bool IsCorruption(Exception ex) =>
        ex is System.Text.Json.JsonException or InvalidDataException or NotSupportedException or FormatException;

    private SemaphoreSlim Gate(Guid id) => _gates.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));

    private async Task<bool> WorkspaceExistsAsync(Guid id) => await WorkspaceAsync(id) is not null;

    private async Task<Kst.Domain.Workspaces.WorkspaceAssignment?> WorkspaceAsync(Guid id) =>
        (await workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == id);
}
