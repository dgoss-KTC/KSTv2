using System.IO.Compression;
using System.Xml.Linq;
using ClosedXML.Excel;
using Kst.Domain.Sop;

namespace Kst.Exports;

public static class SopWorkbook
{
    private sealed record Column(string Kind, int Week = -1, SopPeriod? Period = null, int[]? Members = null, int Quarter = 0, bool Ending = false);

    public static byte[] Create(SopReport report)
    {
        var source = report.Source;
        if (source.Rows.Count == 0 || !SopRules.ValidPresentation(source.WeekLabels, report.Presentation)) throw new ArgumentException("Invalid S&OP workbook.");
        var periods = report.Presentation.Periods;
        var quarters = periods.GroupBy(p => (p.FiscalYear, p.Quarter)).Select(g => new
        {
            g.Key,
            Members = g.SelectMany(p => p.WeekLabels).Select(d => source.WeekLabels.ToList().IndexOf(d)).ToArray()
        }).ToArray();
        var columns = new List<Column>();
        for (var q = 0; q < quarters.Length; q++)
        {
            if (q > 0)
            {
                columns.Add(new("average", Members: quarters[q - 1].Members, Quarter: quarters[q - 1].Key.Quarter, Ending: true));
                columns.Add(new("average", Members: quarters[q].Members, Quarter: quarters[q].Key.Quarter));
            }
            columns.AddRange(quarters[q].Members.Select(i => new Column("week", i)));
        }
        var legacyColumnCount = columns.Count;
        columns.AddRange(periods.Select(p => new Column("period", Period: p,
            Members: p.WeekLabels.Select(d => source.WeekLabels.ToList().IndexOf(d)).ToArray())));
        var weekColumns = columns.Select((c, i) => (c, i)).Where(x => x.c.Kind == "week").ToDictionary(x => x.c.Week, x => x.i + 10);
        var lastColumn = 9 + columns.Count;
        using var book = new XLWorkbook();
        book.Style.Font.FontName = "Aptos Narrow"; book.Style.Font.FontSize = 11;
        var s = book.AddWorksheet("S&OP"); s.RowHeight = 15;
        for (var i = 0; i < 8; i++) s.Cell(1, i + 1).Value = source.Headers[i];
        s.Cell(1, 9).Value = "SCHEDULING NOTES";
        s.Column(9).Style.Fill.BackgroundColor = XLColor.Yellow;
        for (var i = 0; i < columns.Count; i++)
        {
            var spec = columns[i]; var c = i + 10;
            if (spec.Kind == "week") { s.Cell(1, c).Value = source.WeekLabels[spec.Week].ToDateTime(TimeOnly.MinValue); s.Cell(1, c).Style.NumberFormat.Format = "m/d/yyyy"; }
            else if (spec.Kind == "average")
            {
                s.Cell(1, c).Value = $"Qtr {spec.Quarter} Weekly Average";
                s.Column(c).Style.Fill.BackgroundColor = XLColor.FromHtml(spec.Ending ? "#9BC2E6" : "#8EA9DB");
            }
            else
            {
                var p = spec.Period!;
                s.Cell(1, c).Value = $"FY{p.FiscalYear % 100:D2} P{p.Period:D2} Total" + (p.WeekLabels.Count < p.WeekCount ? $" (partial {p.WeekLabels.Count}/{p.WeekCount}w)" : "");
            }
        }
        var groupColumn = report.GroupBy == "customer" ? 1 : 4;
        string Key(SopRow r) => r.Fixed[groupColumn - 1].Text ?? r.Fixed[groupColumn - 1].Number?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
        // Excel sorts blanks last; no secondary sort or normalization of source keys.
        var sorted = source.Rows.OrderBy(r => string.IsNullOrEmpty(Key(r)) ? 1 : 0)
            .ThenBy(Key, StringComparer.CurrentCultureIgnoreCase).ToArray();
        var row = 2; var lastDetail = 2; var named = false;
        foreach (var group in sorted.GroupBy(Key, StringComparer.CurrentCultureIgnoreCase))
        {
            var first = row; var blank = string.IsNullOrWhiteSpace(group.Key);
            foreach (var detail in group)
            {
                for (var i = 0; i < 8; i++) Write(s.Cell(row, i + 1), detail.Fixed[i]);
                foreach (var (week, c) in weekColumns) Write(s.Cell(row, c), detail.Weeks[week]);
                for (var i = 0; i < columns.Count; i++)
                {
                    var spec = columns[i]; if (spec.Kind == "week") continue;
                    var refs = string.Join(',', spec.Members!.Select(w => $"{XLHelper.GetColumnLetterFromNumber(weekColumns[w])}{row}"));
                    s.Cell(row, i + 10).FormulaA1 = spec.Kind == "average" ? $"ROUND(AVERAGE({refs}),0)" : $"SUM({refs})";
                }
                s.Row(row).OutlineLevel = blank ? 1 : 2;
                lastDetail = row++;
            }
            if (blank) continue;
            named = true;
            Total(row, $"{group.Key} Total", first, row - 1); s.Row(row++).OutlineLevel = 1;
        }
        Total(row, "Grand Total", 2, lastDetail);
        if (!named || string.IsNullOrWhiteSpace(Key(sorted[^1]))) s.Row(row).OutlineLevel = 1;
        for (var r = 1; r <= row; r++)
            if (r == 1 || s.Cell(r, groupColumn).GetString().Contains("Total", StringComparison.Ordinal))
            { s.Range(r, 1, r, lastColumn).Style.Fill.BackgroundColor = XLColor.FromHtml("#A9A9A9"); s.Range(r, 1, r, lastColumn).Style.Font.Bold = true; }
        s.Columns(1, lastColumn).AdjustToContents(); s.SheetView.FreezeRows(1);
        book.Properties.Comments = $"S&OP acquired {report.AcquiredAtUtc:o}; fiscal anchor FY{report.Presentation.AnchorFiscalYear} {report.Presentation.AnchorStartDate:yyyy-MM-dd}; exceptions "
            + string.Join(',', report.Presentation.Exceptions.Select(e => $"{e.FiscalYear}:P{e.ExtraWeekPeriod}"));
        using var stream = new MemoryStream(); book.SaveAs(stream, new ClosedXML.Excel.SaveOptions { EvaluateFormulasBeforeSaving = true });
        return LegacyPrintDefaults(stream.ToArray());

        void Total(int r, string label, int first, int last)
        {
            s.Cell(r, groupColumn).Value = label;
            for (var c = 10; c <= 9 + Math.Min(26, legacyColumnCount); c++) Sum(c);
            for (var c = 10 + legacyColumnCount; c <= lastColumn; c++) Sum(c);
            void Sum(int c) { var letter = XLHelper.GetColumnLetterFromNumber(c); s.Cell(r, c).FormulaA1 = $"SUBTOTAL(9,{letter}{first}:{letter}{last})"; }
        }
    }
    private static void Write(IXLCell cell, SopCell value)
    {
        if (value.Text is not null) cell.Value = value.Text;
        else if (value.Number is not null) cell.Value = value.Number.Value;
    }
    private static byte[] LegacyPrintDefaults(byte[] bytes)
    {
        using var stream = new MemoryStream(); stream.Write(bytes); stream.Position = 0;
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, true))
        {
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var entry = zip.GetEntry("xl/worksheets/sheet1.xml")!;
            XDocument xml; using (var read = entry.Open()) xml = XDocument.Load(read);
            foreach (var name in new[] { "pageSetup", "headerFooter", "rowBreaks", "colBreaks", "printOptions" }) xml.Root!.Elements(ns + name).Remove();
            xml.Root!.Element(ns + "sheetPr")?.Element(ns + "pageSetUpPr")?.Remove();
            var margins = xml.Root.Element(ns + "pageMargins");
            if (margins is not null) foreach (var (name, value) in new[] { ("left", "0.7"), ("right", "0.7"), ("top", "0.75"), ("bottom", "0.75"), ("header", "0.3"), ("footer", "0.3") }) margins.SetAttributeValue(name, value);
            entry.Delete(); using var write = zip.CreateEntry("xl/worksheets/sheet1.xml").Open(); xml.Save(write);
        }
        return stream.ToArray();
    }
}
