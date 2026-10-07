using System.Globalization;
using System.Text;
using Kst.Domain.PlanningWorkbook;

namespace Kst.Exports;

/// <summary>
/// Serializes validated QAD Mass Updater rows (Stage 14.6; ledger §7 and the 2026-10-06 owner
/// decisions). The file is an operational proposal for external human-reviewed processing: KST never
/// submits or imports it. Columns are Part, Qty, Date with no header row, where Qty is the signed
/// whole-number adjustment to the existing MPS quantity (never the resulting quantity) and Date is the
/// Monday week label as M/d/yyyy. Rows are written in the order supplied (the Domain validator orders
/// them by retained workspace part order, then week). UTF-8 without a byte-order mark; every row,
/// including the last, ends with CRLF. Fields are quoted only when needed; no formula prefixing.
/// </summary>
public static class MpsMassUpdateCsv
{
    private const int MaxFileNameStem = 80;

    public static byte[] Create(IReadOnlyList<MpsMassUpdateRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
            throw new ArgumentException("A Mass Updater file requires at least one adjustment row.", nameof(rows));

        var text = new StringBuilder();
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Part))
                throw new ArgumentException("A Mass Updater row requires a part.", nameof(rows));
            if (row.Adjustment == 0m || row.Adjustment != decimal.Truncate(row.Adjustment))
                throw new ArgumentException("A Mass Updater adjustment must be a nonzero whole number.", nameof(rows));
            if (row.WeekLabel.DayOfWeek != DayOfWeek.Monday)
                throw new ArgumentException("A Mass Updater date must be a Monday week label.", nameof(rows));

            text.Append(CsvText.Escape(row.Part)).Append(',')
                .Append(row.Adjustment.ToString("0", CultureInfo.InvariantCulture)).Append(',')
                .Append(row.WeekLabel.ToString("M/d/yyyy", CultureInfo.InvariantCulture)).Append("\r\n");
        }
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text.ToString());
    }

    /// <summary>
    /// Suggested Save As name: <c>{Workspace}-MPS-Adjustments-{yyyy-MM-dd}.csv</c>. Characters that are
    /// not valid in Windows file names are replaced; the user chooses the final name and location.
    /// </summary>
    public static string SuggestedFileName(string? workspaceLabel, DateOnly date)
    {
        var invalid = new HashSet<char>(Path.GetInvalidFileNameChars()) { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
        var cleaned = new StringBuilder();
        foreach (var c in workspaceLabel ?? "")
            cleaned.Append(char.IsControl(c) || invalid.Contains(c) ? '-' : c);
        var stem = cleaned.ToString().Trim().Trim('.', '-', ' ');
        if (stem.Length > MaxFileNameStem) stem = stem[..MaxFileNameStem].TrimEnd('.', '-', ' ');
        if (stem.Length == 0) stem = "Workspace";
        return $"{stem}-MPS-Adjustments-{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.csv";
    }
}
