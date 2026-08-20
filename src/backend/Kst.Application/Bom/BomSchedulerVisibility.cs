namespace Kst.Application.Bom;

/// <summary>
/// Scheduler-visible P/M classification for Stage 8 BOM presentation (Application-owned; the
/// structural traversal in Kst.Integrations.Qad never filters by P/M). Visible rows are
/// occurrences whose effective P/M code is P or M; every other code (N, S, 2, 3, 4, C, D, …)
/// and a missing code are not visible.
///
/// Visibility is applied to the flat structural occurrence list after the complete traversal:
/// omitting a hidden intermediate never removes its descendants — they already carry their own
/// P/M codes and remain independently eligible, and actual structural levels are preserved
/// (level gaps are intentional). Pure and testable; no SQL or traversal concepts.
/// </summary>
public static class BomSchedulerVisibility
{
    /// <summary>
    /// Returns true when the effective P/M code selects a scheduler-visible row. Robust by
    /// convention: trims surrounding whitespace and compares case-insensitively (QAD code
    /// fields are short uppercase values that may carry padding; same trim/case-insensitive
    /// comparison convention as the rest of the application layer).
    /// </summary>
    public static bool IsSchedulerVisible(string? pmCode)
    {
        var code = pmCode?.Trim();
        return code is not null
            && (code.Equals("P", StringComparison.OrdinalIgnoreCase)
                || code.Equals("M", StringComparison.OrdinalIgnoreCase));
    }
}
