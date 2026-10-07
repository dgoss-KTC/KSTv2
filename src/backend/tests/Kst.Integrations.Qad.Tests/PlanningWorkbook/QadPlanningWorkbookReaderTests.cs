using Kst.Domain.PartDetail;
using Kst.Integrations.Qad.Options;
using Kst.Integrations.Qad.PartDetail;
using Kst.Integrations.Qad.PlanningWorkbook;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Integrations.Qad.Tests.PlanningWorkbook;

public sealed class QadPartPriceReaderTests
{
    private static readonly DateOnly Today = new(2026, 9, 16);

    [Fact]
    public void BuildBatchQuery_Parameterizes_Domain_Date_And_Every_Part()
    {
        var malicious = "P'; DROP TABLE pi_mstr; --";
        var (sql, parameters) = QadPartPriceReader.BuildBatchQuery("KTC", ["P-1", malicious], Today);

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal(Today.ToDateTime(TimeOnly.MinValue), parameters.Get<DateTime>("Today"));
        Assert.Equal("P-1", parameters.Get<string>("Part0"));
        Assert.Equal(malicious, parameters.Get<string>("Part1"));
        Assert.Contains("(VALUES (@Part0), (@Part1))", sql);
        Assert.DoesNotContain(malicious, sql);
        Assert.DoesNotContain("@Site", sql); // prices are domain + part, no site (ledger §8.1)
    }

    [Fact]
    public void BuildBatchQuery_Reproduces_The_Accepted_Single_Part_Selection_Rule()
    {
        var (batch, _) = QadPartPriceReader.BuildBatchQuery("KTC", ["P-1"], Today);
        var (single, _) = QadPartDetailReader.BuildPriceQuery("KTC", "P-1", Today);

        // Same eligibility predicates as the accepted single-part query.
        Assert.Contains("pi_domain = @Domain", single);
        Assert.Contains("pi.pi_domain = @Domain", batch);
        Assert.Contains("pi_start <= @Today", single);
        Assert.Contains("pi.pi_start <= @Today", batch);
        Assert.Contains("ORDER BY pi_start DESC", single);
        Assert.Contains("ORDER BY pi.pi_start DESC", batch);
        // Latest eligible list per part, then only that list's tiers, ordered by MOQ ascending.
        Assert.Contains("ROW_NUMBER() OVER (PARTITION BY scope.PartNumber ORDER BY pi.pi_start DESC)", batch);
        Assert.Contains("cpl.ListRank = 1", batch);
        Assert.Contains("pid.pid_domain = @Domain", batch);
        Assert.Contains("pid.pid_list_id = cpl.ListId", batch);
        Assert.Contains("pid.pid_qty ASC", batch);
        Assert.Contains("pid.pid_qty AS MinimumOrderQuantity", batch);
        Assert.Contains("pid.pid_amt AS UnitPrice", batch);
        // No currency/UoM filters or invented tie-breakers beyond the accepted rule.
        foreach (var absent in new[] { "pi_curr", "pi_um", "pi_expire", "pi_list_id DESC", "pid_amt ASC", "pid_amt DESC" })
            Assert.DoesNotContain(absent, batch);
        // Every requested part is kept so "no current list" is returned explicitly.
        Assert.Contains("FROM ScopeParts AS scope", batch);
        Assert.Contains("LEFT JOIN RankedPriceLists AS cpl", batch);
        Assert.Contains("LEFT JOIN qadpro2.dbo.pid_det AS pid", batch);
    }

    [Fact]
    public void BuildBatchQuery_Empty_PartList_Throws()
    {
        Assert.Throws<ArgumentException>(() => QadPartPriceReader.BuildBatchQuery("KTC", [], Today));
    }

    [Fact]
    public void Normalize_Returns_One_Result_Per_Requested_Part_With_Tiers_Ordered_And_Zero_Price_Retained()
    {
        IReadOnlyList<QadPartPriceBatchRawRow> rows =
        [
            new("P-1", 1, 100m, 9.5m),
            new("P-1", 1, 1m, 12.345678m),
            new("P-2", 0, null, null),          // no current list or no tiers => unpriced
            new("P-3", 1, 1m, 0m)               // valid zero price
        ];

        var results = QadPartPriceReader.Normalize(["P-1", "P-2", "P-3"], rows);

        Assert.Equal(["P-1", "P-2", "P-3"], results.Select(r => r.PartNumber));
        Assert.Equal([new PartPriceBreak(1m, 12.345678m), new PartPriceBreak(100m, 9.5m)], results[0].Tiers);
        Assert.Empty(results[1].Tiers);
        Assert.Equal([new PartPriceBreak(1m, 0m)], results[2].Tiers);

        Assert.Equal(12.345678m, Kst.Domain.PlanningWorkbook.PlanningPriceSelection.Select(results[0].Tiers));
        Assert.Empty(results[1].Tiers); // successful absence: Planning selects $0
        Assert.Equal(0m, Kst.Domain.PlanningWorkbook.PlanningPriceSelection.Select(results[1].Tiers));
        Assert.Equal(0m, Kst.Domain.PlanningWorkbook.PlanningPriceSelection.Select(results[2].Tiers));
    }

    [Fact]
    public void Normalize_Matches_The_Accepted_Single_Part_Mapping_For_Unambiguous_Tiers()
    {
        // Same tier rows through the accepted Part Info mapping and the batch mapping.
        var single = QadPartDetailReader.Normalize(
            new QadPartMasterRawRow("P-1", null, null, null, null, null, null, null, null), null,
            [new QadPartPriceRawRow(50m, 4m), new QadPartPriceRawRow(10m, 5m), new QadPartPriceRawRow(1m, 6m)]);
        var batch = QadPartPriceReader.Normalize(["P-1"],
            [new("P-1", 1, 50m, 4m), new("P-1", 1, 10m, 5m), new("P-1", 1, 1m, 6m)]);

        Assert.Equal(single.PriceBreaks, batch[0].Tiers);
    }

