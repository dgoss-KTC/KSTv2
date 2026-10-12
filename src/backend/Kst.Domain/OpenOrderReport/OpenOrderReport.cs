using System.Globalization;
using System.Text.RegularExpressions;

namespace Kst.Domain.OpenOrderReport;

public sealed record OpenOrderReportInput(string? CustomerName, string? CustomerNumber, string? SalespersonId,
    string? ProductLineFrom, string? ProductLineTo, string? Ios, string? DueDateFrom, string? DueDateTo,
    string? Site, IReadOnlyList<string> OptionalFields);

public sealed record OpenOrderReportFilter(string? CustomerName, string? CustomerNumber, string? SalespersonId,
    string? ProductLineFrom, string? ProductLineTo, string? Ios, DateOnly? DueDateFrom, DateOnly? DueDateTo,
    string? Site, IReadOnlyList<string> OptionalFields);

/// <summary>Independent nullable facts; no workspace normalization or calculated replacements.</summary>
public sealed record OpenOrderReportCell(string? Text = null, decimal? Number = null, DateTime? Date = null, bool? Boolean = null);
public sealed record OpenOrderReportRow(IReadOnlyList<OpenOrderReportCell> Cells);
public sealed record OpenOrderReportDocument(IReadOnlyList<OpenOrderReportRow> Rows, IReadOnlyList<string> Columns,
    DateOnly AsOfDate, DateTimeOffset AcquiredAtUtc);

public static class OpenOrderReportRules
{
    public const int MaxRows = 10_000;
    public const int MaxCells = 500_000;
    public const long MaxSourceBytes = 16 * 1024 * 1024;
    public const int MaxText = 32_767; // Excel cell capacity; never truncate comments/text.
    public static IReadOnlyList<string> Sites { get; } = Array.AsReadOnly(new[] { "SW", "NW", "AR", "MN", "MS", "KV" });
    public static IReadOnlyList<string> Mandatory { get; } = Array.AsReadOnly(new[] { "Due", "Order", "PO", "Ln", "Item Number", "Site", "Open", "Stat", "Ext Price" });
    public static IReadOnlyList<string> Optional { get; } = Array.AsReadOnly(new[] {
        "Allocated", "Customer", "Customer Name", "Customer Part", "Dock Date", "IOS", "Line Comments", "Line Hold",
        "Partials", "Perform Date", "Picked", "Plnr", "Prod Stat", "Product Line", "QA Hold", "Remarks", "Required Date",
        "Revision", "Ship Acct", "Ship To", "Ship Via", "Site QOH", "SO Hold Status", "SO Type", "Unit Price" });
    public static IReadOnlyList<string> Headers { get; } = Array.AsReadOnly(Mandatory.Concat(Optional).ToArray());
    public static bool IsDate(int index) => index is 0 or 13 or 18 or 25;
    public static bool IsNumber(int index) => index is 3 or 6 or 8 or 9 or 19 or 30 or 33;

