using Kst.Domain.WorkOrders;
using Kst.Integrations.Qad.WorkOrders;

namespace Kst.Integrations.Qad.Tests.WorkOrders;

public sealed class QadWorkOrderSummaryReaderTests
{
    [Fact]
    public void BuildByWoidsQuery_Adds_One_Parameter_Per_Woid_Plus_Domain_And_Site()
    {
        var (sql, parameters) = QadWorkOrderSummaryReader.BuildByWoidsQuery("KTC", "SW", ["1001", "1002"]);

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("SW", parameters.Get<string>("Site"));
        Assert.Equal("1001", parameters.Get<string>("Woid0"));
        Assert.Equal("1002", parameters.Get<string>("Woid1"));
        Assert.Contains("@Woid0", sql);
        Assert.Contains("@Woid1", sql);
    }

    [Fact]
    public void BuildByWoidsQuery_Never_Concatenates_Raw_Woid_Values_Into_Sql_Text()
    {
        var maliciousWoid = "1'; DROP TABLE wo_mstr; --";
        var (sql, parameters) = QadWorkOrderSummaryReader.BuildByWoidsQuery("KTC", "SW", [maliciousWoid]);

        Assert.DoesNotContain(maliciousWoid, sql);
        Assert.Equal(maliciousWoid, parameters.Get<string>("Woid0"));
    }

    [Fact]
    public void BuildByWoidsQuery_Restricts_To_Eligible_Afr_Statuses_And_Excludes_Rmabom()
    {
        var (sql, _) = QadWorkOrderSummaryReader.BuildByWoidsQuery("KTC", "SW", ["1001"]);

        Assert.Contains("wo.wo_status IN ('A', 'F', 'R')", sql);
        Assert.Contains("ISNULL(wo.wo_bom_code, '') <> 'RMABOM'", sql);
    }

    [Fact]
    public void BuildByWoidsQuery_Joins_Requested_Woids_By_Wo_Lot()
    {
        var (sql, _) = QadWorkOrderSummaryReader.BuildByWoidsQuery("KTC", "SW", ["1001"]);

        Assert.Contains("req.Woid = wo.wo_lot", sql);
    }

    [Fact]
    public void BuildByWoidsQuery_Computes_Kitting_Counts_Without_Materializing_Lines()
    {
        var (sql, _) = QadWorkOrderSummaryReader.BuildByWoidsQuery("KTC", "SW", ["1001"]);

        Assert.Contains("OUTER APPLY", sql);
        Assert.Contains("wod.wod_qty_req <> 0", sql);
        Assert.Contains("wod.wod_qty_iss >= wod.wod_qty_req", sql);
        Assert.Contains("ApplicableLineCount", sql);
        Assert.Contains("FullyIssuedLineCount", sql);
    }

