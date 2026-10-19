using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Kst.Api.Dtos;
using Kst.Application.HistoricalShipments;
using Kst.Domain.HistoricalShipments;
using Kst.Infrastructure.HistoricalShipments;
using Xunit.Abstractions;

namespace Kst.Api.IntegrationTests;

public class WorkspaceShipmentViewTests(ITestOutputHelper log)
{
    private static HistoricalShipmentsFilter Projection() => new WorkspaceShipmentsFilter("SW", ["P"], null, null, null).Projection();
    private static async IAsyncEnumerable<ShipmentRow> Rows(int count, [EnumeratorCancellation] CancellationToken ct = default)
    {
        for (var i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var cells = new string?[HistoricalShipmentsRules.Columns.Count];
            cells[5] = $"P{i:D6}"; cells[6] = (i - 500).ToString(System.Globalization.CultureInfo.InvariantCulture);
            cells[38] = "123456789012345678901234567890.123456789";
            cells[39] = i % 3 == 0 ? "1" : i % 3 == 1 ? "0" : null;
            cells[18] = i % 2 == 0 ? "2026-10-01" : "2026-10-19";
            yield return new($"physical-{i}", cells);
            if (i % 256 == 0) await Task.Yield();
        }
    }
    private static string Root() => Path.Combine(Path.GetTempPath(), "kst-shipment-views-" + Guid.NewGuid().ToString("N"));
    private static async Task<Guid> Acquire(FileWorkspaceShipmentsStore store, int count, string owner = "owner")
    {
        var id = Guid.NewGuid(); store.Results.Register(id, owner);
        await store.Results.AcquireAsync(id, owner, Projection(), ct => Rows(count, ct), default, true); return id;
    }

    [Fact]
    public async Task Whole_result_predicates_summary_order_and_workspace_page_limits_are_independent_of_Reports()
    {
        var root = Root(); using var store = new FileWorkspaceShipmentsStore(root); var id = await Acquire(store, 2407);
        var view = await store.SetViewAsync(id, "owner", 0, 1, [new("invoicedQuantity", "compare", Operator: "between", Value: "1000", Upper: "1600")], default);
        Assert.Equal((601L, 201L, 401L), (view.RowCount, view.OnTimeNumerator, view.OnTimeDenominator));
        Assert.Equal(2407, store.Results.Get(id, "owner").RowCount);
        var page = await store.PageAsync(id, "owner", 1, 0, 500, default);
        Assert.Equal(500, page.Rows.Count); Assert.Equal("physical-1500", page.Rows[0].PhysicalDetailId); Assert.Equal("physical-1999", page.Rows[^1].PhysicalDetailId);
        var tail = await store.PageAsync(id, "owner", 1, 500, 1000, default);
        Assert.Equal(101, tail.Rows.Count); Assert.Equal("physical-2100", tail.Rows[^1].PhysicalDetailId);
        Assert.Empty((await store.PageAsync(id, "owner", 1, 601, 100, default)).Rows);
        Assert.Equal(601 * 8, new FileInfo(Directory.GetFiles(root, "view.bin", SearchOption.AllDirectories).Single()).Length);
        await Assert.ThrowsAsync<ArgumentException>(() => store.PageAsync(id, "owner", 1, 0, 1001, default));
        Assert.Equal(409, (await Assert.ThrowsAsync<ShipmentOperationException>(() => store.PageAsync(id, "owner", 0, 0, 100, default))).StatusCode);
        await Assert.ThrowsAsync<ArgumentException>(() => store.PageAsync(id, "owner", 1, long.MaxValue, 100, default));
        using var reports = new FileHistoricalShipmentsStore(Root()); reports.Register(id, "report");
        await reports.AcquireAsync(id, "report", Projection(), ct => Rows(1200, ct), default);
        await Assert.ThrowsAsync<ArgumentException>(() => reports.PageAsync(id, "report", 0, 500, default));
        Assert.Equal(200, (await reports.PageAsync(id, "report", 0, 200, default)).Rows.Count);
    }

