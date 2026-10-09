using ClosedXML.Excel;
using Kst.Domain.ComponentMrp;
using Kst.Domain.LongTermShortages;
using Kst.Exports;

namespace Kst.Domain.Tests.ComponentMrp;

public class ComponentMrpWorkbookTests
{
    private static readonly DateOnly AsOf = new(2026, 9, 24);
    private static LongTermMrpFact Fact(string id, string type, decimal quantity, int day, bool po = false, bool? confirmed = null) =>
        new(1, type, new DateOnly(2026, 9, day), null, quantity, MrpScheduleCategory.Unclassified)
        { SourceRowId = id, IsPoReceipt = po, PoConfirmed = confirmed };
    private static ComponentMrpSource Source(decimal opening, string? uom, params LongTermMrpFact[] facts) =>
        new(new("ABC-123", uom, "A", "Exact part", null, "site-buyer", opening, SafetyStockState.Resolved, 0, [], facts)
        { SitePlanningPresent = true }, new("IOS", null, null, null, 0, .00152m), DateTimeOffset.Parse("2026-09-24T12:00:00Z"));

    [Fact]
    public void F02_past_and_week_one_match_hand_established_seven_metrics()
    {
        var source = Source(10, "EA", Fact("A", "DEMAND", 3, 22), Fact("B", "SUPPLY", 100, 23, true, true),
            Fact("C", "DEMAND", 8, 24), Fact("D", "SUPPLY", 5, 24, true, true));
        using var book = Workbook(source, false);
        var syntheticRoot = Environment.GetEnvironmentVariable("KST_MRP_TEST_ROOT");
        if (syntheticRoot is not null) { Directory.CreateDirectory(syntheticRoot); book.SaveAs(Path.Combine(syntheticRoot, "synthetic-source.xlsx")); }
        var sheet = book.Worksheet(1);
        decimal[] past = [3, 100, 7, 7, 0, 7, 0], week = [8, 5, -1, 4, 0, 4, 0];
        AssertColumn(sheet, 2, past); AssertColumn(sheet, 3, week);
        Assert.Equal("Past Due", sheet.Cell(ComponentMrpWorkbook.HeadingRow, 2).GetString());
        Assert.Equal("2026-09-21", sheet.Cell(ComponentMrpWorkbook.HeadingRow, 3).GetString());
        Assert.Equal("ABC-123", sheet.Cell(2, 2).GetString());
        Assert.Equal("site-buyer", sheet.Cell(5, 6).GetString());
        Assert.Equal("Unavailable", sheet.Cell(7, 9).GetString());
        Assert.Equal(.00152, sheet.Cell(6, 6).GetDouble(), 5);
        Assert.All(sheet.CellsUsed(), cell => Assert.False(cell.HasFormula));
        Assert.Equal(28, sheet.LastColumnUsed()!.ColumnNumber());
        var baselinePath = Environment.GetEnvironmentVariable("KST_MRP_BASELINE_WORKBOOK");
        if (baselinePath is not null)
        {
            using var baseline = new XLWorkbook(baselinePath);
            var old = baseline.Worksheet("Component MRP");
            foreach (var metric in ComponentMrpWorkbook.Metrics)
            {
                var oldMetric = metric == "Projected QOH" ? "Ending Projected Balance" : metric;
                var beforeRow = old.Column(1).CellsUsed().Single(c => c.GetString() == oldMetric).Address.RowNumber;
                var afterRow = sheet.Column(1).CellsUsed().Single(c => c.GetString() == metric).Address.RowNumber;
                for (var column = 2; column <= 28; column++)
                {
                    Assert.Equal(old.Cell(beforeRow, column).DataType, sheet.Cell(afterRow, column).DataType);
                    Assert.Equal(old.Cell(beforeRow, column).GetDouble(), sheet.Cell(afterRow, column).GetDouble());
                }
            }
        }
    }

    [Fact]
    public void F03_modes_keep_daily_low_even_when_selected_receipts_recover()
    {
        var source = Source(5, "EA", Fact("A", "DEMAND", 8, 24), Fact("B", "SUPPLY", 2, 24, true, true), Fact("C", "SUPPLY", 4, 24, true, false));
        using var confirmed = Workbook(source, false); using var all = Workbook(source, true);
        AssertColumn(confirmed.Worksheet(1), 3, [8, 2, -3, -1, 0, -1, 0]);
        AssertColumn(all.Worksheet(1), 3, [8, 6, -3, 3, 0, 3, 0]);
    }

    [Fact]
    public void F06_rounds_after_aggregation_and_stores_numeric_values()
    {
        var source = Source(1, "KG", Fact("A", "DEMAND", .004m, 24), Fact("B", "DEMAND", .004m, 24));
        var row = LongTermShortagesBuilder.Build(AsOf, [source.Input])[0];
        Assert.Equal(.008m, row.Weeks[0].GrossRequirements);
        using var book = Workbook(source, false);
        AssertColumn(book.Worksheet(1), 3, [.01m, 0, .99m, .99m, 0, .99m, 0]);
    }

    [Theory]
    [InlineData(13)]
    [InlineData(26)]
    [InlineData(52)]
    [InlineData(72)]
    public void Empty_mrp_is_valid_and_horizon_is_exact(int weeks)
    {
        var source = Source(-1.5m, "EA");
        var row = LongTermShortagesBuilder.Build(AsOf, [source.Input], weeks)[0];
        using var book = new XLWorkbook(new MemoryStream(ComponentMrpWorkbook.Create("SW", AsOf, false, source, row)));
        Assert.Equal(weeks + 2, book.Worksheet(1).LastColumnUsed()!.ColumnNumber());
        AssertColumn(book.Worksheet(1), 3, [0, 0, -2, -2, 0, -2, 0]);
    }

