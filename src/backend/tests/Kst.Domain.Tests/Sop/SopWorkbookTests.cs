using ClosedXML.Excel;
using Kst.Domain.Sop;
using Kst.Exports;

namespace Kst.Domain.Tests.Sop;

public class SopWorkbookTests
{
    public static SopReport Fixture(string group = "planr", bool blanks = false)
    {
        DateOnly[] weeks = [new(2026, 9, 21), new(2026, 9, 28), new(2026, 10, 5), new(2026, 10, 12)];
        var p = new SopPresentation(2026, new(2025, 6, 29), [], [new(2027, 3, 1, new(2026, 8, 23), 5, [weeks[0]]), new(2027, 4, 2, new(2026, 9, 27), 4, weeks.Skip(1).ToArray())]);
        SopRow Row(string? planner, string customer, double[] quantities) => new([new(Text: customer), new(Text: "=part"), new(Text: "C"), planner is null ? SopCell.Blank : new(Text: planner), new(Number: 2140), new(Text: "SUPPLY"), new(Number: 0.5), new(Number: 999)], quantities.Select(n => new SopCell(Number: n)).ToArray());
        var rows = blanks ? new[] { Row(null, "A", [10, 20, 0, 30]), Row("", "B", [1, 2, 0, 3]) }
            : new[] { Row("Alpha", "B", [10, 20, 0, 30]), Row("alpha", "A", [1, 2, 0, 3]), Row(null, "B", [7, 8, 0, 9]), Row("TotalCare", "C", [0, 0, 0, 0]) };
        return new("SW", group, new(["Customer", "Part", "St", "Planr", "Prod", "type", "Time Std", " Falldown"], weeks, rows), new(2026, 10, 12), DateTimeOffset.UtcNow, p);
    }
    [Fact]
    public void Paired_averages_period_formulas_text_notes_blanks_and_nested_totals()
    {
        var bytes = SopWorkbook.Create(Fixture()); using var book = new XLWorkbook(new MemoryStream(bytes)); var s = book.Worksheet(1);
        Assert.Single(book.Worksheets); Assert.Equal("SCHEDULING NOTES", s.Cell("I1").GetString());
        Assert.Equal("=part", s.Cell("B2").GetString()); Assert.False(s.Cell("B2").HasFormula);
        Assert.Equal("ROUND(AVERAGE(J2),0)", s.Cell("K2").FormulaA1);
        Assert.Equal("ROUND(AVERAGE(M2,N2,O2),0)", s.Cell("L2").FormulaA1);
        Assert.Equal("SUM(J2)", s.Cell("P2").FormulaA1); Assert.Equal("SUM(M2,N2,O2)", s.Cell("Q2").FormulaA1);
        Assert.Equal("FY27 P04 Total (partial 3/4w)", s.Cell("Q1").GetString());
        Assert.Equal("Alpha Total", s.Cell("D4").GetString()); Assert.Equal("SUBTOTAL(9,Q2:Q3)", s.Cell("Q4").FormulaA1);
        Assert.Equal("Grand Total", s.Cell("D8").GetString()); Assert.Equal("SUBTOTAL(9,Q2:Q7)", s.Cell("Q8").FormulaA1);
        Assert.Equal(72, s.Cell("Q8").Value.GetNumber()); Assert.False(s.Cell("H8").HasFormula);
        Assert.Equal(2, s.Row(2).OutlineLevel); Assert.Equal(1, s.Row(7).OutlineLevel); Assert.Equal(1, s.Row(8).OutlineLevel);
        Assert.True(s.Cell("D5").Style.Font.Bold); Assert.Equal(XLColor.FromHtml("#A9A9A9"), s.Cell("D5").Style.Fill.BackgroundColor);
        Assert.Empty(s.Tables); Assert.False(s.AutoFilter.IsEnabled); Assert.False(s.Protection.IsProtected);
        var root = Environment.GetEnvironmentVariable("KST_SOP_SYNTHETIC_ROOT");
        if (!string.IsNullOrEmpty(root)) File.WriteAllBytes(Path.Combine(root, "sop-synthetic.xlsx"), bytes);
    }
    [Theory]
    [InlineData("planr")]
    [InlineData("customer")]
    public void Grouping_mode_and_all_blank_results(string grouping)
    {
        using var book = new XLWorkbook(new MemoryStream(SopWorkbook.Create(Fixture(grouping, true)))); var s = book.Worksheet(1);
        if (grouping == "planr") { Assert.Equal("Grand Total", s.Cell("D4").GetString()); Assert.Equal("SUBTOTAL(9,J2:J3)", s.Cell("J4").FormulaA1); }
        else { Assert.Equal("A Total", s.Cell("A3").GetString()); Assert.Equal("B Total", s.Cell("A5").GetString()); }
    }
    [Fact]
    public void Physical_26_cap_is_not_week_horizon_and_periods_are_separate()
    {
        var start = new DateOnly(2026, 6, 28); var weeks = Enumerable.Range(0, 30).Select(i => start.AddDays(i * 7 + 1)).ToArray();
        int[] sizes = [4, 4, 5, 4, 4, 5, 4, 4]; var periods = new List<SopPeriod>(); var offset = 0;
        for (var i = 0; offset < 30; i++) { var visible = weeks.Skip(offset).Take(sizes[i]).ToArray(); periods.Add(new(2027, i + 1, i / 3 + 1, start.AddDays(offset * 7), sizes[i], visible)); offset += sizes[i]; }
        var source = Fixture().Source with { WeekLabels = weeks, Rows = [new(Fixture().Source.Rows[0].Fixed, Enumerable.Repeat(new SopCell(Number: 1), 30).ToArray())] };
        var report = Fixture() with { Source = source, Presentation = new(2026, new(2025, 6, 29), [], periods) };
        using var book = new XLWorkbook(new MemoryStream(SopWorkbook.Create(report))); var s = book.Worksheet(1);
        Assert.Equal("SUBTOTAL(9,AI2:AI2)", s.Cell("AI3").FormulaA1); Assert.False(s.Cell("AJ3").HasFormula);
        Assert.True(s.Cell("AR3").HasFormula); Assert.Equal(30, source.WeekLabels.Count);
    }
    [Fact]
    public void Descriptor_rejects_wrong_missing_duplicate_and_invalid_partition()
    {
        var f = Fixture(); Assert.True(SopRules.ValidPresentation(f.Source.WeekLabels, f.Presentation));
        Assert.False(SopRules.ValidPresentation(f.Source.WeekLabels, f.Presentation with { Periods = [f.Presentation.Periods[0]] }));
        Assert.False(SopRules.ValidPresentation(f.Source.WeekLabels, f.Presentation with { Periods = [f.Presentation.Periods[0], f.Presentation.Periods[1] with { Quarter = 4 }] }));
        Assert.False(SopRules.ValidPresentation(f.Source.WeekLabels, f.Presentation with { Exceptions = [new(2027, 4), new(2027, 12)] }));
    }
}