    public static OpenOrderReportFilter Validate(OpenOrderReportInput input, DateOnly localDate)
    {
        static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        var name = Trim(input.CustomerName); var customer = Trim(input.CustomerNumber); var salesperson = Trim(input.SalespersonId);
        var from = Trim(input.ProductLineFrom); var to = Trim(input.ProductLineTo); var ios = Trim(input.Ios); var site = Trim(input.Site);
        var dateFrom = Trim(input.DueDateFrom); var dateTo = Trim(input.DueDateTo);
        if (new[] { name, customer, salesperson, from, to, ios, site, dateFrom, dateTo }.Any(v => v?.Length > 1024))
            throw new ArgumentException("Report inputs exceed capacity.");
        if (site is not null && !Sites.Contains(site)) throw new ArgumentException("Select a supported site.");
        if (new[] { name, customer, salesperson, from, to, ios, site }.All(v => v is null))
            throw new ArgumentException("Enter at least one non-date filter. All supported sites alone is not a filter.");
        static bool FourDigits(string value) => value.EnumerateRunes().Count() == 4
            && value.EnumerateRunes().All(r => System.Text.Rune.GetUnicodeCategory(r) == UnicodeCategory.DecimalDigitNumber);
        if ((from is null) != (to is null) || (from is not null && (!FourDigits(from) || !FourDigits(to!))))
            throw new ArgumentException("Enter both Product Line From and To using exactly four digits, or leave both empty.");
        if ((dateFrom is null) != (dateTo is null)) throw new ArgumentException("Enter both due dates or leave both empty.");
        DateOnly? start = dateFrom is null ? null : ParseDate(dateFrom, localDate.Year);
        DateOnly? end = dateTo is null ? null : ParseDate(dateTo, localDate.Year);
        if (start > end) throw new ArgumentException("End date must be on or after start date.");
        if (input.OptionalFields is null || input.OptionalFields.Count > Optional.Count
            || input.OptionalFields.Any(f => !Optional.Contains(f)) || input.OptionalFields.Distinct(StringComparer.Ordinal).Count() != input.OptionalFields.Count)
            throw new ArgumentException("Select unique optional columns from the supported list.");
        return new(name, customer, salesperson, from, to, ios, start, end, site,
            Array.AsReadOnly(Optional.Where(input.OptionalFields.Contains).ToArray()));
    }

    public static DateOnly ParseDate(string input, int localYear)
    {
        var text = input.Trim().Replace('.', '/').Replace('-', '/');
        var match = Regex.Match(text, @"^([0-9]{1,2})/( ?[0-9]{1,2})(?:/([0-9]{2}|\p{Nd}{4}))?$");
        if (!match.Success) throw new ArgumentException("Invalid date. Use MM/DD/YYYY, MM/DD/YY or MM/DD (current year); . and - separators are supported.");
        var month = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var y = match.Groups[3].Value;
        var year = y.Length == 0 ? localYear : y.Aggregate(0, (n, c) => n * 10 + (int)char.GetNumericValue(c));
        if (y.Length == 2) year += year <= 68 ? 2000 : 1900; // Python strptime pivot.
        try
        {
            // Python parses yearless month/day against 1900 first, so 2/29 is invalid even in a leap year.
            if (y.Length == 0) _ = new DateOnly(1900, month, day);
            return new(year, month, day);
        }
        catch (ArgumentOutOfRangeException) { throw new ArgumentException("Invalid calendar date."); }
    }

    public static IReadOnlyList<OpenOrderReportRow> Freeze(IReadOnlyList<OpenOrderReportRow> rows)
    {
        if (rows.Count > MaxRows || (long)rows.Count * Headers.Count > MaxCells) throw new InvalidOperationException("Open Order Report exceeds report capacity.");
        long bytes = 0;
        var frozen = new List<OpenOrderReportRow>(rows.Count);
        foreach (var row in rows)
        {
            if (row.Cells.Count != Headers.Count) throw new InvalidOperationException("Unsupported Open Order Report schema.");
            for (var i = 0; i < row.Cells.Count; i++)
            {
                var c = row.Cells[i];
                if (c.Text?.Length > MaxText || (IsDate(i) ? c.Text is not null || c.Number is not null || c.Boolean is not null
                    : IsNumber(i) ? c.Text is not null || c.Date is not null || c.Boolean is not null
                    : i == 17 ? c.Text is not null || c.Number is not null || c.Date is not null
                    : c.Number is not null || c.Date is not null || c.Boolean is not null))
                    throw new InvalidOperationException("Unsupported Open Order Report value or text capacity.");
                bytes += c.Text is null ? 16 : 16 + (long)c.Text.Length * 2;
            }
            if (bytes > MaxSourceBytes) throw new InvalidOperationException("Open Order Report exceeds report capacity.");
            frozen.Add(new(Array.AsReadOnly(row.Cells.ToArray())));
        }
        return frozen.AsReadOnly();
    }
}
