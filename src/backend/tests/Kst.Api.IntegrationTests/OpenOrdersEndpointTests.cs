using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.Mps;
using Kst.Application.OpenOrders;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.OpenOrders;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;

namespace Kst.Api.IntegrationTests;

public sealed class OpenOrdersEndpointTests
{
    [Fact]
    public async Task DraftRestorationUsesFreshReadAndRetainsConflictsThroughArchiveUntilPermanentDeletion()
    {
        var current = Line(); var reads = 0;
        await using var factory = new KstApiFactory { OpenOrdersSourceReader = new DelegateOpenOrdersSourceReader((_, _, _) =>
        { reads++; return Task.FromResult<IReadOnlyList<OpenOrderLine>>([current]); }) };
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var mps = Seed(factory.Services.GetRequiredService<IMpsSnapshotStore>(), id, "P-1");
        var root = $"/api/v1/workspaces/{id}/open-orders";
        var report = await client.GetFromJsonAsync<JsonElement>($"{root}?mpsSnapshotId={mps.Id}");
        var proposal = new { key = new { domain = "TEST", salesOrder = "SO-1", line = 1 }, site = "SW", itemNumber = "P-1",
            original = new { dueDate = (string?)null, performDate = (string?)null, requiredDate = (string?)null, dockDate = (string?)null, orderQty = "5", price = "0.0125" },
            proposed = new { dueDate = (string?)null, performDate = (string?)null, requiredDate = (string?)null, dockDate = (string?)null, orderQty = "2", price = "0.0125" }, reasonCode = "Quality" };
        using var saved = await client.PutAsJsonAsync(root + "/draft", new { mpsSnapshotId = mps.Id.ToString(),
            openOrdersSnapshotId = report.GetProperty("openOrdersSnapshotId").GetString(), proposals = new[] { proposal } });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal(1, reads);
        var restored = await client.GetFromJsonAsync<JsonElement>(root + $"/draft?mpsSnapshotId={mps.Id}");
        Assert.True(restored.GetProperty("restored").GetBoolean()); Assert.Equal(2, reads);
        Assert.Equal("2", restored.GetProperty("rows")[0].GetProperty("proposal").GetProperty("proposed").GetProperty("orderQty").GetString());
        current = current with { SourceValues = current.SourceValues with { Price = 0.02m } };
        var conflicted = await client.GetFromJsonAsync<JsonElement>(root + $"/draft?mpsSnapshotId={mps.Id}");
        Assert.Equal(3, reads);
        Assert.NotEmpty(conflicted.GetProperty("rows")[0].GetProperty("issues").EnumerateArray());
        Assert.Equal("0.0125", conflicted.GetProperty("rows")[0].GetProperty("proposal").GetProperty("original").GetProperty("price").GetString());
        (await client.PostAsync($"/api/v1/workspaces/{id}/archive", null)).EnsureSuccessStatusCode();
        Assert.True((await client.GetFromJsonAsync<JsonElement>(root + $"/draft?mpsSnapshotId={mps.Id}")).GetProperty("exists").GetBoolean());
        (await client.DeleteAsync($"/api/v1/workspaces/{id}")).EnsureSuccessStatusCode();
        Assert.False((await client.GetFromJsonAsync<JsonElement>(root + $"/draft?mpsSnapshotId={mps.Id}")).GetProperty("exists").GetBoolean());
    }

    [Fact]
    public async Task DraftPresenceDoesNotRefreshOrDeserializeReportBeforeRestoration()
    {
        var reads = 0;
        await using var factory = new KstApiFactory { OpenOrdersSourceReader = new DelegateOpenOrdersSourceReader((_, _, _) =>
        { reads++; return Task.FromResult<IReadOnlyList<OpenOrderLine>>([Line()]); }) };
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var root = $"/api/v1/workspaces/{id}/open-orders";
        var presence = await client.GetFromJsonAsync<JsonElement>(root + "/draft/presence");
        Assert.False(presence.GetProperty("exists").GetBoolean());
        Assert.Equal(0, reads);
        var snapshot = Seed(factory.Services.GetRequiredService<IMpsSnapshotStore>(), id, "P-1");
        var report = await client.GetFromJsonAsync<JsonElement>(root + $"?mpsSnapshotId={snapshot.Id}");
        var original = new { dueDate = (string?)null, performDate = (string?)null, requiredDate = (string?)null, dockDate = (string?)null, orderQty = "5", price = "0.0125" };
        var proposal = new { key = new { domain = "TEST", salesOrder = "SO-1", line = 1 }, site = "SW", itemNumber = "P-1", original,
            proposed = new { original.dueDate, original.performDate, original.requiredDate, original.dockDate, orderQty = "4", original.price }, reasonCode = "Factory" };
        (await client.PutAsJsonAsync(root + "/draft", new { mpsSnapshotId = snapshot.Id.ToString(),
            openOrdersSnapshotId = report.GetProperty("openOrdersSnapshotId").GetString(), proposals = new[] { proposal } })).EnsureSuccessStatusCode();
        Assert.True((await client.GetFromJsonAsync<JsonElement>(root + "/draft/presence")).GetProperty("exists").GetBoolean());
        Assert.Equal(1, reads);
        (await client.DeleteAsync(root + "/draft")).EnsureSuccessStatusCode();
        Assert.False((await client.GetFromJsonAsync<JsonElement>(root + "/draft/presence")).GetProperty("exists").GetBoolean());
    }

