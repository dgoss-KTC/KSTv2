using ClosedXML.Excel;
using Kst.Domain.ComponentMrp;
using Kst.Domain.LongTermShortages;

namespace Kst.Exports;

public static class ComponentMrpWorkbook
{
    public const int HeadingRow = 10;
    public const int FirstMetricRow = 11;
    public static readonly string[] Metrics = ["Gross Requirements", "Scheduled Receipts", "Projected QOH", "Planned Orders Due", "Planned Orders Release"];

    public static byte[] Create(string site, DateOnly asOf, bool includeUnconfirmed, ComponentMrpSource source, LongTermShortageRow row, string period = "Week")
    {
        using var book = new XLWorkbook();
        var sheet = book.AddWorksheet("Component MRP");
        var input = source.Input;
        var meta = source.Metadata;
        var periods = ComponentMrpPeriods.Build(asOf, row, includeUnconfirmed, period);
        var lastColumn = periods.Count + 1;
        var quantityFormat = LongTermQuantityPresentation.DecimalPlaces(input.UnitOfMeasure) == 0 ? "#,##0" : "#,##0.00";
        sheet.Style.Font.FontName = "Calibri";
        sheet.Style.Font.FontSize = 11;
        sheet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.ShowGridLines = false;
        sheet.SheetView.ZoomScale = 100;
        sheet.Column(1).Width = 36;
        sheet.Columns(2, Math.Max(9, lastColumn)).Width = 17;
        sheet.Rows(4, 9).Height = 24;
        // Bounded metadata merges use empty cells only; A:I fits the compact print header.
        void Span(int r, int first, int last, string text)
        {
            var range = sheet.Range(r, first, r, last);
            range.Merge(); range.FirstCell().Value = text; range.Style.Alignment.WrapText = true;
            range.Style.Alignment.Indent = 1;
        }
        void Text(int r, int col, string label, string? value)
        {
            if (col == 1) { sheet.Cell(r, 2).Value = label; sheet.Cell(r, 2).Style.Font.Bold = true; sheet.Cell(r, 2).Style.Alignment.WrapText = true; }
            else { Span(r, col, col + 1, label); sheet.Cell(r, col).Style.Font.Bold = true; }
            sheet.Cell(r, col + 2).Value = value ?? "Unavailable";
            sheet.Cell(r, col + 2).Style.Alignment.WrapText = true;
        }
        void Number(int r, int col, string label, decimal? value, bool quantity = false)
        {
            Text(r, col, label, value is null ? "Unavailable" : "");
            if (value is not null)
            {
                var cell = sheet.Cell(r, col + 2);
                cell.Value = (double)(quantity ? LongTermQuantityPresentation.Round(value.Value, input.UnitOfMeasure) : value.Value);
                cell.Style.NumberFormat.Format = quantity ? quantityFormat : "#,##0.#####";
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                cell.Style.Alignment.Indent = 1;
            }
        }
        Span(1, 2, 3, "Component MRP"); sheet.Cell(1, 2).Style.Font.SetBold().Font.SetFontSize(16);
        Span(1, 4, 9, $"As of {asOf:yyyy-MM-dd}  |  {row.Weeks.Count} weeks · {period}  |  {(includeUnconfirmed ? "Include Unconfirmed POs" : "Exclude Unconfirmed POs")}");
        sheet.Row(1).Height = 28;
        Span(2, 2, 6, input.ComponentPart); sheet.Cell(2, 2).Style.Font.Bold = true;
        // Explicit height for wrapped merged cells (Excel autofit does not reliably size them).
        sheet.Row(2).Height = WrappedHeight(input.ComponentPart, 85, 22);
        Span(2, 7, 9, row.DataQualityWarning ?? "");
        sheet.Row(2).Height = Math.Max(sheet.Row(2).Height, WrappedHeight(row.DataQualityWarning, 42, 22));
        sheet.Cell(2, 7).Style.Font.FontColor = XLColor.FromHtml("#8A5200");
        Span(3, 2, 9, input.Description ?? "Description: Unavailable");
        sheet.Row(3).Height = WrappedHeight(input.Description, 120, 22);
        sheet.Row(4).Height = 6;
        Text(5, 1, "Site", site); Text(6, 1, "UOM", input.UnitOfMeasure); Text(7, 1, "Status", input.QadStatus);
        Text(8, 1, "IOS", meta.Ios); Number(9, 1, "Order Multiple", meta.OrderMultiple, true);
        Text(5, 4, "Buyer/Planner (selected site)", input.BuyerPlannerCode); Number(6, 4, "QCTC", meta.Qctc);
        Number(7, 4, "Purchase Lead (calendar days)", input.PurchasingLeadCalendarDays);
        Number(8, 4, "Minimum Order", meta.MinimumOrder, true);
        Number(5, 7, "Nettable Opening", input.OpeningQoh, true); Number(6, 7, "Adjusted Opening", row.Past.ProjectedQoh, true);
        Number(7, 7, "Safety Time (working days)", input.SafetyTimeWorkingDays);
        Number(8, 7, "Safety Stock", input.SafetyStock, true);
        sheet.Row(5).Height = 30; sheet.Row(7).Height = 30; sheet.Row(9).Height = 30;
        sheet.Cell(HeadingRow, 1).Value = "Metric"; sheet.Cell(HeadingRow, 2).Value = "Past Due";
        for (var col = 0; col < periods.Count; col++)
        {
            var bucket = periods[col];
            if (col > 0) sheet.Cell(HeadingRow, col + 2).Value = bucket.Label;
            decimal[] values = [bucket.Demand, bucket.Receipts, bucket.ProjectedQoh, bucket.PlannedDue, bucket.PlannedRelease];
            for (var metric = 0; metric < Metrics.Length; metric++)
            {
                sheet.Cell(FirstMetricRow + metric, 1).Value = Metrics[metric];
                sheet.Cell(FirstMetricRow + metric, col + 2).Value = (double)LongTermQuantityPresentation.Round(values[metric], input.UnitOfMeasure);
            }
            if (bucket.HasUnconfirmed) sheet.Cell(FirstMetricRow + 1, col + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE2A3");
        }
        var heading = sheet.Range(HeadingRow, 1, HeadingRow, lastColumn);
        heading.Style.Font.Bold = true; heading.Style.Font.FontColor = XLColor.White;
        heading.Style.Fill.BackgroundColor = XLColor.FromHtml("#304A5E");
        heading.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sheet.Row(HeadingRow).Height = 26;
        sheet.Rows(FirstMetricRow, FirstMetricRow + 4).Height = 24;
        var numbers = sheet.Range(FirstMetricRow, 2, FirstMetricRow + 4, lastColumn);
        numbers.Style.NumberFormat.Format = quantityFormat;
        numbers.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        foreach (var offset in new[] { 2 })
        {
            var balance = sheet.Range(FirstMetricRow + offset, 1, FirstMetricRow + offset, lastColumn);
            balance.Style.Fill.BackgroundColor = XLColor.FromHtml("#F0F4F6");
            balance.Style.Font.Bold = true;
            sheet.Range(FirstMetricRow + offset, 2, FirstMetricRow + offset, lastColumn)
                .AddConditionalFormat().WhenLessThan(0).Font.SetFontColor(XLColor.FromHtml("#C00000"));
        }
        foreach (var offset in new[] { 0, 2, 3 })
            sheet.Range(FirstMetricRow + offset, 1, FirstMetricRow + offset, lastColumn).Style.Border.TopBorder = XLBorderStyleValues.Hair;
        var end = periods[^1].EndExclusive!.Value;
        Span(17, 2, 6, $"{asOf:yyyy-MM-dd}–{end.AddDays(-1):yyyy-MM-dd} · {period}");
        Span(17, 7, 9, includeUnconfirmed ? "Amber includes unconfirmed PO receipts." : "");
        Span(18, 2, 9, $"Acquired {source.AcquiredAtUtc:yyyy-MM-dd HH:mm} UTC{(row.DataQualityWarning is { } warning ? " · " + warning : "")}");
        sheet.Rows(17, 18).Height = 30;
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.PaperSize = XLPaperSize.LetterPaper; // Owner laptop print verification pending.
        sheet.PageSetup.Scale = 65; // Letter landscape compact header; never fit a long horizon to one page.
        sheet.PageSetup.PrintAreas.Add(1, 1, 18, Math.Max(9, lastColumn));
        if (lastColumn > 9) sheet.PageSetup.AddVerticalPageBreak(9);
        sheet.PageSetup.Margins.Left = 0.25;
        sheet.PageSetup.Margins.Right = 0.25;
        sheet.PageSetup.SetRowsToRepeatAtTop(HeadingRow, HeadingRow);
        sheet.PageSetup.SetColumnsToRepeatAtLeft(1, 1);
        sheet.PageSetup.Header.Left.AddText($"Component MRP | {input.ComponentPart} | {site} | {asOf:yyyy-MM-dd}");
        sheet.PageSetup.Footer.Left.AddText($"{period} | {asOf:yyyy-MM-dd} to {end.AddDays(-1):yyyy-MM-dd} | {(period == "Month" ? "* Partial calendar month" : period == "Week" ? "First week partial; Monday labels" : "Calendar days")}");
        sheet.PageSetup.Footer.Right.AddText("Page &P");
        using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
    }

    private static double WrappedHeight(string? value, int charactersPerLine, double lineHeight) =>
        Math.Max(lineHeight, (value ?? "").Split('\n').Sum(line => Math.Max(1, (int)Math.Ceiling(line.Length / (double)charactersPerLine))) * lineHeight);

    public static string SuggestedFileName(string part, string site, DateOnly asOf, int sequence = 1)
    {
        var invalid = "<>:\"/\\|?*";
        var safe = new string(part.Select(c => c < 32 || invalid.Contains(c) ? '_' : c).ToArray()).TrimEnd(' ', '.');
        if (safe.Length == 0) safe = "part";
        var stem = safe.Split('.')[0].ToUpperInvariant();
        if (stem is "CON" or "PRN" or "AUX" or "NUL" || System.Text.RegularExpressions.Regex.IsMatch(stem, "^(COM|LPT)[1-9¹²³]$")) safe = "_" + safe;
        // Bound the suggested representation only; report identity is untouched.
        if (safe.Length > 120) safe = safe[..120].TrimEnd(' ', '.');
        if (char.IsHighSurrogate(safe[^1])) safe = safe[..^1];
        return $"{safe}_MRP_{site}_{asOf:yyyy-MM-dd}_{sequence:00}.xlsx";
    }
}
