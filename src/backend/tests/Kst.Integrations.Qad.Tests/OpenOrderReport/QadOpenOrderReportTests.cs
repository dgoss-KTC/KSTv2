using System.Data;
using Kst.Domain.OpenOrderReport;
using Kst.Integrations.Qad.OpenOrderReport;

namespace Kst.Integrations.Qad.Tests.OpenOrderReport;

public class QadOpenOrderReportTests
{
    [Theory]
    [InlineData(null, 6)]
    [InlineData("SW", 1)]
    [InlineData("KV", 1)]
    public void Query_scope_and_independent_python_expressions(string? site, int count)
    {
        var f = OpenOrderReportRules.Validate(new("O'%_", "0001", "SP", "9999", "0001", "IOS", "10/1", "10/31", site, []), new(2026, 10, 12));
        var (sql, p) = QadOpenOrderReportReader.BuildQuery(f);
        Assert.Equal("%O'%_%", p.Get<string>("CustomerName")); Assert.DoesNotContain("O'%_", sql);
        Assert.Equal(count, p.ParameterNames.Count(n => n.StartsWith("Site")));
        var expected = site is null ? new[] { "SW", "NW", "AR", "MN", "MS", "KV" } : new[] { site };
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(expected[i], p.Get<string>($"Site{i}"));
            Assert.Equal(expected[i] == "KV" ? "KTV" : "KTC", p.Get<string>($"Domain{i}"));
            Assert.Contains($"(A.sod_site = @Site{i} AND A.sod_domain = @Domain{i})", sql);
        }
        Assert.DoesNotContain("KTS", p.ParameterNames.Select(n => p.Get<object>(n).ToString()));
        Assert.Contains("B.so_cust = @CustomerNumber", sql); Assert.Contains("A.sod_slspsn##1 = @SalespersonId", sql);
        Assert.Contains("A.sod_prodline BETWEEN @ProductLineFrom AND @ProductLineTo", sql);
        Assert.Contains("A.sod_due_date BETWEEN @DueDateFrom AND @DueDateTo", sql);
        Assert.Contains("PT.pt_warr_cd = @Ios", sql);
        Assert.Contains("(A.sod_qty_ord - A.sod_qty_ship) AS [Open]", sql);
        Assert.Contains("(A.sod_price * (A.sod_qty_ord - A.sod_qty_ship)) AS [Ext Price]", sql);
        Assert.Contains("CASE WHEN A.sod_consignment = 'TRUE' THEN 0 ELSE A.sod_price END", sql);
        Assert.Contains("SUM(LD.ld_qty_oh)", sql); Assert.Contains("LD.ld_site = A.sod_site", sql);
        Assert.Contains("INNER JOIN QADPRO2.dbo.so_mstr B ON A.sod_nbr = B.so_nbr AND A.sod_domain = B.so_domain", sql);
        Assert.Contains("Ship.ad_addr = B.so_ship", sql); Assert.Contains("Cust.ad_addr = B.so_cust", sql);
        Assert.Contains("PT.pt_part = A.sod_part AND PT.pt_domain = A.sod_domain", sql);
        Assert.EndsWith("ORDER BY Cust.ad_name, A.sod_part, A.sod_due_date", sql.Trim());
        var where = sql[(sql.LastIndexOf("WHERE", StringComparison.Ordinal))..];
        Assert.DoesNotContain("qty", where); Assert.DoesNotContain("so_stat", where); Assert.DoesNotContain("hold", where);
        foreach (var forbidden in new[] { "DISTINCT", "TOP ", "GROUP BY", "is_nettable", "ScopeParts", "cmt_seq", "EXEC", "UPDATE " }) Assert.DoesNotContain(forbidden, sql);
        for (var i = 1; i <= 15; i++) Assert.Contains($"ISNULL(cmt_cmmt##{i}, '')", sql);
        Assert.Contains("FOR XML PATH('')", sql); Assert.DoesNotContain(".value(", sql);
    }

    // Fixed expectations independent of the production header collection/index classifiers.
    private static DataTable Source()
    {
        var headers = new[] { "Due", "Order", "PO", "Ln", "Item Number", "Site", "Open", "Stat", "Ext Price", "Allocated",
            "Customer", "Customer Name", "Customer Part", "Dock Date", "IOS", "Line Comments", "Line Hold", "Partials",
            "Perform Date", "Picked", "Plnr", "Prod Stat", "Product Line", "QA Hold", "Remarks", "Required Date", "Revision",
            "Ship Acct", "Ship To", "Ship Via", "Site QOH", "SO Hold Status", "SO Type", "Unit Price" };
        var table = new DataTable();
        foreach (var h in headers) table.Columns.Add(h, h is "Due" or "Dock Date" or "Perform Date" or "Required Date" ? typeof(DateTime)
            : h is "Ln" or "Open" or "Ext Price" or "Allocated" or "Picked" or "Site QOH" or "Unit Price" ? typeof(decimal)
            : h == "Partials" ? typeof(bool) : typeof(string));
        return table;
    }

    [Fact]
    public async Task Adapter_preserves_every_alias_nullable_arithmetic_observations_and_multiplicity()
    {
        using var table = Source();
        foreach (var open in new decimal?[] { 6, 0, -2, null, 6 })
        {
            var r = table.NewRow();
            r["Order"] = "0001"; r["Item Number"] = "P"; r["Site"] = "SW"; r["Ln"] = 2m;
            r["Due"] = new DateTime(2026, 10, 1, 12, 30, 0); r["Partials"] = false;
            r["Open"] = open is null ? DBNull.Value : open.Value;
            r["Ext Price"] = open is null ? DBNull.Value : open.Value * 2.5m;
            r["Unit Price"] = 0m; r["Site QOH"] = -3m; r["Line Comments"] = "A&amp;B&lt;C&gt;";
            table.Rows.Add(r);
        }
        using var reader = table.CreateDataReader();
        var rows = await QadOpenOrderReportReader.ParseAsync(reader, default);
        Assert.Equal(5, rows.Count); Assert.Equal(rows[0].Cells, rows[4].Cells);
        Assert.Equal(new decimal?[] { 6, 0, -2, null, 6 }, rows.Select(r => r.Cells[6].Number));
        Assert.Equal(new decimal?[] { 15, 0, -5, null, 15 }, rows.Select(r => r.Cells[8].Number));
        Assert.All(rows, r => { Assert.Equal(0, r.Cells[33].Number); Assert.Equal(-3, r.Cells[30].Number);
            Assert.Null(r.Cells[11].Text); Assert.Equal("A&amp;B&lt;C&gt;", r.Cells[15].Text); Assert.False(r.Cells[17].Boolean);
            Assert.Equal(12, r.Cells[0].Date!.Value.Hour); });
    }

    [Fact]
    public async Task Every_field_has_an_independent_source_alias_and_type_expectation()
    {
        using var table = Source(); var source = table.NewRow();
        for (var i = 0; i < 34; i++) source[i] = table.Columns[i].DataType == typeof(string) ? table.Columns[i].ColumnName + "-value"
            : table.Columns[i].DataType == typeof(decimal) ? 100m + i
            : table.Columns[i].DataType == typeof(DateTime) ? new DateTime(2026, 10, 1).AddDays(i) : true;
        table.Rows.Add(source); using var reader = table.CreateDataReader();
        var row = Assert.Single(await QadOpenOrderReportReader.ParseAsync(reader, default));
        for (var i = 0; i < 34; i++)
        {
            if (table.Columns[i].DataType == typeof(string)) Assert.Equal(table.Columns[i].ColumnName + "-value", row.Cells[i].Text);
            else if (table.Columns[i].DataType == typeof(decimal)) Assert.Equal(100m + i, row.Cells[i].Number);
            else if (table.Columns[i].DataType == typeof(DateTime)) Assert.Equal(new DateTime(2026, 10, 1).AddDays(i), row.Cells[i].Date);
            else Assert.True(row.Cells[i].Boolean);
        }
    }

    [Fact]
    public async Task Empty_schema_failure_value_failure_and_text_row_source_overflow_are_explicit()
    {
        using var empty = Source(); using (var reader = empty.CreateDataReader()) Assert.Empty(await QadOpenOrderReportReader.ParseAsync(reader, default));
        empty.Columns[0].ColumnName = "Wrong";
        using (var reader = empty.CreateDataReader()) await Assert.ThrowsAsync<InvalidOperationException>(() => QadOpenOrderReportReader.ParseAsync(reader, default));
        using var wrongType = Source(); wrongType.Columns[6].DataType = typeof(string);
        using (var reader = wrongType.CreateDataReader()) await Assert.ThrowsAsync<InvalidOperationException>(() => QadOpenOrderReportReader.ParseAsync(reader, default));
        using var oversized = Source(); var row = oversized.NewRow(); row["Line Comments"] = new string('x', 32768); oversized.Rows.Add(row);
        using (var reader = oversized.CreateDataReader()) await Assert.ThrowsAsync<InvalidOperationException>(() => QadOpenOrderReportReader.ParseAsync(reader, default));
        using var many = Source(); for (var i = 0; i < 10001; i++) many.Rows.Add(many.NewRow());
        using (var reader = many.CreateDataReader()) await Assert.ThrowsAsync<InvalidOperationException>(() => QadOpenOrderReportReader.ParseAsync(reader, default));
        using var bytes = Source(); for (var i = 0; i < 300; i++) { var r = bytes.NewRow(); r["Line Comments"] = new string('x', 32767); bytes.Rows.Add(r); }
        using (var reader = bytes.CreateDataReader()) await Assert.ThrowsAsync<InvalidOperationException>(() => QadOpenOrderReportReader.ParseAsync(reader, default));
    }
}
