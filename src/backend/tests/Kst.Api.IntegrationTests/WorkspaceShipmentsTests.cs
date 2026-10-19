using System.Net;
using System.Net.Http.Json;
using Kst.Api.Dtos;
using Kst.Application.HistoricalShipments;
using Kst.Domain.HistoricalShipments;

namespace Kst.Api.IntegrationTests;

public class WorkspaceShipmentsTests
{
    [Fact]
    public async Task Dedicated_disk_result_release_removes_rows_and_offsets_without_touching_report_store()
    {
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        using var workspace = new Kst.Infrastructure.HistoricalShipments.FileWorkspaceShipmentsStore(System.IO.Path.Combine(root, "workspace"));
        using var report = new Kst.Infrastructure.HistoricalShipments.FileHistoricalShipmentsStore(System.IO.Path.Combine(root, "report"));
        var id = Guid.NewGuid(); var owner = WorkspaceShipmentsService.Owner(Guid.NewGuid(), Guid.NewGuid().ToString("D"));
        workspace.Results.Register(id, owner); report.Register(id, "report");
        var projection = new WorkspaceShipmentsFilter("SW", ["0000123"], null, null, null).Projection();
        await workspace.Results.AcquireAsync(id, owner, projection, _ => Rows(), default, includeOnTimeSummary: true);
        await report.AcquireAsync(id, "report", projection, _ => Rows(), default);
        Assert.Equal(2, Directory.GetFiles(System.IO.Path.Combine(root, "workspace"), "*", SearchOption.AllDirectories).Length);
        workspace.Results.Release(id, owner);
        Assert.Empty(Directory.GetFiles(System.IO.Path.Combine(root, "workspace"), "*", SearchOption.AllDirectories));
        Assert.Equal(4, (await report.PageAsync(id, "report", 0, 100, default)).Rows.Count);
        report.Release(id, "report");
    }
    private static async Task<Guid> Workspace(HttpClient client, string site = "KV", string parent = "0000123",
        string? productLineFrom = "1000", string? productLineTo = "1999", string? display = "Customer 000999 / Group ACME")
    {
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", new CreateWorkspaceRequestDto(
            display, site, productLineFrom, productLineTo, [parent], false, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkspaceAssignmentDto>())!.AssignmentId;
    }
    private static void Session(HttpClient c) => c.DefaultRequestHeaders.Add("X-KST-Workspace-Shipments-Session", Guid.NewGuid().ToString("D"));
    private static string Path(Guid assignment, Guid operation) => $"/api/v1/workspaces/{assignment}/shipments/operations/{operation}";
    private static async IAsyncEnumerable<ShipmentRow> Rows()
    {
        // Independently specified acquired SQL order: dated newest first, same date physical ID,
        // undated last. Equal invoice/order/line identifiers remain separate details.
        foreach (var (id, date) in new[] { ("b", "2026-10-13"), ("c", "2026-10-13"), ("a", "2026-01-01"), ("d", (string?)null) })
        {
            var cells = new string?[HistoricalShipmentsRules.Columns.Count];
            cells[15] = date; cells[3] = "000001"; cells[5] = "0000123"; cells[6] = "-2.5";
            cells[7] = "000002"; cells[8] = "1"; cells[19] = "99"; cells[37] = "2.125"; cells[38] = "-5.3125"; cells[39] = id switch { "b" => "1", "c" => "0", "a" => "1", _ => null };
            yield return new(id, cells); await Task.Yield();
        }
    }

    [Fact]
    public async Task On_time_summary_counts_each_physical_row_once_across_the_full_result_and_ignores_unavailable_values()
    {
        using var store = new Kst.Infrastructure.HistoricalShipments.FileHistoricalShipmentsStore(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var id = Guid.NewGuid(); store.Register(id, "owner");
        var filter = HistoricalShipmentsRules.Validate(new HistoricalShipmentsFilter("SW", null, null, null, null, null,
            null, null, null, null, null, null, HistoricalShipmentsRules.DefaultColumnIds));
        async IAsyncEnumerable<ShipmentRow> IndependentRows()
        {
            foreach (var (key, flag, quantity) in new[]
            {
                ("physical-1", "1", "1"), ("physical-2", "0", "500"), ("physical-3", "1", "-20"),
                ("physical-4", (string?)null, "999999"), ("physical-5", null, "0")
            })
            {
                var cells = new string?[HistoricalShipmentsRules.Columns.Count];
                cells[39] = flag; cells[6] = quantity; yield return new(key, cells); await Task.Yield();
            }
        }

        var operation = await store.AcquireAsync(id, "owner", filter, _ => IndependentRows(), default, includeOnTimeSummary: true);
        var firstPage = await store.PageAsync(id, "owner", 0, 2, default);
        Assert.Equal(2, firstPage.Rows.Count);
        Assert.Equal((2L, 3L), (operation.OnTimeNumerator, operation.OnTimeDenominator));
        Assert.Equal((operation.OnTimeNumerator, operation.OnTimeDenominator),
            (firstPage.Operation.OnTimeNumerator, firstPage.Operation.OnTimeDenominator));
        store.Release(id, "owner");

        async Task<(long? Numerator, long? Denominator)> Count(params string?[] flags)
        {
            var next = Guid.NewGuid(); store.Register(next, "owner");
            async IAsyncEnumerable<ShipmentRow> Values()
            {
                for (var i = 0; i < flags.Length; i++)
                {
                    var cells = new string?[HistoricalShipmentsRules.Columns.Count]; cells[39] = flags[i];
                    yield return new($"{next:N}-{i}", cells); await Task.Yield();
                }
            }
            var result = await store.AcquireAsync(next, "owner", filter, _ => Values(), default, includeOnTimeSummary: true);
            store.Release(next, "owner"); return (result.OnTimeNumerator, result.OnTimeDenominator);
        }

        Assert.Equal((0L, 3L), await Count("0", "0", "0"));
        Assert.Equal((3L, 3L), await Count("1", "1", "1"));
        Assert.Equal((0L, 0L), await Count(null, null));
        Assert.Equal((0L, 0L), await Count());
    }
    private static async IAsyncEnumerable<ShipmentRow> RowsFor(WorkspaceShipmentsFilter filter)
    {
        foreach (var row in await ToList(Rows()))
        {
            var part = row.Cells[5];
            if (filter.ParentParts.Contains(part!, StringComparer.OrdinalIgnoreCase) &&
                (filter.PartNumber is null || string.Equals(filter.PartNumber, part, StringComparison.Ordinal)) && InShipDateRange(row, filter)) yield return row;
        }
        foreach (var part in new[] { "PRODUCT-LINE-A", "PRODUCT-LINE-B", "EXPLICIT-A", "EXPLICIT-B", "EXPLICIT-CHANGED", "OUTSIDE-AT-SAME-SITE" })
        {
            var source = (await ToList(Rows()))[0]; var cells = source.Cells.ToArray(); cells[5] = part;
            var row = new ShipmentRow("detail-" + part, cells);
            if (filter.ParentParts.Contains(part, StringComparer.OrdinalIgnoreCase) &&
                (filter.PartNumber is null || string.Equals(filter.PartNumber, part, StringComparison.Ordinal)) && InShipDateRange(row, filter)) yield return row;
        }
    }
    private static bool InShipDateRange(ShipmentRow row, WorkspaceShipmentsFilter filter)
    {
        if (row.Cells[15] is not { } shipDate) return filter.ShipDateFrom is null && filter.ShipDateTo is null;
        var day = DateOnly.Parse(shipDate);
        return (filter.ShipDateFrom is null || day >= filter.ShipDateFrom) && (filter.ShipDateTo is null || day <= filter.ShipDateTo);
    }
    private static async Task<List<ShipmentRow>> ToList(IAsyncEnumerable<ShipmentRow> rows)
    { var result = new List<ShipmentRow>(); await foreach (var row in rows) result.Add(row); return result; }

    [Fact]
    public async Task Scope_is_server_resolved_union_intersected_with_exact_part_and_refreshed_from_current_configuration()
    {
        var resolved = new List<(Guid Id, string Site, string? ProductLine, string[] Explicit)>();
        var resolver = new DelegateWorkspaceShipmentsScopeResolver((workspace, _) =>
        {
            resolved.Add((workspace.AssignmentId, workspace.Site, workspace.ProductLineFrom, workspace.ParentParts.ToArray()));
            IReadOnlyList<string> parts = workspace.ProductLineFrom switch
            {
                "1000" => ["0000123", "PRODUCT-LINE-A", .. workspace.ParentParts],
                "2000" => ["PRODUCT-LINE-B", .. workspace.ParentParts],
                _ => []
            };
            return Task.FromResult(parts);
        });
        await using var factory = new KstApiFactory
        {
            WorkspaceShipmentsScopeResolver = resolver,
            WorkspaceShipmentsReader = new DelegateWorkspaceShipmentsReader((filter, _) => RowsFor(filter))
        };
        using var client = factory.CreateClient(); Session(client);
        var first = await Workspace(client, "SW", "EXPLICIT-A", "1000", "1099", "Line A");
        var second = await Workspace(client, "SW", "EXPLICIT-B", "2000", "2099", "Line B");
        var firstOperation = Guid.NewGuid(); var firstPath = Path(first, firstOperation);
        (await client.PutAsync(firstPath, null)).EnsureSuccessStatusCode();
        var response = await client.PostAsJsonAsync(firstPath + "/acquire", new WorkspaceShipmentsRequestDto(null, null, null));
        response.EnsureSuccessStatusCode();
        var operation = await response.Content.ReadFromJsonAsync<WorkspaceShipmentsOperationDto>();
        Assert.Equal(6, operation!.RowCount); // Four historic 0000123 details plus discovered and explicit parents.
        Assert.Equal(4, operation.OnTimeNumerator); Assert.Equal(5, operation.OnTimeDenominator);
        var initialResolution = resolved.Single();
        Assert.Equal(first, initialResolution.Id); Assert.Equal("SW", initialResolution.Site);
        Assert.Equal("1000", initialResolution.ProductLine); Assert.Equal(new[] { "EXPLICIT-A" }, initialResolution.Explicit);
        var firstRows = await client.GetFromJsonAsync<HistoricalShipmentsPageDto>(firstPath + "/rows?offset=0&limit=20");
        Assert.Equal(new[] { "0000123", "0000123", "0000123", "0000123", "PRODUCT-LINE-A", "EXPLICIT-A" }, firstRows!.Rows.Select(row => row.Cells[5]));

        // Another assignment at the same site cannot observe the first assignment's resolved parents.
        var secondPath = Path(second, Guid.NewGuid()); (await client.PutAsync(secondPath, null)).EnsureSuccessStatusCode();
        var secondResponse = await client.PostAsJsonAsync(secondPath + "/acquire", new WorkspaceShipmentsRequestDto("0000123", null, null));
        secondResponse.EnsureSuccessStatusCode();
        var secondOperation = await secondResponse.Content.ReadFromJsonAsync<WorkspaceShipmentsOperationDto>();
        Assert.Equal(0, secondOperation!.RowCount); Assert.Equal((0L, 0L), (secondOperation.OnTimeNumerator, secondOperation.OnTimeDenominator));
        Assert.Contains(resolved, x => x.Id == second && x.Site == "SW" && x.ProductLine == "2000" && x.Explicit.SequenceEqual(["EXPLICIT-B"]));

        // Exact part is an additional intersection; leading zeros remain intact.
        var exactPath = Path(first, Guid.NewGuid()); await client.PutAsync(exactPath, null);
        var exact = await client.PostAsJsonAsync(exactPath + "/acquire", new WorkspaceShipmentsRequestDto("OUTSIDE-AT-SAME-SITE", null, null));
        exact.EnsureSuccessStatusCode(); Assert.Equal(0, (await exact.Content.ReadFromJsonAsync<WorkspaceShipmentsOperationDto>())!.RowCount);
        var productLine = Path(first, Guid.NewGuid()); await client.PutAsync(productLine, null);
        var productLineResponse = await client.PostAsJsonAsync(productLine + "/acquire", new WorkspaceShipmentsRequestDto("PRODUCT-LINE-A", null, null));
        productLineResponse.EnsureSuccessStatusCode(); Assert.Equal(1, (await productLineResponse.Content.ReadFromJsonAsync<WorkspaceShipmentsOperationDto>())!.RowCount);
        var leading = Path(first, Guid.NewGuid()); await client.PutAsync(leading, null);
        var leadingResponse = await client.PostAsJsonAsync(leading + "/acquire", new WorkspaceShipmentsRequestDto("0000123", null, null));
        leadingResponse.EnsureSuccessStatusCode(); Assert.Equal(4, (await leadingResponse.Content.ReadFromJsonAsync<WorkspaceShipmentsOperationDto>())!.RowCount);
        var bounded = Path(first, Guid.NewGuid()); await client.PutAsync(bounded, null);
        var boundedResponse = await client.PostAsJsonAsync(bounded + "/acquire", new WorkspaceShipmentsRequestDto(null, new(2026, 10, 13), new(2026, 10, 13)));
        boundedResponse.EnsureSuccessStatusCode(); Assert.Equal(4, (await boundedResponse.Content.ReadFromJsonAsync<WorkspaceShipmentsOperationDto>())!.RowCount);

        // A newly acquired operation re-resolves changed persisted assignment configuration. The prior
        // operation remains an immutable acquired result for subsequent pages.
        var update = await client.PutAsJsonAsync($"/api/v1/workspaces/{first}", new CreateWorkspaceRequestDto(
            "Line A revised", "SW", "2000", "2099", ["EXPLICIT-CHANGED"], false, null));
        update.EnsureSuccessStatusCode();
        var refresh = Path(first, Guid.NewGuid()); await client.PutAsync(refresh, null);
        var refreshed = await client.PostAsJsonAsync(refresh + "/acquire", new WorkspaceShipmentsRequestDto(null, new(2026, 10, 13), new(2026, 10, 13)));
        refreshed.EnsureSuccessStatusCode();
        Assert.Equal(2, (await refreshed.Content.ReadFromJsonAsync<WorkspaceShipmentsOperationDto>())!.RowCount);
        Assert.Contains(resolved, x => x.Id == first && x.ProductLine == "2000" && x.Explicit.SequenceEqual(["EXPLICIT-CHANGED"]));
        var oldPage = await client.GetFromJsonAsync<HistoricalShipmentsPageDto>(firstPath + "/rows?offset=0&limit=20");
        Assert.Equal(6, oldPage!.Rows.Count); Assert.Equal("0000123", oldPage.Rows[0].Cells[5]);
    }

    [Fact]
    public async Task Empty_resolved_product_line_population_returns_empty_without_site_widening()
    {
        var factory = new KstApiFactory
        {
            WorkspaceShipmentsScopeResolver = new DelegateWorkspaceShipmentsScopeResolver((_, _) => Task.FromResult<IReadOnlyList<string>>([])),
            WorkspaceShipmentsReader = new DelegateWorkspaceShipmentsReader((_, _) => throw new InvalidOperationException("Empty scope must not invoke QAD reader."))
        };
        await using (factory)
        using (var client = factory.CreateClient())
        {
            Session(client); var assignment = await Workspace(client, "SW", "", "9000", "9999");
            var path = Path(assignment, Guid.NewGuid()); (await client.PutAsync(path, null)).EnsureSuccessStatusCode();
            var acquired = await client.PostAsJsonAsync(path + "/acquire", new WorkspaceShipmentsRequestDto(null, new(2026, 10, 1), new(2026, 10, 13)));
            acquired.EnsureSuccessStatusCode();
            Assert.Equal(0, (await acquired.Content.ReadFromJsonAsync<WorkspaceShipmentsOperationDto>())!.RowCount);
        }
    }
    [Fact]
    public async Task Persisted_workspace_scope_supplies_parent_membership_and_fixed_fifteen_defaults_without_MPS_snapshot()
    {
        WorkspaceShipmentsFilter? seen = null;
        await using var factory = new KstApiFactory { WorkspaceShipmentsReader = new DelegateWorkspaceShipmentsReader((f, _) => { seen = f; return Rows(); }) };
        using var c = factory.CreateClient(); Session(c); var assignment = await Workspace(c);
        var spec = await c.GetFromJsonAsync<WorkspaceShipmentsSpecDto>($"/api/v1/workspaces/{assignment}/shipments/spec");
        Assert.Equal(new[] { "Ship Date", "Perform Date", "Sales Order", "Customer PO", "Line", "Part", "Qty Shipped", "Unit Price", "Ext Price", "Ship To", "Ship Via", "BOL", "Invoice Number", "SO Type", "On Time" }, spec!.Columns.Select(x => x.Label));
        var path = Path(assignment, Guid.NewGuid()); (await c.PutAsync(path, null)).EnsureSuccessStatusCode();
        var response = await c.PostAsJsonAsync(path + "/acquire", new WorkspaceShipmentsRequestDto("0000123", null, null)); response.EnsureSuccessStatusCode();
        Assert.Equal("KV", seen!.Site); Assert.Equal("0000123", seen.PartNumber);
        Assert.Equal(new[] { "0000123" }, seen.ParentParts);
        var op = await response.Content.ReadFromJsonAsync<WorkspaceShipmentsOperationDto>();
        Assert.Equal(4, op!.RowCount); Assert.Equal(spec.Columns.Select(x => x.Id), op.Columns); Assert.DoesNotContain("currency", op.Columns);
        var page = await c.GetFromJsonAsync<HistoricalShipmentsPageDto>(path + "/rows?offset=0&limit=2");
        Assert.Equal(new[] { "b", "c" }, page!.Rows.Select(x => x.PhysicalDetailId));
        Assert.All(page.Rows, r => { Assert.Equal("0000123", r.Cells[5]); Assert.Equal("-5.3125", r.Cells[8]); });
        var tail = await c.GetFromJsonAsync<HistoricalShipmentsPageDto>(path + "/rows?offset=2&limit=2");
        Assert.Equal(new[] { "a", "d" }, tail!.Rows.Select(x => x.PhysicalDetailId)); Assert.Null(tail.Rows[1].Cells[0]);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync(path + "/rows?offset=0&limit=1001")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await c.GetAsync(path)).StatusCode);
    }
    [Fact]
    public async Task Assignment_visit_and_report_ownership_are_isolated_and_refresh_gets_new_result()
    {
        var reads = 0;
        await using var factory = new KstApiFactory
        {
            WorkspaceShipmentsReader = new DelegateWorkspaceShipmentsReader((_, _) => { reads++; return Rows(); }),
            HistoricalShipmentsReader = new DelegateHistoricalShipmentsReader((_, _) => Rows())
        };
        using var c = factory.CreateClient(); Session(c); c.DefaultRequestHeaders.Add("X-KST-Report-Session", Guid.NewGuid().ToString("D"));
        var assignment = await Workspace(c); var other = await Workspace(c, "SW"); var id = Guid.NewGuid(); var path = Path(assignment, id);
        (await c.PutAsync(path, null)).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync(path + "/acquire", new WorkspaceShipmentsRequestDto(null, null, null))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Gone, (await c.GetAsync(Path(other, id))).StatusCode);
        using var differentVisit = factory.CreateClient(); Session(differentVisit);
        Assert.Equal(HttpStatusCode.Gone, (await differentVisit.GetAsync(path)).StatusCode);
        await differentVisit.DeleteAsync(path); Assert.Equal(HttpStatusCode.OK, (await c.GetAsync(path)).StatusCode);
        var reportPath = $"/api/v1/reports/historical-shipments/operations/{id}";
        (await c.PutAsync(reportPath, null)).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync(reportPath + "/acquire", new { site = "SW", columns = new[] { "shipDate" } })).EnsureSuccessStatusCode();
        await c.DeleteAsync(path); Assert.Equal(HttpStatusCode.OK, (await c.GetAsync(reportPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsync(path, null)).StatusCode);
        var refresh = Path(assignment, Guid.NewGuid()); await c.PutAsync(refresh, null);
        (await c.PostAsJsonAsync(refresh + "/acquire", new WorkspaceShipmentsRequestDto(null, null, null))).EnsureSuccessStatusCode();
        Assert.Equal(2, reads);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync(refresh + "/prepare", null)).StatusCode);
    }
    [Fact]
    public async Task Departure_cancels_uncooperative_retrieval_and_blocks_late_publication()
    {
        var started = new TaskCompletionSource(); var resume = new TaskCompletionSource(); CancellationToken retrieval = default;
        async IAsyncEnumerable<ShipmentRow> Delayed()
        {
            started.SetResult(); await resume.Task; await foreach (var row in Rows()) yield return row;
        }
        await using var factory = new KstApiFactory { WorkspaceShipmentsReader = new DelegateWorkspaceShipmentsReader((_, ct) => { retrieval = ct; return Delayed(); }) };
        using var c = factory.CreateClient(); Session(c); var assignment = await Workspace(c); var path = Path(assignment, Guid.NewGuid()); await c.PutAsync(path, null);
        var pending = c.PostAsJsonAsync(path + "/acquire", new WorkspaceShipmentsRequestDto(null, null, null));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10)); await c.DeleteAsync(path);
        Assert.True(retrieval.IsCancellationRequested); resume.SetResult();
        try { var response = await pending; Assert.False(response.IsSuccessStatusCode); } catch (OperationCanceledException) { }
        Assert.Equal(HttpStatusCode.Gone, (await c.GetAsync(path)).StatusCode);
    }
    [Fact]
    public async Task Missing_archived_and_invalid_requests_do_not_read_and_cancel_before_register_is_final()
    {
        var reads = 0; var scopeCalls = 0;
        await using var factory = new KstApiFactory
        {
            WorkspaceShipmentsReader = new DelegateWorkspaceShipmentsReader((_, _) => { reads++; return Rows(); }),
            WorkspaceShipmentsScopeResolver = new DelegateWorkspaceShipmentsScopeResolver((workspace, _) =>
            { scopeCalls++; return Task.FromResult<IReadOnlyList<string>>(workspace.ParentParts); })
        };
        using var c = factory.CreateClient(); Session(c);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsync(Path(Guid.NewGuid(), Guid.NewGuid()), null)).StatusCode);
        var assignment = await Workspace(c); var cancelled = Path(assignment, Guid.NewGuid()); await c.DeleteAsync(cancelled);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsync(cancelled, null)).StatusCode);
        var invalid = Path(assignment, Guid.NewGuid()); await c.PutAsync(invalid, null);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync(invalid + "/acquire", new WorkspaceShipmentsRequestDto(null, new(2026, 2, 1), new(2026, 1, 31)))).StatusCode);
        Assert.Equal("registered", (await c.GetFromJsonAsync<WorkspaceShipmentsOperationDto>(invalid))!.State);
        Assert.Equal(0, scopeCalls);
        await c.PostAsync($"/api/v1/workspaces/{assignment}/archive", null);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsync(Path(assignment, Guid.NewGuid()), null)).StatusCode);
        Assert.Equal(0, reads); Assert.Equal(0, scopeCalls);
    }

    [Fact]
    public async Task Unregistered_or_wrong_state_acquisition_never_resolves_workspace_scope()
    {
        var scopeCalls = 0;
        await using var factory = new KstApiFactory
        {
            WorkspaceShipmentsScopeResolver = new DelegateWorkspaceShipmentsScopeResolver((workspace, _) =>
            {
                Interlocked.Increment(ref scopeCalls);
                return Task.FromResult<IReadOnlyList<string>>(workspace.ParentParts);
            }),
            WorkspaceShipmentsReader = new DelegateWorkspaceShipmentsReader((_, _) => Rows())
        };
        using var client = factory.CreateClient(); Session(client); var assignment = await Workspace(client);
        for (var i = 0; i < 20; i++)
        {
            var unknown = Path(assignment, Guid.NewGuid());
            Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync(unknown + "/acquire",
                new WorkspaceShipmentsRequestDto(null, null, null))).StatusCode);
        }
        Assert.Equal(0, scopeCalls);
        var path = Path(assignment, Guid.NewGuid()); (await client.PutAsync(path, null)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync(path + "/acquire", new WorkspaceShipmentsRequestDto(null, null, null))).EnsureSuccessStatusCode();
        Assert.Equal(1, scopeCalls);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path + "/acquire",
            new WorkspaceShipmentsRequestDto(null, null, null))).StatusCode);
        Assert.Equal(1, scopeCalls);
    }

    [Fact]
    public async Task Losing_duplicate_acquisition_does_not_release_or_cancel_the_winning_operation()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        await using var factory = new KstApiFactory
        {
            WorkspaceShipmentsScopeResolver = new DelegateWorkspaceShipmentsScopeResolver(async (workspace, ct) =>
            {
                if (Interlocked.Increment(ref calls) == 1) { started.SetResult(); await resume.Task.WaitAsync(ct); }
                return workspace.ParentParts;
            }),
            WorkspaceShipmentsReader = new DelegateWorkspaceShipmentsReader((_, _) => Rows())
        };
        using var client = factory.CreateClient(); Session(client); var assignment = await Workspace(client);
        var path = Path(assignment, Guid.NewGuid()); (await client.PutAsync(path, null)).EnsureSuccessStatusCode();
        var first = client.PostAsJsonAsync(path + "/acquire", new WorkspaceShipmentsRequestDto(null, null, null));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var duplicate = await client.PostAsJsonAsync(path + "/acquire", new WorkspaceShipmentsRequestDto(null, null, null));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        resume.SetResult();
        Assert.Equal(HttpStatusCode.OK, (await first).StatusCode);
        var ready = await client.GetFromJsonAsync<WorkspaceShipmentsOperationDto>(path);
        Assert.Equal("ready", ready!.State); Assert.Equal(4, ready.RowCount); Assert.Equal(1, calls);
    }
}
