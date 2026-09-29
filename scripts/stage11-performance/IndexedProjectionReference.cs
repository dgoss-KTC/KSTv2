using Kst.Domain.Mps;

namespace Kst.Domain.LongTermShortages;

/// <summary>Daily component ledger; weekly buckets are presentation summaries, not calculation inputs.</summary>
public static class IndexedProjectionReference
{
    public const int WeekCount = 26;
    public static DateOnly GetWeekOneStart(DateOnly date) => MpsBusinessCalendar.GetBusinessWeekStart(date);

    public static IReadOnlyList<LongTermShortageRow> Build(DateOnly asOfDate, IReadOnlyList<LongTermShortageInput> inputs, int weeks = WeekCount, bool includeUnconfirmed = false)
    {
        if (weeks is not (13 or 26 or 52 or 72)) throw new ArgumentOutOfRangeException(nameof(weeks));
        var weekStart = GetWeekOneStart(asOfDate);
        return inputs.Select(input => BuildRow(input, asOfDate, weekStart, weeks, includeUnconfirmed))
            .OrderBy(row => row.FirstShortDate ?? DateOnly.MaxValue)
            .ThenBy(row => row.ComponentPart, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static LongTermShortageRow BuildRow(LongTermShortageInput input, DateOnly asOfDate, DateOnly weekStart, int weeks, bool includeUnconfirmed)
    {
        var uom = input.UnitOfMeasure?.Trim().ToUpperInvariant();
        var warning = input.SitePlanningPresent == false ? "Selected-site planning data missing; safety stock and lead times unknown." : null;
        decimal Evaluate(decimal value) => LongTermQuantityPresentation.Round(value, uom);
        LongTermShortageSeverity Status(decimal value) => Evaluate(value) < 0 ? LongTermShortageSeverity.CriticalShort
            : input.SafetyStock is null ? LongTermShortageSeverity.SafetyStockUnavailable
            : input.SafetyStock > 0 && Evaluate(value) < Evaluate(input.SafetyStock.Value) ? LongTermShortageSeverity.SafetyStockShort
             : LongTermShortageSeverity.Healthy;

        var evidence = input.Evidence.Select(f => f with { Category = Classify(f) }).ToList();
        // Index once; preserve each source event and its original summation order.
        var evidenceByDueDate = evidence.ToLookup(f => f.DueDate);
        if (evidence.Any(f => f.Category == MrpScheduleCategory.ScheduledReceipt && f.IsPoReceipt == true && f.PoConfirmed is null))
            throw new InvalidOperationException("PO receipt confirmation is unknown; a projection cannot be computed.");
        if (evidence.Any(f => (string.Equals(f.Type?.Trim(), "SUPPLY", StringComparison.OrdinalIgnoreCase)
                || f.Type?.Trim().StartsWith("DEMAND", StringComparison.OrdinalIgnoreCase) == true) && f.DueDate is null))
            throw new InvalidOperationException("A component MRP event has no due date; a projection cannot be computed.");
        var pastDemand = evidence.Where(f => f.Category == MrpScheduleCategory.GrossRequirement && f.DueDate < asOfDate).Sum(f => f.Quantity);
        var overdue = evidence.Where(f => f.DueDate < asOfDate && f.Category is (MrpScheduleCategory.ScheduledReceipt or MrpScheduleCategory.PlannedOrderDue)).ToList();
        var opening = input.OpeningQoh - pastDemand;
        var pastReceipts = overdue.Where(f => f.Category == MrpScheduleCategory.ScheduledReceipt).ToList();
        var past = new LongTermShortageBucket(null, null, pastDemand,
            pastReceipts.Where(f => f.IsPoReceipt != true || f.PoConfirmed == true).Sum(f => f.Quantity),
            overdue.Where(f => f.Category == MrpScheduleCategory.PlannedOrderDue).Sum(f => f.Quantity),
            evidence.Where(f => f.IsPlannedOrderReleaseEvidence && f.ReleaseDate < asOfDate).Sum(f => f.Quantity), opening, Status(opening))
        { UnconfirmedReceipts = pastReceipts.Where(f => f.IsPoReceipt == true && f.PoConfirmed == false).Sum(f => f.Quantity),
          ConfirmedEnding = opening, AllReceiptsEnding = opening, PlanningEnding = opening, AllReceiptsPlanningEnding = opening,
          OverdueReceipts = overdue.Sum(f => f.Quantity), LowestProjectedBalance = opening, LowestConfirmedBalance = opening,
          LowestAllReceiptsBalance = opening, IncludesUnconfirmed = includeUnconfirmed };
        var confirmed = opening;
        var all = opening;
        var confirmedPlanning = opening;
        var allPlanning = opening;
        var buckets = new List<LongTermShortageBucket>(weeks);
        var episodes = new List<LongTermShortageEpisode>();
        DateOnly? episodeStart = Evaluate(opening) < 0 ? asOfDate : null, deepestDate = Evaluate(opening) < 0 ? asOfDate : null;
        decimal deepest = 0;
        if (episodeStart is not null) deepest = opening;
        DateOnly? firstShort = episodeStart is not null ? asOfDate : null;
        var firstAtRisk = (DateOnly?)null;
        var everAtRisk = false;
        if (Status(opening) == LongTermShortageSeverity.SafetyStockShort) { firstAtRisk = asOfDate; everAtRisk = true; }
        var firstWeekDays = 7 - (asOfDate.DayNumber - weekStart.DayNumber);
        for (var i = 0; i < firstWeekDays + (weeks - 1) * 7; i++)
        {
            var day = asOfDate.AddDays(i);
            var daily = evidenceByDueDate[day];
            var demand = daily.Where(f => f.Category == MrpScheduleCategory.GrossRequirement).Sum(f => f.Quantity);
            var firm = daily.Where(f => f.Category == MrpScheduleCategory.ScheduledReceipt && f.IsPoReceipt != true).Sum(f => f.Quantity);
            var confirmedPo = daily.Where(f => f.Category == MrpScheduleCategory.ScheduledReceipt && f.IsPoReceipt == true && f.PoConfirmed == true).Sum(f => f.Quantity);
            var unconfirmedPo = daily.Where(f => f.Category == MrpScheduleCategory.ScheduledReceipt && f.IsPoReceipt == true && f.PoConfirmed == false).Sum(f => f.Quantity);
            var planned = daily.Where(f => f.Category == MrpScheduleCategory.PlannedOrderDue).Sum(f => f.Quantity);
            confirmed -= demand;
            all -= demand;
            confirmedPlanning -= demand;
            allPlanning -= demand;
            var low = includeUnconfirmed ? all : confirmed;
            if (Status(low) == LongTermShortageSeverity.SafetyStockShort) { firstAtRisk ??= day; everAtRisk = true; }
            // Demand-first lows are action signals even when receipts recover by close of day.
            if (Evaluate(low) < 0 && firstShort is null) firstShort = day;
            if (Evaluate(low) < 0)
            {
                episodeStart ??= day;
                if (low < deepest) { deepest = low; deepestDate = day; }
            }
            confirmed += firm + confirmedPo;
            all += firm + confirmedPo + unconfirmedPo;
            confirmedPlanning += firm + confirmedPo + planned;
            allPlanning += firm + confirmedPo + unconfirmedPo + planned;
            if (Status(includeUnconfirmed ? all : confirmed) == LongTermShortageSeverity.SafetyStockShort) { firstAtRisk ??= day; everAtRisk = true; }
            if (episodeStart is not null && Evaluate(includeUnconfirmed ? all : confirmed) >= 0)
            {
                episodes.Add(new(episodeStart.Value, deepestDate!.Value, -deepest, day, day));
                episodeStart = null; deepestDate = null; deepest = 0;
            }
            if (day.DayOfWeek != DayOfWeek.Saturday) continue;
            var bucketStart = GetWeekOneStart(day);
            var ledgerStart = bucketStart < asOfDate ? asOfDate : bucketStart;
            var weekFacts = evidence.Where(f => f.DueDate >= ledgerStart && f.DueDate <= day).ToList();
            var selectedEnding = includeUnconfirmed ? all : confirmed;
            var confirmedLow = CalculateWeeklyLow(evidence, ledgerStart, confirmed, day, false);
            var allLow = CalculateWeeklyLow(evidence, ledgerStart, all, day, true);
            var selectedLow = includeUnconfirmed ? allLow : confirmedLow;
            var bucket = new LongTermShortageBucket(buckets.Count + 1, bucketStart,
                weekFacts.Where(f => f.Category == MrpScheduleCategory.GrossRequirement).Sum(f => f.Quantity),
                weekFacts.Where(f => f.Category == MrpScheduleCategory.ScheduledReceipt && (f.IsPoReceipt != true || f.PoConfirmed == true)).Sum(f => f.Quantity),
                weekFacts.Where(f => f.Category == MrpScheduleCategory.PlannedOrderDue).Sum(f => f.Quantity),
                evidence.Where(f => f.IsPlannedOrderReleaseEvidence && f.ReleaseDate >= ledgerStart && f.ReleaseDate <= day).Sum(f => f.Quantity),
                selectedEnding, Status(selectedLow < 0 ? selectedLow : selectedEnding))
            {
                UnconfirmedReceipts = weekFacts.Where(f => f.IsPoReceipt == true && f.PoConfirmed == false && f.Category == MrpScheduleCategory.ScheduledReceipt).Sum(f => f.Quantity),
                ConfirmedEnding = confirmed, AllReceiptsEnding = all, PlanningEnding = confirmedPlanning,
                AllReceiptsPlanningEnding = allPlanning,
                LowestProjectedBalance = selectedLow,
                LowestConfirmedBalance = confirmedLow,
                LowestAllReceiptsBalance = allLow,
                IncludesUnconfirmed = includeUnconfirmed
            };
            buckets.Add(bucket);
        }
        if (episodeStart is not null) episodes.Add(new(episodeStart.Value, deepestDate!.Value, -deepest, null, null));
        for (var index = 0; index + 1 < episodes.Count; index++)
            episodes[index] = episodes[index] with { StableClearDate = null };
        var severity = firstShort == asOfDate ? LongTermShortageSeverity.CriticalShort
            : firstShort is not null ? LongTermShortageSeverity.FutureShort
            : buckets.Any(b => b.Severity == LongTermShortageSeverity.SafetyStockShort) || everAtRisk ? LongTermShortageSeverity.SafetyStockShort
            : input.SafetyStock is null ? LongTermShortageSeverity.SafetyStockUnavailable : LongTermShortageSeverity.Healthy;
        return new(input.ComponentPart, uom, input.QadStatus, input.Description, input.Planner, input.BuyerPlannerCode,
            input.OpeningQoh, input.SafetyStockState, input.SafetyStock, severity, firstShort, input.DemandParentParts,
            past, buckets, evidence, input.Presentation)
        {
            Episodes = episodes, DataQualityWarning = warning, FirstAtRiskDate = firstAtRisk,
            EffectivePmCode = input.EffectivePmCode, PartStatusDescription = DescribeStatus(input.QadStatus),
            OrderPeriodDays = input.OrderPeriodDays, SafetyTimeWorkingDays = input.SafetyTimeWorkingDays,
            ManufacturingLeadWorkingDays = input.ManufacturingLeadWorkingDays,
            PurchasingLeadCalendarDays = input.PurchasingLeadCalendarDays,
            CumulativeLeadCalendarDays = input.CumulativeLeadCalendarDays, SitePlanningPresent = input.SitePlanningPresent
        };
    }

    private static decimal CalculateWeeklyLow(IReadOnlyList<LongTermMrpFact> evidence, DateOnly start, decimal ending, DateOnly end, bool includeUnconfirmed)
    {
        // Reconstruct from the ending balance so the low includes post-demand, pre-receipt positions.
        var week = evidence.Where(f => f.DueDate >= start && f.DueDate <= end).ToList();
        var balance = ending + week.Where(f => f.Category == MrpScheduleCategory.GrossRequirement).Sum(f => f.Quantity)
            - week.Where(f => f.Category == MrpScheduleCategory.ScheduledReceipt && (f.IsPoReceipt != true || f.PoConfirmed == true || includeUnconfirmed && f.PoConfirmed == false)).Sum(f => f.Quantity);
        var low = balance;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var today = week.Where(f => f.DueDate == date).ToList();
            balance -= today.Where(f => f.Category == MrpScheduleCategory.GrossRequirement).Sum(f => f.Quantity);
            low = Math.Min(low, balance);
            balance += today.Where(f => f.Category == MrpScheduleCategory.ScheduledReceipt && (f.IsPoReceipt != true || f.PoConfirmed == true || includeUnconfirmed && f.PoConfirmed == false)).Sum(f => f.Quantity);
        }
        return low;
    }

    private static MrpScheduleCategory Classify(LongTermMrpFact fact) => fact.Type?.Trim().ToUpperInvariant() switch
    {
        var type when type is not null && type.StartsWith("DEMAND", StringComparison.Ordinal) && fact.DueDate is not null => MrpScheduleCategory.GrossRequirement,
        "SUPPLY" when fact.DueDate is not null => MrpScheduleCategory.ScheduledReceipt,
        "SUPPLYP" when fact.DueDate is not null => MrpScheduleCategory.PlannedOrderDue,
        "SUPPLYP" when fact.ReleaseDate is not null => MrpScheduleCategory.PlannedOrderRelease,
        _ => MrpScheduleCategory.Unclassified
    };

    private static string? DescribeStatus(string? code) => code?.Trim().ToUpperInvariant() switch
    {
        "A" => "AEMR", "B" => "BYPASS", "C" => "CURRENTLY IN PRODUCTION", "E" => "END OF LIFE",
        "F" => "FORECAST OR FAMILY BORN", "H" => "PURCHASING HOLD", "I" => "INACTIVE PURCHASED PART",
        "M" => "MFA", "N" => "NPI", "O" => "OBSOLETE", "P" => "PROTO", "Q" => "QUOTED PART", "U" => "UNRELEASED",
        _ => null
    };
}
