using Kst.Domain.Inventory;
using Kst.Integrations.Qad.Inventory;

namespace Kst.Integrations.Qad.Tests.Inventory;

public sealed class QadPartInventoryReaderTests
{
    [Fact]
    public void BuildBatchQuery_Adds_One_Parameter_Per_Part_Plus_Domain_And_Site()
    {
        var (sql, parameters) = QadPartInventoryReader.BuildBatchQuery("KTC", "SW", ["ABC100", "ABC200"]);

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("SW", parameters.Get<string>("Site"));
        Assert.Equal("ABC100", parameters.Get<string>("Part0"));
        Assert.Equal("ABC200", parameters.Get<string>("Part1"));
        Assert.Contains("@Part0", sql);
        Assert.Contains("@Part1", sql);
    }

    [Fact]
    public void BuildBatchQuery_SinglePartScope_Uses_Single_Value_Row()
    {
        // Stage 6 PartDetail consumes this builder with a one-part scope; the scope must be exactly
        // one VALUES row so the query returns exactly one summary row.
        var (sql, parameters) = QadPartInventoryReader.BuildBatchQuery("KTC", "SW", ["ABC100"]);

        Assert.Equal("ABC100", parameters.Get<string>("Part0"));
        Assert.DoesNotContain("@Part1", sql);
        Assert.Contains("(VALUES (@Part0))", sql);
    }

    [Fact]
    public void BuildBatchQuery_Never_Concatenates_Raw_Part_Values_Into_Sql_Text()
    {
        var maliciousPart = "ABC'; DROP TABLE ld_det; --";
        var (sql, parameters) = QadPartInventoryReader.BuildBatchQuery("KTC", "SW", [maliciousPart]);

        Assert.DoesNotContain(maliciousPart, sql);
        Assert.Equal(maliciousPart, parameters.Get<string>("Part0"));
    }

