using System.IO.Compression;
using System.Xml.Linq;
using ClosedXML.Excel;
using Kst.Domain.ShipmentsToGo;
using Kst.Exports;

namespace Kst.Domain.Tests.ShipmentsToGo;

public class ShipmentsToGoTests
{
    private static ShipmentToGoLine Row(string order, string? crc, double ext, double qoh = 10, string? comments = "", string site = "SW", string? orderSite = "SW") =>
        new(new(2026, 10, 9), order, "=not-a-formula", "Standard", 1, null, "000123", null, site, orderSite,
            2, null, 0, qoh, "Ship name", ext, crc, qoh == 0 ? -ext : 0, comments);

    [Theory]
    [InlineData(9, false, 10)]
    [InlineData(9, true, 17)]
    [InlineData(10, false, 17)]
    [InlineData(10, true, 24)]
    public void Saturday_and_next_week_cutoffs_are_preserved(int day, bool next, int cutoff) =>
        Assert.Equal(new DateOnly(2026, 10, cutoff), ShipmentsToGoRules.Cutoff(new(2026, 10, day), next));

    [Fact]
    public void Risk_repair_sort_and_routing_preserve_python_quirks()
    {
        var rows = new[] {
            Row("123", "", 100, -1), Row("donor", "Z", 50, comments: "Ref 123!", site: "NW"),
            Row("memo", "A", 200, 0, "Memo"), Row("adj", "A", 200, 0, "Memo ADJ"),
            Row("ar-header", "A", 10, site: "SW", orderSite: "AR"), Row("kv", "A", 10, site: "KV", orderSite: "KV") };
        var report = ShipmentsToGoRules.Build(["SW", "NW", "AR", "KV"], rows, new Dictionary<string, double>(), new(2026, 10, 9), DateTimeOffset.UtcNow);
        Assert.Equal(new[] { "KTC-SW S2G", "KTC-AR S2G", "KTV S2G" }, report.Sheets.Select(s => s.Name));
        var main = report.Sheets[0].Lines;
        Assert.Equal("Z", main.Single(r => r.Order == "123").Crc);
        Assert.Equal(0, main.Single(r => r.Order == "123").Risk);
        Assert.Equal("No Stock; ", main.Single(r => r.Order == "123").Comments);
        Assert.Equal(0, main.Single(r => r.Order == "memo").Risk);
        Assert.Equal(-200, main.Single(r => r.Order == "adj").Risk);
        Assert.Equal("adj", main[0].Order);
        Assert.False(ShipmentsToGoRules.ValidSites(["KS"]));
    }

    [Fact]
    public void Workbook_retains_live_formulas_types_outlines_cf_and_approved_summary_removal()
    {
        var report = ShipmentsToGoRules.Build(["SW", "KV"], [Row("A2", "Alpha", 200, 0), Row("B1", "Beta", 50), Row("A1", "Alpha", 100), Row("KV", "Beta", 80, site: "KV", orderSite: "KV")],
            new Dictionary<string, double> { ["SW"] = 20, ["KV"] = -10 }, new(2026, 10, 9), DateTimeOffset.UtcNow);
        var bytes = ShipmentsToGoWorkbook.Create(report);
        var root = Environment.GetEnvironmentVariable("KST_STG_EVIDENCE_ROOT");
        if (!string.IsNullOrEmpty(root)) { Directory.CreateDirectory(root); File.WriteAllBytes(Path.Combine(root, "synthetic-main-kv.xlsx"), bytes); }
        using var book = new XLWorkbook(new MemoryStream(bytes));
        var s = book.Worksheet(1);
        Assert.Equal("KTC-SW S2G", s.Name); Assert.Equal(20, s.Tables.Single().ColumnCount());
        Assert.Equal("SUBTOTAL(9,P2:P3)", s.Cell("P4").FormulaA1);
        Assert.Equal("SUBTOTAL(9,P2:P5)", s.Cell("P7").FormulaA1);
        Assert.Equal("P7+R7", s.Cell("T7").FormulaA1);
        Assert.True(s.Cell("T2").IsEmpty()); Assert.False(s.Cell("D2").HasFormula);
        Assert.Equal("=not-a-formula", s.Cell("D2").GetString()); Assert.Equal("000123", s.Cell("H2").GetString());
        Assert.True(s.Cell("L2").IsEmpty()); Assert.Equal(0, s.Cell("M2").GetDouble());
        Assert.Equal(2, s.Row(2).OutlineLevel); Assert.Equal(1, s.Row(4).OutlineLevel);
        Assert.True(s.Column(1).IsHidden); Assert.True(s.Column(4).IsHidden);
        Assert.Equal("KV GOOD-TO-GO:", s.Cell(13, 17).GetString());
        Assert.Contains("'KTV S2G'!$P$4", s.Cell(13, 18).FormulaA1);
        Assert.Equal("SW Shipments 10/9", s.Cell(15, 17).GetString());
        Assert.Equal("KTV Shipments 10/9", s.Cell(16, 17).GetString());
        Assert.DoesNotContain(s.CellsUsed(), c => c.GetString().Contains("KTS Shipments") || c.GetString().Contains("KS GOOD"));
        using var zip = new ZipArchive(new MemoryStream(bytes));
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        using var xmlStream = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        var xml = XDocument.Load(xmlStream);
        Assert.Empty(xml.Descendants(ns + "pageSetup"));
        var rules = xml.Descendants(ns + "cfRule").ToArray();
        Assert.Equal(11, rules.Length); Assert.Null(rules[3].Attribute("stopIfTrue"));
        Assert.Equal("notEqual", rules[9].Attribute("operator")!.Value);
        Assert.Equal(Enumerable.Range(1, 11).Select(i => i.ToString()), rules.Select(r => r.Attribute("priority")!.Value));
        Assert.Equal("350", xml.Descendants(ns + "c").Single(c => c.Attribute("r")?.Value == "P7").Element(ns + "v")!.Value);
        Assert.Equal("frozen", xml.Descendants(ns + "pane").Single().Attribute("state")!.Value);
    }

