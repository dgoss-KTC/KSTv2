using ClosedXML.Excel;
using Kst.Domain.LongTermShortages;
using Kst.Exports.Contracts;
namespace Kst.Exports;
public sealed class PlaceholderExportService : IExportService
{
    public byte[] CreateLongTermShortagesWorkbook(string workspaceName, DateOnly refreshDate, IReadOnlyList<LongTermShortageRow> rows)
    {
        using var book = new XLWorkbook(); var sheet = book.Worksheets.Add("Shortages");
        var headers = new List<string> { "Comp", "QAD Status", "Shortage Severity", "Description", "Manufacturer Item", "Demand Parents", "KSS", "PO Number", "PO Line", "PO Due", "PO Open Qty", "PO Confirmed", "Planner", "B/P", "First Short", "Opening QOH", "Past Gross Requirements", "Past Scheduled Receipts", "Past Planned Due", "Past Planned Release Evidence", "Past Projected QOH" };
        headers.AddRange(Enumerable.Range(1, LongTermShortagesBuilder.WeekCount).Select(n => $"Week {n} Projected QOH"));
        for (var i = 0; i < headers.Count; i++) sheet.Cell(1, i + 1).Value = headers[i]; sheet.Row(1).Style.Font.Bold = true;
        for (var index = 0; index < rows.Count; index++) { var row = rows[index]; var values = new object?[] { row.ComponentPart, row.QadStatus, row.Severity.ToString(), row.Description, row.Presentation?.ManufacturerItem, string.Join(", ", row.DemandParentParts), row.Presentation?.IsKss == true ? "KSS" : null, row.Presentation?.PoNumber, row.Presentation?.PoLine, row.Presentation?.PoDueDate, row.Presentation?.PoOpenQuantity, row.Presentation?.PoConfirmed, row.Planner, row.BuyerPlannerCode, row.FirstShortDate, row.OpeningQoh, row.Past.GrossRequirements, row.Past.ScheduledReceipts, row.Past.PlannedOrdersDue, row.Past.PlannedOrdersRelease, row.Past.ProjectedQoh }; for (var i = 0; i < values.Length; i++) sheet.Cell(index + 2, i + 1).Value = XLCellValue.FromObject(values[i]); foreach (var week in row.Weeks) { var cell = sheet.Cell(index + 2, 21 + week.WeekNumber!.Value); cell.Value = week.ProjectedQoh; if (week.ProjectedQoh < 0) cell.Style.Fill.BackgroundColor = XLColor.MistyRose; } }
        var evidence = book.Worksheets.Add("Raw MRP Evidence");
        var evidenceHeaders = new[] { "Comp", "Evidence Ordinal", "MRP Type", "Due Date", "Release Date", "Quantity", "Schedule Category" };
        for (var i = 0; i < evidenceHeaders.Length; i++) evidence.Cell(1, i + 1).Value = evidenceHeaders[i]; evidence.Row(1).Style.Font.Bold = true;
        var evidenceRow = 2;
        foreach (var row in rows) foreach (var fact in row.Evidence) { var values = new object?[] { row.ComponentPart, fact.EvidenceOrdinal, fact.Type, fact.DueDate, fact.ReleaseDate, fact.Quantity, fact.Category.ToString() }; for (var i = 0; i < values.Length; i++) evidence.Cell(evidenceRow, i + 1).Value = XLCellValue.FromObject(values[i]); evidenceRow++; }
        evidence.Columns().AdjustToContents();
        sheet.Columns().AdjustToContents(); using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
    }
}