    [Fact]
    public void BuildByWoidsQuery_Selects_Accepted_Card_Fields_Only()
    {
        var (sql, _) = QadWorkOrderSummaryReader.BuildByWoidsQuery("KTC", "SW", ["1001"]);

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

    [Fact]
    public void BuildCandidateQuery_Parameterizes_Component()
    {
        var (sql, parameters) = QadWorkOrderSummaryReader.BuildCandidateQuery(
            "KTC", "SW", "COMP1", limit: 10);

        Assert.Equal("COMP1", parameters.Get<string>("ComponentPart"));
        Assert.Equal(11, parameters.Get<int>("FetchLimit"));
        Assert.Contains("@ComponentPart", sql);
    }

    [Fact]
    public void BuildCandidateQuery_Fetches_One_Row_Beyond_Limit_For_Truncation_Detection()
    {
        var (_, parameters) = QadWorkOrderSummaryReader.BuildCandidateQuery(
            "KTC", "SW", "COMP1", limit: 10);

        Assert.Equal(11, parameters.Get<int>("FetchLimit"));
    }

    [Fact]
    public void BuildCandidateQuery_Filters_Status_And_Rmabom_Without_Due_Date_Boundary()
    {
        var (sql, _) = QadWorkOrderSummaryReader.BuildCandidateQuery(
            "KTC", "SW", "COMP1", limit: 10);

        Assert.Contains("wo.wo_part = @ComponentPart", sql);
        Assert.Contains("wo.wo_status IN ('A', 'F', 'R')", sql);
        Assert.Contains("ISNULL(wo.wo_bom_code, '') <> 'RMABOM'", sql);
        Assert.DoesNotContain("wo_due_date <=", sql);
        Assert.DoesNotContain("@ParentDueDate", sql);
    }

    [Fact]
    public void BuildCandidateQuery_Orders_By_DueDate_ReleaseDate_Descending_Then_Woid_TieBreak()
    {
        var (sql, _) = QadWorkOrderSummaryReader.BuildCandidateQuery(
            "KTC", "SW", "COMP1", limit: 10);

        var orderByIndex = sql.IndexOf("ORDER BY", StringComparison.Ordinal);
        var orderByClause = sql[orderByIndex..];
        Assert.Contains("wo.wo_due_date DESC", orderByClause);
        Assert.Contains("wo.wo_rel_date DESC", orderByClause);
        Assert.Contains("wo.wo_lot ASC", orderByClause);
        Assert.True(orderByClause.IndexOf("wo_due_date", StringComparison.Ordinal)
            < orderByClause.IndexOf("wo_rel_date", StringComparison.Ordinal));
        Assert.True(orderByClause.IndexOf("wo_rel_date", StringComparison.Ordinal)
            < orderByClause.IndexOf("wo_lot", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildCandidateQuery_Never_Concatenates_Raw_Component_Value_Into_Sql_Text()
    {
        var maliciousComponent = "X'; DROP TABLE wo_mstr; --";
        var (sql, parameters) = QadWorkOrderSummaryReader.BuildCandidateQuery(
            "KTC", "SW", maliciousComponent, limit: 10);

        Assert.DoesNotContain(maliciousComponent, sql);
        Assert.Equal(maliciousComponent, parameters.Get<string>("ComponentPart"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ReadCandidatesAsync_Rejects_Non_Positive_Limit(int limit)
    {
        var reader = new QadWorkOrderSummaryReader(
            new Kst.Integrations.Qad.Options.QadConnectionOptions { Server = "srv", Database = "db" },
            Microsoft.Extensions.Logging.Abstractions.NullLogger<QadWorkOrderSummaryReader>.Instance);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => reader.ReadCandidatesAsync("SW", "COMP1", limit, CancellationToken.None));
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

    [Theory]
    [InlineData("A", WorkOrderStatus.Allocating)]
    [InlineData("F", WorkOrderStatus.Frozen)]
    [InlineData("R", WorkOrderStatus.Released)]
    public void NormalizeStatus_Maps_Eligible_Codes(string raw, WorkOrderStatus expected)
    {
        Assert.Equal(expected, QadWorkOrderSummaryReader.NormalizeStatus(raw));
    }

    [Theory]
    [InlineData("P")]
    [InlineData("e")]
    [InlineData("C")]
    [InlineData("Z")]
    public void NormalizeStatus_Throws_For_Ineligible_Or_Unknown_Codes(string raw)
    {
        Assert.Throws<InvalidOperationException>(() => QadWorkOrderSummaryReader.NormalizeStatus(raw));
    }

    [Fact]
    public void Normalize_Maps_Raw_Row_Into_Typed_WorkOrderSummary()
    {
        var raw = Raw(
            releaseDate: new DateTime(2026, 8, 3),
            dueDate: new DateTime(2026, 8, 15),
            applicableLineCount: 4,
            fullyIssuedLineCount: 2,
            salesOrder: "SO-4521");

        var normalized = QadWorkOrderSummaryReader.Normalize(raw);

        Assert.Equal("ABC100", normalized.PartNumber);
        Assert.Equal("1001", normalized.Woid);
        Assert.Equal(WorkOrderStatus.Released, normalized.Status);
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
    public void Normalize_Maps_Null_Dates_To_Null()
    {
        var normalized = QadWorkOrderSummaryReader.Normalize(Raw(releaseDate: null, dueDate: null));

        Assert.Null(normalized.ReleaseDate);
        Assert.Null(normalized.DueDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_Maps_Blank_Or_Missing_SalesOrder_To_Null(string? rawSalesOrder)
    {
        var normalized = QadWorkOrderSummaryReader.Normalize(Raw(salesOrder: rawSalesOrder));

        Assert.Null(normalized.SalesOrder);
    }

    [Fact]
    public void Normalize_Zero_Applicable_Lines_Produces_Null_Kitting_Percent()
    {
        var normalized = QadWorkOrderSummaryReader.Normalize(Raw(applicableLineCount: 0, fullyIssuedLineCount: 0));

        Assert.Null(normalized.Kitting.KittingPercent);
    }

    [Fact]
    public void ComposeCandidateResult_Not_Truncated_When_RowCount_At_Or_Below_Limit()
    {
        var rows = new List<QadWorkOrderSummaryRawRow> { Raw(woid: "1"), Raw(woid: "2") };

        var result = QadWorkOrderSummaryReader.ComposeCandidateResult(rows, limit: 2);

        Assert.False(result.IsTruncated);
        Assert.Equal(2, result.Candidates.Count);
    }

    [Fact]
    public void ComposeCandidateResult_Truncated_When_RowCount_Exceeds_Limit_And_Trims_To_Limit()
    {
        var rows = new List<QadWorkOrderSummaryRawRow> { Raw(woid: "1"), Raw(woid: "2"), Raw(woid: "3") };

        var result = QadWorkOrderSummaryReader.ComposeCandidateResult(rows, limit: 2);

        Assert.True(result.IsTruncated);
        Assert.Equal(2, result.Candidates.Count);
        Assert.Equal(["1", "2"], result.Candidates.Select(c => c.Woid));
    }
}