    [Fact]
    public async Task Cascading_facets_exclude_their_own_filter_retain_zero_count_exceptions_and_do_not_reread()
    {
        var root = Root(); using var store = new FileWorkspaceShipmentsStore(root); var reads = 0;
        var id = Guid.NewGuid(); store.Results.Register(id, "owner");
        await store.Results.AcquireAsync(id, "owner", Projection(), ct => { reads++; return Rows(2407, ct); }, default, true);
        var filters = new ShipmentViewFilter[] { new("invoicedQuantity", "compare", Operator: "between", Value: "1000", Upper: "1600"), new("itemNumber", "values", "exclude", ["P001800", "absent"]) };
        var view = await store.SetViewAsync(id, "owner", 0, 1, filters, default);
        Assert.Equal((600L, 200L, 400L), (view.RowCount, view.OnTimeNumerator, view.OnTimeDenominator));
        var facet = await store.FacetsAsync(id, "owner", 1, "itemNumber", null, ["not-present"], default);
        Assert.Equal(601, facet.DistinctTotal); Assert.Equal(601, facet.MatchingDistinctTotal);
        Assert.Contains(facet.Values, v => v.Value == "P001800" && v.Count == 1);
        Assert.Contains(facet.RetainedValues, v => v.Value == "absent" && v.Count == 0);
        Assert.Contains(facet.RetainedValues, v => v.Value == "not-present" && v.Count == 0);
        var onTime = await store.FacetsAsync(id, "owner", 1, "onTime", null, [], default);
        Assert.Equal(200, onTime.BlankCount); Assert.Equal(new[] { 200L, 200L }, onTime.Values.Select(v => v.Count));
        await store.PageAsync(id, "owner", 1, 0, 1000, default); Assert.Equal(1, reads);
        Assert.DoesNotContain(Directory.GetDirectories(root, "*", SearchOption.AllDirectories), d => d.EndsWith("facet-scratch"));
    }

