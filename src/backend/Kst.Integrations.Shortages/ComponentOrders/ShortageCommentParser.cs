using System.Globalization;
using System.Text.RegularExpressions;
using Kst.Domain.ComponentOrders;

namespace Kst.Integrations.Shortages.ComponentOrders;

/// <summary>Reads only the first newest-first block. No historical-entry collection or date sorting.</summary>
public static class ShortageCommentParser
{
    // NonBacktracking guarantees linear matching, including long malformed input. No lookarounds,
    // backreferences or nested backtracking. The same complete header grammar bounds the body.
    private const string Header = @"(?<month>[0-9]{1,2})/(?<day>[0-9]{1,2})/(?<year>[0-9]{4})\s+(?<hour>[0-9]{1,2}):(?<minute>[0-9]{2})\s+(?<period>AM|PM)\s+(?<author>\S+)";
    private const RegexOptions Options = RegexOptions.NonBacktracking | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
    private static readonly Regex Leading = new(@"\A\s*" + Header, Options);
    private static readonly Regex Following = new(@"\s+" + Header, Options);

    public static BuyerComment Parse(string? history, string? currentComments)
    {
        if (string.IsNullOrWhiteSpace(history)) return new(currentComments);
        var first = Leading.Match(history);
        if (!first.Success || !TryTimestamp(first, out var recordedAt)) return new(currentComments);

        var bodyStart = first.Index + first.Length;
        var next = Following.Match(history, bodyStart);
        // A date/time-looking but invalid calendar value is prose, not a complete header.
        while (next.Success && !TryTimestamp(next, out _)) next = next.NextMatch();
        var bodyEnd = next.Success ? next.Index : history.Length;
        return new(history.AsSpan(bodyStart, bodyEnd - bodyStart).Trim().ToString(), recordedAt, first.Groups["author"].Value);
    }

    private static bool TryTimestamp(Match header, out DateTime value)
    {
        int Number(string group) => int.Parse(header.Groups[group].Value, CultureInfo.InvariantCulture);
        var year = Number("year");
        var month = Number("month");
        var day = Number("day");
        var hour = Number("hour");
        var minute = Number("minute");
        value = default;
        if (year is < 1 or > 9999 || month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)
            || hour is < 1 or > 12 || minute > 59) return false;
        hour = hour % 12 + (header.Groups["period"].Value.Equals("PM", StringComparison.OrdinalIgnoreCase) ? 12 : 0);
        value = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        return true;
    }
}