    [Fact]
    public void BuildBatchQuery_Empty_PartList_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => QadPartInventoryReader.BuildBatchQuery("KTC", "SW", []));
    }

    [Fact]
    public void BuildBatchQuery_Requires_Positive_Qty()
    {
        // Only positive rows qualify; zero/negative balances are ignored (accepted Stage 6 rule).
        var (sql, _) = QadPartInventoryReader.BuildBatchQuery("KTC", "SW", ["ABC100"]);

        Assert.Contains("ld.ld_qty_oh > 0", sql);
    }

    [Fact]
    public void BuildBatchQuery_Joins_LocMstr_And_IsMstr_On_Domain_Site_Location()
    {
        var (sql, _) = QadPartInventoryReader.BuildBatchQuery("KTC", "SW", ["ABC100"]);

        Assert.Contains("loc.loc_domain = ld.ld_domain", sql);
        Assert.Contains("loc.loc_site = ld.ld_site", sql);
        Assert.Contains("loc.loc_loc = ld.ld_loc", sql);
        Assert.Contains("ism.is_domain = loc.loc_domain", sql);
        Assert.Contains("ism.is_status = loc.loc_status", sql);
        Assert.Contains("INNER JOIN qadpro2.dbo.loc_mstr", sql);
        Assert.Contains("INNER JOIN qadpro2.dbo.is_mstr", sql);
    }

    [Fact]
    public void BuildBatchQuery_Splits_Nettable_NonNettable_And_Rma_Totals()
    {
        var (sql, _) = QadPartInventoryReader.BuildBatchQuery("KTC", "SW", ["ABC100"]);

        Assert.Contains("ld.ld_lot NOT LIKE 'RA%' AND ism.is_nettable = 1", sql);
        Assert.Contains("ld.ld_lot NOT LIKE 'RA%' AND ism.is_nettable = 0", sql);
        Assert.Contains("WHEN ld.ld_lot LIKE 'RA%' THEN ld.ld_qty_oh", sql);
        Assert.Contains("AS NetQuantityOnHand", sql);
        Assert.Contains("AS NonNetQuantityOnHand", sql);
        Assert.Contains("AS RmaQuantityOnHand", sql);
    }

    [Fact]
    public void BuildBatchQuery_Rma_Classification_Happens_In_Select_Not_Where()
    {
        // RMA lots must still flow through the WHERE clause (only ld_qty_oh > 0 filters rows);
        // classification into RMA/Net/Non-Net happens entirely in the SELECT CASE expressions.
        var (sql, _) = QadPartInventoryReader.BuildBatchQuery("KTC", "SW", ["ABC100"]);

        var whereClauseIndex = sql.IndexOf("WHERE", StringComparison.Ordinal);
        var whereClause = sql[whereClauseIndex..];
        Assert.DoesNotContain("NOT LIKE 'RA%'", whereClause);
    }

    [Fact]
    public void BuildBatchQuery_Scopes_Aggregation_To_Requested_Parts_And_Groups_By_Part()
    {
        // The aggregate CTE must be limited to the requested scope and aggregated per part so
        // duplicate source/location rows for one part sum into one row (Stage 6 aggregation grain).
        var (sql, _) = QadPartInventoryReader.BuildBatchQuery("KTC", "SW", ["ABC100", "ABC200"]);

        Assert.Contains("ld.ld_part IN (SELECT PartNumber FROM ScopeParts)", sql);
        Assert.Contains("GROUP BY ld.ld_part", sql);
        Assert.Contains("ld.ld_domain = @Domain", sql);
        Assert.Contains("ld.ld_site = @Site", sql);
        Assert.DoesNotContain("DISTINCT", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildBatchQuery_ZeroFills_Parts_Without_Qualifying_Rows_Via_LeftJoin_And_Isnull()
    {
        // Every requested part must receive exactly one summary row: the outer SELECT is driven by
        // the requested scope, LEFT JOINs the aggregates, and ISNULLs missing totals to authoritative
        // zeroes (a part with no qualifying inventory rows is 0/0/0, never a missing row).
        var (sql, _) = QadPartInventoryReader.BuildBatchQuery("KTC", "SW", ["ABC100", "ABC200"]);

        Assert.Contains("FROM ScopeParts AS scope", sql);
        Assert.Contains("LEFT JOIN InventoryAggregates AS inv", sql);
        Assert.Contains("ON inv.PartNumber = scope.PartNumber", sql);
        Assert.Contains("ISNULL(inv.NetQuantityOnHand, 0) AS NetQuantityOnHand", sql);
        Assert.Contains("ISNULL(inv.NonNetQuantityOnHand, 0) AS NonNetQuantityOnHand", sql);
        Assert.Contains("ISNULL(inv.RmaQuantityOnHand, 0) AS RmaQuantityOnHand", sql);
        Assert.Contains("scope.PartNumber AS PartNumber", sql);
        Assert.Contains("@Site AS Site", sql);
    }

    [Fact]
    public void Normalize_Maps_RawRow_Into_Shared_Summary()
    {
        var raw = new QadPartInventoryRawRow(
            Site: "SW",
            PartNumber: "ABC100",
            NetQuantityOnHand: 1325m,
            NonNetQuantityOnHand: 75m,
            RmaQuantityOnHand: 25m);

        var summary = QadPartInventoryReader.Normalize(raw);

        Assert.Equal("SW", summary.Site);
        Assert.Equal("ABC100", summary.PartNumber);
        Assert.Equal(1325m, summary.NetQuantityOnHand);
        Assert.Equal(75m, summary.NonNetQuantityOnHand);
        Assert.Equal(25m, summary.RmaQuantityOnHand);
    }

    [Fact]
    public void NormalizePartNumbers_Trims_Entries()
    {
        var keys = QadPartInventoryReader.NormalizePartNumbers(["  ABC100  "]);

        Assert.Equal(["ABC100"], keys);
    }

    [Fact]
    public void NormalizePartNumbers_Deduplicates_Case_Insensitively_Keeping_First_Occurrence()
    {
        // A repeated requested part (including case variants) must produce one lookup key — and
        // therefore one inventory summary — with no reliance on SQL DISTINCT.
        var keys = QadPartInventoryReader.NormalizePartNumbers(["ABC100", "abc100", " ABC100 ", "ABC200"]);

        Assert.Equal(["ABC100", "ABC200"], keys);
    }

    [Fact]
    public void NormalizePartNumbers_Preserves_First_Occurrence_Order()
    {
        var keys = QadPartInventoryReader.NormalizePartNumbers(["ZED", "alpha", "ZED", "BETA", "alpha"]);

        Assert.Equal(["ZED", "alpha", "BETA"], keys);
    }

    [Fact]
    public void NormalizePartNumbers_Throws_On_Blank_Entry()
    {
        // Silently dropping a blank key would let a caller infer zero from a missing row.
        Assert.Throws<ArgumentException>(
            () => QadPartInventoryReader.NormalizePartNumbers(["ABC100", "   "]));
    }

    [Fact]
    public void NormalizePartNumbers_Throws_On_Null_Entry()
    {
        var parts = new List<string> { "ABC100", null! };

        Assert.Throws<ArgumentNullException>(
            () => QadPartInventoryReader.NormalizePartNumbers(parts));
    }

    [Fact]
    public void NormalizePartNumbers_Empty_List_Returns_Empty()
    {
        Assert.Empty(QadPartInventoryReader.NormalizePartNumbers([]));
    }
}