    [Fact]
    public void Filename_sanitizes_only_representation_and_sequence_grows()
    {
        Assert.Equal("ABC-123_MRP_SW_2026-09-24_01.xlsx", ComponentMrpWorkbook.SuggestedFileName("ABC-123", "SW", AsOf));
        Assert.StartsWith("_CON_MRP_", ComponentMrpWorkbook.SuggestedFileName("CON", "SW", AsOf));
        Assert.Equal("A_B_MRP_SW_2026-09-24_100.xlsx", ComponentMrpWorkbook.SuggestedFileName("A/B", "SW", AsOf, 100));
    }

    private static XLWorkbook Workbook(ComponentMrpSource source, bool all) => new(new MemoryStream(ComponentMrpWorkbook.Create("SW", AsOf, all, source,
        LongTermShortagesBuilder.Build(AsOf, [source.Input], 26, all)[0])));
    private static void AssertColumn(IXLWorksheet sheet, int column, decimal[] expected)
    {
        // Historical seven-value fixture arithmetic is retained; only exported rows changed.
        var indices = new[] { 0, 1, 3, 4, 6 };
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(ComponentMrpWorkbook.Metrics[i], sheet.Cell(ComponentMrpWorkbook.FirstMetricRow + i, 1).GetString());
            Assert.Equal(XLDataType.Number, sheet.Cell(ComponentMrpWorkbook.FirstMetricRow + i, column).DataType);
            Assert.Equal((double)expected[indices[i]], sheet.Cell(ComponentMrpWorkbook.FirstMetricRow + i, column).GetDouble(), 5);
        }
    }

    [Theory]
    [InlineData(13, "EA")]
    [InlineData(26, "EA")]
    [InlineData(26, "KG")]
    [InlineData(52, "KG")]
    [InlineData(72, "KG")]
    [InlineData(13, null)]
    public void Compact_layout_and_print_settings_preserve_all_horizons(int weeks, string? uom)
    {
        var source = Source(5, uom, Fact("A", "DEMAND", 8, 24), Fact("B", "SUPPLY", 2, 24, true, true));
        if (uom == "KG") source = Source(5.125m, uom, Fact("A", "DEMAND", 8.375m, 24), Fact("B", "SUPPLY", 2.25m, 24, true, true));
        if (uom == "KG") source = source with
        {
            Input = source.Input with
            {
                Description = string.Join(" ", Enumerable.Repeat("Long synthetic component description for wrapping and readability", 8)),
                SitePlanningPresent = false,
                SafetyStock = null,
                BuyerPlannerCode = null
            }
        };
        var projection = LongTermShortagesBuilder.Build(AsOf, [source.Input], weeks)[0];
        using var book = new XLWorkbook(new MemoryStream(ComponentMrpWorkbook.Create("SW", AsOf, false, source, projection)));
        var sheet = Assert.Single(book.Worksheets);
        Assert.Equal("Component MRP", sheet.Name);
        Assert.False(sheet.ShowGridLines);
        Assert.Equal(100, sheet.SheetView.ZoomScale);
        Assert.All(sheet.MergedRanges, range => Assert.True(range.LastRow().RowNumber() < ComponentMrpWorkbook.HeadingRow || range.FirstRow().RowNumber() > 15));
        Assert.Equal(XLPageOrientation.Landscape, sheet.PageSetup.PageOrientation);
        Assert.Equal(65, sheet.PageSetup.Scale);
        Assert.Single(sheet.PageSetup.PrintAreas);
        Assert.All(sheet.Column(1).CellsUsed(), cell => Assert.InRange(cell.Address.RowNumber, 10, 15));
        Assert.DoesNotContain(sheet.CellsUsed(), cell => new[] { "Inspection Lead", "Cumulative Lead (calendar days)", "Time Fence" }.Contains(cell.GetString()));
        Assert.Contains(sheet.CellsUsed(), cell => cell.GetString() == "Safety Stock");
        Assert.Contains(sheet.CellsUsed(), cell => cell.GetString() == "Purchase Lead (calendar days)");
        Assert.DoesNotContain(sheet.MergedRanges, range => range.FirstColumn().ColumnNumber() == 1);
        Assert.Equal(17, sheet.Column(2).Width); Assert.Equal(sheet.Column(2).Width, sheet.Column(weeks + 2).Width);
        Assert.Equal(uom == "EA" ? "#,##0" : "#,##0.00", sheet.Cell(11, 3).Style.NumberFormat.Format);
        // ClosedXML consolidates identical rules over disjoint balance ranges on save.
        Assert.Single(sheet.ConditionalFormats);
        foreach (var balanceRow in new[] { 13 })
            Assert.Contains(sheet.ConditionalFormats.Single().Ranges, range => range.Contains(sheet.Cell(balanceRow, 3)));
        Assert.DoesNotContain(sheet.ConditionalFormats.Single().Ranges, range => range.Contains(sheet.Cell(15, 3)));
        AssertColumn(sheet, 3, uom == "KG" ? [8.38m, 2.25m, -3.25m, -1, 0, -1, 0] : [8, 2, -3, -1, 0, -1, 0]);
        AssertColumn(sheet, weeks + 2, [0, 0, -1, -1, 0, -1, 0]);
        Assert.All(sheet.CellsUsed(), cell => Assert.False(cell.HasFormula));
        if (uom == "KG") { Assert.True(sheet.Row(3).Height > 22); Assert.Contains("planning data missing", sheet.Cell(2, 7).GetString()); }
        var root = Environment.GetEnvironmentVariable("KST_MRP_FORMAT_ROOT");
        if (root is not null) { Directory.CreateDirectory(root); book.SaveAs(Path.Combine(root, $"ComponentMRP_{weeks}_{uom}.xlsx")); }
    }
}