    [Fact]
    public void Mixed_main_ar_kv_and_missing_target_preserve_summary_coverage()
    {
        var report = ShipmentsToGoRules.Build(["SW", "NW", "AR", "KV", "MS"], [Row("main", "CRC", 100),
            Row("nw", "CRC", 30, site: "NW", orderSite: "NW"), Row("AR", "CRC", 500, orderSite: "AR"),
            Row("KV", "CRC", 80, site: "KV", orderSite: "KV"), Row("ms", "CRC", 7, site: "MS", orderSite: "MS")],
            new Dictionary<string, double> { ["SW"] = 20, ["NW"] = 999, ["KV"] = 10 }, new(2026, 10, 9), DateTimeOffset.UtcNow);
        using var book = new XLWorkbook(new MemoryStream(ShipmentsToGoWorkbook.Create(report)));
        Assert.Equal(new[] { "KTC-SW S2G", "KTC-AR S2G", "KTV S2G" }, book.Worksheets.Select(s => s.Name));
        Assert.Equal(137, book.Worksheet(1).Cell("P6").GetDouble());
        Assert.Equal(80, book.Worksheet(1).Cell("R12").GetDouble());
        Assert.Equal(20, book.Worksheet(1).Cell("R14").GetDouble());
        var root = Environment.GetEnvironmentVariable("KST_STG_EVIDENCE_ROOT");
        if (!string.IsNullOrEmpty(root)) File.WriteAllBytes(Path.Combine(root, "synthetic-mixed-main-ar-kv.xlsx"), ShipmentsToGoWorkbook.Create(report));
    }

    [Fact]
    public void Crc_total_substring_and_blank_groups_are_not_silently_fixed()
    {
        var report = ShipmentsToGoRules.Build(["SW"], [Row("a", "Total Person", 10), Row("b", "", 20)], new Dictionary<string, double>(), new(2026, 10, 9), DateTimeOffset.UtcNow);
        using var book = new XLWorkbook(new MemoryStream(ShipmentsToGoWorkbook.Create(report)));
        Assert.True(book.Worksheet(1).Cell("Q2").IsEmpty());
        Assert.Equal("Total Person", book.Worksheet(1).Cell("Q3").GetString());
        Assert.Equal("P3+R3", book.Worksheet(1).Cell("T3").FormulaA1);
        Assert.Equal("SUBTOTAL(9,P2:P3)", book.Worksheet(1).Cell("P5").FormulaA1);
    }

