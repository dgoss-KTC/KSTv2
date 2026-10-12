using ClosedXML.Excel;
using Kst.Domain.OpenOrderReport;
using Kst.Exports;

namespace Kst.Domain.Tests.OpenOrderReport;

public class OpenOrderReportTests
{
    private static OpenOrderReportInput Input(string? site = "SW", string? from = null, string? to = null,
        string? dateFrom = null, string? dateTo = null, params string[] columns) =>
        new(null, null, null, from, to, null, dateFrom, dateTo, site, columns);

    [Fact]
    public void Required_filter_paired_bounds_and_allowlist_preserve_legacy_semantics()
    {
        var today = new DateOnly(2026, 10, 12);
        Assert.Throws<ArgumentException>(() => OpenOrderReportRules.Validate(Input(null), today));
        Assert.Throws<ArgumentException>(() => OpenOrderReportRules.Validate(Input(null, dateFrom: "10/1", dateTo: "10/2"), today));
        Assert.Throws<ArgumentException>(() => OpenOrderReportRules.Validate(Input("KS"), today));
        Assert.Throws<ArgumentException>(() => OpenOrderReportRules.Validate(Input(from: "0001"), today));
        Assert.Throws<ArgumentException>(() => OpenOrderReportRules.Validate(Input(dateFrom: "10/2", dateTo: "10/1"), today));
        Assert.Throws<ArgumentException>(() => OpenOrderReportRules.Validate(Input(columns: ["Unit Price", "Unit Price"]), today));
        Assert.Throws<ArgumentException>(() => OpenOrderReportRules.Validate(Input(columns: ["SQL"]), today));
        var filter = OpenOrderReportRules.Validate(Input(from: "９９９９", to: "０００１", columns: ["Unit Price", "Allocated"]), today);
        Assert.Equal("９９９９", filter.ProductLineFrom);
        Assert.Equal(new[] { "Allocated", "Unit Price" }, filter.OptionalFields);
        Assert.Equal("0001", OpenOrderReportRules.Validate(Input(from: "0001", to: "0000"), today).ProductLineFrom);
        Assert.Null(OpenOrderReportRules.Validate(new(" Alpha ", null, null, null, null, null, null, null, null, []), today).Site);
    }

    [Theory]
    [InlineData("10/1", "2026-10-01")]
    [InlineData("10-1-26", "2026-10-01")]
    [InlineData("10.1.2026", "2026-10-01")]
    [InlineData("1/1/68", "2068-01-01")]
    [InlineData("1/1/69", "1969-01-01")]
    [InlineData("02/29/2024", "2024-02-29")]
    [InlineData("10/ 1/2026", "2026-10-01")]
    [InlineData("10/1/２０２６", "2026-10-01")]
    public void Python_dates(string text, string expected) => Assert.Equal(DateOnly.Parse(expected), OpenOrderReportRules.ParseDate(text, 2026));

    [Theory]
    [InlineData("2/29")]
    [InlineData("2/30/2026")]
    [InlineData("2026-10-01")]
    [InlineData("10/1/1")]
    public void Invalid_dates(string text) => Assert.Throws<ArgumentException>(() => OpenOrderReportRules.ParseDate(text, 2024));