    [Fact]
    public void Normalize_Rejects_A_Requested_Part_With_No_Row_Instead_Of_Reporting_It_Unpriced()
    {
        // Only an explicit HasTier = 0 row means unpriced; a missing row is an incomplete read.
        var ex = Assert.Throws<InvalidDataException>(() => QadPartPriceReader.Normalize(
            ["P-1", "P-4"], [new("P-1", 1, 1m, 2m)]));
        Assert.DoesNotContain("P-4", ex.Message);
        Assert.Throws<InvalidDataException>(() => QadPartPriceReader.Normalize(["P-1"], []));
        Assert.Single(QadPartPriceReader.Normalize(["P-1"], [new("P-1", 0, null, null)]));
    }

    [Fact]
    public void Normalize_Rejects_Malformed_Rows_Instead_Of_Zeroing_Or_Dropping()
    {
        Assert.Throws<InvalidDataException>(() => QadPartPriceReader.Normalize(["P-1"], [new("P-1", 1, null, 3m)]));
        Assert.Throws<InvalidDataException>(() => QadPartPriceReader.Normalize(["P-1"], [new("P-1", 1, 1m, null)]));
        Assert.Throws<InvalidDataException>(() => QadPartPriceReader.Normalize(["P-1"], [new("OTHER", 1, 1m, 3m)]));
    }

    [Fact]
    public async Task ReadAsync_Empty_Scope_Opens_No_Connection()
    {
        // The server name is a reserved non-resolvable host; an attempted connection would throw.
        var reader = new QadPartPriceReader(
            new QadConnectionOptions { Server = "kst-no-such-host.invalid", Database = "none", ConnectTimeoutSeconds = 1 },
            NullLogger<QadPartPriceReader>.Instance);

        Assert.Empty(await reader.ReadAsync("SW", [], Today));
    }

    [Fact]
    public async Task ReadAsync_When_Not_Configured_Throws()
    {
        var reader = new QadPartPriceReader(new QadConnectionOptions(), NullLogger<QadPartPriceReader>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadAsync("SW", ["P-1"], Today));
    }
}

public sealed class QadForecastReaderTests
{
    [Fact]
    public void BuildBatchQuery_Parameterizes_Domain_Site_And_Parts_With_Forecast_Filters_Only()
    {
        var malicious = "P'; DROP TABLE mrp_det; --";
        var (sql, parameters) = QadForecastReader.BuildBatchQuery("KTC", "SW", ["P-1", malicious]);

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("SW", parameters.Get<string>("Site"));
        Assert.Equal("P-1", parameters.Get<string>("Part0"));
        Assert.Equal(malicious, parameters.Get<string>("Part1"));
        Assert.DoesNotContain(malicious, sql);
        Assert.Contains("mrp.mrp_domain = @Domain", sql);
        Assert.Contains("mrp.mrp_site = @Site", sql);
        Assert.Contains("mrp.mrp_part = scope.ParentPart", sql);
        Assert.Contains("mrp.mrp_detail = 'FORECAST'", sql);
        Assert.Contains("mrp.mrp_qty <> 0", sql);
        Assert.Contains("mrp.mrp_due_date AS DueDate", sql);
        Assert.Contains("mrp.mrp_qty      AS Quantity", sql);
        // No invented dataset/type filters (live values unverified, E1); no date filter.
        Assert.DoesNotContain("mrp_dataset", sql);
        Assert.DoesNotContain("mrp_type", sql);
        Assert.DoesNotContain("@Today", sql);
        Assert.DoesNotContain("ABS(", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildBatchQuery_Empty_PartList_Throws()
    {
        Assert.Throws<ArgumentException>(() => QadForecastReader.BuildBatchQuery("KTC", "SW", []));
    }

    [Fact]
    public void Normalize_Preserves_Signed_Quantity_And_Own_Due_Date()
    {
        var negative = QadForecastReader.Normalize(new("P-1", new DateTime(2026, 9, 9), -12.5m));
        var positive = QadForecastReader.Normalize(new("P-1", new DateTime(2026, 11, 3), 7m));

        Assert.Equal(new Kst.Domain.PlanningWorkbook.PlanningForecastRow("P-1", new DateOnly(2026, 9, 9), -12.5m), negative);
        Assert.Equal(new Kst.Domain.PlanningWorkbook.PlanningForecastRow("P-1", new DateOnly(2026, 11, 3), 7m), positive);
    }

    [Fact]
    public void Normalize_Rejects_Malformed_Required_Facts()
    {
        Assert.Throws<InvalidDataException>(() => QadForecastReader.Normalize(new("P-1", null, 3m)));
        Assert.Throws<InvalidDataException>(() => QadForecastReader.Normalize(new("P-1", new DateTime(2026, 9, 9), null)));
        Assert.Throws<InvalidDataException>(() => QadForecastReader.Normalize(new(null, new DateTime(2026, 9, 9), 3m)));
    }

    [Fact]
    public async Task ReadAsync_Empty_Scope_Opens_No_Connection()
    {
        var reader = new QadForecastReader(
            new QadConnectionOptions { Server = "kst-no-such-host.invalid", Database = "none", ConnectTimeoutSeconds = 1 },
            NullLogger<QadForecastReader>.Instance);

        Assert.Empty(await reader.ReadAsync("SW", []));
    }
}
