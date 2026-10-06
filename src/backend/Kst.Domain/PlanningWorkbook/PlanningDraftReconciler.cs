namespace Kst.Domain.PlanningWorkbook;

/// <summary>A saved draft entry: signed adjustment text and the MPS quantity it was last saved against.</summary>
public sealed record PlanningDraftEntry(string Part, DateOnly WeekLabel, decimal OriginalMpsQuantity, string AdjustmentText);

/// <summary>
/// Restoration comparison for one saved entry. The signed adjustment is always retained and is
/// never converted into a target quantity. <see cref="CurrentMps"/> is null when the entry can no
/// longer be evaluated (for example, its week has passed or its part is out of scope); such entries
/// are retained and flagged by <see cref="Issue"/>.
/// </summary>
public sealed record PlanningDraftComparison(
    PlanningDraftEntry Saved,
    decimal? CurrentMps,
    bool BaselineChanged,
    decimal? RestoredAdjustment,
    decimal? ProposedMps,
    MpsAdjustmentIssue? Issue)
{
    public bool ExportEligible => Issue is null;
}

/// <summary>
/// Pure saved-draft comparison against the currently loaded MPS snapshot. Uses the same per-entry
/// rules as export validation; there is no acknowledgment step and no QAD access.
/// </summary>
public static class PlanningDraftReconciler
{
    public static IReadOnlyList<PlanningDraftComparison> Compare(
        IReadOnlyList<PlanningDraftEntry> saved, MpsAdjustmentContext context)
    {
        ArgumentNullException.ThrowIfNull(saved);
        ArgumentNullException.ThrowIfNull(context);

        var evaluated = MpsAdjustmentValidator.Evaluate(
            saved.Select(s => new MpsAdjustmentEntry(s.Part, s.WeekLabel, s.AdjustmentText)).ToList(), context);

        return saved.Select((entry, i) =>
        {
            var result = evaluated[i];
            var adjustment = MpsAdjustmentInput.Parse(entry.AdjustmentText).Value;
            return new PlanningDraftComparison(
                entry,
                result.ExistingMps,
                result.ExistingMps is { } current && current != entry.OriginalMpsQuantity,
                adjustment,
                result.ProposedMps,
                result.Issue);
        }).ToList();
    }
}