    [Theory]
    [InlineData(false, 6, "P2:P4", "R2:R4", 60, -6)]
    [InlineData(true, 5, "P2:P4", "R2:R4", 60, -6)]
    public void Unresolved_null_empty_and_whitespace_match_Excel_blank_subtotals(bool allBlank,
        int grand, string pRange, string rRange, double expectedExt, double expectedRisk)
    {
        // Hand-established Excel observations: blank details are not subtotaled, even all-blank.
        var lines = new[] { Row("NULL", null, 10) with { Risk = -1 },
            Row("EMPTY", "", 20) with { Risk = -2 },
            Row("LAST", allBlank ? " \t " : "Alpha", 30) with { Risk = -3 } };
        var report = ShipmentsToGoRules.Build(["SW"], lines, new Dictionary<string, double>(), new(2026, 10, 9), DateTimeOffset.UtcNow);
        Assert.Null(report.Sheets[0].Lines[0].Crc);
        Assert.Equal("", report.Sheets[0].Lines[1].Crc);
        if (allBlank) Assert.Equal("", report.Sheets[0].Lines[2].Crc);
        var bytes = ShipmentsToGoWorkbook.Create(report);
        var root = Environment.GetEnvironmentVariable("KST_STG_EVIDENCE_ROOT");
        if (!string.IsNullOrEmpty(root)) { Directory.CreateDirectory(root); File.WriteAllBytes(Path.Combine(root, allBlank ? "corrected-all-blank.xlsx" : "corrected-mixed.xlsx"), bytes); }
        using var book = new XLWorkbook(new MemoryStream(bytes));
        var s = book.Worksheet(1);
        Assert.Equal("NULL", s.Cell("C2").GetString()); Assert.Equal("EMPTY", s.Cell("C3").GetString()); Assert.Equal("LAST", s.Cell("C4").GetString());
        Assert.Equal(XLDataType.Number, s.Cell("P2").DataType);
        Assert.Equal(1, s.Row(2).OutlineLevel); Assert.Equal(1, s.Row(3).OutlineLevel);
        if (!allBlank)
        {
            Assert.Equal("Alpha Total", s.Cell("Q5").GetString());
            Assert.Equal("SUBTOTAL(9,P4:P4)", s.Cell("P5").FormulaA1);
            Assert.Equal("SUBTOTAL(9,R4:R4)", s.Cell("R5").FormulaA1);
            Assert.Equal(2, s.Row(4).OutlineLevel); Assert.Equal(1, s.Row(5).OutlineLevel);
        }
        else { Assert.True(s.Cell("Q4").IsEmpty()); Assert.Equal(1, s.Row(4).OutlineLevel); }
        Assert.Equal(allBlank ? 1 : 0, s.Row(grand).OutlineLevel);
        Assert.Equal("Grand Total", s.Cell(grand, 17).GetString());
        Assert.Equal($"SUBTOTAL(9,{pRange})", s.Cell(grand, 16).FormulaA1);
        Assert.Equal($"SUBTOTAL(9,{rRange})", s.Cell(grand, 18).FormulaA1);
        Assert.Equal(expectedExt, s.Cell(grand, 16).GetDouble()); Assert.Equal(expectedRisk, s.Cell(grand, 18).GetDouble());
        Assert.Equal($"P{grand}+R{grand}", s.Cell(grand, 20).FormulaA1);
        Assert.Equal(54, s.Cell(grand, 20).GetDouble());
        Assert.Equal(grand, s.Tables.Single().RangeAddress.LastAddress.RowNumber);
        Assert.Equal($"'KTC-SW S2G'!$P${grand}", s.Cell(grand + 2, 18).FormulaA1);
        Assert.DoesNotContain(s.CellsUsed(), c => c.GetString() == " Total");
    }

    [Fact]
    public void Successful_donor_repair_keeps_named_group_subtotal_and_every_detail_once()
    {
        var report = ShipmentsToGoRules.Build(["SW"], [Row("001", null, 10), Row("002", " ", 20),
            Row("003", "", 30), Row("donor", "Alpha", 40, comments: "001;002;003")], new Dictionary<string, double>(), new(2026, 10, 9), DateTimeOffset.UtcNow);
        Assert.All(report.Sheets[0].Lines, line => Assert.Equal("Alpha", line.Crc));
        using var book = new XLWorkbook(new MemoryStream(ShipmentsToGoWorkbook.Create(report)));
        var s = book.Worksheet(1);
        Assert.Equal("Alpha Total", s.Cell("Q6").GetString());
        Assert.Equal("SUBTOTAL(9,P2:P5)", s.Cell("P6").FormulaA1);
        Assert.Equal("SUBTOTAL(9,P2:P5)", s.Cell("P7").FormulaA1);
        Assert.Equal(100, s.Cell("P7").GetDouble());
        Assert.Equal(2, s.Row(2).OutlineLevel); Assert.Equal(1, s.Row(6).OutlineLevel);
    }

    [Theory]
    [InlineData("AR", "AR", "KTC-AR S2G")]
    [InlineData("KV", "KV", "KTV S2G")]
    [InlineData("MS", "MS", "KTC-MS S2G")]
    [InlineData("NW", "NW", "KTC-NW S2G")]
    public void Non_main_and_single_main_combinations(string site, string orderSite, string name)
    {
        var report = ShipmentsToGoRules.Build([site], [Row("one", "CRC", 1, site: site, orderSite: orderSite)], new Dictionary<string, double>(), new(2026, 10, 9), DateTimeOffset.UtcNow);
        using var book = new XLWorkbook(new MemoryStream(ShipmentsToGoWorkbook.Create(report)));
        Assert.Equal(name, book.Worksheet(1).Name);
        Assert.DoesNotContain(book.Worksheet(1).CellsUsed(), c => c.GetString().Contains("Shipments 10/9"));
        if (site is "AR" or "KV") Assert.DoesNotContain(book.Worksheet(1).CellsUsed(), c => c.GetString() == "Open Order Total");
        else Assert.Equal("0", book.Worksheet(1).Cell(10, 18).FormulaA1);
    }
}
