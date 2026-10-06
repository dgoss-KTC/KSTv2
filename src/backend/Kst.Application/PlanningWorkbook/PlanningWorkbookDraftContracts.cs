namespace Kst.Application.PlanningWorkbook;

/// <summary>
/// One saved Planning adjustment (Stage 14.5, ledger §6.2): the signed whole-number adjustment and the
/// MPS quantity it was last saved against. <see cref="SavedMpsQuantity"/> is null when the baseline was
/// unavailable when saved (for example a retained entry whose week had already passed); it is never
/// replaced by zero, because zero is a genuine MPS quantity.
/// </summary>
public sealed record PlanningWorkbookDraftEntry(string Part, DateOnly WeekLabel, string Adjustment, decimal? SavedMpsQuantity);

/// <summary>Optional per-workspace saved Planning draft. Saving never applies a change to QAD.</summary>
public sealed record PlanningWorkbookDraft(
    Guid WorkspaceId, string Site, string MpsSnapshotId, DateTimeOffset SavedAtUtc,
    IReadOnlyList<PlanningWorkbookDraftEntry> Entries);

/// <summary>Local, workspace-assignment-scoped draft persistence (atomic writes; corrupt files retained).</summary>
public interface IPlanningWorkbookDraftStore
{
    Task<bool> ExistsAsync(Guid workspaceId, CancellationToken ct);
    Task<PlanningWorkbookDraft?> LoadAsync(Guid workspaceId, CancellationToken ct);
    Task SaveAsync(PlanningWorkbookDraft draft, CancellationToken ct);
    Task DeleteAsync(Guid workspaceId, CancellationToken ct);
}

/// <summary>An adjustment submitted for saving. Text follows the accepted Domain adjustment parser.</summary>
public sealed record PlanningDraftSaveEntry(string Part, DateOnly WeekLabel, string? Adjustment);

public enum PlanningDraftOutcomeKind
{
    Loaded,
    NoDraft,
    Corrupt,
    UnknownWorkspace,
    MpsNotLoaded,
    MpsSnapshotChanged,
    Invalid,
    ExistingDraftCorrupt
}

/// <summary>
/// Saved-versus-current comparison for one saved entry. <see cref="CurrentMps"/> and
/// <see cref="ProposedMps"/> are null when the entry cannot be evaluated against the loaded MPS
/// snapshot (an unavailable baseline), which is distinct from a numeric zero.
/// </summary>
public sealed record PlanningDraftEntryComparison(
    PlanningWorkbookDraftEntry Saved,
    decimal? CurrentMps,
    bool BaselineChanged,
    decimal? ProposedMps,
    string? IssueCode);

public sealed record PlanningDraftResult(
    PlanningDraftOutcomeKind Kind,
    PlanningWorkbookDraft? Draft = null,
    IReadOnlyList<PlanningDraftEntryComparison>? Rows = null,
    string? Warning = null,
    string? InvalidReason = null);
