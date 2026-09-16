using Kst.Exports.Contracts;
using ClosedXML.Excel;
using Kst.Domain.LongTermShortages;

namespace Kst.Exports;

/// <summary>
/// Stage 11-A controlled workbook export. Its input is the previously loaded projection, never QAD.
/// </summary>
public sealed class PlaceholderExportService : IExportService
{
    public byte[] CreateLongTermShortagesWorkbook(string workspaceName, DateOnly refreshDate, IReadOnlyList<LongTermShortageRow> rows)
    {
        using var book = new XLWorkbook();
        var sheet = book.Worksheets.Add("Shortages");
        var headers = new List<string> { "Comp", "Comments", "QAD Status", "Shortage Severity", "Description", "Manufacturer Item", "Demand", "KSS", "PO Due", "PO Qty", "Confirmed", "Wks LT", "Planner", "B/P", "First Short", "On Hand" };
        headers.AddRange(Enumerable.Range(1, LongTermShortagesBuilder.WeekCount).Select(x => $"Week {x}"));
        for (var i = 0; i < headers.Count; i++) sheet.Cell(1, i + 1).Value = headers[i];
        sheet.Row(1).Style.Font.Bold = true;
        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var qualifyingPurchaseOrders = row.PurchaseOrders.Where(x => x.Confirmed == true && !x.IsScheduled && x.DueDate is not null).ToList();
            var po = qualifyingPurchaseOrders.Where(x => x.DueDate >= row.Weeks[0].WeekStart)
                .OrderBy(x => x.DueDate).ThenBy(x => x.PoNumber).ThenBy(x => x.PoLine).FirstOrDefault();
            var comment = !qualifyingPurchaseOrders.Any() ? "NO PO PLACED"
                : qualifyingPurchaseOrders.Any(x => x.DueDate < row.Weeks[0].WeekStart) ? "PAST DUE" : "";
            var values = new object?[] { row.ComponentPart, comment, row.QadStatus, row.Severity.ToString(), row.Description, po?.ManufacturerItem, string.Join(", ", row.DemandParentParts), row.IsKss ? "KSS" : "", po?.DueDate, po is null ? null : LongTermShortageQuantityDisplayPolicy.Round(po.OpenQuantity, row.UnitOfMeasure), po?.Confirmed, row.LeadTimeWeeks, row.Planner, row.BuyerPlannerCode, row.FirstSafetyStockShortWeek, LongTermShortageQuantityDisplayPolicy.Round(row.OpeningQoh, row.UnitOfMeasure) };
            for (var i = 0; i < values.Length; i++) sheet.Cell(r + 2, i + 1).Value = XLCellValue.FromObject(values[i]);
            foreach (var week in row.Weeks)
            {
                var cell = sheet.Cell(r + 2, 16 + week.WeekNumber);
                cell.Value = LongTermShortageQuantityDisplayPolicy.Round(week.Balance, row.UnitOfMeasure);
                if (week.Balance < 0) cell.Style.Fill.BackgroundColor = XLColor.MistyRose;
            }
        }
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        book.SaveAs(stream);
        return stream.ToArray();
    }
}
