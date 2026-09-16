using Kst.Domain.Mps;

namespace Kst.Domain.LongTermShortages;

/// <summary>Pure Stage 11-A weekly site-wide projection. Source qualification belongs outside this builder.</summary>
public static class LongTermShortagesBuilder
{
    public const int WeekCount = 24;

    public static IReadOnlyList<LongTermShortageRow> Build(DateOnly refreshDate, IReadOnlyList<LongTermShortageInput> inputs)
    {
        var weekOneStart = MpsBusinessCalendar.GetBusinessWeekStart(refreshDate);
        var horizonEnd = weekOneStart.AddDays(WeekCount * 7);
        return inputs.Select(input => BuildRow(input, weekOneStart, horizonEnd))
            .OrderBy(row => row.FirstSafetyStockShortWeek ?? int.MaxValue)
            .ThenBy(row => row.ComponentPart, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static LongTermShortageRow BuildRow(LongTermShortageInput input, DateOnly weekOneStart, DateOnly horizonEnd)
    {
        var balance = input.OpeningQoh;
        var weeks = new List<LongTermShortageWeek>(WeekCount);
        for (var index = 0; index < WeekCount; index++)
        {
            var start = weekOneStart.AddDays(index * 7);
            var end = start.AddDays(7);
            var workOrderDemand = input.DemandEvents.Where(e => !e.IsForecast && (e.DueDate < weekOneStart ? index == 0 : e.DueDate >= start && e.DueDate < end)).Sum(e => e.Quantity);
            var forecastDemand = input.DemandEvents.Where(e => e.IsForecast && e.DueDate >= start && e.DueDate < end && e.DueDate < horizonEnd).Sum(e => e.Quantity);
            var supply = input.PurchaseOrders.Where(po => po.Confirmed == true && !po.IsScheduled && po.DueDate >= start && po.DueDate < end).Sum(po => po.OpenQuantity);
            balance += supply - workOrderDemand - forecastDemand;
            weeks.Add(new LongTermShortageWeek(index + 1, start, workOrderDemand, forecastDemand, supply, balance, Classify(balance, input.SafetyStockState, input.SafetyStock)));
        }

        var firstSafety = weeks.FirstOrDefault(w => w.Severity is LongTermShortageSeverity.SafetyStockShort or LongTermShortageSeverity.CriticalShort)?.WeekNumber;
        var firstCritical = weeks.FirstOrDefault(w => w.Severity == LongTermShortageSeverity.CriticalShort)?.WeekNumber;
        var severity = input.SafetyStockState == SafetyStockState.SelectedSiteValueMissing
            ? LongTermShortageSeverity.SafetyStockUnavailable
            : firstCritical.HasValue ? LongTermShortageSeverity.CriticalShort
            : firstSafety.HasValue ? LongTermShortageSeverity.SafetyStockShort
            : LongTermShortageSeverity.None;

        return new LongTermShortageRow(input.ComponentPart, input.UnitOfMeasure, input.QadStatus, input.Description, input.IsKss,
            input.LeadTimeDays is null ? null : (int)Math.Ceiling(input.LeadTimeDays.Value / 7m), input.Planner,
            input.BuyerPlannerCode, input.OpeningQoh, input.SafetyStockState, input.SafetyStock, severity,
            firstSafety, firstCritical, input.DemandParentParts, input.OtherProgramParentParts, input.PurchaseOrders, weeks);
    }

    private static LongTermShortageSeverity Classify(decimal balance, SafetyStockState safetyStockState, decimal? safetyStock) =>
        safetyStockState == SafetyStockState.SelectedSiteValueMissing || safetyStock is null ? LongTermShortageSeverity.SafetyStockUnavailable :
        balance < 0 ? LongTermShortageSeverity.CriticalShort :
        balance < safetyStock.Value ? LongTermShortageSeverity.SafetyStockShort : LongTermShortageSeverity.None;
}
