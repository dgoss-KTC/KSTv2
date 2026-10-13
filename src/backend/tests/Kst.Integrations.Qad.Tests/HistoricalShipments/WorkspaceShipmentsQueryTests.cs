using Kst.Domain.HistoricalShipments;
using Kst.Integrations.Qad.HistoricalShipments;

namespace Kst.Integrations.Qad.Tests.HistoricalShipments;

public class WorkspaceShipmentsQueryTests
{
    [Theory]
    [InlineData("SW", "KTC")]
    [InlineData("NW", "KTC")]
    [InlineData("AR", "KTC")]
    [InlineData("MN", "KTC")]
    [InlineData("MS", "KTC")]
    [InlineData("KV", "KTV")]
    public void Scope_is_single_site_domain_and_all_history_keeps_null_ship_dates(string site, string domain)
    {
        var (sql, p) = QadHistoricalShipmentsReader.BuildWorkspaceQuery(new(site, ["0000123", "PARENT-X"], null, null, null));
        Assert.Equal(site, p.Get<string>("Site")); Assert.Equal(domain, p.Get<string>("Domain"));
        Assert.Contains("d.idh_site = @Site AND d.idh_domain = @Domain", sql);
        Assert.Contains("FROM dbo.idh_hist d", sql); Assert.Contains("ORDER BY h.ih_ship_date DESC, d.prrowid", sql);
        Assert.DoesNotContain("ih_ship_date IS NOT NULL", sql); Assert.DoesNotContain("@ShipDate", sql);
        Assert.DoesNotContain("@InvoiceDate", sql); Assert.DoesNotContain("@Item", sql);
        Assert.DoesNotContain("@ProductLine", sql); Assert.DoesNotContain("@Customer", sql);
        Assert.Contains("INNER JOIN WorkspaceParentParts scope ON scope.ParentLength = LEN(d.idh_part) AND scope.ParentPart = d.idh_part", sql);
        Assert.Contains("@WorkspaceParentParts.nodes('/parts/part')", sql);
        Assert.Equal(new[] { "0000123", "PARENT-X" }, System.Xml.Linq.XDocument.Parse(p.Get<string>("WorkspaceParentParts")).Root!.Elements("part").Select(e => e.Value));
        Assert.DoesNotContain("TOP ", sql); Assert.Contains("SELECT DISTINCT", sql);
        Assert.Contains("partNode.value('.', 'nvarchar(4000)') AS ParentPart", sql);
        Assert.Contains("partNode.value('string-length(.)', 'int') AS ParentLength", sql);
        Assert.Contains("hc.HeaderCount = 1", sql); Assert.Contains("d.idh_retro_price AS retroUnitPrice", sql);
    }
    [Theory]
    [InlineData("0000123")]
    [InlineData("%_[literal]")]
    [InlineData("x' OR 1=1--")]
    public void Exact_part_is_parameterized_literal_without_master_validation(string part)
    {
        var (sql, p) = QadHistoricalShipmentsReader.BuildWorkspaceQuery(new("SW", ["0001"], part, null, null));
        Assert.Contains("d.idh_part = @PartNumber", sql); Assert.Equal(part, p.Get<string>("PartNumber"));
        Assert.DoesNotContain(part, sql); Assert.DoesNotContain(" LIKE ", sql);
        Assert.DoesNotContain("INNER JOIN dbo.pt_mstr", sql);
    }
    [Fact]
    public void Independent_inclusive_ship_bounds_do_not_filter_invoice_dates_and_include_end_timestamps()
    {
        var from = QadHistoricalShipmentsReader.BuildWorkspaceQuery(new("SW", ["0001"], "  ", new(2026, 1, 1), null));
        Assert.Contains("hf.ih_ship_date >= @ShipDateFrom", from.Sql);
        Assert.Equal(new DateTime(2026, 1, 1), from.Parameters.Get<DateTime>("ShipDateFrom"));
        Assert.DoesNotContain("@ShipDateEndExclusive", from.Sql); Assert.DoesNotContain("@PartNumber", from.Sql);
        var to = QadHistoricalShipmentsReader.BuildWorkspaceQuery(new("SW", ["0001"], null, null, new(2026, 1, 31)));
        Assert.Contains("hf.ih_ship_date < @ShipDateEndExclusive", to.Sql);
        Assert.Equal(new DateTime(2026, 2, 1), to.Parameters.Get<DateTime>("ShipDateEndExclusive"));
        Assert.DoesNotContain("@ShipDateFrom", to.Sql); Assert.DoesNotContain("@InvoiceDate", to.Sql);
        var fixtures = new[] { (Ship: new DateTime(2026, 1, 31, 23, 59, 59), Invoice: new DateTime(2026, 2, 8), Expected: true),
            (Ship: new DateTime(2026, 2, 1), Invoice: new DateTime(2026, 1, 31), Expected: false) };
        foreach (var row in fixtures) Assert.Equal(row.Expected, row.Ship < to.Parameters.Get<DateTime>("ShipDateEndExclusive"));
        Assert.Contains("hf.ih_domain = @Domain", to.Sql);
        var max = QadHistoricalShipmentsReader.BuildWorkspaceQuery(new("SW", ["0001"], null, null, DateOnly.MaxValue));
        Assert.Contains("hf.ih_ship_date IS NOT NULL", max.Sql);
        Assert.Throws<ArgumentException>(() => QadHistoricalShipmentsReader.BuildWorkspaceQuery(new("SW", ["0001"], null, new(2026, 2, 1), new(2026, 1, 31))));
    }
    [Fact]
    public void Parent_scope_is_parameterized_complete_for_large_sets_and_empty_scope_cannot_build_a_widened_query()
    {
        var parts = Enumerable.Range(0, 1200).Select(i => $"P{i:D6}").ToArray();
        var (sql, parameters) = QadHistoricalShipmentsReader.BuildWorkspaceQuery(new("SW", parts, null, null, null));
        var xml = parameters.Get<string>("WorkspaceParentParts");
        Assert.Equal(1200, System.Xml.Linq.XDocument.Parse(xml).Root!.Elements("part").Count());
        Assert.DoesNotContain("P000000", sql); Assert.Contains("INNER JOIN WorkspaceParentParts", sql);
        Assert.Contains("ParentLength = LEN(d.idh_part)", sql);
        Assert.Equal(0, parameters.ParameterNames.Count(n => n.StartsWith("ParentPart", StringComparison.Ordinal)));
        Assert.Throws<ArgumentException>(() => QadHistoricalShipmentsReader.BuildWorkspaceQuery(new("SW", [], null, null, null)));
    }
    [Fact]
    public void Parent_XML_escaping_preserves_identifiers_and_oversized_values_cannot_match_after_SQL_conversion()
    {
        const string special = "A<&>'\"";
        var longPart = new string('X', 4001);
        var (sql, parameters) = QadHistoricalShipmentsReader.BuildWorkspaceQuery(new("SW", [special, longPart], null, null, null));
        var parts = System.Xml.Linq.XDocument.Parse(parameters.Get<string>("WorkspaceParentParts")).Root!.Elements("part").Select(x => x.Value).ToArray();
        Assert.Equal(special, parts[0]); Assert.Equal(4001, parts[1].Length);
        Assert.DoesNotContain(special, sql); Assert.DoesNotContain(longPart, sql);
        Assert.Contains("partNode.value('string-length(.)', 'int') AS ParentLength", sql);
    }
    [Fact]
    public async Task Empty_scope_returns_empty_without_opening_qad_or_falling_back_to_site_history()
    {
        var reader = new QadHistoricalShipmentsReader(new Kst.Integrations.Qad.Options.QadConnectionOptions());
        var rows = new List<ShipmentRow>();
        await foreach (var row in reader.ReadWorkspaceAsync(new("SW", [], null, null, null), default)) rows.Add(row);
        Assert.Empty(rows);
    }
    [Fact]
    public void Reports_keep_invoice_dates_and_physical_order()
    {
        var f = new HistoricalShipmentsFilter("SW", null, null, null, null, null, null, null, null, null, new(2026, 1, 1), new(2026, 1, 31), ["shipDate"]);
        var sql = QadHistoricalShipmentsReader.BuildQuery(f).Sql;
        Assert.Contains("hf.ih_inv_date >= @InvoiceDateFrom", sql); Assert.Contains("hf.ih_inv_date < @InvoiceDateEndExclusive", sql);
        Assert.Contains("ORDER BY d.prrowid", sql); Assert.DoesNotContain("@ShipDate", sql);
    }
}
