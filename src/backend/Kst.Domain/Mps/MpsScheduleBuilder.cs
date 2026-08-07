namespace Kst.Domain.Mps;

/// <summary>
/// Builds deterministic MPS parent/bucket schedules from normalized source facts. Pure aggregation
/// logic with no I/O: Falldown, weekly bucketing, quantity aggregation, and A/F/R/Mixed/P/e status
/// classification. Falldown is always Due-Date based even when <paramref name="dateBasis"/> selects
/// Release Date for weekly buckets. Every resolved parent part is retained even with zero source rows.
/// </summary>
public static class MpsScheduleBuilder
{
    public static IReadOnlyList<MpsPartSchedule> Build(
        IReadOnlyList<MpsResolvedPart> resolvedParts,
        IReadOnlyList<MpsSourceRow> sourceRows,
        MpsDateBasis dateBasis,
        int horizonWeeks,
        DateOnly today)
    {
        if (horizonWeeks <= 0)
            throw new ArgumentOutOfRangeException(nameof(horizonWeeks), "Horizon must be positive.");

        var currentWeekStart = MpsBusinessCalendar.GetBusinessWeekStart(today);
        var weekStarts = Enumerable.Range(0, horizonWeeks)
            .Select(i => currentWeekStart.AddDays(7 * i))
            .ToList();

        var rowsByPart = sourceRows
            .GroupBy(r => r.ParentPart, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<MpsSourceRow>)g.ToList(), StringComparer.OrdinalIgnoreCase);

        var schedules = new List<MpsPartSchedule>(resolvedParts.Count);
        foreach (var part in resolvedParts)
        {
            var rows = rowsByPart.TryGetValue(part.ParentPart, out var found)
                ? found
                : [];

            var buckets = new List<MpsBucket>(weekStarts.Count + 1)
            {
                BuildBucket(
                    MpsBucketKind.Falldown,
                    weekLabel: null,
                    rows.Where(r => MpsBusinessCalendar.IsFalldown(r.DueDate, today)).ToList())
            };

            foreach (var weekStart in weekStarts)
            {
                var weekEndExclusive = weekStart.AddDays(7);
                var weekRows = rows.Where(r =>
                {
                    var basisDate = dateBasis == MpsDateBasis.DueDate ? r.DueDate : r.ReleaseDate;
                    return basisDate is { } d && d >= weekStart && d < weekEndExclusive;
                }).ToList();

                buckets.Add(BuildBucket(MpsBucketKind.Weekly, MpsBusinessCalendar.GetWeekLabel(weekStart), weekRows));
            }

            schedules.Add(new MpsPartSchedule(part.ParentPart, part.Description, buckets));
        }

        return schedules;
    }

    private static MpsBucket BuildBucket(MpsBucketKind kind, DateOnly? weekLabel, IReadOnlyList<MpsSourceRow> rows)
    {
        var quantity = rows.Sum(r => r.Quantity);

        var distinctAfr = rows
            .Select(r => r.WorkOrderState)
            .Where(s => s is MpsWorkOrderState.Allocating or MpsWorkOrderState.Frozen or MpsWorkOrderState.Released)
            .Distinct()
            .ToList();

        var executionStatus = distinctAfr.Count switch
        {
            0 => MpsExecutionStatus.None,
            1 => distinctAfr[0] switch
            {
                MpsWorkOrderState.Allocating => MpsExecutionStatus.Allocating,
                MpsWorkOrderState.Frozen => MpsExecutionStatus.Frozen,
                MpsWorkOrderState.Released => MpsExecutionStatus.Released,
                _ => MpsExecutionStatus.None
            },
            _ => MpsExecutionStatus.Mixed
        };

        var containsPlannedWork = rows.Any(r => r.WorkOrderState == MpsWorkOrderState.Planned);
        var containsExplicitlyScheduledWork = rows.Any(r => r.WorkOrderState == MpsWorkOrderState.ExplicitlyScheduled);

        var workOrders = rows
            .Select(r => new MpsWorkOrderRef(r.WorkOrderId, r.WorkOrderState))
            .Distinct()
            .ToList();

        return new MpsBucket(
            kind,
            weekLabel,
            quantity,
            executionStatus,
            containsPlannedWork,
            containsExplicitlyScheduledWork,
            workOrders);
    }
}
