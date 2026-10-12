using Kst.Integrations.Qad.ShipmentsToGo;

namespace Kst.Integrations.Qad.Tests.ShipmentsToGo;

public class QadShipmentsToGoTests
{
    [Fact]
    public void Sql_scope_clocks_mapping_and_ambiguous_join_match_python()
    {
        var (sql, p) = QadShipmentsToGoReader.BuildOrdersQuery(["SW", "KV", "MS"], true, new(2026, 10, 10));
        Assert.Equal("KTV", p.Get<string>("Domain1")); Assert.Equal(7, p.Get<int>("ExtraDays"));
        Assert.Contains("so.so_channel AS Channel", sql); Assert.Contains("ad1.ad_name AS ShipToName", sql);
        Assert.Contains("SUM(im.in_qty_oh)", sql); Assert.Contains("WHEN 'Saturday' THEN 7", sql);
        Assert.Contains("@FixtureServerNow", sql); Assert.DoesNotContain("GETDATE()", sql);
        var outer = sql[sql.IndexOf(") mrp_pegging ON", StringComparison.Ordinal)..sql.IndexOf("LEFT JOIN QADPRO2.dbo.ad_mstr ad1", StringComparison.Ordinal)];
        Assert.DoesNotContain("domain", outer); Assert.DoesNotContain("site", outer);
        Assert.DoesNotContain("DISTINCT", sql); Assert.DoesNotContain("ld_det", sql);
        Assert.Throws<ArgumentException>(() => QadShipmentsToGoReader.BuildOrdersQuery(["KS"], false));
        var (shipped, _) = QadShipmentsToGoReader.BuildShippedQuery(["SW", "KV"]);
        Assert.Contains("tr_price * (tr_qty_chg * -1)", shipped); Assert.DoesNotContain("tr_domain", shipped);
        Assert.Contains("GETDATE()", shipped);
    }

    [Fact]
    public async Task Shipped_empty_failure_and_processing_partial_are_zero_or_partial_with_warning()
    {
        var warnings = 0;
        var empty = await QadShipmentsToGoReader.AccumulateShippedAsync(["SW", "KV"], () => Task.FromResult<IEnumerable<QadShipmentsToGoReader.ShippedRow>>([]), () => warnings++, default);
        Assert.Equal(0, empty["SW"]); Assert.Equal(0, warnings);
        var failed = await QadShipmentsToGoReader.AccumulateShippedAsync(["SW", "KV"], () => throw new Exception("synthetic"), () => warnings++, default);
        Assert.Equal(0, failed["KV"]); Assert.Equal(1, warnings);
        var partial = await QadShipmentsToGoReader.AccumulateShippedAsync(["SW", "KV"], () => Task.FromResult<IEnumerable<QadShipmentsToGoReader.ShippedRow>>([
            new() { Site = " sw ", Total = 20 }, new() { Site = "KV", Total = null }]), () => warnings++, default);
        Assert.Equal(20, partial["SW"]); Assert.Equal(0, partial["KV"]); Assert.Equal(2, warnings);
    }

    [Fact]
    public void Raw_mapping_preserves_nulls_precision_and_text()
    {
        var r = QadShipmentsToGoReader.Map(new() { Order = "0001", ItemNumber = "=text", DueDate = new(2026, 10, 9, 13, 0, 0), Site = "SW", Open = 1.23456789m, ExtendedPrice = 0, Qoh = -1 });
        Assert.Equal("0001", r.Order); Assert.Null(r.Allocated); Assert.Null(r.Risk);
        Assert.Equal(1.23456789d, r.Open); Assert.Equal(0, r.ExtendedPrice); Assert.Equal(13, r.DueDate.Hour);
    }
}
