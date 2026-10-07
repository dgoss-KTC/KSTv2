namespace Kst.Exports;

/// <summary>
/// Shared CSV field escaping for the controlled human-reviewable CSV exporters: a field is quoted only
/// when it contains a comma, double quote, CR or LF (RFC 4180). No spreadsheet-formula prefixing is
/// applied; it would corrupt signed quantities (Stage 14 ledger §7).
/// </summary>
internal static class CsvText
{
    public static string Escape(string? value)
    {
        value ??= "";
        return value.IndexOfAny([',', '"', '\r', '\n']) < 0 ? value : $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
