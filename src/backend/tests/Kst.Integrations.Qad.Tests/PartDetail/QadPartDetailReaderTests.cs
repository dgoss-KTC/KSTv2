using Kst.Domain.Inventory;
using Kst.Integrations.Qad.PartDetail;

namespace Kst.Integrations.Qad.Tests.PartDetail;

public sealed class QadPartDetailReaderTests
{
    [Fact]
    public void BuildPartMasterQuery_Parameterizes_Domain_Site_And_Part()
    {
        var (sql, parameters) = QadPartDetailReader.BuildPartMasterQuery("KTC", "SW", "ABC100");

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("SW", parameters.Get<string>("Site"));
        Assert.Equal("ABC100", parameters.Get<string>("Part"));
        Assert.Contains("@Domain", sql);
        Assert.Contains("@Site", sql);
        Assert.Contains("@Part", sql);
        Assert.Contains("pt_domain", sql);
        Assert.Contains("pt_part", sql);
        Assert.Contains("TOP (1)", sql);
    }

    [Fact]
    public void BuildPartMasterQuery_Joins_PtMstr_To_PtpDet_On_Domain_And_Part_Not_PtSite()
    {
        var (sql, _) = QadPartDetailReader.BuildPartMasterQuery("KTC", "SW", "ABC100");

        Assert.Contains("LEFT JOIN qadpro2.dbo.ptp_det", sql);
        Assert.Contains("ptp.ptp_domain = pt.pt_domain", sql);
        Assert.Contains("ptp.ptp_part = pt.pt_part", sql);
        Assert.Contains("ptp.ptp_site = @Site", sql);
        Assert.DoesNotContain("pt_site", sql);
    }

    [Fact]
    public void BuildPartMasterQuery_Sources_SiteSpecific_Fields_From_PtpDet_Not_PtMstr()
    {
        var (sql, _) = QadPartDetailReader.BuildPartMasterQuery("KTC", "SW", "ABC100");

        Assert.Contains("ptp.ptp_mfg_lead", sql);
        Assert.Contains("AS ManufacturingLeadTimeDays", sql);
        Assert.Contains("ptp.ptp_sfty_tme", sql);
        Assert.Contains("AS SafetyTimeDays", sql);
        Assert.Contains("ptp.ptp_sfty_stk", sql);
        Assert.Contains("AS SafetyStockQuantity", sql);
        Assert.DoesNotContain("pt_mfg_lead", sql);
        Assert.DoesNotContain("pt_sfty_time", sql);
    }

    [Fact]
    public void BuildPartMasterQuery_Sources_NonSiteSpecific_Fields_From_PtMstr()
    {
        var (sql, _) = QadPartDetailReader.BuildPartMasterQuery("KTC", "SW", "ABC100");

        Assert.Contains("pt.pt_part      AS PartNumber", sql);
        Assert.Contains("pt.pt_buyer     AS PlannerCode", sql);
        Assert.Contains("pt.pt_status    AS PartStatusCode", sql);
        Assert.Contains("pt.pt_rev       AS CurrentRevision", sql);
        Assert.Contains("pt.pt_desc1     AS Description", sql);
        Assert.Contains("pt.pt_warr_cd   AS IosCode", sql);
        Assert.DoesNotContain("ptp.ptp_rev", sql);
    }

    [Fact]
    public void BuildPartMasterQuery_Uses_LeftJoin_So_Missing_PtpDet_Row_Does_Not_Drop_Part()
    {
        var (sql, _) = QadPartDetailReader.BuildPartMasterQuery("KTC", "SW", "ABC100");

        Assert.Contains("LEFT JOIN", sql);
        Assert.DoesNotContain("INNER JOIN qadpro2.dbo.ptp_det", sql);
    }

    [Fact]
    public void BuildPartMasterQuery_Never_Concatenates_Raw_Part_Value_Into_Sql_Text()
    {
        var maliciousPart = "ABC'; DROP TABLE pt_mstr; --";
        var (sql, parameters) = QadPartDetailReader.BuildPartMasterQuery("KTC", "SW", maliciousPart);

        Assert.DoesNotContain(maliciousPart, sql);
        Assert.Equal(maliciousPart, parameters.Get<string>("Part"));
    }

    // Stage 6 inventory SQL shape is now covered by QadPartInventoryReaderTests against the shared
    // QadPartInventoryReader.BuildBatchQuery builder (asserted at single-part scope for equivalence).

    [Fact]
    public void BuildPriceQuery_Filters_By_Start_Date_And_Orders_Latest_First()
    {
        var today = new DateOnly(2026, 8, 10);
        var (sql, parameters) = QadPartDetailReader.BuildPriceQuery("KTC", "ABC100", today);

        Assert.Equal(today.ToDateTime(TimeOnly.MinValue), parameters.Get<DateTime>("Today"));
        Assert.Contains("pi_start <= @Today", sql);
        Assert.Contains("ORDER BY pi_start DESC", sql);
        Assert.Contains("TOP (1) pi_list_id", sql);
    }