    [Fact]
    public async Task High_cardinality_external_sort_searches_all_values_and_caps_only_presentation()
    {
        var root = Root(); using var store = new FileWorkspaceShipmentsStore(root); var id = Guid.NewGuid(); store.Results.Register(id, "owner");
        async IAsyncEnumerable<ShipmentRow> Large()
        {
            for (var i = 0; i < 100004; i++)
            {
                var cells = new string?[HistoricalShipmentsRules.Columns.Count];
                cells[5] = i < 90001 ? $"P{i:D6}" : i < 100002 ? "REPEATED" : i == 100002 ? null : "";
                cells[39] = "1"; yield return new($"large-{i}", cells);
                if (i % 256 == 0) await Task.Yield();
            }
        }
        var stopwatch = Stopwatch.StartNew();
        await store.Results.AcquireAsync(id, "owner", Projection(), _ => Large(), default, true);
        var before = Process.GetCurrentProcess().WorkingSet64;
        var facet = await store.FacetsAsync(id, "owner", 0, "itemNumber", null, ["P089999", "missing"], default);
        Assert.Equal(90002, facet.DistinctTotal); Assert.True(facet.Truncated); Assert.Equal(1000, facet.Values.Count);
        Assert.Equal(new ShipmentFacetValue("REPEATED", 10001), facet.Values[0]); Assert.Equal("P000000", facet.Values[1].Value);
        Assert.Equal(2, facet.BlankCount); Assert.Contains(facet.RetainedValues, v => v.Value == "P089999" && v.Count == 1);
        Assert.Contains(facet.RetainedValues, v => v.Value == "missing" && v.Count == 0);
        var searched = await store.FacetsAsync(id, "owner", 0, "itemNumber", "p089999", [], default);
        Assert.Single(searched.Values); Assert.Equal("P089999", searched.Values[0].Value); Assert.Equal(1, searched.MatchingDistinctTotal);
        var view = await store.SetViewAsync(id, "owner", 0, 1, [new("itemNumber", "values", "exclude", ["REPEATED"], true)], default);
        Assert.Equal(90003, view.RowCount); // all hidden unique values and two blanks survive
        var all = await store.SetViewAsync(id, "owner", 1, 2, [new("itemNumber", "values", "exclude", [], true)], default);
        Assert.Equal(100004, all.RowCount);
        Assert.Equal(3, Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length);
        log.WriteLine($"Synthetic 100004 rows, 90002 distinct: {stopwatch.ElapsedMilliseconds} ms; working set before facets {before}, after {Process.GetCurrentProcess().WorkingSet64}; final disk {Directory.GetFiles(root, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length)} bytes. Observational, not a production capacity guarantee.");
        store.Results.Release(id, "owner"); Assert.Empty(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Cancel_failed_build_and_out_of_order_requests_preserve_the_previous_complete_view()
    {
        var root = Root(); using var store = new FileWorkspaceShipmentsStore(root); var id = await Acquire(store, 20001);
        var first = await store.SetViewAsync(id, "owner", 0, 1, [new("itemNumber", "values", "include", ["P000001"])], default);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SetViewAsync(id, "owner", 1, 2, [], cancel.Token));
        Assert.Equal(first, store.GetView(id, "owner"));
        store.CancelView(id, "owner", 10); // cancel can arrive before request registration
        Assert.Equal(409, (await Assert.ThrowsAsync<ShipmentOperationException>(() => store.SetViewAsync(id, "owner", 1, 10, [], default))).StatusCode);
        var pending = store.SetViewAsync(id, "owner", 1, 11, [], default); store.CancelView(id, "owner", 11);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(first, store.GetView(id, "owner"));
        Assert.Single((await store.PageAsync(id, "owner", 1, 0, 1000, default)).Rows);
        // A malformed tail fails only the derived build; last good index and original rows stay owned.
        var rows = Directory.GetFiles(root, "rows.bin", SearchOption.AllDirectories).Single();
        using (var file = new FileStream(rows, FileMode.Append)) file.WriteByte(0);
        await Assert.ThrowsAsync<InvalidDataException>(() => store.SetViewAsync(id, "owner", 1, 12, [], default));
        Assert.Equal(first, store.GetView(id, "owner"));
        Assert.Single((await store.PageAsync(id, "owner", 1, 0, 1000, default)).Rows);
        Assert.DoesNotContain(Directory.GetFiles(root, "*", SearchOption.AllDirectories), f => f.EndsWith("pending"));
        using (var file = new FileStream(rows, FileMode.Open, FileAccess.Write)) file.SetLength(file.Length - 1);
        var final = await store.SetViewAsync(id, "owner", 1, 14, [], default); Assert.Equal(2, final.Version);
        Assert.Equal(409, (await Assert.ThrowsAsync<ShipmentOperationException>(() => store.SetViewAsync(id, "owner", 2, 13, [], default))).StatusCode);
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    [Fact]
    public async Task View_and_facet_work_share_the_existing_two_worker_budget_without_using_Reports_workers()
    {
        using var store = new FileWorkspaceShipmentsStore(Root()); var ready = await Acquire(store, 200);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var active = 0;
        async IAsyncEnumerable<ShipmentRow> Blocked([EnumeratorCancellation] CancellationToken ct)
        {
            if (Interlocked.Increment(ref active) == 2) started.SetResult();
            await resume.Task.WaitAsync(ct); await foreach (var row in Rows(1, ct)) yield return row;
        }
        var first = Guid.NewGuid(); var second = Guid.NewGuid(); store.Results.Register(first, "owner"); store.Results.Register(second, "owner");
        var a = store.Results.AcquireAsync(first, "owner", Projection(), ct => Blocked(ct), default, true);
        var b = store.Results.AcquireAsync(second, "owner", Projection(), ct => Blocked(ct), default, true);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            Assert.Equal(503, (await Assert.ThrowsAsync<ShipmentOperationException>(() => store.SetViewAsync(ready, "owner", 0, 1, [], default))).StatusCode);
            Assert.Equal(503, (await Assert.ThrowsAsync<ShipmentOperationException>(() => store.FacetsAsync(ready, "owner", 0, "itemNumber", null, [], default))).StatusCode);
            Assert.Equal(0, store.GetView(ready, "owner").Version);
            using var reports = new FileHistoricalShipmentsStore(Root()); var reportId = Guid.NewGuid(); reports.Register(reportId, "report");
            await reports.AcquireAsync(reportId, "report", Projection(), ct => Rows(1, ct), default);
            Assert.Single((await reports.PageAsync(reportId, "report", 0, 100, default)).Rows);
        }
        finally { resume.SetResult(); await Task.WhenAll(a, b); }
        Assert.Equal(200, (await store.SetViewAsync(ready, "owner", 0, 2, [], default)).RowCount);
    }
    [Fact]
    public async Task Pending_index_write_failure_keeps_the_last_good_view_and_can_retry_without_reacquiring()
    {
        var root = Root(); using var store = new FileWorkspaceShipmentsStore(root); var id = await Acquire(store, 500);
        var old = await store.SetViewAsync(id, "owner", 0, 1, [new("itemNumber", "values", "include", ["P000301"])], default);
        var directory = Path.GetDirectoryName(Directory.GetFiles(root, "rows.bin", SearchOption.AllDirectories).Single())!;
        var obstruction = Path.Combine(directory, "view.pending"); Directory.CreateDirectory(obstruction);
        var failure = await Record.ExceptionAsync(() => store.SetViewAsync(id, "owner", 1, 2, [], default));
        Assert.True(failure is IOException or UnauthorizedAccessException);
        Assert.Equal(old, store.GetView(id, "owner")); Assert.Equal("physical-301", (await store.PageAsync(id, "owner", 1, 0, 100, default)).Rows.Single().PhysicalDetailId);
        Directory.Delete(obstruction);
        Assert.Equal(500, (await store.SetViewAsync(id, "owner", 1, 3, [], default)).RowCount);
    }
    [Fact]
    public async Task Cancelling_facets_after_first_online_merge_begins_cleans_scratch_and_keeps_acquisition()
    {
        var root = Root(); using var store = new FileWorkspaceShipmentsStore(root); var id = await Acquire(store, 90001);
        using var cancel = new CancellationTokenSource();
        using var watcher = new FileSystemWatcher(root, "run-16.bin") { IncludeSubdirectories = true, EnableRaisingEvents = true };
        var mergeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.Created += (_, _) => { mergeStarted.TrySetResult(); cancel.Cancel(); };
        var pending = store.FacetsAsync(id, "owner", 0, "itemNumber", null, [], cancel.Token);
        await mergeStarted.Task.WaitAsync(TimeSpan.FromSeconds(15));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(0, store.GetView(id, "owner").Version); Assert.Equal(90001, store.Results.Get(id, "owner").RowCount);
        Assert.Equal(2, Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length);
        Assert.Equal("physical-90000", (await store.PageAsync(id, "owner", 0, 90000, 1000, default)).Rows.Single().PhysicalDetailId);
    }
    [Fact]
    public async Task Ownership_release_and_expiry_precede_work_and_delete_all_derived_artifacts()
    {
        var root = Root(); var clock = new Clock(); using var store = new FileWorkspaceShipmentsStore(root, clock, TimeSpan.FromMinutes(30)); var id = await Acquire(store, 20001);
        await Assert.ThrowsAsync<ShipmentOperationException>(() => store.SetViewAsync(id, "other", 0, 1, [], default));
        await Assert.ThrowsAsync<ShipmentOperationException>(() => store.FacetsAsync(id, "other", 0, "itemNumber", null, [], default));
        Assert.Throws<ShipmentOperationException>(() => store.CancelView(id, "other", 100));
        var originalExpiry = store.Results.Get(id, "owner").Expires;
        await store.SetViewAsync(id, "owner", 0, 1, [], default);
        Assert.Equal(originalExpiry, store.Results.Get(id, "owner").Expires);
        var pending = store.SetViewAsync(id, "owner", 1, 2, [], default); store.Results.Release(id, "owner");
        await Assert.ThrowsAnyAsync<Exception>(() => pending); Assert.Empty(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
        id = await Acquire(store, 20001); await store.SetViewAsync(id, "owner", 0, 1, [], default);
        var expiring = store.FacetsAsync(id, "owner", 1, "itemNumber", null, [], default);
        clock.Now += TimeSpan.FromMinutes(31);
        Assert.Throws<ShipmentOperationException>(() => store.GetView(id, "owner"));
        await Assert.ThrowsAnyAsync<Exception>(() => expiring); Assert.Empty(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Typed_HTTP_views_facets_versions_and_request_bounds_preserve_source_acquisition_and_report_caps()
    {
        var reads = 0;
        await using var factory = new KstApiFactory { WorkspaceShipmentsReader = new DelegateWorkspaceShipmentsReader((_, ct) => { reads++; return Rows(2407, ct); }) };
        using var client = factory.CreateClient(); client.DefaultRequestHeaders.Add("X-KST-Workspace-Shipments-Session", Guid.NewGuid().ToString("D"));
        var created = await client.PostAsJsonAsync("/api/v1/workspaces", new CreateWorkspaceRequestDto("Synthetic", "SW", null, null, ["P"], false, null)); created.EnsureSuccessStatusCode();
        var assignment = (await created.Content.ReadFromJsonAsync<WorkspaceAssignmentDto>())!.AssignmentId;
        var path = $"/api/v1/workspaces/{assignment}/shipments/operations/{Guid.NewGuid()}";
        (await client.PutAsync(path, null)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync(path + "/acquire", new WorkspaceShipmentsRequestDto(null, null, null))).EnsureSuccessStatusCode();
        var spec = await client.GetFromJsonAsync<WorkspaceShipmentsSpecDto>($"/api/v1/workspaces/{assignment}/shipments/spec");
        Assert.Equal(1000, spec!.MaximumPageSize); Assert.Equal(new[] { 100, 500, 1000 }, spec.PageSizes);
        var body = new WorkspaceShipmentsViewRequestDto(0, 1, [new("itemNumber", "values", "include", ["P001800"])]);
        var response = await client.PostAsJsonAsync(path + "/view", body); response.EnsureSuccessStatusCode();
        var view = (await response.Content.ReadFromJsonAsync<WorkspaceShipmentsViewDto>())!; Assert.Equal((1L, 1L, 1L), (view.RowCount, view.OnTimeNumerator, view.OnTimeDenominator));
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync(path + "/rows?offset=0&limit=1000&viewVersion=0")).StatusCode);
        var page = await client.GetFromJsonAsync<WorkspaceShipmentsPageDto>(path + "/rows?offset=0&limit=1000&viewVersion=1"); Assert.Equal("physical-1800", page!.Rows.Single().PhysicalDetailId);
        var facets = await client.PostAsJsonAsync(path + "/values", new WorkspaceShipmentsFacetRequestDto(1, "itemNumber", "P002406", ["not-present"])); facets.EnsureSuccessStatusCode();
        Assert.Equal(2407, (await facets.Content.ReadFromJsonAsync<WorkspaceShipmentsFacetsDto>())!.DistinctTotal);
        Assert.Equal(1, reads); Assert.Equal(2407, (await client.GetFromJsonAsync<WorkspaceShipmentsOperationDto>(path))!.RowCount);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path + "/view", body)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path + "/view", body with { ExpectedVersion = 1, Sequence = 2, Filters = [new("shipDate", "dateRange")] })).StatusCode);
        var oversized = new StringContent(new string(' ', 5 * 1024 * 1024 + 1), System.Text.Encoding.UTF8, "application/json");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(path + "/view", oversized)).StatusCode);
        using var streamed = new UnmeasuredContent(5 * 1024 * 1024 + 1);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(path + "/view", streamed)).StatusCode);
        using var stranger = factory.CreateClient(); stranger.DefaultRequestHeaders.Add("X-KST-Workspace-Shipments-Session", Guid.NewGuid().ToString("D"));
        Assert.Equal(HttpStatusCode.Gone, (await stranger.PostAsJsonAsync(path + "/view", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await stranger.PostAsJsonAsync(path + "/values", new WorkspaceShipmentsFacetRequestDto(1, "itemNumber", null, []))).StatusCode);
        var reportSpec = await client.GetFromJsonAsync<HistoricalShipmentsSpecDto>("/api/v1/reports/historical-shipments/spec"); Assert.Equal(200, reportSpec!.MaximumPageSize);
        await client.DeleteAsync(path); Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync(path + "/view")).StatusCode);
    }
    private sealed class UnmeasuredContent(int length) : HttpContent
    {
        protected override bool TryComputeLength(out long computedLength) { computedLength = 0; return false; }
        protected override async Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context)
        {
            var block = System.Text.Encoding.UTF8.GetBytes(new string(' ', 8192));
            for (var written = 0; written < length; written += block.Length)
                await stream.WriteAsync(block.AsMemory(0, Math.Min(block.Length, length - written)));
        }
    }
}
