using ClosedXML.Excel;
using Kst.Domain.ComponentMrp;
using Kst.Domain.LongTermShortages;
using Kst.Exports;

namespace Kst.Domain.Tests.ComponentMrp;

public class ComponentMrpPeriodTests
{
    private static readonly DateOnly AsOf = new(2026, 9, 29);
    private static LongTermMrpFact F(string type, string due, decimal qty, bool? confirmed = null, string? release = null) =>
        new(1, type, DateOnly.Parse(due), release is null ? null : DateOnly.Parse(release), qty, MrpScheduleCategory.Unclassified)
        { IsPoReceipt = confirmed is not null, PoConfirmed = confirmed };
    private static ComponentMrpSource Source() => new(new("I3-PART", "KG", "A", "Customer sharing synthetic", null, "selected-buyer", 10,
        SafetyStockState.Resolved, 0, [], [F("DEMAND", "2026-09-28", 2), F("SUPPLY", "2026-09-28", 50, false),
        F("DEMAND", "2026-09-29", .004m), F("DEMAND", "2026-09-30", .004m), F("SUPPLY", "2026-09-30", 3, true),
        F("SUPPLY", "2026-10-01", 5, false), F("SUPPLY", "2026-10-02", -5, false),
        F("SUPPLYP", "2026-10-03", 7, release:"2026-09-30")]), new(null, null, null, null, null, .00152m), DateTimeOffset.Parse("2026-09-29T12:00Z"));
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Daily_weekly_monthly_window_balance_flows_and_amber(bool include)
    {
        var source = Source(); var row = LongTermShortagesBuilder.Build(AsOf, [source.Input], 13, include)[0];
        var day = ComponentMrpPeriods.Build(AsOf, row, include, "Day"); var week = ComponentMrpPeriods.Build(AsOf, row, include, "Week"); var month = ComponentMrpPeriods.Build(AsOf, row, include, "Month");
        Assert.Equal(new DateOnly(2026, 12, 27), day[^1].EndExclusive); Assert.Equal(day[^1].EndExclusive, week[^1].EndExclusive); Assert.Equal(day[^1].EndExclusive, month[^1].EndExclusive);
        Assert.Equal(8, day[0].ProjectedQoh); Assert.Equal(include ? 50 : 0, day[0].Receipts); // overdue receipts do not seed balance
        Assert.Equal(7.996m, day[1].ProjectedQoh); Assert.Equal(10.992m, day[2].ProjectedQoh);
        Assert.Equal(7, day[2].PlannedRelease); Assert.Equal(7, day[5].PlannedDue); // due not receipts
        Assert.Equal(include ? 15.992m : 10.992m, day[3].ProjectedQoh);
        Assert.Equal(.008m, month[1].Demand); Assert.Equal(3, month[1].Receipts); Assert.Equal(10.992m, month[1].ProjectedQoh);
        Assert.Equal(0, month[2].Receipts); Assert.Equal(include, month[2].HasUnconfirmed); // +5/-5 still amber
        Assert.Equal(10.992m, month[2].ProjectedQoh); Assert.Equal(0, month[3].Demand); Assert.Equal(10.992m, month[3].ProjectedQoh);
        for (var i = 0; i < row.Weeks.Count; i++)
        {
            var b = week[i + 1]; var days = day.Skip(1).Where(d => d.Start >= b.Start && d.EndExclusive <= b.EndExclusive).ToArray();
            Assert.Equal(row.Weeks[i].ProjectedQoh, days[^1].ProjectedQoh); Assert.Equal(b.Demand, days.Sum(d => d.Demand)); Assert.Equal(b.Receipts, days.Sum(d => d.Receipts));
            Assert.Equal(b.PlannedRelease, days.Sum(d => d.PlannedRelease)); Assert.Equal(b.PlannedDue, days.Sum(d => d.PlannedDue));
        }
        foreach (var period in new[] { "Day", "Week", "Month" })
        {
            using var book = new XLWorkbook(new MemoryStream(ComponentMrpWorkbook.Create("SW", AsOf, include, source, row, period)));
            var sheet = book.Worksheet(1); Assert.Single(book.Worksheets); Assert.All(sheet.CellsUsed(), c => Assert.False(c.HasFormula));
            if (period == "Month") { Assert.Equal(.01, sheet.Cell(ComponentMrpWorkbook.FirstMetricRow, 3).GetDouble()); Assert.Equal(include ? "FFE2A3" : "FFFFFF", sheet.Cell(ComponentMrpWorkbook.FirstMetricRow + 1, 4).Style.Fill.BackgroundColor.Color.ToArgb().ToString("X8")[2..]); }
            var baselineRoot = Environment.GetEnvironmentVariable("KST_MRP_COMPARE_ROOT");
            if (baselineRoot is not null)
            {
                using var old = new XLWorkbook(Path.Combine(baselineRoot, $"I3_{period}_{include}.xlsx")); var before = old.Worksheet(1);
                foreach (var metric in ComponentMrpWorkbook.Metrics)
                {
                    var a = before.Column(1).CellsUsed().Single(c => c.GetString() == metric).Address.RowNumber; var b = sheet.Column(1).CellsUsed().Single(c => c.GetString() == metric).Address.RowNumber;
                    for (var col = 2; col <= sheet.Row(ComponentMrpWorkbook.HeadingRow).LastCellUsed()!.Address.ColumnNumber; col++) { Assert.Equal(before.Cell(a, col).DataType, sheet.Cell(b, col).DataType); Assert.Equal(before.Cell(a, col).GetDouble(), sheet.Cell(b, col).GetDouble()); Assert.Equal(before.Cell(a, col).Style.Fill.BackgroundColor, sheet.Cell(b, col).Style.Fill.BackgroundColor); }
                }
            }
            var root = Environment.GetEnvironmentVariable("KST_MRP_FORMAT_ROOT"); if (root is not null) { Directory.CreateDirectory(root); book.SaveAs(Path.Combine(root, $"I3_{period}_{include}.xlsx")); }
        }
    }
}