    [Fact]
    public async Task ReportExportUsesCachedScopedRowsInClientOrderAndRejectsChangedSnapshot()
    {
        var sourceReads = 0;
        await using var factory = new KstApiFactory
        {
            OpenOrdersSourceReader = new DelegateOpenOrdersSourceReader((_, _, _) =>
            {
                sourceReads++;
                return Task.FromResult<IReadOnlyList<OpenOrderLine>>([Line(), Line() with
                {
                    Key = new OpenOrderLineKey("TEST", "SO-2", 2),
                    ItemNumber = "P-2",
                    SiteQoh = -2m,
                    Consignment = false
                }]);
            })
        };
        using var client = factory.CreateClient();
        var id = await CreateAsync(client, "Shure SMT");
        var mps = factory.Services.GetRequiredService<IMpsSnapshotStore>();
        var snapshot = Seed(mps, id, "P-1", "P-2");
        var report = await client.GetFromJsonAsync<JsonElement>($"/api/v1/workspaces/{id}/open-orders?mpsSnapshotId={snapshot.Id}");
        var keys = report.GetProperty("lines").EnumerateArray().Select(l => l.GetProperty("key")).Reverse().ToArray();
        var request = new { mpsSnapshotId = snapshot.Id.ToString(), openOrdersSnapshotId = report.GetProperty("openOrdersSnapshotId").GetString(),
            lineKeys = keys, columns = new[] { "order", "stat", "customer", "plnr", "itemNumber", "siteQoh", "unitPrice", "extPrice", "dueDate" } };
        using var response = await client.PostAsJsonAsync($"/api/v1/workspaces/{id}/open-orders/report-export", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"Shure-SMT-Open-Orders-{report.GetProperty("acquiredAtUtc").GetString()![..10]}.xlsx", response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        Assert.Equal(1, sourceReads); // exporting never asks QAD again
        var workbookBytes = await response.Content.ReadAsByteArrayAsync();
        var evidencePath = Environment.GetEnvironmentVariable("KST_STAGE13_SYNTHETIC_WORKBOOK_PATH");
        if (!string.IsNullOrEmpty(evidencePath)) File.WriteAllBytes(evidencePath, workbookBytes);
        using var stream = new MemoryStream(workbookBytes);
        using var book = new XLWorkbook(stream);
        var sheet = book.Worksheet("Open Orders");
        Assert.Equal(new[] { "SO", "Status", "Customer #", "Planner", "Item Number", "Site QOH", "Unit Price", "Ext Price", "Due Date" },
            Enumerable.Range(1, 9).Select(i => sheet.Cell(1, i).GetString()));
        Assert.Equal("SO-2", sheet.Cell(2, 1).GetString());
        Assert.Equal("P-2", sheet.Cell(2, 5).GetString());
        Assert.Equal(-2d, sheet.Cell(2, 6).GetDouble());
        Assert.Equal(0.0125d, sheet.Cell(2, 7).GetDouble());
        Assert.Equal("P-1", sheet.Cell(3, 5).GetString());
        Assert.True(sheet.Cell(3, 6).IsEmpty());
        Assert.Equal(0d, sheet.Cell(3, 7).GetDouble()); // consignment display, raw ext price
        Assert.Equal(0.0375d, sheet.Cell(3, 8).GetDouble());
        Assert.Contains("not operational", book.Worksheet("Report Metadata").Cell(2, 2).GetString());
        var onlyFirst = new { request.mpsSnapshotId, request.openOrdersSnapshotId, lineKeys = keys.Take(1).ToArray(), request.columns };
        using var filteredResponse = await client.PostAsJsonAsync($"/api/v1/workspaces/{id}/open-orders/report-export", onlyFirst);
        Assert.Equal(HttpStatusCode.OK, filteredResponse.StatusCode);
        using (var filteredStream = new MemoryStream(await filteredResponse.Content.ReadAsByteArrayAsync()))
        using (var filteredBook = new XLWorkbook(filteredStream))
        {
            Assert.Equal("SO-2", filteredBook.Worksheet("Open Orders").Cell(2, 1).GetString());
            Assert.True(filteredBook.Worksheet("Open Orders").Cell(3, 1).IsEmpty());
        }
        Assert.Equal(1, sourceReads);
        var invalidColumns = new { request.mpsSnapshotId, request.openOrdersSnapshotId, request.lineKeys, columns = new[] { "unknown" } };
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync($"/api/v1/workspaces/{id}/open-orders/report-export", invalidColumns)).StatusCode);
        Assert.Equal(1, sourceReads);
        Seed(mps, id, "P-1");
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync($"/api/v1/workspaces/{id}/open-orders/report-export", request)).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SnapshotIdentifierIsRequiredUuidInOpenApi(bool refresh)
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var path = "/api/v1/workspaces/{assignmentId}/open-orders" + (refresh ? "/refresh" : "");
        var method = refresh ? "post" : "get";
        var parameters = document.RootElement.GetProperty("paths").GetProperty(path)
            .GetProperty(method).GetProperty("parameters");
        var parameter = parameters.EnumerateArray().Single(p =>
            p.GetProperty("name").GetString() == "mpsSnapshotId");
        Assert.Equal("query", parameter.GetProperty("in").GetString());
        Assert.True(parameter.GetProperty("required").GetBoolean());
        Assert.Equal("string", parameter.GetProperty("schema").GetProperty("type").GetString());
        Assert.Equal("uuid", parameter.GetProperty("schema").GetProperty("format").GetString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SnapshotIdentifierValidationOnBothRoutes(bool refresh)
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var root = $"/api/v1/workspaces/{id}/open-orders" + (refresh ? "/refresh" : "");

        foreach (var query in new[] { "", "?mpsSnapshotId=invalid", $"?mpsSnapshotId={Guid.Empty}" })
        {
            using var response = refresh ? await client.PostAsync(root + query, null)
                                         : await client.GetAsync(root + query);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        var valid = $"?mpsSnapshotId={Guid.NewGuid()}";
        using var accepted = refresh ? await client.PostAsync(root + valid, null)
                                     : await client.GetAsync(root + valid);
        Assert.Equal(HttpStatusCode.Conflict, accepted.StatusCode); // MPS is not loaded; GUID binding succeeded.
    }

    [Fact]
    public async Task EndpointsValidateInputWorkspaceAndUnloadedMps()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var root = $"/api/v1/workspaces/{id}/open-orders";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(root)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(root + "/refresh?mpsSnapshotId=bad", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/workspaces/{Guid.NewGuid()}/open-orders?mpsSnapshotId={Guid.NewGuid()}")).StatusCode);
        var response = await client.GetAsync(root + $"?mpsSnapshotId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task LoadedEmptyIsSuccessAndSnapshotChangeIsConflict()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var mps = factory.Services.GetRequiredService<IMpsSnapshotStore>();
        var snapshot = Seed(mps, id);
        var root = $"/api/v1/workspaces/{id}/open-orders";
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync(root + $"?mpsSnapshotId={Guid.NewGuid()}")).StatusCode);
        var response = await client.GetAsync(root + $"?mpsSnapshotId={snapshot.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Empty(json.RootElement.GetProperty("lines").EnumerateArray());
        Assert.Equal(snapshot.Id.ToString(), json.RootElement.GetProperty("mpsSnapshotId").GetString());
        Assert.False(json.RootElement.GetProperty("isStale").GetBoolean());
        Seed(mps, id);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync(root + $"/refresh?mpsSnapshotId={snapshot.Id}", null)).StatusCode);
    }

    [Fact]
    public async Task UnavailableWithoutCacheThenStaleAndRetryExposeFullHiddenSourceFacts()
    {
        var calls = 0;
        await using var factory = new KstApiFactory
        {
            OpenOrdersSourceReader = new DelegateOpenOrdersSourceReader((_, _, _) =>
            {
                calls++;
                if (calls is 1 or 3) throw new InvalidOperationException("Sensitive unavailable source detail");
                return Task.FromResult<IReadOnlyList<OpenOrderLine>>([Line()]);
            })
        };
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var snapshot = Seed(factory.Services.GetRequiredService<IMpsSnapshotStore>(), id, "P-1");
        var root = $"/api/v1/workspaces/{id}/open-orders";
        var url = root + $"?mpsSnapshotId={snapshot.Id}";
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync(url)).StatusCode);
        var good = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        using var goodJson = JsonDocument.Parse(await good.Content.ReadAsStringAsync());
        var goodId = goodJson.RootElement.GetProperty("openOrdersSnapshotId").GetString();
        var line = goodJson.RootElement.GetProperty("lines")[0];
        Assert.Equal("SP", line.GetProperty("salesperson").GetString());
        Assert.Equal(5m, line.GetProperty("sourceValues").GetProperty("orderQty").GetDecimal());
        Assert.Equal(2m, line.GetProperty("shippedQty").GetDecimal());
        Assert.Equal(0.0125m, line.GetProperty("sourceValues").GetProperty("price").GetDecimal());
        Assert.Equal(0.0375m, line.GetProperty("extPrice").GetDecimal());
        Assert.Equal(0m, line.GetProperty("unitPrice").GetDecimal());
        var failed = await client.PostAsync(root + $"/refresh?mpsSnapshotId={snapshot.Id}", null);
        using var staleJson = JsonDocument.Parse(await failed.Content.ReadAsStringAsync());
        Assert.True(staleJson.RootElement.GetProperty("isStale").GetBoolean());
        Assert.Equal(goodId, staleJson.RootElement.GetProperty("openOrdersSnapshotId").GetString());
        Assert.DoesNotContain("Sensitive", staleJson.RootElement.GetProperty("warning").GetString());
        var staleExport = await client.PostAsJsonAsync($"{root}/report-export", new {
            mpsSnapshotId = snapshot.Id.ToString(), openOrdersSnapshotId = goodId,
            lineKeys = new[] { new { domain = "TEST", salesOrder = "SO-1", line = 1 } }, columns = new[] { "order" }
        });
        Assert.Equal(HttpStatusCode.OK, staleExport.StatusCode);
        using (var stream = new MemoryStream(await staleExport.Content.ReadAsByteArrayAsync()))
        using (var book = new XLWorkbook(stream))
            Assert.Contains("STALE", book.Worksheet("Report Metadata").Cell(2, 2).GetString());
        using var hitJson = JsonDocument.Parse(await client.GetStringAsync(url));
        Assert.True(hitJson.RootElement.GetProperty("isStale").GetBoolean());
        var retried = await client.PostAsync(root + $"/refresh?mpsSnapshotId={snapshot.Id}", null);
        using var retriedJson = JsonDocument.Parse(await retried.Content.ReadAsStringAsync());
        Assert.False(retriedJson.RootElement.GetProperty("isStale").GetBoolean());
        Assert.NotEqual(goodId, retriedJson.RootElement.GetProperty("openOrdersSnapshotId").GetString());
        Assert.Equal(4, calls);
    }

    [Theory]
    [InlineData("  Shure   / SMT.  ", "Shure-SMT")]
    [InlineData("<\\/:\"|?*\t . ", "SW")]
    [InlineData("Acme___ / : Co...", "Acme-Co")]
    [InlineData("A\\B:C*D?E\"F<G>H|I", "A-B-C-D-E-F-G-H-I")]
    [InlineData("Acme\u0085West", "Acme-West")]
    [InlineData("CON", "CON")]
    public async Task ExportSuggestsNormalizedWorkspaceFilename(string displayName, string expectedPrefix)
    {
        await using var factory = new KstApiFactory { OpenOrdersSourceReader = new DelegateOpenOrdersSourceReader((_, _, _) =>
            Task.FromResult<IReadOnlyList<OpenOrderLine>>([Line()])) };
        using var client = factory.CreateClient();
        var id = await CreateAsync(client, displayName);
        var snapshot = Seed(factory.Services.GetRequiredService<IMpsSnapshotStore>(), id, "P-1");
        var report = await client.GetFromJsonAsync<JsonElement>($"/api/v1/workspaces/{id}/open-orders?mpsSnapshotId={snapshot.Id}");
        using var response = await client.PostAsJsonAsync($"/api/v1/workspaces/{id}/open-orders/report-export", new {
            mpsSnapshotId = snapshot.Id.ToString(), openOrdersSnapshotId = report.GetProperty("openOrdersSnapshotId").GetString(),
            lineKeys = new[] { new { domain = "TEST", salesOrder = "SO-1", line = 1 } }, columns = new[] { "order" }
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"{expectedPrefix}-Open-Orders-{report.GetProperty("acquiredAtUtc").GetString()![..10]}.xlsx", response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
    }

    private static async Task<Guid> CreateAsync(HttpClient client, string? displayName = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/workspaces",
            new { displayName, site = "SW", parentParts = new[] { "P-1" }, isTemporary = false });
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("assignmentId").GetGuid();
    }

    private static MpsSnapshot Seed(IMpsSnapshotStore store, Guid id, params string[] parents)
    {
        var snapshot = new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, "SW",
            parents.Select(p => new MpsResolvedPart(p, null)).ToArray(), []);
        store.SetLoaded(id, snapshot);
        return snapshot;
    }

    private static OpenOrderLine Line() => new(new("TEST", "SO-1", 1), "P-1", "SW", null, null,
        2m, new(null, null, null, null, 5m, 0.0125m), null, null, null, "SP", null,
        null, "", null, null, null, null, null, null, null, null, null, null,
        null, null, null, null, null, true);
}
