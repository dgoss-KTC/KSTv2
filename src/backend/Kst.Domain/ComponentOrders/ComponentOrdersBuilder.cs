namespace Kst.Domain.ComponentOrders;

/// <summary>
/// Pure Stage 10 Component Orders composition. Groups qualifying conventional open PO lines by
/// component part (case-insensitively — QAD key comparisons are case-insensitive) and orders them
/// for presentation:
///
/// - Within a group, lines order by due date ascending with missing due dates first, then PO
///   number, then line. This is the accepted Stage 9 next-PO ordering (SQL NULLS FIRST on an
///   ascending due-date sort), so the group's <c>DisplayLine</c> — its earliest qualifying line —
///   matches what a scheduler already sees in Work Order incoming context.
/// - Groups order by their display line under the same comparison: components whose earliest
///   qualifying PO has no due date therefore sort first as exceptions, and all other groups sort
///   earliest-due to latest with PO number then Line breaking ties.
///
/// No I/O, no QAD concepts, no frontend concepts; deterministic for a given input sequence.
/// </summary>
public static class ComponentOrdersBuilder
{
    /// <summary>Groups and orders the qualifying lines; an empty input yields an empty result (a loaded-empty outcome).</summary>
    public static IReadOnlyList<ComponentOrderGroup> BuildGroups(IReadOnlyList<ComponentOrderLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var groups = new List<ComponentOrderGroup>();
        foreach (var partGroup in lines.GroupBy(line => line.ComponentPart, StringComparer.OrdinalIgnoreCase))
        {
            var ordered = OrderLines(partGroup).ToList();
            groups.Add(new ComponentOrderGroup(ordered[0].ComponentPart, ordered[0], ordered.Skip(1).ToList()));
        }

        return groups.OrderBy(group => group.DisplayLine, LineComparer.Instance).ToList();
    }

    /// <summary>Orders one component's lines: missing due dates first, then due date ascending, PO number, line.</summary>
    public static IReadOnlyList<ComponentOrderLine> OrderLines(IEnumerable<ComponentOrderLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        return lines.OrderBy(line => line, LineComparer.Instance).ToList();
    }

    private sealed class LineComparer : IComparer<ComponentOrderLine>
    {
        public static readonly LineComparer Instance = new();

        public int Compare(ComponentOrderLine? x, ComponentOrderLine? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            var dueComparison = CompareDueDates(x.DueDate, y.DueDate);
            if (dueComparison != 0) return dueComparison;

            var poComparison = string.CompareOrdinal(x.PoNumber, y.PoNumber);
            if (poComparison != 0) return poComparison;

            return x.PoLine.CompareTo(y.PoLine);
        }

        private static int CompareDueDates(DateOnly? left, DateOnly? right) => (left, right) switch
        {
            ({ }, { }) => left.Value.CompareTo(right.Value),
            (null, null) => 0,
            (null, _) => -1,
            _ => 1
        };
    }
}
