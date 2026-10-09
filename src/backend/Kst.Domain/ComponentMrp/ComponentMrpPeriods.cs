using Kst.Domain.LongTermShortages;

namespace Kst.Domain.ComponentMrp;

public sealed record ComponentMrpPeriod(string Label, DateOnly? Start, DateOnly? EndExclusive,
    decimal Demand, decimal Receipts, decimal ProjectedQoh, decimal PlannedDue, decimal PlannedRelease, bool HasUnconfirmed);

/// <summary>Stage18 presentation regrouping of the locked builder's classified evidence, not planning/netting.</summary>
public static class ComponentMrpPeriods
{
    public static IReadOnlyList<ComponentMrpPeriod> Build(DateOnly asOf, LongTermShortageRow row, bool include, string period)
    {
        if (period is not ("Day" or "Week" or "Month")) throw new ArgumentException("Unknown report period.");
        var end = LongTermShortagesBuilder.GetWeekOneStart(asOf).AddDays(row.Weeks.Count * 7);
        var facts = row.Evidence; // Classification, completeness guards and raw-row identity mapping already applied.
        bool Unconfirmed(DateOnly? start, DateOnly stop) => include && facts.Any(f => f.Category == MrpScheduleCategory.ScheduledReceipt
            && f.IsPoReceipt == true && f.PoConfirmed == false && f.DueDate < stop && (start is null || f.DueDate >= start));
        var result = new List<ComponentMrpPeriod> { new("Past Due", null, asOf, row.Past.GrossRequirements,
            row.Past.ScheduledReceipts + (include ? row.Past.UnconfirmedReceipts : 0), row.Past.ProjectedQoh,
            row.Past.PlannedOrdersDue, row.Past.PlannedOrdersRelease, Unconfirmed(null, asOf)) };
        if (period == "Week")
        {
            foreach (var b in row.Weeks)
            {
                var start = b.WeekStart!.Value; var stop = start.AddDays(7);
                result.Add(new(start.AddDays(1).ToString("yyyy-MM-dd"), start < asOf ? asOf : start, stop, b.GrossRequirements,
                    b.ScheduledReceipts + (include ? b.UnconfirmedReceipts : 0), b.ProjectedQoh, b.PlannedOrdersDue, b.PlannedOrdersRelease,
                    Unconfirmed(start < asOf ? asOf : start, stop)));
            }
            return result.AsReadOnly();
        }
        // Accumulate due/release flows in evidence order before rounding. Daily selected balance
        // follows the exact Stage11 demand-first, firm+confirmed(+unconfirmed) addition order.
        var days = end.DayNumber - asOf.DayNumber;
        var demand = new decimal[days]; var firm = new decimal[days]; var confirmed = new decimal[days]; var unconfirmed = new decimal[days];
        foreach (var f in facts)
        {
            if (f.DueDate is not { } due || due < asOf || due >= end) continue;
            var i = due.DayNumber - asOf.DayNumber;
            if (f.Category == MrpScheduleCategory.GrossRequirement) demand[i] += f.Quantity;
            if (f.Category != MrpScheduleCategory.ScheduledReceipt) continue;
            if (f.IsPoReceipt != true) firm[i] += f.Quantity;
            else if (f.PoConfirmed == true) confirmed[i] += f.Quantity;
            else if (f.PoConfirmed == false) unconfirmed[i] += f.Quantity;
        }
        var ending = new decimal[days]; var balance = row.Past.ProjectedQoh;
        for (var i = 0; i < days; i++) { balance -= demand[i]; balance += include ? firm[i] + confirmed[i] + unconfirmed[i] : firm[i] + confirmed[i]; ending[i] = balance; }
        for (var start = asOf; start < end;)
        {
            var naturalEnd = period == "Day" ? start.AddDays(1) : new DateOnly(start.Year, start.Month, 1).AddMonths(1);
            var stop = naturalEnd < end ? naturalEnd : end;
            var dueFacts = facts.Where(f => f.DueDate >= start && f.DueDate < stop).ToArray();
            var receipts = dueFacts.Where(f => f.Category == MrpScheduleCategory.ScheduledReceipt &&
                (f.IsPoReceipt != true || f.PoConfirmed == true || include && f.PoConfirmed == false)).Sum(f => f.Quantity);
            var label = period == "Day" ? start.ToString("yyyy-MM-dd") : start.ToString("yyyy-MM") + (start.Day != 1 || stop != naturalEnd ? " *" : "");
            result.Add(new(label, start, stop, dueFacts.Where(f => f.Category == MrpScheduleCategory.GrossRequirement).Sum(f => f.Quantity), receipts,
                ending[stop.DayNumber - asOf.DayNumber - 1], dueFacts.Where(f => f.Category == MrpScheduleCategory.PlannedOrderDue).Sum(f => f.Quantity),
                facts.Where(f => f.IsPlannedOrderReleaseEvidence && f.ReleaseDate >= start && f.ReleaseDate < stop).Sum(f => f.Quantity), Unconfirmed(start, stop)));
            start = stop;
        }
        return result.AsReadOnly();
    }
}
