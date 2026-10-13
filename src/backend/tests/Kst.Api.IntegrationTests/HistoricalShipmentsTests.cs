using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using ClosedXML.Excel;
using Kst.Api.Dtos;
using Kst.Application.HistoricalShipments;
using Kst.Domain.HistoricalShipments;
using Kst.Exports;
using Kst.Infrastructure.HistoricalShipments;
using Kst.Integrations.Qad.HistoricalShipments;

namespace Kst.Api.IntegrationTests;

public class HistoricalShipmentsTests
{
    private static HistoricalShipmentsFilter Filter() => new("SW", null, null, null, null, null, null, null, null, null, null, null,
        ["site", "customerNumber", "invoiceNumber", "invoicedQuantity", "unitPrice", "extendedPrice"]);
    private static ShipmentRow Row(int i)
    {
        var cells = new string?[HistoricalShipmentsRules.Columns.Count];
        cells[0] = "SW"; cells[1] = "000123"; cells[3] = "000009"; cells[6] = "-2.5";
        cells[7] = "000001"; cells[8] = "1"; cells[19] = "1234567890123456.123456789";
        cells[20] = "-3086419725308640.3086419725"; cells[21] = "CAD";
        cells[32] = "=HYPERLINK(\"bad\")"; cells[34] = i % 3 == 0 ? "RMA" : i % 3 == 1 ? "ADJ" : "MEMO";
        return new($"{i:D36}", cells, true);
    }
    private static async IAsyncEnumerable<ShipmentRow> Rows(int n, [EnumeratorCancellation] CancellationToken ct = default)
    {
        for (var i = 0; i < n; i++) { ct.ThrowIfCancellationRequested(); yield return Row(i); if (i % 256 == 0) await Task.Yield(); }
    }
    [Fact]
    public async Task Ordered_catalog_defaults_display_and_workbook_agree_with_currency_and_full_identifiers()
    {
        var labels = new[] { "Ship Date", "Perform Date", "Sales Order", "Customer PO", "Line", "Part", "Qty Shipped", "Unit Price", "Ext Price", "Ship To", "Ship Via", "BOL", "Invoice Number", "SO Type", "On Time" };
        var defaults = HistoricalShipmentsRules.Catalog.Where(c => c.DefaultSelected).Select(c => c.Id).ToArray();
        Assert.Equal(labels, HistoricalShipmentsRules.Catalog.Where(c => c.DefaultSelected).Select(c => c.Label));
        var f = HistoricalShipmentsRules.Validate(Filter() with { Columns = defaults });
        Assert.Equal(defaults, f.Columns);
        Assert.DoesNotContain("currency", f.Columns);
        var optional = HistoricalShipmentsRules.Validate(f with { Columns = defaults.Concat(new[] { "dueDate", "requiredDate", "currency" }).ToArray() });
        Assert.Equal(new[] { "dueDate", "requiredDate", "currency" }, optional.Columns.TakeLast(3));
        var cells = Row(0).Cells.ToArray(); cells[37] = "2.125"; cells[38] = "-5.3125"; cells[39] = "0"; cells[36] = "2026-01-02";
        cells[3] = "00000012345678901234567890";
        using var store = new FileHistoricalShipmentsStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        var id = Guid.NewGuid(); store.Register(id, "owner");
        async IAsyncEnumerable<ShipmentRow> One() { yield return new("physical", cells); await Task.CompletedTask; }
        await store.AcquireAsync(id, "owner", f, _ => One(), default);
        var page = await store.PageAsync(id, "owner", 0, 100, default);
        Assert.Equal("2.125", page.Rows[0].Cells[7]); Assert.Equal("-5.3125", page.Rows[0].Cells[8]);
        await store.PrepareAsync(id, "owner", HistoricalShipmentsWorkbook.WriteAsync, default);
        using var file = store.OpenPreparation(id, "owner"); using var zip = new ZipArchive(file);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = XDocument.Load(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open()).Descendants(ns + "row").ToArray();
        string?[] Values(XElement row) => row.Elements(ns + "c").Select(c => c.Descendants(ns + "t").FirstOrDefault()?.Value ?? c.Element(ns + "v")?.Value).ToArray();
        Assert.Equal(labels, Values(rows[0])); Assert.Equal(page.Rows[0].Cells, Values(rows[1]));
        Assert.Equal("00000012345678901234567890", Values(rows[1])[12]); Assert.Empty(rows[1].Descendants(ns + "f"));
    }
    [Fact]
    public void Filter_and_SQL_expectations_are_explicit_and_history_scoped()
    {
        var f = HistoricalShipmentsRules.Validate(Filter()); Assert.DoesNotContain("currency", f.Columns);
        Assert.Throws<ArgumentException>(() => HistoricalShipmentsRules.Validate(f with { Site = "" }));
        Assert.Throws<ArgumentException>(() => HistoricalShipmentsRules.Validate(f with { Site = "KS" }));
        Assert.Throws<ArgumentException>(() => HistoricalShipmentsRules.Validate(f with { CorpGroup = "ACME", CustomerFrom = "0" }));
        Assert.Throws<ArgumentException>(() => HistoricalShipmentsRules.Validate(f with { InvoiceDateFrom = new(2026, 2, 2), InvoiceDateTo = new(2026, 2, 1) }));
        var blank = QadHistoricalShipmentsReader.BuildQuery(f);
        Assert.DoesNotContain("InvoiceDateFrom", blank.Sql); Assert.DoesNotContain("InvoiceDateEndExclusive", blank.Sql);
        foreach (var site in HistoricalShipmentsRules.Sites)
        {
            var (sql, p) = QadHistoricalShipmentsReader.BuildQuery(f with { Site = site });
            Assert.Equal(site, p.Get<string>("Site")); Assert.Equal(Kst.Integrations.Qad.Mps.QadSiteDomainMap.Resolve(site), p.Get<string>("Domain"));
            Assert.Contains("d.idh_site = @Site AND d.idh_domain = @Domain", sql);
        }
        var from = QadHistoricalShipmentsReader.BuildQuery(f with { InvoiceDateFrom = new(2026, 1, 1) });
        Assert.Contains("hf.ih_inv_date >= @InvoiceDateFrom", from.Sql); Assert.DoesNotContain("InvoiceDateEndExclusive", from.Sql);
        var to = QadHistoricalShipmentsReader.BuildQuery(f with { InvoiceDateTo = new(2026, 1, 31) });
        Assert.Equal(new DateTime(2026, 2, 1), to.Parameters.Get<DateTime>("InvoiceDateEndExclusive"));
        Assert.DoesNotContain("InvoiceDateFrom", to.Sql);
        var range = QadHistoricalShipmentsReader.BuildQuery(f with
        {
            CustomerFrom = "000001",
            CustomerTo = "999999",
            ItemFrom = "A",
            ItemTo = "Z",
            ProductLineFrom = "01",
            ProductLineTo = "99",
            SalespersonFrom = "001",
            SalespersonTo = "999",
            InvoiceDateFrom = new(2000, 1, 1),
            InvoiceDateTo = new(2026, 1, 31)
        });
        Assert.Contains("hf.ih_cust >= @CustomerFrom", range.Sql); Assert.Contains("hf.ih_cust <= @CustomerTo", range.Sql);
        Assert.Contains("hf.[ih_slspsn##1] >= @SalespersonFrom", range.Sql);
        Assert.Contains("d.idh_part <= @ItemTo", range.Sql); Assert.Contains("d.idh_prodline >= @ProductLineFrom", range.Sql);
        var group = QadHistoricalShipmentsReader.BuildQuery(f with { CorpGroup = "ACME" }).Sql;
        Assert.Contains("br.businessrelationisdomrestr = 0 OR", group); Assert.Contains("dm.domaincode = @Domain", group);
        Assert.Contains("cg.corporategroupcode = @CorpGroup", group); Assert.Contains("EXISTS (SELECT 1 FROM dbo.ad_mstr", group);
        Assert.DoesNotContain("INNER JOIN dbo.cm_mstr", group); Assert.DoesNotContain("TOP", group);
        Assert.DoesNotContain("cncu", group); Assert.DoesNotContain("tr_hist", group); Assert.DoesNotContain("ih_channel <>", group);
        Assert.Equal("-0.00000000000000000001", QadHistoricalShipmentsReader.Multiply("-0.0000000001", "0.0000000001"));
        Assert.Equal("-3086419725308640.3086419725", QadHistoricalShipmentsReader.Multiply("-2.5", "1234567890123456.123456789"));
        Assert.DoesNotContain(HistoricalShipmentsRules.Columns, c => c.Id.Contains("serial", StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task Frozen_large_result_pages_and_independent_workbook_inspection_preserve_physical_rows()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        using var store = new FileHistoricalShipmentsStore(root);
        var id = Guid.NewGuid(); store.Register(id, "owner");
        var f = HistoricalShipmentsRules.Validate(Filter() with { Columns = ["customerNumber", "salesOrder", "invoiceNumber", "invoicedQuantity", "unitPrice", "extendedPrice", "remarks", "currency"] });
        var operation = await store.AcquireAsync(id, "owner", f, ct => Rows(12001, ct), default);
        Assert.Equal(12001, operation.RowCount); Assert.Equal(12001, operation.UnavailableEnrichmentRows);
        var tail = await store.PageAsync(id, "owner", 12000, 200, default);
        Assert.Single(tail.Rows); Assert.Equal($"{12000:D36}", tail.Rows[0].PhysicalDetailId);
        Assert.Equal("000009", tail.Rows[0].Cells[2]);
        var prepared = await store.PrepareAsync(id, "owner", HistoricalShipmentsWorkbook.WriteAsync, default);
        using (var file = store.OpenPreparation(id, "owner"))
        using (var workbook = new XLWorkbook(file))
        {
            var sheet = workbook.Worksheet(1);
            Assert.Equal(12002, sheet.LastRowUsed()!.RowNumber());
            Assert.Equal("000123", sheet.Cell(2, 1).GetString()); Assert.Equal(XLDataType.Text, sheet.Cell(2, 1).DataType);
            Assert.Equal(-2.5, sheet.Cell(2, 4).GetDouble());
            Assert.Equal("1234567890123456.123456789", sheet.Cell(2, 5).GetString());
            Assert.Equal("-3086419725308640.3086419725", sheet.Cell(2, 6).GetString());
            Assert.Equal("=HYPERLINK(\"bad\")", sheet.Cell(2, 7).GetString()); Assert.False(sheet.Cell(2, 7).HasFormula);
            Assert.Equal("CAD", sheet.Cell(2, 8).GetString());
        }
        using (var file = store.OpenPreparation(id, "owner"))
        using (var zip = new ZipArchive(file))
        {
            var xml = XDocument.Load(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            Assert.Empty(xml.Descendants(ns + "f")); Assert.Equal(12002, xml.Descendants(ns + "row").Count());
        }
        Assert.True(prepared.ByteLength > 0);
        Assert.Throws<ShipmentOperationException>(() => store.Get(id, "another"));
        store.Release(id, "owner"); Assert.Empty(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
        Assert.Throws<ShipmentOperationException>(() => store.Register(id, "owner"));
        var cancelledBeforeRegister = Guid.NewGuid(); store.Release(cancelledBeforeRegister, "owner");
        Assert.Throws<ShipmentOperationException>(() => store.Register(cancelledBeforeRegister, "owner"));
    }
    [Fact]
    public async Task Workbook_transfer_size_above_eight_megabytes_is_not_truncated()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()); using var store = new FileHistoricalShipmentsStore(root);
        var id = Guid.NewGuid(); store.Register(id, "o");
        async IAsyncEnumerable<ShipmentRow> Large()
        {
            var random = new Random(19);
            for (var i = 0; i < 12000; i++)
            {
                var row = Row(i); var cells = row.Cells.ToArray(); var data = new byte[900]; random.NextBytes(data);
                cells[32] = Convert.ToBase64String(data); yield return row with { Cells = cells }; if (i % 256 == 0) await Task.Yield();
            }
        }
        var f = HistoricalShipmentsRules.Validate(Filter() with { Columns = ["remarks"] });
        await store.AcquireAsync(id, "o", f, _ => Large(), default);
        var p = await store.PrepareAsync(id, "o", HistoricalShipmentsWorkbook.WriteAsync, default);
        Assert.True(p.ByteLength > 8 * 1024 * 1024);
        using var stream = store.OpenPreparation(id, "o"); Assert.Equal(p.ByteLength, stream.Length);
        using var zip = new ZipArchive(stream); var xml = XDocument.Load(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"; Assert.Equal(12001, xml.Descendants(ns + "row").Count());
    }
    [Fact]
    public async Task Release_during_acquisition_blocks_late_publication_and_removes_all_files()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()); using var store = new FileHistoricalShipmentsStore(root);
        var id = Guid.NewGuid(); store.Register(id, "o");
        var started = new TaskCompletionSource(); var resume = new TaskCompletionSource();
        async IAsyncEnumerable<ShipmentRow> Delayed()
        {
            yield return Row(0); started.SetResult(); await resume.Task; yield return Row(1);
        }
        var task = store.AcquireAsync(id, "o", HistoricalShipmentsRules.Validate(Filter()), _ => Delayed(), default);
        await started.Task; store.Release(id, "o"); resume.SetResult();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Empty(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
        Assert.Throws<ShipmentOperationException>(() => store.Get(id, "o"));
        var fail = Guid.NewGuid(); store.Register(fail, "o");
        await Assert.ThrowsAsync<ShipmentSourceException>(() => store.AcquireAsync(fail, "o", HistoricalShipmentsRules.Validate(Filter()), _ => Failure(), default));
        Assert.Empty(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
        static async IAsyncEnumerable<ShipmentRow> Failure() { yield return Row(0); await Task.Yield(); throw new ShipmentSourceException("missing header"); }
    }
    [Fact]
    public async Task API_owned_lifecycle_validation_and_failure_are_explicit()
    {
        await using var factory = new KstApiFactory { HistoricalShipmentsReader = new DelegateHistoricalShipmentsReader((_, ct) => Rows(2, ct)) };
        using var client = factory.CreateClient(); client.DefaultRequestHeaders.Add("X-KST-Report-Session", Guid.NewGuid().ToString());
        var id = Guid.NewGuid(); var path = $"/api/v1/reports/historical-shipments/operations/{id}";
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsync(path, null)).StatusCode);
        var f = Filter();
        HistoricalShipmentsRequestDto Request(string site) => new(site, null, "000000", "000999", null, null, null, null, null, null, null, null, f.Columns);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path + "/acquire", Request(""))).StatusCode);
        var report = await (await client.PostAsJsonAsync(path + "/acquire", Request("SW"))).Content.ReadFromJsonAsync<HistoricalShipmentsOperationDto>();
        Assert.Equal(2, report!.RowCount); Assert.DoesNotContain("currency", report.Columns);
        var page = await client.GetFromJsonAsync<HistoricalShipmentsPageDto>(path + "/rows?offset=0&limit=100"); Assert.Equal(2, page!.Rows.Count);
        Assert.NotEqual(page.Rows[0].PhysicalDetailId, page.Rows[1].PhysicalDetailId);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(path + "/rows?offset=0&limit=201")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync(path + "/prepare", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/reports/historical-shipments/preparations/{id}/bytes")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsync(path, null)).StatusCode);
    }
    [Fact]
    public async Task API_header_failure_is_sanitized_explicit_and_releases_operation()
    {
        static async IAsyncEnumerable<ShipmentRow> MissingHeader() { yield return Row(0); await Task.Yield(); throw new ShipmentSourceException("An invoice detail has no matching header. No complete report was acquired."); }
        await using var factory = new KstApiFactory { HistoricalShipmentsReader = new DelegateHistoricalShipmentsReader((_, _) => MissingHeader()) };
        using var client = factory.CreateClient(); client.DefaultRequestHeaders.Add("X-KST-Report-Session", Guid.NewGuid().ToString());
        var id = Guid.NewGuid(); var path = $"/api/v1/reports/historical-shipments/operations/{id}"; await client.PutAsync(path, null);
        var response = await client.PostAsJsonAsync(path + "/acquire", new { site = "SW", columns = new[] { "site" } });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.Contains("no matching header", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync(path)).StatusCode);
    }
    [Fact]
    public async Task Explicit_XLSX_cell_failure_and_cancelled_preparation_remove_partial_files()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()); using var store = new FileHistoricalShipmentsStore(root);
        var id = Guid.NewGuid(); store.Register(id, "o");
        async IAsyncEnumerable<ShipmentRow> LongCell() { var r = Row(0); var cells = r.Cells.ToArray(); cells[32] = new string('a', 32768); yield return r with { Cells = cells }; await Task.Yield(); }
        var filter = HistoricalShipmentsRules.Validate(Filter() with { Columns = ["remarks"] });
        await store.AcquireAsync(id, "o", filter, _ => LongCell(), default);
        var error = await Assert.ThrowsAsync<ShipmentWorkbookCapacityException>(() => store.PrepareAsync(id, "o", HistoricalShipmentsWorkbook.WriteAsync, default));
        Assert.Contains("not truncated", error.Message); Assert.Empty(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
        var next = Guid.NewGuid(); store.Register(next, "o"); await store.AcquireAsync(next, "o", filter, ct => Rows(1, ct), default);
        using var cancel = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.PrepareAsync(next, "o", async (_, _, stream, ct) =>
        { await stream.WriteAsync(new byte[100], ct); cancel.Cancel(); ct.ThrowIfCancellationRequested(); }, cancel.Token));
        Assert.Empty(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
    }
    [Fact]
    public void Header_filters_drive_from_the_indexed_header_table_and_detail_only_filters_do_not()
    {
        var f = new HistoricalShipmentsFilter("SW", null, null, null, null, null, null, null, null, null, null, null, ["site"]);
        var detail = QadHistoricalShipmentsReader.BuildQuery(f with { ItemFrom = "A", ProductLineTo = "9" }).Sql;
        Assert.Contains("FROM dbo.idh_hist d", detail); Assert.DoesNotContain("hf.", detail); Assert.Contains("d.idh_part >= @ItemFrom", detail);
        foreach (var g in new[] { f with { CustomerFrom = "C1" }, f with { SalespersonTo = "S9" }, f with { InvoiceDateFrom = new(2026, 1, 1) }, f with { InvoiceDateTo = new(2026, 1, 31) }, f with { CorpGroup = "G" } })
        {
            var sql = QadHistoricalShipmentsReader.BuildQuery(g).Sql;
            Assert.Contains("FROM dbo.ih_hist hf INNER JOIN dbo.idh_hist d", sql); Assert.Contains("hf.ih_domain = @Domain", sql);
            Assert.Contains("d.idh_site = @Site", sql); Assert.Contains("hc.HeaderCount", sql);
        }
        var combined = QadHistoricalShipmentsReader.BuildQuery(f with { CustomerFrom = "C1", CustomerTo = "C2", ItemFrom = "A", InvoiceDateFrom = new(2026, 1, 1), InvoiceDateTo = new(2026, 1, 31), SalespersonFrom = "S1" });
        Assert.Contains("hf.ih_cust >= @CustomerFrom", combined.Sql); Assert.Contains("hf.[ih_slspsn##1] >= @SalespersonFrom", combined.Sql); Assert.Contains("d.idh_part >= @ItemFrom", combined.Sql);
        Assert.Equal(new DateTime(2026, 2, 1), combined.Parameters.Get<DateTime>("InvoiceDateEndExclusive"));
    }
    [Fact]
    public async Task Abandoned_roots_from_killed_processes_are_swept_but_recent_and_foreign_ones_are_kept()
    {
        var parent = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var old = Path.Combine(parent, Guid.NewGuid().ToString("N")); var recent = Path.Combine(parent, Guid.NewGuid().ToString("N")); var foreign = Path.Combine(parent, "keep-me");
        foreach (var d in new[] { old, recent, foreign }) { Directory.CreateDirectory(d); await File.WriteAllTextAsync(Path.Combine(d, "rows.bin"), "x"); }
        Directory.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddHours(-3));
        using var store = new FileHistoricalShipmentsStore(parent);
        Assert.False(Directory.Exists(old)); Assert.True(Directory.Exists(recent)); Assert.True(Directory.Exists(foreign));
    }
    [Fact]
    public async Task Expiry_releases_owned_files_without_a_followup_request()
    {
        var clock = new ManualClock(); var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        using var store = new FileHistoricalShipmentsStore(root, clock, TimeSpan.FromMinutes(1)); var id = Guid.NewGuid(); store.Register(id, "o");
        await store.AcquireAsync(id, "o", HistoricalShipmentsRules.Validate(Filter()), ct => Rows(1, ct), default);
        clock.Advance(TimeSpan.FromMinutes(2)); Assert.Empty(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
        Assert.Throws<ShipmentOperationException>(() => store.Get(id, "o"));
    }
    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow; private TimerCallback? _callback; private object? _state;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan span) { _now += span; _callback!(_state); }
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        { _callback = callback; _state = state; return new FakeTimer(); }
        private sealed class FakeTimer : ITimer { public bool Change(TimeSpan dueTime, TimeSpan period) => true; public void Dispose() { } public ValueTask DisposeAsync() => ValueTask.CompletedTask; }
    }
    [Fact]
    public async Task Workbook_row_capacity_is_explicit_whole_preparation_failure()
    {
        async IAsyncEnumerable<ShipmentRow> Oversize()
        {
            for (var i = 0; i < 1048576; i++) { yield return new("physical", [null]); if (i % 4096 == 0) await Task.Yield(); }
        }
        using var output = new MemoryStream();
        var error = await Assert.ThrowsAsync<ShipmentWorkbookCapacityException>(() => HistoricalShipmentsWorkbook.WriteAsync(Oversize(), ["site"], output, default));
        Assert.Contains("1,048,575", error.Message); Assert.Contains("not truncated", error.Message);
    }
}
