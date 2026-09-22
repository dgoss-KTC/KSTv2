namespace Kst.Domain.LongTermShortages;

/// <summary>Pure shared-MRP schedule calculation. All source qualification remains outside the domain.</summary>
public static class LongTermShortagesBuilder
{
    public const int WeekCount = 24;

    public static DateOnly GetWeekOneStart(DateOnly refreshDate) => refreshDate.AddDays(-(((int)refreshDate.DayOfWeek + 6) % 7));

    public static IReadOnlyList<LongTermShortageRow> Build(DateOnly refreshDate, IReadOnlyList<LongTermShortageInput> inputs)
    {
        var weekOneStart = GetWeekOneStart(refreshDate);
        return inputs.Select(input => BuildRow(input, weekOneStart))
            .OrderBy(row => row.FirstShortDate ?? DateOnly.MaxValue)
            .ThenBy(row => row.ComponentPart, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static LongTermShortageRow BuildRow(LongTermShortageInput input, DateOnly weekOneStart)
    {
        // Presentation context is deliberately carried through only after raw-MRP arithmetic.
        var evidence = input.Evidence.Select(fact => fact with { Category = Classify(fact) }).ToList();
        var pastFacts = evidence.Where(f => (IsScheduleFact(f) && f.DueDate < weekOneStart) || (f.IsPlannedOrderReleaseEvidence && f.ReleaseDate < weekOneStart)).ToList();
        var past = BuildBucket(null, null, input.OpeningQoh, pastFacts, input.SafetyStockState, input.SafetyStock);
        var balance = past.ProjectedQoh;
        var weeks = new List<LongTermShortageBucket>(WeekCount);
        for (var i = 0; i < WeekCount; i++)
        {
            var start = weekOneStart.AddDays(i * 7);
            var facts = evidence.Where(f => (IsScheduleFact(f) && f.DueDate >= start && f.DueDate < start.AddDays(7)) || (f.IsPlannedOrderReleaseEvidence && f.ReleaseDate >= start && f.ReleaseDate < start.AddDays(7))).ToList();
            var bucket = BuildBucket(i + 1, start, balance, facts, input.SafetyStockState, input.SafetyStock);
            weeks.Add(bucket);
            balance = bucket.ProjectedQoh;
        }

        var first = weeks.FirstOrDefault(w => w.Severity is LongTermShortageSeverity.CriticalShort or LongTermShortageSeverity.SafetyStockShort);
        var severity = input.SafetyStockState == SafetyStockState.SelectedSiteValueMissing ? LongTermShortageSeverity.SafetyStockUnavailable
            : weeks.Any(w => w.Severity == LongTermShortageSeverity.CriticalShort) ? LongTermShortageSeverity.CriticalShort
            : first is not null ? LongTermShortageSeverity.SafetyStockShort : LongTermShortageSeverity.None;
        return new(input.ComponentPart, input.UnitOfMeasure, input.QadStatus, input.Description, input.Planner, input.BuyerPlannerCode,
            input.OpeningQoh, input.SafetyStockState, input.SafetyStock, severity, first?.WeekStart, input.DemandParentParts, past, weeks, evidence, input.Presentation);
    }

    private static LongTermShortageBucket BuildBucket(int? weekNumber, DateOnly? weekStart, decimal priorBalance, IReadOnlyList<LongTermMrpFact> facts, SafetyStockState state, decimal? safety) {
        var gross = facts.Where(f => f.Category == MrpScheduleCategory.GrossRequirement).Sum(f => f.Quantity);
        var receipts = facts.Where(f => f.Category == MrpScheduleCategory.ScheduledReceipt).Sum(f => f.Quantity);
        var plannedDue = facts.Where(f => f.Category == MrpScheduleCategory.PlannedOrderDue).Sum(f => f.Quantity);
        var releases = facts.Where(f => f.IsPlannedOrderReleaseEvidence).Sum(f => f.Quantity);
        var balance = priorBalance + receipts + plannedDue - gross;
        return new(weekNumber, weekStart, gross, receipts, plannedDue, releases, balance, ClassifyBalance(balance, state, safety));
    }

    private static bool IsScheduleFact(LongTermMrpFact fact) => fact.Category is MrpScheduleCategory.GrossRequirement or MrpScheduleCategory.ScheduledReceipt or MrpScheduleCategory.PlannedOrderDue;
    private static MrpScheduleCategory Classify(LongTermMrpFact fact) {
        var type = fact.Type?.Trim().ToUpperInvariant();
        if (type?.StartsWith("DEMAND", StringComparison.Ordinal) == true && fact.DueDate is not null) return MrpScheduleCategory.GrossRequirement;
        if (type == "SUPPLY" && fact.DueDate is not null) return MrpScheduleCategory.ScheduledReceipt;
        if (type == "SUPPLYP" && fact.DueDate is not null) return MrpScheduleCategory.PlannedOrderDue;
        if (type == "SUPPLYP" && fact.ReleaseDate is not null) return MrpScheduleCategory.PlannedOrderRelease;
        return MrpScheduleCategory.Unclassified;
    }
    private static LongTermShortageSeverity ClassifyBalance(decimal balance, SafetyStockState state, decimal? safety) =>
        state == SafetyStockState.SelectedSiteValueMissing || safety is null ? LongTermShortageSeverity.SafetyStockUnavailable :
        balance < 0 ? LongTermShortageSeverity.CriticalShort : balance < safety ? LongTermShortageSeverity.SafetyStockShort : LongTermShortageSeverity.None;
}
