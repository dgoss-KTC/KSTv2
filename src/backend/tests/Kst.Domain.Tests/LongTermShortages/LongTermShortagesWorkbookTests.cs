using ClosedXML.Excel;
using Kst.Domain.LongTermShortages;
using Kst.Exports;

namespace Kst.Domain.Tests.LongTermShortages;

public sealed class LongTermShortagesWorkbookTests
{
    [Fact]
    public void CreateWorkbook_PreservesPresentationContextAndRawMrpEvidence()
    {
        var row = new LongTermShortageRow("COMP", "EA", "P", "Description", "Planner", "BP", 10m,
            SafetyStockState.Resolved, 0m, LongTermShortageSeverity.Healthy, null, ["PARENT"],
            new(null, null, 1m, 2m, 3m, 4m, 14m, LongTermShortageSeverity.Healthy),
            [new(1, new DateOnly(2026, 9, 14), 4m, 0m, 0m, 0m, 10m, LongTermShortageSeverity.Healthy)],
            [new(1, "DEMAND", new DateOnly(2026, 9, 15), null, 4m, MrpScheduleCategory.GrossRequirement)],
            new("MFG-1", "PO-1", 2, new DateOnly(2026, 9, 16), 7m, true, true));

        using var stream = new MemoryStream(new PlaceholderExportService().CreateLongTermShortagesWorkbook("Workspace", new DateOnly(2026, 9, 15), [row], new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero), LongTermShortageAcquisition.ConsistencyMode));
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("Shortages");

        Assert.Equal("Manufacturer Item", sheet.Cell(1, 5).GetString());
        Assert.Equal("MFG-1", sheet.Cell(2, 5).GetString());
        Assert.Equal("KSS", sheet.Cell(2, 7).GetString());
        Assert.Equal("PO-1", sheet.Cell(2, 8).GetString());
        Assert.Equal(14m, sheet.Cell(2, 21).GetValue<decimal>());
        Assert.Equal(10m, sheet.Cell(2, 22).GetValue<decimal>());
        Assert.Equal("DEMAND", workbook.Worksheet("Raw MRP Evidence").Cell(2, 3).GetString());
        Assert.Equal(4m, workbook.Worksheet("Raw MRP Evidence").Cell(2, 6).GetValue<decimal>());
        Assert.Equal("Monday Label", workbook.Worksheet("Weekly Projection").Cell(1, 3).GetString());
        Assert.Equal(4m, workbook.Worksheet("Weekly Projection").Cell(2, 4).GetValue<decimal>());
        Assert.Equal("PRO2_READ_UNCOMMITTED", workbook.Worksheet("Report Metadata").Cell(4, 2).GetString());
        Assert.Contains("2026-09-15T12:00:00", workbook.Worksheet("Report Metadata").Cell(3, 2).GetString());
    }

    [Fact]
    public void CreateWorkbook_PreservesSeverityAndFullWarningWithoutChangingHeaders()
    {
        const string warning = "Selected-site planning data missing; safety stock and lead times unknown.";
        var row = new LongTermShortageRow("COMP", "EA", "P", "Description", "Planner", "BP", 10m,
            SafetyStockState.SelectedSiteValueMissing, null, LongTermShortageSeverity.SafetyStockUnavailable, null, ["PARENT"],
            new(null, null, 0m, 0m, 0m, 0m, 10m, LongTermShortageSeverity.SafetyStockUnavailable),
            [], [], null) { DataQualityWarning = warning };

        using var stream = new MemoryStream(new PlaceholderExportService().CreateLongTermShortagesWorkbook("Workspace", new DateOnly(2026, 9, 15), [row]));
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("Shortages");

        Assert.Equal("QAD Status", sheet.Cell(1, 2).GetString());
        Assert.Equal("Shortage Severity", sheet.Cell(1, 3).GetString());
        Assert.Equal("Opening QOH", sheet.Cell(1, 16).GetString());
        Assert.Equal("P", sheet.Cell(2, 2).GetString());
        Assert.Equal("SafetyStockUnavailable", sheet.Cell(2, 3).GetString());
        Assert.Equal(10m, sheet.Cell(2, 16).GetValue<decimal>());
        Assert.Contains(warning, sheet.Cell(2, 3).GetComment().Text);
    }
}
