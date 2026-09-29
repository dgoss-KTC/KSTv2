using Kst.Domain.LongTermShortages;

// Offline prototypes only. Neither implementation is referenced by the application.
// Separate evidence-order sums are intentional: decimal addition is not freely associative.
static class ProjectionAlternatives
{
    public static (IReadOnlyList<LongTermShortageRow> Confirmed, IReadOnlyList<LongTermShortageRow> All) Build(
        DateOnly asOf, IReadOnlyList<LongTermShortageInput> inputs, bool sparse, int weeks = 26)
    {
        var pairs = inputs.Select(input => BuildRow(input, asOf, weeks, sparse)).ToList();
        IReadOnlyList<LongTermShortageRow> Sort(IEnumerable<LongTermShortageRow> rows) => rows
            .OrderBy(r => r.FirstShortDate ?? DateOnly.MaxValue).ThenBy(r => r.ComponentPart, StringComparer.OrdinalIgnoreCase).ToList();
        return (Sort(pairs.Select(p => p.Confirmed)), Sort(pairs.Select(p => p.All)));
    }

    private sealed class Totals
    {
        public decimal Demand, Firm, ConfirmedPo, Unconfirmed, Planned, Release, ConfirmedReceipts, AllReceipts;
        public void Add(LongTermMrpFact fact)
        {
            switch (fact.Category)
            {
                case MrpScheduleCategory.GrossRequirement: Demand += fact.Quantity; break;
                case MrpScheduleCategory.PlannedOrderDue: Planned += fact.Quantity; break;
                case MrpScheduleCategory.ScheduledReceipt:
                    if (fact.IsPoReceipt != true) Firm += fact.Quantity;
                    else if (fact.PoConfirmed == true) ConfirmedPo += fact.Quantity;
                    else if (fact.PoConfirmed == false) Unconfirmed += fact.Quantity;
                    if (fact.IsPoReceipt != true || fact.PoConfirmed == true) ConfirmedReceipts += fact.Quantity;
                    if (fact.IsPoReceipt != true || fact.PoConfirmed == true || fact.PoConfirmed == false) AllReceipts += fact.Quantity;
                    break;
            }
        }
    }

    private sealed class Mode(decimal opening, DateOnly asOf, Func<decimal, decimal> evaluate, Func<decimal, LongTermShortageSeverity> status)
    {
        public DateOnly? FirstShort = evaluate(opening) < 0 ? asOf : null;
        public DateOnly? FirstRisk = status(opening) == LongTermShortageSeverity.SafetyStockShort ? asOf : null;
        private DateOnly? start = evaluate(opening) < 0 ? asOf : null;
        private DateOnly? deepestDate = evaluate(opening) < 0 ? asOf : null;
        private decimal deepest = evaluate(opening) < 0 ? opening : 0;
        public readonly List<LongTermShortageEpisode> Episodes = [];
        public readonly List<LongTermShortageBucket> Buckets = [];
        public void Low(DateOnly day, decimal low)
        {
            if (status(low) == LongTermShortageSeverity.SafetyStockShort) FirstRisk ??= day;
            if (evaluate(low) >= 0) return;
            FirstShort ??= day;
            start ??= day;
            if (low < deepest) { deepest = low; deepestDate = day; }
        }
        public void End(DateOnly day, decimal ending)
        {
            if (status(ending) == LongTermShortageSeverity.SafetyStockShort) FirstRisk ??= day;
            if (start is null || evaluate(ending) < 0) return;
            Episodes.Add(new(start.Value, deepestDate!.Value, -deepest, day, day));
            start = null; deepestDate = null; deepest = 0;
        }
        public void Finish()
        {
            if (start is not null) Episodes.Add(new(start.Value, deepestDate!.Value, -deepest, null, null));
            for (var i = 0; i + 1 < Episodes.Count; i++) Episodes[i] = Episodes[i] with { StableClearDate = null };
        }
    }

