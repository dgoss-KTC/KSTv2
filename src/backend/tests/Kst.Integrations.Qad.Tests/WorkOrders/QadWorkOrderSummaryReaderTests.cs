using Kst.Domain.Mps;
using Kst.Domain.WorkOrders;
using Kst.Integrations.Qad.WorkOrders;

namespace Kst.Integrations.Qad.Tests.WorkOrders;

public sealed class QadWorkOrderSummaryReaderTests
{
    // Current business week begins Sunday 2026-08-30; the four-week forward window ends
    // exclusively Sunday 2026-09-27 (first day of Week 4).
    private static readonly DateOnly WeekStart = new(2026, 8, 30);
    private static readonly DateOnly WindowEnd = new(2026, 9, 27);

    // -- Planning window query ---------------------------------------------

    [Fact]
    public void BuildPlanningWindowQuery_Adds_Domain_Site_Parent_WeekStart_And_Basis_Params()
    {
        var (sql, parameters) = QadWorkOrderSummaryReader.BuildPlanningWindowQuery(
            "KTC", "SW", "ABC100", MpsDateBasis.DueDate, WeekStart, WindowEnd, bucketKind: null, bucketWeekStart: null);

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("SW", parameters.Get<string>("Site"));
        Assert.Equal("ABC100", parameters.Get<string>("ParentPart"));
        Assert.Equal(WeekStart.ToDateTime(TimeOnly.MinValue), parameters.Get<DateTime>("WeekStart"));
        Assert.Equal("dueDate", parameters.Get<string>("DateBasis"));
        Assert.Contains("@Domain", sql);
        Assert.Contains("@Site", sql);
        Assert.Contains("@ParentPart", sql);
        Assert.Contains("@WeekStart", sql);
    }

    [Fact]
    public void BuildPlanningWindowQuery_FullWindow_Falldown_Predicate_Always_Uses_DueDate()
    {
        var (sql, _) = QadWorkOrderSummaryReader.BuildPlanningWindowQuery(
            "KTC", "SW", "ABC100", MpsDateBasis.ReleaseDate, WeekStart, WindowEnd, bucketKind: null, bucketWeekStart: null);

        // Falldown is Due-Date based even in Release mode.
        Assert.Contains("wo.wo_due_date < @WeekStart", sql);
    }

    [Fact]
    public void BuildPlanningWindowQuery_FullWindow_DueBasis_Forward_Predicate_Uses_DueDate()
    {
        var (sql, parameters) = QadWorkOrderSummaryReader.BuildPlanningWindowQuery(
            "KTC", "SW", "ABC100", MpsDateBasis.DueDate, WeekStart, WindowEnd, bucketKind: null, bucketWeekStart: null);

        Assert.Equal("dueDate", parameters.Get<string>("DateBasis"));
        Assert.Equal(WindowEnd.ToDateTime(TimeOnly.MinValue), parameters.Get<DateTime>("WindowEnd"));
        Assert.Contains("@DateBasis = 'dueDate' AND wo.wo_due_date >= @WeekStart AND wo.wo_due_date < @WindowEnd", sql);
    }

    [Fact]
    public void BuildPlanningWindowQuery_FullWindow_ReleaseBasis_Forward_Predicate_Uses_ReleaseDate()
    {
        var (sql, parameters) = QadWorkOrderSummaryReader.BuildPlanningWindowQuery(
            "KTC", "SW", "ABC100", MpsDateBasis.ReleaseDate, WeekStart, WindowEnd, bucketKind: null, bucketWeekStart: null);

        Assert.Equal("releaseDate", parameters.Get<string>("DateBasis"));
        Assert.Equal(WindowEnd.ToDateTime(TimeOnly.MinValue), parameters.Get<DateTime>("WindowEnd"));
        Assert.Contains("@DateBasis = 'releaseDate' AND wo.wo_rel_date >= @WeekStart AND wo.wo_rel_date < @WindowEnd", sql);
    }

    [Fact]
    public void BuildPlanningWindowQuery_HorizonEnd_Is_Parameterized_Not_Hardcoded()
    {
        var (sql, parameters) = QadWorkOrderSummaryReader.BuildPlanningWindowQuery(
            "KTC", "SW", "ABC100", MpsDateBasis.DueDate, WeekStart, WindowEnd, bucketKind: null, bucketWeekStart: null);

        Assert.Equal(WindowEnd.ToDateTime(TimeOnly.MinValue), parameters.Get<DateTime>("WindowEnd"));
        Assert.Contains("@WindowEnd", sql);
        // The literal horizon date must not be concatenated into the SQL text.
        Assert.DoesNotContain(WindowEnd.ToString("yyyy-MM-dd"), sql);
    }

