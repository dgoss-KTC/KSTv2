using ClosedXML.Excel;
using Kst.Domain.LongTermShortages;
using Kst.Exports.Contracts;
namespace Kst.Exports;
public sealed class PlaceholderExportService : IExportService
{
    public byte[] CreateLongTermShortagesWorkbook(string workspaceName, DateOnly refreshDate, IReadOnlyList<LongTermShortageRow> rows, DateTimeOffset? acquiredAtUtc = null, string? consistencyMode = null)
    {
        using var book = new XLWorkbook(); var sheet = book.Worksheets.Add("Shortages");
        var metadata = book.Worksheets.Add("Report Metadata");
        metadata.Cell(1, 1).Value = "Workspace"; metadata.Cell(1, 2).Value = workspaceName;
        metadata.Cell(2, 1).Value = "Report Date"; metadata.Cell(2, 2).Value = refreshDate.ToString("yyyy-MM-dd");
        metadata.Cell(3, 1).Value = "Acquired At UTC"; metadata.Cell(3, 2).Value = acquiredAtUtc?.ToUniversalTime().ToString("O") ?? "Unavailable";
        metadata.Cell(4, 1).Value = "Consistency Mode"; metadata.Cell(4, 2).Value = consistencyMode ?? "Unavailable";
        metadata.Columns().AdjustToContents();
        var headers = new List<string> { "Comp", "QAD Status", "Shortage Severity", "Description", "Manufacturer Item", "Demand Parents", "KSS", "PO Number", "PO Line", "PO Due", "PO Open Qty", "PO Confirmed", "Planner", "B/P", "First Short", "Opening QOH", "Past Gross Requirements", "Past Scheduled Receipts", "Past Planned Due", "Past Planned Release Evidence", "Past Projected QOH" };
        headers.AddRange(Enumerable.Range(1, rows.FirstOrDefault()?.Weeks.Count ?? LongTermShortagesBuilder.WeekCount).Select(n => $"Week {n} Official Ending"));
        for (var i = 0; i < headers.Count; i++) sheet.Cell(1, i + 1).Value = headers[i]; sheet.Row(1).Style.Font.Bold = true;
        for (var index = 0; index < rows.Count; index++) { var row = rows[index]; var values = new object?[] { row.ComponentPart, row.QadStatus, row.Severity.ToString(), row.Description, row.Presentation?.ManufacturerItem, string.Join(", ", row.DemandParentParts), row.Presentation?.IsKss == true ? "KSS" : null, row.Presentation?.PoNumber, row.Presentation?.PoLine, row.Presentation?.PoDueDate, row.Presentation?.PoOpenQuantity is decimal poQty ? LongTermQuantityPresentation.Round(poQty, row.UnitOfMeasure) : null, row.Presentation?.PoConfirmed, row.Planner, row.BuyerPlannerCode, row.FirstShortDate, LongTermQuantityPresentation.Round(row.OpeningQoh, row.UnitOfMeasure), LongTermQuantityPresentation.Round(row.Past.GrossRequirements, row.UnitOfMeasure), LongTermQuantityPresentation.Round(row.Past.ScheduledReceipts, row.UnitOfMeasure), LongTermQuantityPresentation.Round(row.Past.PlannedOrdersDue, row.UnitOfMeasure), LongTermQuantityPresentation.Round(row.Past.PlannedOrdersRelease, row.UnitOfMeasure), LongTermQuantityPresentation.Round(row.Past.ProjectedQoh, row.UnitOfMeasure) }; for (var i = 0; i < values.Length; i++) sheet.Cell(index + 2, i + 1).Value = XLCellValue.FromObject(values[i]); if (!string.IsNullOrEmpty(row.DataQualityWarning)) sheet.Cell(index + 2, 3).CreateComment().AddText(row.DataQualityWarning); foreach (var week in row.Weeks) { var cell = sheet.Cell(index + 2, 21 + week.WeekNumber!.Value); cell.Value = LongTermQuantityPresentation.Round(week.ProjectedQoh, row.UnitOfMeasure); if (week.ProjectedQoh < 0) cell.Style.Fill.BackgroundColor = XLColor.MistyRose; } }
        var weekly = book.Worksheets.Add("Weekly Projection");
        var weekHeaders = new[] { "Comp", "Week", "Monday Label", "Gross Requirements", "Confirmed Receipts", "Unconfirmed Receipts", "Scheduled Receipts (Selected)", "Lowest Projected Balance", "Ending Projected Balance", "Planned Orders Due", "Ending Planning Balance", "Planned Orders Release" };
        for (var i = 0; i < weekHeaders.Length; i++) weekly.Cell(1, i + 1).Value = weekHeaders[i];
        weekly.Row(1).Style.Font.Bold = true;
        var weekRow = 2;
        foreach (var row in rows) foreach (var week in row.Weeks)
        {
            var round = (decimal value) => LongTermQuantityPresentation.Round(value, row.UnitOfMeasure);
            var values = new object?[] { row.ComponentPart, week.WeekNumber, week.WeekStart?.AddDays(1), round(week.GrossRequirements), round(week.ScheduledReceipts), round(week.UnconfirmedReceipts), round(week.ScheduledReceipts + (week.IncludesUnconfirmed ? week.UnconfirmedReceipts : 0)), round(week.LowestProjectedBalance), round(week.ProjectedQoh), round(week.PlannedOrdersDue), round(week.IncludesUnconfirmed ? week.AllReceiptsPlanningEnding : week.PlanningEnding), round(week.PlannedOrdersRelease) };
            for (var i = 0; i < values.Length; i++) weekly.Cell(weekRow, i + 1).Value = XLCellValue.FromObject(values[i]);
            if (week.ProjectedQoh < 0) weekly.Cell(weekRow, 9).Style.Fill.BackgroundColor = XLColor.MistyRose;
            weekRow++;
        }
        weekly.Columns().AdjustToContents();
        var episodes = book.Worksheets.Add("Shortage Episodes");
        var episodeHeaders = new[] { "Comp", "Start Date", "Deepest Date", "Maximum Shortage", "First Recovery", "Stable Clear" };
        for (var i = 0; i < episodeHeaders.Length; i++) episodes.Cell(1, i + 1).Value = episodeHeaders[i];
        var episodeRow = 2;
        foreach (var row in rows) foreach (var episode in row.Episodes)
        {
            var values = new object?[] { row.ComponentPart, episode.StartDate, episode.DeepestDate, LongTermQuantityPresentation.Round(episode.MaximumShortage, row.UnitOfMeasure), episode.FirstRecoveryDate, episode.StableClearDate };
            for (var i = 0; i < values.Length; i++) episodes.Cell(episodeRow, i + 1).Value = XLCellValue.FromObject(values[i]);
            episodeRow++;
        }
        episodes.Row(1).Style.Font.Bold = true;
        episodes.Columns().AdjustToContents();
        var evidence = book.Worksheets.Add("Raw MRP Evidence");
        var evidenceHeaders = new[] { "Comp", "Evidence Ordinal", "MRP Type", "Due Date", "Release Date", "Quantity", "Schedule Category", "Source Number", "Source Line", "Source Line 2", "PO Receipt", "Confirmed", "Source Row ID" };
        for (var i = 0; i < evidenceHeaders.Length; i++) evidence.Cell(1, i + 1).Value = evidenceHeaders[i]; evidence.Row(1).Style.Font.Bold = true;
        var evidenceRow = 2;
        foreach (var row in rows) foreach (var fact in row.Evidence) { var values = new object?[] { row.ComponentPart, fact.EvidenceOrdinal, fact.Type, fact.DueDate, fact.ReleaseDate, fact.Quantity, fact.Category.ToString(), fact.SourceNumber, fact.SourceLine, fact.SourceLine2, fact.IsPoReceipt, fact.PoConfirmed, fact.SourceRowId }; for (var i = 0; i < values.Length; i++) evidence.Cell(evidenceRow, i + 1).Value = XLCellValue.FromObject(values[i]); evidenceRow++; }
        evidence.Columns().AdjustToContents();
        sheet.Columns().AdjustToContents(); using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
    }
}
