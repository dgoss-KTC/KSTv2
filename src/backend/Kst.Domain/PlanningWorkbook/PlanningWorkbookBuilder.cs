using Kst.Domain.Mps;
using Kst.Domain.OpenOrders;

namespace Kst.Domain.PlanningWorkbook;

/// <summary>
/// Pure Planning Workbook calculation core. Uses Due-Date bucketing on the accepted Sunday-Saturday
/// business calendar (Monday labels) for MPS, sales orders and forecast alike. MPS quantities come
/// only from the accepted MPS projection (<see cref="MpsScheduleBuilder"/>, Due-Date basis); no other
/// supply source is added. Rows beyond the horizon are not shown. Fiscal-quarter grouping is
/// frontend-owned and is not calculated here.
/// </summary>
public static class PlanningWorkbookBuilder
{
    /// <summary>Workbook-only RMA exclusion: sales order numbers beginning with <c>RA</c> (ordinal).</summary>
    public static bool IsExcludedReturnOrder(OpenOrderLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        return line.Key.SalesOrder.StartsWith("RA", StringComparison.Ordinal);
    }

    public static PlanningWorkbook Build(PlanningWorkbookInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.HorizonWeeks <= 0)
            throw new ArgumentOutOfRangeException(nameof(input), "Horizon must be positive.");

        var comparer = StringComparer.OrdinalIgnoreCase;
        var currentWeekStart = MpsBusinessCalendar.GetBusinessWeekStart(input.Today);
        var weekLabels = Enumerable.Range(0, input.HorizonWeeks)
            .Select(i => MpsBusinessCalendar.GetWeekLabel(currentWeekStart.AddDays(7 * i)))
            .ToList();
        var weekIndex = weekLabels.Select((label, i) => (label, i)).ToDictionary(x => x.label, x => x.i);

        var netQoh = new Dictionary<string, decimal>(input.NetQuantityOnHand, comparer);
        var tiers = new Dictionary<string, IReadOnlyList<Kst.Domain.PartDetail.PartPriceBreak>>(input.PriceTiers, comparer);

        var schedules = MpsScheduleBuilder.Build(
            input.ResolvedParts, input.MpsRows, MpsDateBasis.DueDate, input.HorizonWeeks, input.Today);

        var adjustments = new Dictionary<(string Part, DateOnly Week), decimal>(PartWeekComparer.Instance);
        foreach (var adjustment in input.Adjustments)
        {
            if (!adjustments.TryAdd((adjustment.Part, adjustment.WeekLabel), adjustment.Adjustment))
                throw new ArgumentException(
                    "Duplicate adjustment identity for a part and week.", nameof(input));
        }

        var salesByPart = input.SalesOrderLines
            .Where(l => !IsExcludedReturnOrder(l))
            .GroupBy(l => l.ItemNumber, comparer)
            .ToDictionary(g => g.Key, g => g.ToList(), comparer);
        var forecastByPart = input.ForecastRows
            .GroupBy(f => f.Part, comparer)
            .ToDictionary(g => g.Key, g => g.ToList(), comparer);

        var blocks = new List<PlanningPartBlock>(schedules.Count);
        foreach (var schedule in schedules)
        {
            var part = schedule.ParentPart;
            if (!netQoh.TryGetValue(part, out var opening))
                throw new ArgumentException($"Net QOH is missing for a resolved part.", nameof(input));
            if (!tiers.TryGetValue(part, out var partTiers))
                throw new ArgumentException($"Price tiers are missing for a resolved part.", nameof(input));
            var price = PlanningPriceSelection.Select(partTiers);

            var slots = weekLabels.Count + 1; // index 0 = Falldown, 1..n = weeks
            var so = new decimal[slots];
            var soDollars = new decimal[slots];
            var forecast = new decimal[slots];
            var undatedQty = 0m;
            var undatedDollars = 0m;
            var undatedCount = 0;

            foreach (var line in salesByPart.GetValueOrDefault(part) ?? [])
            {
                if (line.SourceValues.DueDate is not { } due)
                {
                    undatedQty += line.Open;
                    undatedDollars += line.ExtPrice;
                    undatedCount++;
                    continue;
                }
                if (Slot(due) is { } s)
                {
                    so[s] += line.Open;
                    soDollars[s] += line.ExtPrice;
                }
            }

            foreach (var row in forecastByPart.GetValueOrDefault(part) ?? [])
            {
                if (Slot(row.DueDate) is { } s)
                    forecast[s] += Math.Abs(row.Quantity);
            }

            var mpsFalldown = schedule.Buckets.Single(b => b.Kind == MpsBucketKind.Falldown).Quantity;
            var seed = opening + mpsFalldown - so[0];
            var falldown = Bucket(MpsBucketKind.Falldown, null, so[0], soDollars[0], mpsFalldown, forecast[0], 0m, price, seed, seed);

            var estimated = seed;
            var adjusted = seed;
            var weeks = new List<PlanningBucket>(weekLabels.Count);
            foreach (var mpsBucket in schedule.Buckets.Where(b => b.Kind == MpsBucketKind.Weekly))
            {
                var label = mpsBucket.WeekLabel!.Value;
                var s = weekIndex[label] + 1;
                var adjustment = adjustments.GetValueOrDefault((part, label));
                estimated = estimated + mpsBucket.Quantity - so[s];
                adjusted = adjusted + mpsBucket.Quantity + adjustment - so[s];
                weeks.Add(Bucket(MpsBucketKind.Weekly, label, so[s], soDollars[s], mpsBucket.Quantity, forecast[s], adjustment, price, estimated, adjusted));
            }

            blocks.Add(new PlanningPartBlock(
                part, schedule.Description, opening, partTiers, price, falldown, weeks,
                new PlanningUndatedDemand(undatedQty, undatedDollars, undatedCount)));
        }

        return new PlanningWorkbook(input.Today, weekLabels, blocks);

        int? Slot(DateOnly date)
        {
            if (MpsBusinessCalendar.IsFalldown(date, input.Today))
                return 0;
            var label = MpsBusinessCalendar.GetWeekLabel(MpsBusinessCalendar.GetBusinessWeekStart(date));
            return weekIndex.TryGetValue(label, out var i) ? i + 1 : null;
        }
    }

    private static PlanningBucket Bucket(
        MpsBucketKind kind, DateOnly? label, decimal so, decimal soDollars, decimal mps, decimal forecast,
        decimal adjustment, decimal? price, decimal estimated, decimal adjusted)
    {
        var proposed = mps + adjustment;
        return new PlanningBucket(
            kind, label, so, soDollars, mps, mps * price, forecast, adjustment, proposed, proposed * price,
            estimated, adjusted);
    }
}

/// <summary>Part (case-insensitive, as MPS) + week identity comparer.</summary>
internal sealed class PartWeekComparer : IEqualityComparer<(string Part, DateOnly Week)>
{
    public static readonly PartWeekComparer Instance = new();

    public bool Equals((string Part, DateOnly Week) x, (string Part, DateOnly Week) y) =>
        StringComparer.OrdinalIgnoreCase.Equals(x.Part, y.Part) && x.Week == y.Week;

    public int GetHashCode((string Part, DateOnly Week) obj) =>
        HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Part), obj.Week);
}