    [Fact]
    public void BuildPlanningWindowQuery_Excludes_Closed_And_Rmabom_At_The_Sql_Boundary()
    {
        var (sql, _) = QadWorkOrderSummaryReader.BuildPlanningWindowQuery(
            "KTC", "SW", "ABC100", MpsDateBasis.DueDate, WeekStart, WindowEnd, bucketKind: null, bucketWeekStart: null);

        Assert.Contains("wo.wo_status <> 'C'", sql);
        Assert.Contains("ISNULL(wo.wo_bom_code, '') <> 'RMABOM'", sql);
        // The top-level planning population is not limited to A/F/R.
        Assert.DoesNotContain("wo.wo_status IN ('A', 'F', 'R')", sql);
    }

    [Fact]
    public void BuildPlanningWindowQuery_Scopes_By_Domain_Site_And_ParentPart()
    {
        var (sql, _) = QadWorkOrderSummaryReader.BuildPlanningWindowQuery(
            "KTC", "SW", "ABC100", MpsDateBasis.DueDate, WeekStart, WindowEnd, bucketKind: null, bucketWeekStart: null);

        Assert.Contains("wo.wo_domain = @Domain", sql);
        Assert.Contains("wo.wo_site = @Site", sql);
        Assert.Contains("wo.wo_part = @ParentPart", sql);
    }

    [Fact]
    public void BuildPlanningWindowQuery_Never_Concatenates_Raw_ParentPart_Into_Sql_Text()
    {
        var maliciousParent = "X'; DROP TABLE wo_mstr; --";
        var (sql, parameters) = QadWorkOrderSummaryReader.BuildPlanningWindowQuery(
            "KTC", "SW", maliciousParent, MpsDateBasis.DueDate, WeekStart, WindowEnd, bucketKind: null, bucketWeekStart: null);

        Assert.DoesNotContain(maliciousParent, sql);
        Assert.Equal(maliciousParent, parameters.Get<string>("ParentPart"));
    }

    [Fact]
    public void BuildPlanningWindowQuery_Falldown_Bucket_Uses_DueDate_Only_And_No_Forward_Window()
    {
        var (sql, parameters) = QadWorkOrderSummaryReader.BuildPlanningWindowQuery(
            "KTC", "SW", "ABC100", MpsDateBasis.ReleaseDate, WeekStart, WindowEnd,
            bucketKind: MpsBucketKind.Falldown, bucketWeekStart: null);

        var where = WhereClause(sql);
        Assert.Contains("wo.wo_due_date < @WeekStart", where);
        // A Falldown-only request has no forward window, no basis switch, and no horizon end.
        Assert.DoesNotContain("@WindowEnd", where);
        Assert.DoesNotContain("@DateBasis", where);
        Assert.DoesNotContain("@BucketWeekStart", where);
        _ = parameters; // parameter set is asserted through the SQL text above
    }

    [Fact]
    public void BuildPlanningWindowQuery_Weekly_Bucket_Uses_Active_Basis_And_Bucket_Week_Bounds()
    {
        var bucketWeekStart = WeekStart.AddDays(14); // Week 2
        var (sql, parameters) = QadWorkOrderSummaryReader.BuildPlanningWindowQuery(
            "KTC", "SW", "ABC100", MpsDateBasis.ReleaseDate, WeekStart, WindowEnd,
            bucketKind: MpsBucketKind.Weekly, bucketWeekStart: bucketWeekStart);

        Assert.Equal(bucketWeekStart.ToDateTime(TimeOnly.MinValue), parameters.Get<DateTime>("BucketWeekStart"));
        Assert.Equal(bucketWeekStart.AddDays(7).ToDateTime(TimeOnly.MinValue), parameters.Get<DateTime>("BucketWeekEnd"));
        var where = WhereClause(sql);
        Assert.Contains("@DateBasis = 'releaseDate' AND wo.wo_rel_date >= @BucketWeekStart AND wo.wo_rel_date < @BucketWeekEnd", where);
        Assert.Contains("@DateBasis = 'dueDate' AND wo.wo_due_date >= @BucketWeekStart AND wo.wo_due_date < @BucketWeekEnd", where);
        // A single-week request has no full-window horizon end and no Falldown path.
        Assert.DoesNotContain("@WindowEnd", where);
        Assert.DoesNotContain("wo.wo_due_date < @WeekStart", where);
    }

