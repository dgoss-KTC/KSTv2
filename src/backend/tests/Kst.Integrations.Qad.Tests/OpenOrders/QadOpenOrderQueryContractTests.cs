using Kst.Integrations.Qad.OpenOrders;

namespace Kst.Integrations.Qad.Tests.OpenOrders;

public sealed class QadOpenOrderQueryContractTests
{
    [Fact]
    public void BatchQuery_ParametersBindDomainSiteAndExactResolvedParents()
    {
        const string unsafePart = "P'; DELETE FROM sod_det; --";
        var (sql, parameters) = QadOpenOrderQueryContract.BuildBatchQuery("TEST", "S1", ["P-1", unsafePart]);

        Assert.Equal("TEST", parameters.Get<string>("Domain"));
        Assert.Equal("S1", parameters.Get<string>("Site"));
        Assert.Equal("P-1", parameters.Get<string>("Part0"));
        Assert.Equal(unsafePart, parameters.Get<string>("Part1"));
        Assert.Contains("VALUES (@Part0), (@Part1)", sql);
        Assert.DoesNotContain(unsafePart, sql);
        Assert.DoesNotContain("P-1", sql);
        Assert.Contains("scope.ParentPart = sod.sod_part", sql);
        Assert.Contains("sod.sod_domain = @Domain", sql);
        Assert.Contains("sod.sod_site = @Site", sql);
        Assert.Throws<ArgumentException>(() => QadOpenOrderQueryContract.BuildBatchQuery("TEST", "S1", []));
    }

    [Fact]
    public void BatchQuery_HasPositiveOpenPredicateAndNoExtraQualificationOrTruncation()
    {
        var (sql, _) = QadOpenOrderQueryContract.BuildBatchQuery("TEST", "S1", ["P-1"]);

        Assert.Contains("sod.sod_qty_ord - sod.sod_qty_ship > 0", sql);
        Assert.Contains("so.so_nbr = sod.sod_nbr AND so.so_domain = sod.sod_domain", sql);
        Assert.DoesNotContain("DISTINCT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TOP (", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OFFSET ", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sod.sod_status =", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("so.so_stat =", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RMABOM", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RA%", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sod.sod_dock AS DockDate", sql);
        Assert.Contains("sod.sod__dte01 AS DockDate", sql);
        Assert.Contains("so.so_partial AS Partials", sql);
        Assert.Contains("sod.sod_qty_all AS Allocated", sql);
        Assert.Contains("so.so_hold_stat AS SoHoldStatus", sql);
        Assert.Contains("sod.sod_price * (sod.sod_qty_ord - sod.sod_qty_ship) AS ExtPrice", sql);
        Assert.Contains("CASE WHEN sod.sod_consignment = 'TRUE' THEN 0", sql);
    }

    [Fact]
    public void BatchQuery_ReturnsSalespersonForLocalFilteringWithoutMandatorySqlFilter()
    {
        var (sql, parameters) = QadOpenOrderQueryContract.BuildBatchQuery("TEST", "S1", ["P-1"]);

        var select = sql[..sql.IndexOf("FROM qadpro2.dbo.sod_det AS sod", StringComparison.Ordinal)];
        var qualification = sql[sql.IndexOf("WHERE sod.sod_domain = @Domain", StringComparison.Ordinal)..];
        Assert.Contains("sod.sod_slspsn##1 AS Salesperson", select);
        Assert.DoesNotContain("sod_slspsn##1", qualification, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Salesperson", parameters.ParameterNames);
        Assert.Equal("""
            WHERE sod.sod_domain = @Domain
              AND sod.sod_site = @Site
              AND sod.sod_qty_ord - sod.sod_qty_ship > 0
            """, qualification[..qualification.IndexOf("ORDER BY", StringComparison.Ordinal)].TrimEnd());
    }

    [Fact]
    public void BatchQuery_IsReadOnlyAndKeepsEnrichmentWithinDomainAndSite()
    {
        var (sql, _) = QadOpenOrderQueryContract.BuildBatchQuery("TEST", "S1", ["P-1"]);

        Assert.Contains("cmt.cmt_domain = sod.sod_domain", sql);
        Assert.Contains("cust.ad_domain = sod.sod_domain", sql);
        Assert.Contains("ship.ad_domain = sod.sod_domain", sql);
        Assert.Contains("pt.pt_domain = sod.sod_domain", sql);
        Assert.Contains("ld.ld_domain = sod.sod_domain", sql);
        Assert.Contains("ld.ld_site = sod.sod_site", sql);
        Assert.Contains("ORDER BY cust.ad_name, sod.sod_part, sod.sod_due_date", sql);
        foreach (var verb in new[] { "INSERT ", "UPDATE ", "DELETE ", "MERGE ", "EXEC ", "CREATE ", "DROP " })
            Assert.DoesNotContain(verb, sql, StringComparison.OrdinalIgnoreCase);
    }
}