    [Fact]
    public void BuildPriceQuery_Orders_PriceTiers_By_Moq_Ascending()
    {
        var (sql, _) = QadPartDetailReader.BuildPriceQuery("KTC", "ABC100", new DateOnly(2026, 8, 10));

        Assert.Contains("ORDER BY pid.pid_qty ASC", sql);
    }

    [Fact]
    public void BuildPriceQuery_Never_Concatenates_Raw_Part_Value_Into_Sql_Text()
    {
        var maliciousPart = "ABC'; DROP TABLE pi_mstr; --";
        var (sql, parameters) = QadPartDetailReader.BuildPriceQuery("KTC", maliciousPart, new DateOnly(2026, 8, 10));

        Assert.DoesNotContain(maliciousPart, sql);
        Assert.Equal(maliciousPart, parameters.Get<string>("Part"));
    }

    [Fact]
    public void Normalize_Maps_PartMaster_Inventory_And_PriceRows_Into_SourceFacts()
    {
        var part = new QadPartMasterRawRow(
            PartNumber: "ABC100",
            PlannerCode: "JSMITH",
            ManufacturingLeadTimeDays: 10m,
            SafetyTimeDays: 2m,
            PartStatusCode: "C",
            CurrentRevision: "B",
            Description: "WIDGET CONTROL ASSEMBLY",
            IosCode: "1234",
            SafetyStockQuantity: 250m);
        var inventory = new PartInventorySummary(
            Site: "SW",
            PartNumber: "ABC100",
            NetQuantityOnHand: 1325m,
            NonNetQuantityOnHand: 75m,
            RmaQuantityOnHand: 25m);
        var priceRows = new List<QadPartPriceRawRow>
        {
            new(200m, 10.00m),
            new(100m, 12.45m),
        };

        var facts = QadPartDetailReader.Normalize(part, inventory, priceRows);

        Assert.Equal("ABC100", facts.PartNumber);
        Assert.Equal("JSMITH", facts.PlannerCode);
        Assert.Equal(10m, facts.ManufacturingLeadTimeDays);
        Assert.Equal(2m, facts.SafetyTimeDays);
        Assert.Equal("C", facts.PartStatusCode);
        Assert.Equal("B", facts.CurrentRevision);
        Assert.Equal("WIDGET CONTROL ASSEMBLY", facts.Description);
        Assert.Equal("1234", facts.IosCode);
        Assert.Equal(250m, facts.SafetyStockQuantity);
        Assert.Equal(1325m, facts.QuantityOnHand);
        Assert.Equal(75m, facts.QuantityNonNet);
        Assert.Equal(25m, facts.QuantityRmaOnHand);
        Assert.Equal(2, facts.PriceBreaks.Count);
        Assert.Equal(100m, facts.PriceBreaks[0].MinimumOrderQuantity);
        Assert.Equal(200m, facts.PriceBreaks[1].MinimumOrderQuantity);
    }

    [Fact]
    public void Normalize_Defaults_Inventory_To_Zero_When_No_Inventory_Row()
    {
        var part = new QadPartMasterRawRow(
            PartNumber: "ABC100", PlannerCode: null, ManufacturingLeadTimeDays: null, SafetyTimeDays: null,
            PartStatusCode: null, CurrentRevision: null, Description: null, IosCode: null, SafetyStockQuantity: null);

        var facts = QadPartDetailReader.Normalize(part, inventory: null, priceRows: []);

        Assert.Equal(0m, facts.QuantityOnHand);
        Assert.Equal(0m, facts.QuantityNonNet);
        Assert.Equal(0m, facts.QuantityRmaOnHand);
        Assert.Empty(facts.PriceBreaks);
    }

    [Fact]
    public void Normalize_Leaves_SiteSpecific_Fields_Null_When_PtpDet_Row_Missing()
    {
        // A LEFT JOIN with no matching ptp_det row for the site produces a QadPartMasterRawRow with
        // null site-specific fields (the missing join side maps to nulls) while pt_mstr fields remain
        // populated. Normalize must not substitute/fall back to any pt_mstr value here.
        var part = new QadPartMasterRawRow(
            PartNumber: "ABC100",
            PlannerCode: "JSMITH",
            ManufacturingLeadTimeDays: null,
            SafetyTimeDays: null,
            PartStatusCode: "C",
            CurrentRevision: null,
            Description: "WIDGET CONTROL ASSEMBLY",
            IosCode: "1234",
            SafetyStockQuantity: null);

        var facts = QadPartDetailReader.Normalize(part, inventory: null, priceRows: []);

        Assert.Null(facts.ManufacturingLeadTimeDays);
        Assert.Null(facts.SafetyTimeDays);
        Assert.Null(facts.CurrentRevision);
        Assert.Null(facts.SafetyStockQuantity);
        Assert.Equal("JSMITH", facts.PlannerCode);
        Assert.Equal("C", facts.PartStatusCode);
        Assert.Equal("WIDGET CONTROL ASSEMBLY", facts.Description);
        Assert.Equal("1234", facts.IosCode);
    }
}
