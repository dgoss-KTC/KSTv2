using Kst.Integrations.Qad.Inventory;
using Kst.Integrations.Qad.Mps;

namespace Kst.Integrations.Qad.Tests.Inventory;

public sealed class QadInventoryLocationReaderTests
{
    [Fact]
    public void QueryHasOnlyParameterizedScopeAndNoRowFilteringOrAggregation()
    {
        var (sql, parameters) = QadInventoryLocationReader.BuildQuery("KTC", "SW", "x' ;--");
        Assert.Equal("x' ;--", parameters.Get<string>("Part"));
        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("SW", parameters.Get<string>("Site"));
        Assert.Contains("ld.ld_domain = @Domain AND ld.ld_site = @Site AND ld.ld_part = @Part", sql);
        Assert.Contains("ld.ld_status AS Status", sql);
        Assert.Contains("CONVERT(varchar(100), ld.ld_qty_oh)", sql);
        foreach (var forbidden in new[] { "DISTINCT", "GROUP BY", "JOIN", "COALESCE", "ISNULL", " > ", "RA%", "MRB", "price", "x'" })
            Assert.DoesNotContain(forbidden, sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(QadSiteDomainMap.Resolve("SW"), QadSiteDomainMap.Resolve("sw"));
    }

    [Fact]
    public void AllPhysicalRowsAndTextAndExactQuantitiesSurvive()
    {
        var rows = new[] {
            new QadInventoryLocationRawRow("ktc", "sw", "comp", " mRb ", "RA123", "HOLD", "-0.0000000001"),
            new QadInventoryLocationRawRow("KTC", "SW", "COMP", " mRb ", "RA123", "HOLD", "-0.0000000001"),
            new QadInventoryLocationRawRow("KTC", "SW", "COMP", "", "", "", "0.0000000000"),
            new QadInventoryLocationRawRow("KTC", "SW", "COMP", null, null, null, null),
            new QadInventoryLocationRawRow("KTC", "SW", "COMP", "Bin", "Serial", "anything", "999999999999999999.1234567890") };
        var result = QadInventoryLocationReader.Normalize("KTC", "SW", "COMP", rows);
        Assert.Equal(rows.Length, result.Count);
        Assert.Equal(rows.Select(r => r.QuantityOnHand), result.Select(r => r.QuantityOnHand));
        Assert.Equal(rows.Select(r => r.Location), result.Select(r => r.Location));
        Assert.Equal(rows.Select(r => r.LotSerial), result.Select(r => r.LotSerial));
        Assert.Equal(rows.Select(r => r.Status), result.Select(r => r.Status));
        Assert.Equal(rows.Length, result.Select(r => r.RowId).Distinct().Count());
        Assert.Empty(QadInventoryLocationReader.Normalize("KTC", "SW", "COMP", []));
    }

    [Theory]
    [InlineData("KTV", "SW", "COMP")]
    [InlineData("KTC", "KV", "COMP")]
    [InlineData("KTC", "SW", "OTHER")]
    public void ScopeLeakIsFailure(string domain, string site, string part) =>
        Assert.Throws<InvalidOperationException>(() => QadInventoryLocationReader.Normalize("KTC", "SW", "COMP",
            [new(domain, site, part, null, null, null, "1")]));

    [Fact]
    public void InvalidQuantityFailsInsteadOfBecomingNullOrZero() =>
        Assert.Throws<InvalidOperationException>(() => QadInventoryLocationReader.Normalize("KTC", "SW", "COMP",
            [new("KTC", "SW", "COMP", null, null, null, "NaN")]));
}
