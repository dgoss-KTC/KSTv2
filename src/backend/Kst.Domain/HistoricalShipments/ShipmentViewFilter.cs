using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Kst.Domain.HistoricalShipments;

/// <summary>View predicates only. Null/empty are blanks; whitespace and captions are actual values.</summary>
public sealed record ShipmentViewFilter(string Column, string Kind, string? Mode = null,
    IReadOnlyList<string>? Values = null, bool IncludeBlanks = false, string? Text = null,
    string? Operator = null, string? Value = null, string? Upper = null,
    DateOnly? From = null, DateOnly? To = null);

public static class ShipmentViewRules
{
    public const int MaximumPageSize = 1000, MaximumValues = 1000, MaximumTextLength = 256;
    public const int MaximumDecimalDigits = 76, MaximumDecimalScale = 76;
    public static readonly IReadOnlyList<int> PageSizes = Array.AsReadOnly(new[] { 100, 500, 1000 });

    public static ShipmentColumn Column(string id) => HistoricalShipmentsRules.Catalog
        .SingleOrDefault(c => c.DefaultSelected && c.Id != "shipDate" && c.Id == id)
        ?? throw new ArgumentException("Invalid filter column.");

    public static void TextBound(string? text)
    {
        if (text?.Length > MaximumTextLength || text?.Any(char.IsControl) == true)
            throw new ArgumentException("Filter text exceeds its length bound or contains control characters.");
    }

    public static IReadOnlyList<ShipmentViewFilter> Validate(IReadOnlyList<ShipmentViewFilter>? filters)
    {
        if (filters is null || filters.Count > 14 || filters.Select(f => f?.Column).Distinct(StringComparer.Ordinal).Count() != filters.Count)
            throw new ArgumentException("Use distinct, supported column filters (at most fourteen).");
        return Array.AsReadOnly(filters.Select(f =>
        {
            if (f is null) throw new ArgumentException("Invalid column filter.");
            var column = Column(f.Column);
            var kind = f.Column == "onTime" ? "values" : column.Kind;
            if (f.Kind == "values" && (kind == "text" || kind == "values"))
            {
                if (f.Mode is not ("include" or "exclude") || f.Values is null || f.Values.Count > MaximumValues ||
                    f.Values.Distinct(StringComparer.Ordinal).Count() != f.Values.Count ||
                    f.Text is not null || f.Operator is not null || f.Value is not null || f.Upper is not null || f.From is not null || f.To is not null)
                    throw new ArgumentException("Invalid values filter.");
                foreach (var v in f.Values)
                {
                    if (string.IsNullOrEmpty(v)) throw new ArgumentException("Use IncludeBlanks for blank values.");
                    TextBound(v);
                    if (kind == "values" && v is not ("1" or "0")) throw new ArgumentException("Invalid On Time value.");
                }
                return f with { Values = Array.AsReadOnly(f.Values.ToArray()) };
            }
            if (f.Mode is not null || f.Values is not null) throw new ArgumentException("Incompatible filter fields.");
            if (f.Kind == "contains" && kind == "text")
            {
                TextBound(f.Text);
                if (string.IsNullOrEmpty(f.Text) || f.Value is not null || f.Upper is not null || f.Operator is not null || f.From is not null || f.To is not null)
                    throw new ArgumentException("Enter literal Contains text.");
            }
            else if (f.Kind == "compare" && kind == "decimal")
            {
                if (f.Operator is not ("eq" or "gt" or "lt" or "between") || f.Text is not null || f.From is not null || f.To is not null)
                    throw new ArgumentException("Invalid numeric condition.");
                var lower = Exact(f.Value);
                if (f.Operator == "between")
                {
                    if (Compare(lower, Exact(f.Upper)) > 0) throw new ArgumentException("Numeric range start must not exceed end.");
                }
                else if (f.Upper is not null) throw new ArgumentException("Unexpected upper numeric bound.");
            }
            else if (f.Kind == "dateRange" && kind == "date")
            {
                if (f.From > f.To || f.Text is not null || f.Value is not null || f.Upper is not null || f.Operator is not null)
                    throw new ArgumentException("Invalid inclusive date range.");
            }
            else throw new ArgumentException("Filter operator is incompatible with column type.");
            return f;
        }).ToArray());
    }

    // Source extensions can exceed System.Decimal's 29 digits. Bounded coefficient/scale arithmetic
    // preserves them exactly rather than rounding a comparison or narrowing the accepted source.
    private static (BigInteger N, int Scale) Exact(string? text)
    {
        if (text is null || text.Length > 80 || !Regex.IsMatch(text, @"^[+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Enter an exact decimal without exponent or grouping.");
        var unsigned = text.TrimStart('+', '-'); var pieces = unsigned.Split('.');
        var scale = pieces.Length == 2 ? pieces[1].Length : 0;
        var digits = unsigned.Replace(".", "");
        if (digits.Length > MaximumDecimalDigits || scale > MaximumDecimalScale)
            throw new ArgumentException("Decimal precision or scale exceeds the supported bound (76).");
        return (BigInteger.Parse((text[0] == '-' ? "-" : "") + digits, CultureInfo.InvariantCulture), scale);
    }
    private static int Compare((BigInteger N, int Scale) a, (BigInteger N, int Scale) b) =>
        (a.N * BigInteger.Pow(10, Math.Max(0, b.Scale - a.Scale))).CompareTo(b.N * BigInteger.Pow(10, Math.Max(0, a.Scale - b.Scale)));

    public static Func<ShipmentRow, bool> Predicate(IReadOnlyList<string> columns, IReadOnlyList<ShipmentViewFilter> filters, string? except = null)
    {
        var tests = filters.Where(f => f.Column != except).Select(f =>
        {
            var index = columns.ToList().IndexOf(f.Column);
            if (index < 0) throw new ArgumentException("Filter column is not in this result.");
            var values = new HashSet<string>(f.Values ?? [], StringComparer.Ordinal);
            var lower = f.Kind == "compare" ? Exact(f.Value) : default;
            var upper = f.Operator == "between" ? Exact(f.Upper) : default;
            return (Func<ShipmentRow, bool>)(row =>
            {
                var value = row.Cells[index];
                if (string.IsNullOrEmpty(value)) return f.IncludeBlanks;
                return f.Kind switch
                {
                    "values" => f.Mode == "include" ? values.Contains(value) : !values.Contains(value),
                    "contains" => value.Contains(f.Text!, StringComparison.OrdinalIgnoreCase),
                    "compare" => f.Operator switch
                    {
                        "eq" => Compare(Exact(value), lower) == 0,
                        "gt" => Compare(Exact(value), lower) > 0,
                        "lt" => Compare(Exact(value), lower) < 0,
                        "between" => Compare(Exact(value), lower) >= 0 && Compare(Exact(value), upper) <= 0,
                        _ => false
                    },
                    "dateRange" => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                        ? (f.From is null || date >= f.From) && (f.To is null || date <= f.To)
                        : throw new InvalidDataException("Frozen date is invalid."),
                    _ => false
                };
            });
        }).ToArray();
        return row => tests.All(test => test(row));
    }
}
