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
            SafetyStockState.Resolved, 0m, LongTermShortageSeverity.None, null, ["PARENT"],
            new(null, null, 1m, 2m, 3m, 4m, 14m, LongTermShortageSeverity.None),
            [new(1, new DateOnly(2026, 9, 14), 4m, 0m, 0m, 0m, 10m, LongTermShortageSeverity.None)],
            [new(1, "DEMAND", new DateOnly(2026, 9, 15), null, 4m, MrpScheduleCategory.GrossRequirement)],
            new("MFG-1", "PO-1", 2, new DateOnly(2026, 9, 16), 7m, true, true));

        using var stream = new MemoryStream(new PlaceholderExportService().CreateLongTermShortagesWorkbook("Workspace", new DateOnly(2026, 9, 15), [row]));
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("Shortages");

        Assert.Equal("Manufacturer Item", sheet.Cell(1, 5).GetString());
        Assert.Equal("MFG-1", sheet.Cell(2, 5).GetString());
        Assert.Equal("KSS", sheet.Cell(2, 7).GetString());
        Assert.Equal("PO-1", sheet.Cell(2, 8).GetString());
        Assert.Equal(14m, sheet.Cell(2, 21).GetValue<decimal>());
        Assert.Equal(10m, sheet.Cell(2, 22).GetValue<decimal>());
        Assert.Equal("DEMAND", workbook.Worksheet("Raw MRP Evidence").Cell(2, 3).GetString());
    }
}
