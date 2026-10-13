using System.Data;
using Kst.Integrations.Qad.HistoricalShipments;
using Kst.Domain.HistoricalShipments;

namespace Kst.Integrations.Qad.Tests.HistoricalShipments;

public class QadHistoricalShipmentsTests
{
    private static DataTable Source()
    {
        var table = new DataTable(); table.Columns.Add("PhysicalDetailId", typeof(string));
        table.Columns.Add("HeaderCount", typeof(long)); table.Columns.Add("UnavailableEnrichment", typeof(int));
        var names = new[] { "site", "customerNumber", "customerName", "invoiceNumber", "invoiceDate", "itemNumber", "invoicedQuantity",
            "salesOrder", "salesOrderLine", "customerPo", "shipTo", "shipToName", "customerItem", "description1", "description2",
            "shipDate", "orderDate", "dueDate", "originalCommit", "unitPrice", "extendedPrice", "currency", "exchangeRate", "lineType",
            "shipVia", "billOfLading", "boxes", "pallets", "salesperson", "shippingAccount", "ncAccount", "release", "remarks", "productLine", "channel", "recordedUom",
            "requiredDate", "retroUnitPrice", "retroExtendedPrice", "onTime", "detailSalesOrder" };
        foreach (var name in names) table.Columns.Add(name, name is "invoiceDate" or "shipDate" or "orderDate" or "dueDate" or "originalCommit" or "requiredDate" ? typeof(DateTime)
            : name == "onTime" ? typeof(int) : name is "invoicedQuantity" or "unitPrice" or "retroUnitPrice" or "exchangeRate" or "boxes" or "pallets" ? typeof(decimal) : typeof(string));
        return table;
    }
    [Fact]
    public async Task Retro_price_is_distinct_exact_signed_and_never_falls_back_to_original_price()
    {
        using var table = Source();
        foreach (var (id, retro) in new[] { ("a", (decimal?)2.125m), ("b", null), ("c", 0.0000000001m) })
        {
            var row = table.NewRow(); row[0] = id; row[1] = 1L; row[2] = 0;
            row["invoicedQuantity"] = -2.5m; row["unitPrice"] = 99m; row["retroUnitPrice"] = retro is null ? DBNull.Value : retro.Value;
            row["onTime"] = 1; row["requiredDate"] = new DateTime(2026, 1, 2); row["detailSalesOrder"] = "000007"; table.Rows.Add(row);
        }
        using var reader = table.CreateDataReader(); var rows = new List<ShipmentRow>();
        await foreach (var row in QadHistoricalShipmentsReader.ParseAsync(reader, default)) rows.Add(row);
        Assert.Equal(new string?[] { "-5.3125", null, "-0.00000000025" }, rows.Select(r => r.Cells[38]));
        Assert.All(rows, r => { Assert.Equal("-247.5", r.Cells[20]); Assert.Equal("1", r.Cells[39]); Assert.Equal("2026-01-02", r.Cells[36]); Assert.Equal("000007", r.Cells[40]); });
    }
    [Fact]
    public void Projection_keeps_original_and_retro_prices_and_SQL_datetime_flag_rule_separate()
    {
        var f = new HistoricalShipmentsFilter("SW", null, null, null, null, null, null, null, null, null, null, null, ["shipDate"]);
        var sql = QadHistoricalShipmentsReader.BuildQuery(f).Sql;
        Assert.Contains("d.idh_price AS unitPrice", sql); Assert.Contains("d.idh_retro_price AS retroUnitPrice", sql);
        Assert.Contains("d.idh_req_date AS requiredDate", sql); Assert.Contains("d.idh_nbr AS detailSalesOrder", sql);
        Assert.Contains("WHEN h.ih_channel = 'Standard' AND d.idh_due_date <= d.idh_per_date THEN 1", sql);
        Assert.Contains("WHEN h.ih_channel = 'Standard' AND d.idh_due_date > d.idh_per_date THEN 0 ELSE NULL END AS onTime", sql);
        Assert.DoesNotContain("CAST(d.idh_due_date", sql); Assert.DoesNotContain("idh_qty_inv *", sql);
    }
    // Independently stated SQL-result fixtures. These verify parsing/projection, not live SQL execution.
    [Theory]
    [InlineData("Standard", "2026-01-01T12:00:00", "2026-01-02T12:00:00", 1)]
    [InlineData("Standard", "2026-01-02T12:00:00", "2026-01-02T12:00:00", 1)]
    [InlineData("Standard", "2026-01-03T12:00:00", "2026-01-02T12:00:00", 0)]
    [InlineData("Standard", "2026-01-02T13:00:00", "2026-01-02T12:00:00", 0)]
    [InlineData("Standard", "2026-01-02T11:00:00", "2026-01-02T12:00:00", 1)]
    [InlineData("Standard", null, "2026-01-02T12:00:00", null)]
    [InlineData("Standard", "2026-01-02T12:00:00", null, null)]
    [InlineData("RMA", "2026-01-01T12:00:00", "2026-01-02T12:00:00", null)]
    public async Task Datetime_flag_result_fixtures_preserve_nulls_and_flags(string channel, string? due, string? perform, int? expected)
    {
        using var table = Source(); var row = table.NewRow(); row[0] = "detail"; row[1] = 1L; row[2] = 0;
        row["channel"] = channel; row["dueDate"] = due is null ? DBNull.Value : DateTime.Parse(due, System.Globalization.CultureInfo.InvariantCulture);
        row["originalCommit"] = perform is null ? DBNull.Value : DateTime.Parse(perform, System.Globalization.CultureInfo.InvariantCulture);
        row["onTime"] = expected is null ? DBNull.Value : expected.Value; table.Rows.Add(row);
        using var reader = table.CreateDataReader();
        await foreach (var parsed in QadHistoricalShipmentsReader.ParseAsync(reader, default)) Assert.Equal(expected?.ToString(), parsed.Cells[39]);
    }
    [Fact]
    public async Task Signed_RMA_memo_adjustment_details_and_missing_optional_enrichment_survive()
    {
        using var table = Source();
        foreach (var (id, qty, channel, type) in new[] { ("a", -2.5m, "RMA-1", ""), ("b", 3m, "ADJ", "M"), ("c", 0m, "", "M") })
        {
            var row = table.NewRow(); row["PhysicalDetailId"] = id; row["HeaderCount"] = 1L; row["UnavailableEnrichment"] = 1;
            row["site"] = "SW"; row["customerNumber"] = "000999"; row["invoiceNumber"] = "000001"; row["salesOrder"] = "000002";
            row["salesOrderLine"] = "1"; row["invoicedQuantity"] = qty; row["unitPrice"] = 2.125m; row["currency"] = "EUR";
            row["invoiceDate"] = new DateTime(2026, 1, 31, 23, 59, 59); row["channel"] = channel; row["lineType"] = type; table.Rows.Add(row);
        }
        using var reader = table.CreateDataReader(); var rows = new List<ShipmentRow>();
        await foreach (var row in QadHistoricalShipmentsReader.ParseAsync(reader, default)) rows.Add(row);
        Assert.Equal(3, rows.Count); Assert.Equal(new[] { "a", "b", "c" }, rows.Select(r => r.PhysicalDetailId));
        Assert.Equal(new[] { "-2.5", "3", "0" }, rows.Select(r => r.Cells[6]));
        Assert.Equal(new[] { "-5.3125", "6.375", "0" }, rows.Select(r => r.Cells[20]));
        Assert.All(rows, r =>
        {
            Assert.Null(r.Cells[2]); Assert.Null(r.Cells[11]); Assert.Null(r.Cells[13]);
            Assert.True(r.UnavailableEnrichment); Assert.Equal("2026-01-31", r.Cells[4]); Assert.Equal("000999", r.Cells[1]);
        });
    }
    [Theory]
    [InlineData(0, "no matching header")]
    [InlineData(2, "multiple matching headers")]
    public async Task Missing_or_ambiguous_headers_are_explicit_failure(long count, string message)
    {
        using var table = Source(); var row = table.NewRow(); row[0] = "detail"; row[1] = count; row[2] = 1; table.Rows.Add(row);
        using var reader = table.CreateDataReader();
        var error = await Assert.ThrowsAsync<ShipmentSourceException>(async () => { await foreach (var _ in QadHistoricalShipmentsReader.ParseAsync(reader, default)) { } });
        Assert.Contains(message, error.Message);
    }
    [Fact]
    public async Task Repeated_physical_identity_fails_instead_of_deduplicating()
    {
        using var table = Source(); for (var i = 0; i < 2; i++) { var row = table.NewRow(); row[0] = "same"; row[1] = 1L; row[2] = 1; table.Rows.Add(row); }
        using var reader = table.CreateDataReader();
        await Assert.ThrowsAsync<ShipmentSourceException>(async () => { await foreach (var _ in QadHistoricalShipmentsReader.ParseAsync(reader, default)) { } });
    }
}
