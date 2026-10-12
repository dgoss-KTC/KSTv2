using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using ClosedXML.Excel;
using Kst.Domain.ShipmentsToGo;

namespace Kst.Exports;

public static class ShipmentsToGoWorkbook
{
    public static readonly string[] Headers = ["SOLN", "Due Date", "Order", "PO", "SO Type", "Ln", "IOS", "Item Number", "P/L", "Site", "Open", "Alloc", "Picked", "QoH", "Ship To", "Ext Price", "CRC", "Risk", "Comments", "Good-To-Go"];
    public const string Accounting = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)";
    private static readonly XLColor Blue = XLColor.FromHtml("#0070C0");
    private static readonly XLColor Green = XLColor.FromHtml("#00B550");

    public static byte[] Create(ShipmentsToGoReport report)
    {
        if (report.Sheets.Count == 0) throw new ArgumentException("No workbook for empty orders.");
        using var book = new XLWorkbook();
        book.Style.Font.FontName = "Aptos Narrow";
        book.Style.Font.FontSize = 11;
        var totals = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var spec in report.Sheets)
        {
            var sheet = book.AddWorksheet(spec.Name);
            sheet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            sheet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;
            sheet.RowHeight = 15;
            for (var c = 1; c <= 20; c++) sheet.Cell(1, c).Value = Headers[c - 1];
            foreach (var c in new[] { 4, 15, 19, 20 }) sheet.Column(c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            sheet.Column(2).Style.NumberFormat.NumberFormatId = 14;
            foreach (var c in new[] { 16, 18 }) sheet.Column(c).Style.NumberFormat.Format = Accounting;
            var row = 2;
            var lastDetail = row;
            var hasNamedGroup = false;
            foreach (var group in spec.Lines.GroupBy(r => r.Crc, StringComparer.Ordinal))
            {
                // Excel Subtotal leaves leading blank CRC details without a group total.
                // Process has already trimmed unresolved whitespace CRCs to empty.
                var blankCrc = string.IsNullOrEmpty(group.Key);
                var first = row;
                foreach (var line in group)
                {
                    WriteLine(sheet, row, line);
                    lastDetail = row;
                    sheet.Row(row++).OutlineLevel = blankCrc ? 1 : 2;
                }
                if (blankCrc) continue;
                hasNamedGroup = true;
                WriteTotal(sheet, row, $"{group.Key} Total", first, row - 1);
                sheet.Row(row++).OutlineLevel = 1;
            }
            var grand = row;
            WriteTotal(sheet, grand, "Grand Total", 2, lastDetail);
            if (!hasNamedGroup) sheet.Row(grand).OutlineLevel = 1;
            totals[spec.Name] = grand;
            var table = sheet.Range(1, 1, grand, 20).CreateTable(spec.TableName);
            table.Theme = XLTableTheme.TableStyleLight13;
            table.ShowRowStripes = true;
            table.ShowAutoFilter = true;
            table.ShowTotalsRow = false;
            var header = sheet.Range(1, 1, 1, 20);
            header.Style.Font.Bold = true;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#156082");
            // Python styles any CRC text containing Total, including genuine detail CRCs.
            for (var r = 2; r <= grand; r++)
            {
                if (sheet.Cell(r, 17).GetString().Contains("Total", StringComparison.Ordinal)) StyleTotal(sheet, r);
            }
            sheet.Range(1, 1, grand, 20).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            sheet.Range(1, 1, grand, 20).Style.Border.TopBorderColor = XLColor.FromHtml("#156082");
            for (var r = 2; r <= grand; r++)
            {
                sheet.Range(r, 1, r, 20).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                sheet.Range(r, 1, r, 20).Style.Border.TopBorderColor = XLColor.FromHtml("#156082");
            }
            sheet.Range(1, 1, grand, 20).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            sheet.Range(1, 1, grand, 20).Style.Border.OutsideBorderColor = XLColor.FromHtml("#156082");
            ConditionalFormats(sheet, grand);
            sheet.TabColor = spec.Name.Contains("KTV", StringComparison.Ordinal)
                ? XLColor.FromHtml("#A000A0") : XLColor.FromHtml("#0080C0");
        }
        var main = report.Sheets.FirstOrDefault(s => s.TableName == "stg_main_post");
        if (main is not null) Summary(book.Worksheet(main.Name), totals, report);
        foreach (var sheet in book.Worksheets)
        {
            sheet.Columns(1, 20).AdjustToContents();
            sheet.Column(1).Hide(); sheet.Column(4).Hide();
        }
        book.Worksheet(1).SheetView.FreezeRows(1);
        using var stream = new MemoryStream();
        book.SaveAs(stream, new ClosedXML.Excel.SaveOptions { ConsolidateConditionalFormatRanges = false, EvaluateFormulasBeforeSaving = true });
        return PreserveExcelDefaults(stream.ToArray());
    }

    private static void WriteLine(IXLWorksheet sheet, int row, ShipmentToGoLine line)
    {
        void Text(int col, string? value) { if (!string.IsNullOrEmpty(value)) sheet.Cell(row, col).Value = value; }
        void Number(int col, double? value) { if (value is not null) sheet.Cell(row, col).Value = value.Value; }
        Text(1, line.Soln); sheet.Cell(row, 2).Value = line.DueDate; Text(3, line.Order);
        Text(4, line.PurchaseOrder); Text(5, line.Channel); Number(6, line.Line); Text(7, line.Ios);
        Text(8, line.ItemNumber); Text(9, line.ProductLine); Text(10, line.Site); Number(11, line.Open);
        Number(12, line.Allocated); Number(13, line.Picked); Number(14, line.Qoh); Text(15, line.ShipToName);
        Number(16, line.ExtendedPrice); Text(17, line.Crc); Number(18, line.Risk); Text(19, line.Comments);
    }

    private static void WriteTotal(IXLWorksheet sheet, int row, string label, int first, int last)
    {
        sheet.Cell(row, 17).Value = label;
        sheet.Cell(row, 16).FormulaA1 = $"SUBTOTAL(9,P{first}:P{last})";
        sheet.Cell(row, 18).FormulaA1 = $"SUBTOTAL(9,R{first}:R{last})";
        sheet.Row(row).Height = 18.75;
        sheet.Cell(row, 17).Style.Font.Bold = true;
    }

    private static void StyleTotal(IXLWorksheet sheet, int row)
    {
        sheet.Cell(row, 20).FormulaA1 = $"P{row}+R{row}";
        foreach (var (column, color) in new[] { (16, Blue), (18, XLColor.Red), (20, Green) })
        {
            var cell = sheet.Cell(row, column);
            cell.Style.NumberFormat.Format = Accounting;
            cell.Style.Font.FontSize = 14; cell.Style.Font.Bold = true; cell.Style.Font.FontColor = color;
        }
        sheet.Cell(row, 17).Style.Font.FontSize = 14;
    }

    private static void Summary(IXLWorksheet sheet, IReadOnlyDictionary<string, int> totals, ShipmentsToGoReport report)
    {
        var grand = totals[sheet.Name];
        string Ref(string name, string col, int row) => $"'{name.Replace("'", "''", StringComparison.Ordinal)}'!${col}${row}";
        var ext = Ref(sheet.Name, "P", grand); var risk = Ref(sheet.Name, "R", grand);
        void Label(int row, string label, int size, XLColor color, string? formula = null, double? number = null)
        {
            sheet.Cell(row, 17).Value = label;
            if (formula is not null) sheet.Cell(row, 18).FormulaA1 = formula;
            if (number is not null) sheet.Cell(row, 18).Value = number.Value;
            var range = sheet.Range(row, 17, row, 18);
            range.Style.Font.FontSize = size; range.Style.Font.Bold = true; range.Style.Font.FontColor = color;
            range.Style.NumberFormat.Format = Accounting;
            sheet.Row(row).Height = size == 18 ? 24 : size == 16 ? 21 : 18.75;
        }
        Label(grand + 2, "Open Order Total", 16, Blue, ext);
        Label(grand + 3, "Risk Total", 16, XLColor.Red, risk);
        Label(grand + 5, "SW GOOD-TO-GO:", 18, Green, $"({ext}+{risk})");
        var kv = totals.TryGetValue("KTV S2G", out var kvGrand)
            ? $"({Ref("KTV S2G", "P", kvGrand)}+{Ref("KTV S2G", "R", kvGrand)})" : "0";
        Label(grand + 6, "KV GOOD-TO-GO:", 18, Green, kv);
        if (sheet.Name.Contains("SW", StringComparison.Ordinal))
        {
            var date = report.LabelDate.ToString("M/d", CultureInfo.InvariantCulture);
            Label(grand + 8, $"SW Shipments {date}", 14, XLColor.Black, number: report.ShippedToday.GetValueOrDefault("SW"));
            Label(grand + 9, $"KTV Shipments {date}", 14, XLColor.Black, number: report.ShippedToday.GetValueOrDefault("KV"));
        }
    }

    private static void ConditionalFormats(IXLWorksheet s, int last)
    {
        var due = s.Range($"B2:B{last}").AddConditionalFormat();
        due.WhenIsTrue("AND(NOT(ISBLANK(B2)), B2<TODAY())").Fill.SetBackgroundColor(XLColor.Red).Font.SetFontColor(XLColor.White);
        foreach (var col in new[] { "N", "P" })
        {
            var cf = s.Range($"{col}2:{col}{last}").AddConditionalFormat();
            cf.WhenEquals(0).Fill.SetBackgroundColor(XLColor.Yellow);
        }
        var risk = s.Range($"R2:R{last}").AddConditionalFormat();
        risk.WhenLessThan(0).Font.SetFontColor(XLColor.Red);
        foreach (var (token, color) in new[] { ("QA Hold", "#FFF2CC"), ("CredHold", "#EAF1DD"), (" Hold", "#F2F2F2"), ("KS: ", "#FFF2CC"), ("KV: ", "#FFFF00") })
        {
            var cf = s.Range($"A2:T{last}").AddConditionalFormat();
            cf.WhenIsTrue($"ISNUMBER(SEARCH(\"{token}\", $S2))").Fill.SetBackgroundColor(XLColor.FromHtml(color));
        }
        foreach (var col in new[] { "L", "M" })
        {
            var cf = s.Range($"{col}2:{col}{last}").AddConditionalFormat();
            cf.WhenNotEquals(0).Fill.SetBackgroundColor(XLColor.Lime);
        }
    }

    /// <summary>ClosedXML emits explicit print defaults/theme. Match Python's Excel-default anchor without changing formulas/data.</summary>
    private static byte[] PreserveExcelDefaults(byte[] bytes)
    {
        using var stream = new MemoryStream(); stream.Write(bytes); stream.Position = 0;
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, true))
        {
            XNamespace s = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            foreach (var entry in zip.Entries.Where(e => e.FullName.StartsWith("xl/worksheets/sheet", StringComparison.Ordinal) && e.FullName.EndsWith(".xml", StringComparison.Ordinal)).ToArray())
            {
                XDocument xml; using (var read = entry.Open()) xml = XDocument.Load(read);
                foreach (var name in new[] { "pageSetup", "headerFooter", "rowBreaks", "colBreaks" }) xml.Root!.Elements(s + name).Remove();
                var margins = xml.Root!.Element(s + "pageMargins");
                if (margins is not null)
                {
                    foreach (var (name, value) in new[] { ("left", "0.7"), ("right", "0.7"), ("top", "0.75"), ("bottom", "0.75"), ("header", "0.3"), ("footer", "0.3") }) margins.SetAttributeValue(name, value);
                }
                xml.Root.Element(s + "sheetPr")?.Element(s + "pageSetUpPr")?.Remove();
                var pane = xml.Descendants(s + "pane").FirstOrDefault();
                if (pane is not null) { pane.SetAttributeValue("state", "frozen"); pane.SetAttributeValue("xSplit", null); }
                // Preserve insertion order/priorities rather than ClosedXML range consolidation.
                var priority = 1;
                foreach (var rule in xml.Descendants(s + "cfRule"))
                {
                    rule.SetAttributeValue("priority", priority);
                    rule.SetAttributeValue("stopIfTrue", priority++ == 4 ? null : "1");
                }
                var namePath = entry.FullName; entry.Delete();
                using var write = zip.CreateEntry(namePath).Open(); xml.Save(write);
            }
            XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
            var themeEntry = zip.GetEntry("xl/theme/theme1.xml")!;
            XDocument theme; using (var read = themeEntry.Open()) theme = XDocument.Load(read);
            var scheme = theme.Descendants(a + "clrScheme").Single();
            foreach (var (name, value) in new[] { ("dk2", "0E2841"), ("lt2", "E8E8E8"), ("accent1", "156082"), ("accent2", "E97132"), ("accent3", "196B24"), ("accent4", "0F9ED5"), ("accent5", "A02B93"), ("accent6", "4EA72E"), ("hlink", "467886"), ("folHlink", "96607D") })
                scheme.Element(a + name)!.ReplaceNodes(new XElement(a + "srgbClr", new XAttribute("val", value)));
            themeEntry.Delete(); using var themeWrite = zip.CreateEntry("xl/theme/theme1.xml").Open(); theme.Save(themeWrite);
        }
        return stream.ToArray();
    }
}
