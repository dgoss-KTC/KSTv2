using Kst.Domain.Mps;

namespace Kst.Domain.PlanningWorkbook;

/// <summary>
/// Owner-confirmed Stage 14.4 Planning horizon range (2026-10-02). The selectable minimum (and the
/// default) is <see cref="MinimumWeeks"/>. The maximum runs through one week after the last business
/// week containing a nonzero existing MPS quantity across the workspace's resolved parts, determined
/// from the complete loaded MPS snapshot on Due Date with the accepted Sunday–Saturday calendar:
/// <c>max(12, lastMpsWeekIndex + 2)</c> with the current week as index zero. Falldown, sales orders,
/// forecast and adjustments never extend the range.
/// </summary>
public static class PlanningHorizonRange
{
    public const int MinimumWeeks = 12;

    public static int MaximumWeeks(
        IReadOnlyList<MpsResolvedPart> resolvedParts, IReadOnlyList<MpsSourceRow> mpsRows, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(resolvedParts);
        ArgumentNullException.ThrowIfNull(mpsRows);

        var parts = new HashSet<string>(resolvedParts.Select(p => p.ParentPart), StringComparer.OrdinalIgnoreCase);
        var currentWeekStart = MpsBusinessCalendar.GetBusinessWeekStart(today);

        // A week "contains" MPS when its aggregated part/week quantity (as the grid shows it) is nonzero.
        var lastIndex = mpsRows
            .Where(r => parts.Contains(r.ParentPart) && !MpsBusinessCalendar.IsFalldown(r.DueDate, today))
            .GroupBy(r => (Part: r.ParentPart.ToUpperInvariant(),
                Week: (MpsBusinessCalendar.GetBusinessWeekStart(r.DueDate).DayNumber - currentWeekStart.DayNumber) / 7))
            .Where(g => g.Sum(r => r.Quantity) != 0m)
            .Select(g => (int?)g.Key.Week)
            .Max();

        return lastIndex is { } index ? Math.Max(MinimumWeeks, index + 2) : MinimumWeeks;
    }
}
