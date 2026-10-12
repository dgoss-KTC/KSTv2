namespace Kst.Domain.Sop;

/// <summary>Procedure-produced scalar; null, text (including empty), and numeric zero remain distinct.</summary>
public sealed record SopCell(string? Text = null, double? Number = null)
{
    public static SopCell Blank { get; } = new();
}
public sealed record SopRange(string From, string To);
public sealed record SopRow(IReadOnlyList<SopCell> Fixed, IReadOnlyList<SopCell> Weeks);
public sealed record SopSource(IReadOnlyList<string> Headers, IReadOnlyList<DateOnly> WeekLabels, IReadOnlyList<SopRow> Rows);
public sealed record SopPeriod(int FiscalYear, int Period, int Quarter, DateOnly StartSunday,
    int WeekCount, IReadOnlyList<DateOnly> WeekLabels);
public sealed record SopCalendarException(int FiscalYear, int ExtraWeekPeriod);
public sealed record SopPresentation(int AnchorFiscalYear, DateOnly AnchorStartDate,
    IReadOnlyList<SopCalendarException> Exceptions, IReadOnlyList<SopPeriod> Periods);
public sealed record SopReport(string Site, string GroupBy, SopSource Source, DateOnly AsOfDate,
    DateTimeOffset AcquiredAtUtc, SopPresentation Presentation);

public static class SopRules
{
    public const int MaxWeeks = 156;
    public const int MaxRows = 10000;
    public const int MaxCells = 500000;
    public const int MaxText = 1024;
    public const int MaxSourceBytes = 16 * 1024 * 1024;

    public static bool ValidRequest(string site, string groupBy, IReadOnlyList<SopRange>? ranges) =>
        new[] { "SW", "NW", "AR", "MN", "MS", "KV" }.Contains(site, StringComparer.Ordinal)
        && groupBy is "planr" or "customer" && ranges is { Count: >= 1 and <= 5 }
        && ranges.All(r => r is not null && Digits(r.From) && Digits(r.To));

    private static bool Digits(string? value) => value is { Length: 4 } && value.All(c => c is >= '0' and <= '9');

    /// <summary>Export-only shape/partition validation, never fiscal calendar resolution.</summary>
    public static bool ValidPresentation(IReadOnlyList<DateOnly> weeks, SopPresentation? p)
    {
        if (p is null || p.AnchorFiscalYear is < 1900 or > 2200 || p.AnchorStartDate.DayOfWeek != DayOfWeek.Sunday
            || p.AnchorStartDate.Year is < 1900 or > 2200 || p.Exceptions is null || p.Exceptions.Count > 100
            || p.Exceptions.Any(e => e is null || e.FiscalYear is < 1900 or > 2200 || e.ExtraWeekPeriod is < 1 or > 12)
            || p.Exceptions.Select(e => e.FiscalYear).Distinct().Count() != p.Exceptions.Count
            || p.Periods is null || p.Periods.Count is < 1 or > 40 || weeks.Count is < 1 or > MaxWeeks) return false;
        var covered = new List<DateOnly>();
        var keys = new HashSet<(int, int)>();
        SopPeriod? previous = null;
        foreach (var period in p.Periods)
        {
            if (period is null || period.FiscalYear is < 1900 or > 2200 || period.Period is < 1 or > 12
                || period.Quarter is < 1 or > 4 || period.Quarter != (period.Period - 1) / 3 + 1
                || period.WeekCount is < 4 or > 6 || period.StartSunday.DayOfWeek != DayOfWeek.Sunday
                || period.StartSunday.Year is < 1900 or > 2200 || period.WeekLabels is null
                || period.WeekLabels.Count == 0 || period.WeekLabels.Count > period.WeekCount
                || !keys.Add((period.FiscalYear, period.Period))) return false;
            if (previous is not null && previous.WeekLabels[^1] != previous.StartSunday.AddDays(previous.WeekCount * 7 - 6)) return false;
            if (previous is not null && period.WeekLabels[0] != period.StartSunday.AddDays(1)) return false;
            if (previous is not null && (period.StartSunday != previous.StartSunday.AddDays(previous.WeekCount * 7)
                || (period.FiscalYear, period.Period) != (previous.Period == 12
                    ? (previous.FiscalYear + 1, 1) : (previous.FiscalYear, previous.Period + 1)))) return false;
            for (var i = 0; i < period.WeekLabels.Count; i++)
            {
                var label = period.WeekLabels[i];
                if (label.DayOfWeek != DayOfWeek.Monday || label < period.StartSunday.AddDays(1)
                    || label >= period.StartSunday.AddDays(period.WeekCount * 7)
                    || (i > 0 && label != period.WeekLabels[i - 1].AddDays(7))) return false;
            }
            covered.AddRange(period.WeekLabels);
            previous = period;
        }
        return covered.SequenceEqual(weeks);
    }
}