    private static (LongTermShortageRow Confirmed, LongTermShortageRow All) BuildRow(LongTermShortageInput input, DateOnly asOf, int weeks, bool sparse)
    {
        var weekStart = LongTermShortagesBuilder.GetWeekOneStart(asOf);
        var end = weekStart.AddDays(weeks * 7);
        var days = end.DayNumber - asOf.DayNumber;
        var uom = input.UnitOfMeasure?.Trim().ToUpperInvariant();
        decimal Evaluate(decimal value) => LongTermQuantityPresentation.Round(value, uom);
        LongTermShortageSeverity Status(decimal value) => Evaluate(value) < 0 ? LongTermShortageSeverity.CriticalShort
            : input.SafetyStock is null ? LongTermShortageSeverity.SafetyStockUnavailable
            : input.SafetyStock > 0 && Evaluate(value) < Evaluate(input.SafetyStock.Value) ? LongTermShortageSeverity.SafetyStockShort : LongTermShortageSeverity.Healthy;
        var evidence = input.Evidence.Select(f => f with { Category = Classify(f) }).ToList();
        if (evidence.Any(f => f.Category == MrpScheduleCategory.ScheduledReceipt && f.IsPoReceipt == true && f.PoConfirmed is null))
            throw new InvalidOperationException("PO receipt confirmation is unknown; a projection cannot be computed.");
        if (evidence.Any(f => (string.Equals(f.Type?.Trim(), "SUPPLY", StringComparison.OrdinalIgnoreCase)
                || f.Type?.Trim().StartsWith("DEMAND", StringComparison.OrdinalIgnoreCase) == true) && f.DueDate is null))
            throw new InvalidOperationException("A component MRP event has no due date; a projection cannot be computed.");
        var pastDemand = evidence.Where(f => f.Category == MrpScheduleCategory.GrossRequirement && f.DueDate < asOf).Sum(f => f.Quantity);
        var overdue = evidence.Where(f => f.DueDate < asOf && f.Category is (MrpScheduleCategory.ScheduledReceipt or MrpScheduleCategory.PlannedOrderDue)).ToList();
        var opening = input.OpeningQoh - pastDemand;
        var pastReceipts = overdue.Where(f => f.Category == MrpScheduleCategory.ScheduledReceipt).ToList();
        var past = new LongTermShortageBucket(null, null, pastDemand,
            pastReceipts.Where(f => f.IsPoReceipt != true || f.PoConfirmed == true).Sum(f => f.Quantity),
            overdue.Where(f => f.Category == MrpScheduleCategory.PlannedOrderDue).Sum(f => f.Quantity),
            evidence.Where(f => f.IsPlannedOrderReleaseEvidence && f.ReleaseDate < asOf).Sum(f => f.Quantity), opening, Status(opening))
        { UnconfirmedReceipts = pastReceipts.Where(f => f.IsPoReceipt == true && f.PoConfirmed == false).Sum(f => f.Quantity),
          ConfirmedEnding = opening, AllReceiptsEnding = opening, PlanningEnding = opening, AllReceiptsPlanningEnding = opening,
          OverdueReceipts = overdue.Sum(f => f.Quantity), LowestProjectedBalance = opening, LowestConfirmedBalance = opening, LowestAllReceiptsBalance = opening };

        var dense = sparse ? null : Enumerable.Range(0, days).Select(_ => new Totals()).ToArray();
        var events = sparse ? new Dictionary<int, Totals>() : null;
        var weekly = Enumerable.Range(0, weeks).Select(_ => new Totals()).ToArray();
        var empty = new Totals();
        Totals At(int i) => dense is not null ? dense[i] : events!.GetValueOrDefault(i) ?? empty;
        // Accumulate in original evidence order, independently for each daily/weekly expression.
        foreach (var fact in evidence)
        {
            if (fact.DueDate is { } due && due >= asOf && due < end)
            {
                var i = due.DayNumber - asOf.DayNumber;
                if (events is not null && !events.ContainsKey(i)) events.Add(i, new Totals());
                At(i).Add(fact);
                weekly[(due.DayNumber - weekStart.DayNumber) / 7].Add(fact);
            }
            if (fact.IsPlannedOrderReleaseEvidence && fact.ReleaseDate is { } release && release >= asOf && release < end)
                weekly[(release.DayNumber - weekStart.DayNumber) / 7].Release += fact.Quantity;
        }
        IEnumerable<int> indices = Enumerable.Range(0, days);
        if (sparse)
        {
            // Next-day observation preserves the existing ledger even for negative receipt values.
            // Saturday boundaries retain every weekly bucket; asOf retains opening status.
            indices = events!.Keys.SelectMany(i => new[] { i, i + 1 }).Where(i => i < days)
                .Concat(Enumerable.Range(0, weeks).Select(w => weekStart.AddDays(w * 7 + 6).DayNumber - asOf.DayNumber))
                .Append(0).Distinct().Order().ToArray();
        }
        var confirmed = opening; var all = opening; var planning = opening; var allPlanning = opening;
        var confirmedMode = new Mode(opening, asOf, Evaluate, Status);
        var allMode = new Mode(opening, asOf, Evaluate, Status);
        foreach (var i in indices)
        {
            var day = asOf.AddDays(i);
            var daily = At(i);
            confirmed -= daily.Demand; all -= daily.Demand; planning -= daily.Demand; allPlanning -= daily.Demand;
            confirmedMode.Low(day, confirmed); allMode.Low(day, all);
            confirmed += daily.Firm + daily.ConfirmedPo;
            all += daily.Firm + daily.ConfirmedPo + daily.Unconfirmed;
            planning += daily.Firm + daily.ConfirmedPo + daily.Planned;
            allPlanning += daily.Firm + daily.ConfirmedPo + daily.Unconfirmed + daily.Planned;
            confirmedMode.End(day, confirmed); allMode.End(day, all);
            if (day.DayOfWeek != DayOfWeek.Saturday) continue;
            var w = (day.DayNumber - weekStart.DayNumber) / 7;
            var sums = weekly[w];
            var start = Math.Max(0, weekStart.AddDays(w * 7).DayNumber - asOf.DayNumber);
            decimal Low(decimal ending, bool include)
            {
                var balance = ending + sums.Demand - (include ? sums.AllReceipts : sums.ConfirmedReceipts);
                var low = balance;
                for (var d = start; d <= i; d++)
                {
                    var value = At(d);
                    balance -= value.Demand;
                    low = Math.Min(low, balance);
                    balance += include ? value.AllReceipts : value.ConfirmedReceipts;
                }
                return low;
            }
            var confirmedLow = Low(confirmed, false); var allLow = Low(all, true);
            LongTermShortageBucket Bucket(bool include) => new(w + 1, weekStart.AddDays(w * 7), sums.Demand,
                sums.ConfirmedReceipts, sums.Planned, sums.Release, include ? all : confirmed,
                Status((include ? allLow : confirmedLow) < 0 ? (include ? allLow : confirmedLow) : (include ? all : confirmed)))
            { UnconfirmedReceipts = sums.Unconfirmed, ConfirmedEnding = confirmed, AllReceiptsEnding = all,
              PlanningEnding = planning, AllReceiptsPlanningEnding = allPlanning, LowestProjectedBalance = include ? allLow : confirmedLow,
              LowestConfirmedBalance = confirmedLow, LowestAllReceiptsBalance = allLow, IncludesUnconfirmed = include };
            confirmedMode.Buckets.Add(Bucket(false)); allMode.Buckets.Add(Bucket(true));
        }
        LongTermShortageRow Row(Mode mode, bool include)
        {
            mode.Finish();
            var severity = mode.FirstShort == asOf ? LongTermShortageSeverity.CriticalShort
                : mode.FirstShort is not null ? LongTermShortageSeverity.FutureShort
                : mode.Buckets.Any(b => b.Severity == LongTermShortageSeverity.SafetyStockShort) || mode.FirstRisk is not null ? LongTermShortageSeverity.SafetyStockShort
                : input.SafetyStock is null ? LongTermShortageSeverity.SafetyStockUnavailable : LongTermShortageSeverity.Healthy;
            return new(input.ComponentPart, uom, input.QadStatus, input.Description, input.Planner, input.BuyerPlannerCode,
                input.OpeningQoh, input.SafetyStockState, input.SafetyStock, severity, mode.FirstShort, input.DemandParentParts,
                past with { IncludesUnconfirmed = include }, mode.Buckets, evidence, input.Presentation)
            { Episodes = mode.Episodes, FirstAtRiskDate = mode.FirstRisk,
              DataQualityWarning = input.SitePlanningPresent == false ? "Selected-site planning data missing; safety stock and lead times unknown." : null,
              EffectivePmCode = input.EffectivePmCode, PartStatusDescription = DescribeStatus(input.QadStatus),
              OrderPeriodDays = input.OrderPeriodDays, SafetyTimeWorkingDays = input.SafetyTimeWorkingDays,
              ManufacturingLeadWorkingDays = input.ManufacturingLeadWorkingDays, PurchasingLeadCalendarDays = input.PurchasingLeadCalendarDays,
              CumulativeLeadCalendarDays = input.CumulativeLeadCalendarDays, SitePlanningPresent = input.SitePlanningPresent };
        }
        return (Row(confirmedMode, false), Row(allMode, true));
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
        "M" => "MFA", "N" => "NPI", "O" => "OBSOLETE", "P" => "PROTO", "Q" => "QUOTED PART", "U" => "UNRELEASED", _ => null
    };
}
