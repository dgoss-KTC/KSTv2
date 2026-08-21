using Kst.Integrations.Qad.ComponentDetail;

namespace Kst.Integrations.Qad.Tests.ComponentDetail;

public sealed class QadComponentSourceReaderTests
{
    [Fact]
    public void BuildMasterPlanningQuery_Parameterizes_Domain_Site_And_Part()
    {
        var (sql, parameters) = QadComponentSourceReader.BuildMasterPlanningQuery("KTC", "SW", "ABC100");

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
    public void BuildMasterPlanningQuery_Joins_PtMstr_To_PtpDet_With_Site_Filter_In_Join_Not_Where()
    {
        var (sql, _) = QadComponentSourceReader.BuildMasterPlanningQuery("KTC", "SW", "ABC100");

        Assert.Contains("LEFT JOIN qadpro2.dbo.ptp_det", sql);
        Assert.Contains("ptp.ptp_domain = pt.pt_domain", sql);
        Assert.Contains("ptp.ptp_part = pt.pt_part", sql);
        Assert.Contains("ptp.ptp_site = @Site", sql);

        // The site filter belongs to the JOIN condition, not the WHERE clause -- a missing
        // ptp_det row for the selected site must still return the pt_mstr-sourced fields.
        var whereIndex = sql.IndexOf("WHERE", StringComparison.Ordinal);
        Assert.True(whereIndex > 0);
        var whereClause = sql[whereIndex..];
        Assert.DoesNotContain("ptp_site", whereClause);
    }

    [Fact]
    public void BuildMasterPlanningQuery_Sources_Planning_LeadTime_And_Ordering_Fields_From_PtpDet()
    {
        var (sql, _) = QadComponentSourceReader.BuildMasterPlanningQuery("KTC", "SW", "ABC100");

        Assert.Contains("ptp.ptp_timefnce AS TimeFence", sql);
        Assert.Contains("ptp.ptp_sfty_tme AS SafetyTime", sql);
        Assert.Contains("ptp.ptp_sfty_stk AS SafetyStock", sql);
        Assert.Contains("ptp.ptp_buyer AS BuyerPlanner", sql);
        Assert.Contains("ptp.ptp_pur_lead AS PurchaseLeadTimeDays", sql);
        Assert.Contains("ptp.ptp_ins_lead AS InspectionLeadTimeDays", sql);
        Assert.Contains("ptp.ptp_cum_lead AS CumulativeLeadTimeDays", sql);
        Assert.Contains("ptp.ptp_ord_min AS MinimumOrderQuantity", sql);
        Assert.Contains("ptp.ptp_ord_mult AS OrderMultiple", sql);
    }

    [Fact]
    public void BuildMasterPlanningQuery_Never_Concatenates_Raw_Part_Value_Into_Sql_Text()
    {
        var maliciousPart = "ABC'; DROP TABLE pt_mstr; --";
        var (sql, parameters) = QadComponentSourceReader.BuildMasterPlanningQuery("KTC", "SW", maliciousPart);

        Assert.DoesNotContain(maliciousPart, sql);
        Assert.Equal(maliciousPart, parameters.Get<string>("Part"));
    }

    [Fact]
    public void BuildStandardCostQuery_Parameterizes_Domain_Site_And_Part_And_Filters_Sim_Standard()
    {
        var (sql, parameters) = QadComponentSourceReader.BuildStandardCostQuery("KTC", "SW", "ABC100");

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("SW", parameters.Get<string>("Site"));
        Assert.Equal("ABC100", parameters.Get<string>("Part"));
        Assert.Contains("sct_domain = @Domain", sql);
        Assert.Contains("sct_site = @Site", sql);
        Assert.Contains("sct_part = @Part", sql);
        Assert.Contains("sct_sim = 'Standard'", sql);
        Assert.Contains("TOP (1)", sql);
        Assert.Contains("ORDER BY sct_cst_date DESC", sql);
    }

    [Fact]
    public void BuildStandardCostQuery_Never_Concatenates_Raw_Part_Value_Into_Sql_Text()
    {
        var maliciousPart = "ABC'; DROP TABLE sct_det; --";
        var (sql, parameters) = QadComponentSourceReader.BuildStandardCostQuery("KTC", "SW", maliciousPart);

        Assert.DoesNotContain(maliciousPart, sql);
        Assert.Equal(maliciousPart, parameters.Get<string>("Part"));
    }

    [Fact]
    public void BuildQctcQuery_Parameterizes_Domain_Site_And_Part_And_Filters_Source_QtbomDet()
    {
        var (sql, parameters) = QadComponentSourceReader.BuildQctcQuery("KTC", "SW", "ABC100");

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("SW", parameters.Get<string>("Site"));
        Assert.Equal("ABC100", parameters.Get<string>("Part"));
        Assert.Contains("inp_domain = @Domain", sql);
        Assert.Contains("inp_site = @Site", sql);
        Assert.Contains("inp_part = @Part", sql);
        Assert.Contains("inp_source = 'qtbom_det'", sql);
        Assert.Contains("Analysis.dbo.in_price", sql);
        Assert.Contains("TOP (1)", sql);
        Assert.Contains("ORDER BY inp_start_date DESC", sql);
    }

    [Fact]
    public void BuildQctcQuery_Never_Concatenates_Raw_Part_Value_Into_Sql_Text()
    {
        var maliciousPart = "ABC'; DROP TABLE in_price; --";
        var (sql, parameters) = QadComponentSourceReader.BuildQctcQuery("KTC", "SW", maliciousPart);

        Assert.DoesNotContain(maliciousPart, sql);
        Assert.Equal(maliciousPart, parameters.Get<string>("Part"));
    }

    [Fact]
    public void Normalize_Maps_Master_Cost_And_Qctc_Rows_Into_SourceFacts()
    {
        var master = new QadComponentMasterRawRow(
            ComponentPart: "ABC100",
            Description1: "WIDGET",
            Description2: "CONTROL ASSEMBLY",
            PartStatusCode: "C",
            IosCode: "1234",
            TimeFence: 14,
            SafetyTime: 3m,
            SafetyStock: 100m,
            BuyerPlanner: "JSMITH",
            PurchaseLeadTimeDays: 21,
            InspectionLeadTimeDays: 2,
            CumulativeLeadTimeDays: 30,
            MinimumOrderQuantity: 500m,
            OrderMultiple: 100m);
        var cost = new QadStandardCostRawRow(StandardCost: 12.3456m, StandardCostDate: new DateTime(2026, 1, 1));
        var qctc = new QadQctcRawRow(Qctc: 9.8765m, QctcStartDate: new DateTime(2026, 2, 1));

        var facts = QadComponentSourceReader.Normalize(master, cost, qctc);

        Assert.Equal("ABC100", facts.ComponentPart);
        Assert.Equal("WIDGET CONTROL ASSEMBLY", facts.Description);
        Assert.Equal("C", facts.PartStatusCode);
        Assert.Equal("1234", facts.IosCode);
        Assert.Equal(12.3456m, facts.StandardCost);
        Assert.Equal(9.8765m, facts.Qctc);
        Assert.Equal(14, facts.TimeFence);
        Assert.Equal(3m, facts.SafetyTime);
        Assert.Equal(100m, facts.SafetyStock);
        Assert.Equal("JSMITH", facts.BuyerPlanner);
        Assert.Equal(21, facts.PurchaseLeadTimeDays);
        Assert.Equal(2, facts.InspectionLeadTimeDays);
        Assert.Equal(30, facts.CumulativeLeadTimeDays);
        Assert.Equal(500m, facts.MinimumOrderQuantity);
        Assert.Equal(100m, facts.OrderMultiple);
    }

    [Fact]
    public void Normalize_Leaves_StandardCost_And_Qctc_Null_When_No_Matching_Row()
    {
        var master = new QadComponentMasterRawRow(
            ComponentPart: "ABC100", Description1: null, Description2: null, PartStatusCode: null,
            IosCode: null, TimeFence: null, SafetyTime: null, SafetyStock: null, BuyerPlanner: null,
            PurchaseLeadTimeDays: null, InspectionLeadTimeDays: null, CumulativeLeadTimeDays: null,
            MinimumOrderQuantity: null, OrderMultiple: null);

        var facts = QadComponentSourceReader.Normalize(master, cost: null, qctc: null);

        Assert.Null(facts.StandardCost);
        Assert.Null(facts.Qctc);
    }

    [Fact]
    public void Normalize_Leaves_Planning_Fields_Null_When_PtpDet_Row_Missing()
    {
        // A LEFT JOIN with no matching ptp_det row for the site produces a QadComponentMasterRawRow
        // with null site-specific fields while pt_mstr fields remain populated. Normalize must not
        // substitute/fall back to any other value here.
        var master = new QadComponentMasterRawRow(
            ComponentPart: "ABC100",
            Description1: "WIDGET",
            Description2: null,
            PartStatusCode: "C",
            IosCode: "1234",
            TimeFence: null,
            SafetyTime: null,
            SafetyStock: null,
            BuyerPlanner: null,
            PurchaseLeadTimeDays: null,
            InspectionLeadTimeDays: null,
            CumulativeLeadTimeDays: null,
            MinimumOrderQuantity: null,
            OrderMultiple: null);

        var facts = QadComponentSourceReader.Normalize(master, cost: null, qctc: null);

        Assert.Equal("WIDGET", facts.Description);
        Assert.Equal("C", facts.PartStatusCode);
        Assert.Null(facts.TimeFence);
        Assert.Null(facts.SafetyTime);
        Assert.Null(facts.SafetyStock);
        Assert.Null(facts.BuyerPlanner);
        Assert.Null(facts.PurchaseLeadTimeDays);
        Assert.Null(facts.InspectionLeadTimeDays);
        Assert.Null(facts.CumulativeLeadTimeDays);
        Assert.Null(facts.MinimumOrderQuantity);
        Assert.Null(facts.OrderMultiple);
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("CAP", null, "CAP")]
    [InlineData(null, "ASSY", "ASSY")]
    [InlineData("CAP", "ASSY", "CAP ASSY")]
    public void CombineDescription_Is_Null_Safe(string? d1, string? d2, string? expected)
    {
        Assert.Equal(expected, QadComponentSourceReader.CombineDescription(d1, d2));
    }

    [Fact]
    public void CombineDescription_All_Blank_Is_Null()
    {
        Assert.Null(QadComponentSourceReader.CombineDescription(null, null));
        Assert.Null(QadComponentSourceReader.CombineDescription("   ", " \t "));
    }

    [Fact]
    public void CombineDescription_TrimSegments()
    {
        Assert.Equal("CAP ASSY", QadComponentSourceReader.CombineDescription("  CAP  ", "  ASSY  "));
    }
}
