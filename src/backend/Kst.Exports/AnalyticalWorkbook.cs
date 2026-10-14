using System.Globalization;
using ClosedXML.Excel;
using Kst.Domain.Exports;

namespace Kst.Exports;

/// <summary>Loaded MPS/BOM analytical exports. Exact values beyond Excel precision remain text.</summary>
public static class AnalyticalWorkbook
{
    public const int MaximumCells = 250000;
    public static byte[] Create(AnalyticalWorkbookDocument document)
    {
        if (document.Sheets.Count is < 1 or > 4 || document.Sheets.Sum(s => (long)(s.Rows.Count + 1) * s.Headers.Count) > MaximumCells)
            throw new ArgumentException("Workbook exceeds the supported 250,000-cell capacity; nothing was truncated.");
        using var book = new XLWorkbook();
        foreach (var source in document.Sheets)
        {
            if (source.Headers.Count is < 1 or > 16384 || source.Rows.Count > 1048575) throw new ArgumentException("Worksheet exceeds Excel capacity.");
            var sheet = book.AddWorksheet(source.Name);
            for (var c = 0; c < source.Headers.Count; c++) Write(sheet.Cell(1, c + 1), new(source.Headers[c]));
            for (var r = 0; r < source.Rows.Count; r++)
            {
                if (source.Rows[r].Count != source.Headers.Count) throw new ArgumentException("Incomplete workbook row.");
                for (var c = 0; c < source.Headers.Count; c++) Write(sheet.Cell(r + 2, c + 1), source.Rows[r][c]);
            }
            sheet.Row(1).Style.Font.Bold = true;
            sheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#DAE8F2");
            sheet.SheetView.Freeze(1, source.FrozenColumns);
            sheet.Columns().Width = 18;
            for (var c = 1; c <= source.FrozenColumns; c++) sheet.Column(c).Width = c == 1 ? 28 : 35;
            sheet.Row(1).Style.Alignment.WrapText = true;
            sheet.Row(1).Height = 42;
            if (document.ReportType == "BOM") sheet.Column(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            // Keep important stale-data warnings available without a separate information worksheet.
            var warnings = document.Metadata.Where(p => p.Key == "Warning").Select(p => p.Value);
            var warning = string.Join("\n", warnings);
            if (warning.Length > 0) Write(sheet.Cell(1, 1), new(source.Headers[0], Note: warning));
            if (document.ReportType == "MPS")
            {
                sheet.Row(1).Height = 24;
                sheet.Columns(3, source.Headers.Count).Width = 13;
                for (var c = 3; c < source.Headers.Count; c++)
                {
                    var header = source.Headers[c].Split('\n');
                    if (DateOnly.TryParseExact(header[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var week))
                    {
                        var cell = sheet.Cell(1, c + 1);
                        cell.Value = week.ToDateTime(TimeOnly.MinValue);
                        cell.Style.DateFormat.Format = "m/d/yyyy";
                        // Fiscal labels come from the same frontend-owned calendar as the loaded view.
                        var periodText = header.Length > 1 ? header[1].Split(' ').LastOrDefault() : null;
                        if (periodText?.StartsWith('P') == true && int.TryParse(periodText[1..], out var period) && period is >= 1 and <= 12)
                        {
                            string[] colors = ["#D9EAF7", "#E2EFDA", "#FFF2CC", "#E4DFEC", "#FCE4D6", "#DDEBF7", "#D0E0E3", "#EAD1DC", "#D9EAD3", "#FCE5CD", "#CFE2F3", "#D9D2E9"];
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(colors[period - 1]);
                        }
                    }
                }
            }
            if (source.Rows.Count > 0) sheet.Range(1, 1, source.Rows.Count + 1, source.Headers.Count).SetAutoFilter();
        }
        using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
    }

    private static void Write(IXLCell cell, AnalyticalCell fact)
    {
        if (fact.Value is null) return;
        if (fact.Value.Length > 32767 || fact.Note?.Length > 32767) throw new ArgumentException("Cell exceeds Excel text capacity; nothing was truncated.");
        if (fact.Kind == "date" && DateOnly.TryParseExact(fact.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            cell.Value = date.ToDateTime(TimeOnly.MinValue); cell.Style.DateFormat.Format = "m/d/yyyy";
        }
        else if (fact.Kind is "quantity" or "currency" && decimal.TryParse(fact.Value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number)
            && Canonical(fact.Value).TrimStart('-', '+').Replace(".", "").TrimStart('0').Length <= 15
            && Canonical(fact.Value) == Canonical(number.ToString(CultureInfo.InvariantCulture)))
        {
            cell.Value = (double)number;
            cell.Style.NumberFormat.Format = fact.Kind == "currency" ? "$#,##0.00;[Red]-$#,##0.00"
                : decimal.Truncate(number) == number ? "#,##0;[Red]-#,##0"
                : "#,##0.############################;[Red]-#,##0.############################";
        }
        else cell.Value = fact.Value; // ClosedXML literal Value, never FormulaA1.
        if (!string.IsNullOrEmpty(fact.Note)) cell.CreateComment().AddText(fact.Note);
    }

    private static string Canonical(string value)
    {
        var negative = value.StartsWith('-');
        var parts = value.TrimStart('-', '+').Split('.');
        var whole = parts[0].TrimStart('0');
        var fraction = parts.Length > 1 ? parts[1].TrimEnd('0') : "";
        var magnitude = (whole.Length == 0 ? "0" : whole) + (fraction.Length == 0 ? "" : "." + fraction);
        return negative && magnitude != "0" ? "-" + magnitude : magnitude;
    }
}
