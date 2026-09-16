using ClosedXML.Excel;
using Kst.Domain.LongTermShortages;
using Kst.Exports;

namespace Kst.Domain.Tests.LongTermShortages;

public sealed class LongTermShortagesWorkbookTests
{
    [Fact]
    public void CreateWorkbook_UsesApprovedHeadersCommentsAndNegativeStyle()
    {
        var weekStart = new DateOnly(2026, 9, 13);
        var weeks = Enumerable.Range(1, LongTermShortagesBuilder.WeekCount)
            .Select(week => new LongTermShortageWeek(week, weekStart.AddDays((week - 1) * 7), 0, 0, 0, week == 1 ? -1 : 0, LongTermShortageSeverity.CriticalShort)).ToList();
        var row = new LongTermShortageRow("COMP", "EA", null, "Description", false, 2, "Planner", "BP", 1,
            SafetyStockState.Resolved, 0, LongTermShortageSeverity.CriticalShort, 1, 1, ["PARENT"], [],
            [new("PO-PAST", 1, weekStart.AddDays(-1), 2, true, "MFG", false), new("PO-NEXT", 2, weekStart, 3, true, "MFG2", false)], weeks);

        var bytes = new PlaceholderExportService().CreateLongTermShortagesWorkbook("Workspace", new DateOnly(2026, 9, 15), [row]);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("Shortages");
        Assert.Equal("Comp", sheet.Cell(1, 1).GetString());
        Assert.Equal("Week 24", sheet.Cell(1, 40).GetString());
        Assert.Equal("PAST DUE", sheet.Cell(2, 2).GetString());
        Assert.Equal(3m, sheet.Cell(2, 10).GetValue<decimal>());
        Assert.NotEqual(XLColor.NoColor, sheet.Cell(2, 17).Style.Fill.BackgroundColor);
    }

    [Fact]
    public void CreateWorkbook_RoundsAllQuantityCellsButPreservesNumericCells()
    {
        var weekStart = new DateOnly(2026, 9, 13);
        var ea = new LongTermShortageRow("EA-COMP", "EACH", null, null, false, null, null, null, -0.4m,
            SafetyStockState.Resolved, -4.5m, LongTermShortageSeverity.CriticalShort, 1, 1, [], [],
            [new("EA-PO", 1, weekStart, 4.5m, true, null, false)],
            [new(1, weekStart, -4.5m, 4.5m, 4.5m, -0.4m, LongTermShortageSeverity.CriticalShort)]);
        var ml = ea with { ComponentPart = "ML-COMP", UnitOfMeasure = "ML", OpeningQoh = -73.44360902m, SafetyStock = 73.44360902m, PurchaseOrders = [new("ML-PO", 1, weekStart, -73.44360902m, true, null, false)], Weeks = [new(1, weekStart, 73.44360902m, -73.44360902m, 73.44360902m, -73.44360902m, LongTermShortageSeverity.CriticalShort)] };

        using var stream = new MemoryStream(new PlaceholderExportService().CreateLongTermShortagesWorkbook("Workspace", weekStart, [ea, ml]));
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("Shortages");

        Assert.Equal(0m, sheet.Cell(2, 16).GetValue<decimal>());
        Assert.Equal(5m, sheet.Cell(2, 10).GetValue<decimal>());
        Assert.Equal(0m, sheet.Cell(2, 17).GetValue<decimal>());
        Assert.Equal(-73.4437m, sheet.Cell(3, 16).GetValue<decimal>());
        Assert.Equal(-73.4437m, sheet.Cell(3, 10).GetValue<decimal>());
        Assert.Equal(-73.4437m, sheet.Cell(3, 17).GetValue<decimal>());
        Assert.True(sheet.Cell(2, 17).DataType is XLDataType.Number);
        Assert.True(sheet.Cell(3, 17).DataType is XLDataType.Number);
    }
}