    [Theory]
    [InlineData(false, 9)]
    [InlineData(true, 34)]
    public void Workbook_sparse_nullable_numeric_text_and_table_contract(bool all, int count)
    {
        var cells = Enumerable.Range(0, 34).Select(_ => new OpenOrderReportCell()).ToArray();
        cells[0] = new(Date: new DateTime(2026, 10, 1)); cells[1] = new(Text: "00012"); cells[2] = new(Text: "=1+1");
        cells[3] = new(Number: 2); cells[4] = new(Text: "P"); cells[5] = new(Text: "SW");
        cells[6] = new(Number: -2); cells[8] = new(Number: -5); cells[15] = new(Text: "A&amp;B&lt;C&gt;");
        cells[17] = new(Boolean: false); cells[30] = new(Number: -3); cells[33] = new(Number: 0);
        var second = cells.ToArray(); second[6] = new(); second[8] = new(Number: 0); second[30] = new();
        var columns = all ? OpenOrderReportRules.Headers : OpenOrderReportRules.Mandatory;
        var bytes = OpenOrderReportWorkbook.Create(new([new(cells), new(second)], columns, new(2026, 10, 12), DateTimeOffset.UtcNow));
        using var book = new XLWorkbook(new MemoryStream(bytes));
        Assert.Single(book.Worksheets); var sheet = book.Worksheet("OOR");
        var expectedHeaders = new[] { "Due", "Order", "PO", "Ln", "Item Number", "Site", "Open", "Stat", "Ext Price", "Allocated",
            "Customer", "Customer Name", "Customer Part", "Dock Date", "IOS", "Line Comments", "Line Hold", "Partials", "Perform Date",
            "Picked", "Plnr", "Prod Stat", "Product Line", "QA Hold", "Remarks", "Required Date", "Revision", "Ship Acct", "Ship To",
            "Ship Via", "Site QOH", "SO Hold Status", "SO Type", "Unit Price" };
        Assert.Equal(expectedHeaders.Take(count), Enumerable.Range(1, count).Select(c => sheet.Cell(1, c).GetString()));
        var table = Assert.Single(sheet.Tables); Assert.Equal("OpenOrders", table.Name);
        Assert.Equal(XLTableTheme.TableStyleLight9, table.Theme); Assert.True(table.ShowAutoFilter); Assert.False(table.ShowTotalsRow);
        Assert.Equal(3, table.RangeAddress.LastAddress.RowNumber); Assert.Equal(count, table.RangeAddress.LastAddress.ColumnNumber);
        Assert.Equal(1, sheet.SheetView.SplitRow); Assert.Equal(0, sheet.SheetView.SplitColumn);
        Assert.Equal(XLDataType.Number, sheet.Cell(2, 7).DataType); Assert.Equal(-2, sheet.Cell(2, 7).GetDouble());
        Assert.True(sheet.Cell(3, 7).IsEmpty()); Assert.Equal(0, sheet.Cell(3, 9).GetDouble());
        Assert.Equal("=1+1", sheet.Cell(2, 3).GetString()); Assert.False(sheet.Cell(2, 3).HasFormula);
        Assert.Equal("m/d/yyyy", sheet.Cell(2, 1).Style.DateFormat.Format);
        Assert.False(sheet.Protection.IsProtected); Assert.DoesNotContain(sheet.CellsUsed(), c => c.HasFormula);
        if (all)
        {
            foreach (var c in new[] { 14, 19, 26 }) Assert.Equal("m/d/yyyy", sheet.Cell(2, c).Style.DateFormat.Format);
            Assert.Equal(XLDataType.Boolean, sheet.Cell(2, 18).DataType);
            Assert.Equal(0, sheet.Cell(2, 34).GetDouble()); Assert.True(sheet.Cell(3, 31).IsEmpty());
            Assert.Equal("A&amp;B&lt;C&gt;", sheet.Cell(2, 16).GetString());
        }
    }

    [Fact]
    public void Oversize_and_invalid_facts_fail_without_truncation()
    {
        var blank = new OpenOrderReportRow(Enumerable.Repeat(new OpenOrderReportCell(), 34).ToArray());
        Assert.Throws<InvalidOperationException>(() => OpenOrderReportRules.Freeze(Enumerable.Repeat(blank, 10001).ToArray()));
        var cells = blank.Cells.ToArray(); cells[15] = new(Text: new string('x', 32768));
        Assert.Throws<InvalidOperationException>(() => OpenOrderReportRules.Freeze([new(cells)]));
        Assert.Throws<InvalidOperationException>(() => OpenOrderReportRules.Freeze([new([])]));
    }
}