    private static string WhereClause(string sql)
    {
        var whereIndex = sql.IndexOf("WHERE", StringComparison.Ordinal);
        var orderByIndex = sql.IndexOf("ORDER BY", StringComparison.Ordinal);
        return sql[whereIndex..orderByIndex];
    }

    [Fact]
    public void BuildPlanningWindowQuery_Weekly_Bucket_Without_A_Week_Start_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => QadWorkOrderSummaryReader.BuildPlanningWindowQuery(
                "KTC", "SW", "ABC100", MpsDateBasis.DueDate, WeekStart, WindowEnd,
                bucketKind: MpsBucketKind.Weekly, bucketWeekStart: null));
    }

    [Fact]
    public void BuildPlanningWindowQuery_Computes_Kitting_Counts_Without_Materializing_Lines()
    {
        var (sql, _) = QadWorkOrderSummaryReader.BuildPlanningWindowQuery(
            "KTC", "SW", "ABC100", MpsDateBasis.DueDate, WeekStart, WindowEnd, bucketKind: null, bucketWeekStart: null);

        Assert.Contains("OUTER APPLY", sql);
        Assert.Contains("wod.wod_qty_req <> 0", sql);
        Assert.Contains("wod.wod_qty_iss >= wod.wod_qty_req", sql);
        Assert.Contains("ApplicableLineCount", sql);
        Assert.Contains("FullyIssuedLineCount", sql);
    }

    [Fact]
    public void BuildPlanningWindowQuery_Selects_Accepted_Card_Fields_Only()
    {
        var (sql, _) = QadWorkOrderSummaryReader.BuildPlanningWindowQuery(
            "KTC", "SW", "ABC100", MpsDateBasis.DueDate, WeekStart, WindowEnd, bucketKind: null, bucketWeekStart: null);

        Assert.Contains("wo.wo_lot           AS Woid", sql);
        Assert.Contains("wo.wo_status        AS Status", sql);
        Assert.Contains("wo.wo_qty_ord       AS OrderedQuantity", sql);
        Assert.Contains("wo.wo_qty_comp      AS CompletedQuantity", sql);
        Assert.Contains("wo.wo_rel_date      AS ReleaseDate", sql);
        Assert.Contains("wo.wo_due_date      AS DueDate", sql);
        Assert.Contains("wo.wo_so_job        AS SalesOrder", sql);
        Assert.DoesNotContain("wo_nbr", sql);
        Assert.DoesNotContain("wo_start", sql);
        Assert.DoesNotContain("wo_line", sql);
        Assert.DoesNotContain("wo_pm_code", sql);
    }

    // -- Single-WOID query (Stage 7R parent resolution) ---------------------

    [Fact]
    public void BuildByWoidQuery_Parameterizes_Woid_And_Scopes_By_Domain_Site()
    {
        var (sql, parameters) = QadWorkOrderSummaryReader.BuildByWoidQuery("KTC", "SW", "1001");

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("SW", parameters.Get<string>("Site"));
        Assert.Equal("1001", parameters.Get<string>("Woid"));
        Assert.Contains("wo.wo_lot = @Woid", sql);
        Assert.Contains("wo.wo_domain = @Domain", sql);
        Assert.Contains("wo.wo_site = @Site", sql);
    }

    [Fact]
    public void BuildByWoidQuery_Excludes_Closed_And_Rmabom_But_Not_Limited_To_Afr()
    {
        var (sql, _) = QadWorkOrderSummaryReader.BuildByWoidQuery("KTC", "SW", "1001");

        Assert.Contains("wo.wo_status <> 'C'", sql);
        Assert.Contains("ISNULL(wo.wo_bom_code, '') <> 'RMABOM'", sql);
        // A planning-window parent may carry any non-closed status.
        Assert.DoesNotContain("wo.wo_status IN ('A', 'F', 'R')", sql);
    }

    [Fact]
    public void BuildByWoidQuery_Never_Concatenates_Raw_Woid_Into_Sql_Text()
    {
        var maliciousWoid = "1'; DROP TABLE wo_mstr; --";
        var (sql, parameters) = QadWorkOrderSummaryReader.BuildByWoidQuery("KTC", "SW", maliciousWoid);

        Assert.DoesNotContain(maliciousWoid, sql);
        Assert.Equal(maliciousWoid, parameters.Get<string>("Woid"));
    }

    private static QadWorkOrderSummaryRawRow Raw(
        string partNumber = "ABC100",
        string woid = "1001",
        string status = "R",
        decimal ordered = 100m,
        decimal completed = 40m,
        DateTime? releaseDate = null,
        DateTime? dueDate = null,
        int applicableLineCount = 4,
        int fullyIssuedLineCount = 2,
        string? salesOrder = null) => new(
        PartNumber: partNumber,
        Woid: woid,
        Status: status,
        OrderedQuantity: ordered,
        CompletedQuantity: completed,
        ReleaseDate: releaseDate,
        DueDate: dueDate,
        ApplicableLineCount: applicableLineCount,
        FullyIssuedLineCount: fullyIssuedLineCount,
        SalesOrder: salesOrder);

    // -- Status normalization ------------------------------------------------

    [Theory]
    [InlineData("P")]
    [InlineData("e")]
    [InlineData("E")]
    [InlineData("Z")]
    [InlineData("S")]
    public void NormalizePlanningWindowStatus_Passes_Through_Unknown_NonClosed_Codes(string raw)
    {
        Assert.Equal(raw, QadWorkOrderSummaryReader.NormalizePlanningWindowStatus(raw));
    }

    [Fact]
    public void NormalizePlanningWindowStatus_Trimms_Whitespace()
    {
        Assert.Equal("P", QadWorkOrderSummaryReader.NormalizePlanningWindowStatus("  P  "));
    }

    [Fact]
    public void NormalizePlanningWindow_Maps_Raw_Row_Into_Typed_WorkOrderSummary()
    {
        var raw = Raw(
            releaseDate: new DateTime(2026, 8, 3),
            dueDate: new DateTime(2026, 8, 15),
            applicableLineCount: 4,
            fullyIssuedLineCount: 2,
            salesOrder: "SO-4521");

        var normalized = QadWorkOrderSummaryReader.NormalizePlanningWindow(raw);

        Assert.Equal("ABC100", normalized.PartNumber);
        Assert.Equal("1001", normalized.Woid);
        Assert.Equal("R", normalized.Status);
        Assert.Equal(100m, normalized.OrderedQuantity);
        Assert.Equal(40m, normalized.CompletedQuantity);
        Assert.Equal(60m, normalized.OpenQuantity);
        Assert.Equal(new DateOnly(2026, 8, 3), normalized.ReleaseDate);
        Assert.Equal(new DateOnly(2026, 8, 15), normalized.DueDate);
        Assert.Equal("SO-4521", normalized.SalesOrder);
        Assert.Equal(4, normalized.Kitting.ApplicableLineCount);
        Assert.Equal(2, normalized.Kitting.FullyIssuedLineCount);
        Assert.Equal(50m, normalized.Kitting.KittingPercent);
    }

    [Fact]
    public void NormalizePlanningWindow_Preserves_An_Unknown_NonClosed_Status_Code()
    {
        var raw = Raw(status: "P", dueDate: new DateTime(2026, 9, 1));

        var normalized = QadWorkOrderSummaryReader.NormalizePlanningWindow(raw);

        Assert.Equal("P", normalized.Status);
        Assert.Equal(new DateOnly(2026, 9, 1), normalized.DueDate);
    }

    [Fact]
    public void NormalizePlanningWindow_Maps_Null_Dates_To_Null()
    {
        var normalized = QadWorkOrderSummaryReader.NormalizePlanningWindow(Raw(releaseDate: null, dueDate: null));

        Assert.Null(normalized.ReleaseDate);
        Assert.Null(normalized.DueDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizePlanningWindow_Maps_Blank_Or_Missing_SalesOrder_To_Null(string? rawSalesOrder)
    {
        var normalized = QadWorkOrderSummaryReader.NormalizePlanningWindow(Raw(salesOrder: rawSalesOrder));

        Assert.Null(normalized.SalesOrder);
    }

    [Fact]
    public void NormalizePlanningWindow_Zero_Applicable_Lines_Produces_Null_Kitting_Percent()
    {
        var normalized = QadWorkOrderSummaryReader.NormalizePlanningWindow(Raw(applicableLineCount: 0, fullyIssuedLineCount: 0));

        Assert.Null(normalized.Kitting.KittingPercent);
    }

}
