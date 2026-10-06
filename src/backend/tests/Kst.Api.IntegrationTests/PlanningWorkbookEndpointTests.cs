using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.Inventory;
using Kst.Application.Mps;
using Kst.Application.OpenOrders;
using Kst.Application.PlanningWorkbook;
using Kst.Domain.Common;
using Kst.Domain.Inventory;
using Kst.Domain.Mps;
using Kst.Domain.OpenOrders;
using Kst.Domain.PartDetail;
using Kst.Domain.PlanningWorkbook;
using Microsoft.Extensions.DependencyInjection;

namespace Kst.Api.IntegrationTests;

/// <summary>Stage 14.3 Planning Workbook endpoint tests. All QAD readers are offline fakes.</summary>
public sealed class PlanningWorkbookEndpointTests
{
    private const string SecretDetail = "Server=qad-secret;Database=hidden";

    private bool _failInventory;
    private int _inventoryReads;

    private KstApiFactory Factory() => new()
    {
        OpenOrdersSourceReader = new DelegateOpenOrdersSourceReader((_, _, _) =>
            Task.FromResult<IReadOnlyList<OpenOrderLine>>([Line("SO-1", 3.5m, 2.25m), Line("RA-1", 99m, 1m)])),
        PartInventoryReader = new DelegatePartInventoryReader((site, parts, _) =>
        {
            _inventoryReads++;
            if (_failInventory) throw new InvalidOperationException(SecretDetail);
            return Task.FromResult<IReadOnlyList<PartInventorySummary>>(
                parts.Select(p => new PartInventorySummary(site, p, p == "P-1" ? 10.125m : 0m, 0m, 0m)).ToList());
        }),
        PlanningPriceReader = new DelegatePlanningPriceReader((_, parts, _, _) =>
            Task.FromResult<IReadOnlyList<PlanningPartPrice>>(parts.Select(p => new PlanningPartPrice(p, p switch
            {
                "P-1" => [],                                          // unpriced
                _ => [new PartPriceBreak(10m, 1m), new PartPriceBreak(1m, 0m)] // valid zero first tier
            })).ToList())),
        PlanningForecastReader = new DelegatePlanningForecastReader((_, _, _) =>
            Task.FromResult<IReadOnlyList<PlanningForecastRow>>([new("P-1", Today, -4.5m)]))
    };

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    [Fact]
    public async Task Malformed_Requests_Return_400()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var mps = Seed(factory, id);
        var root = $"/api/v1/workspaces/{id}/planning-workbook";

        foreach (var query in new[]
                 {
                     "?horizonWeeks=4", $"?mpsSnapshotId={mps.Id}", $"?mpsSnapshotId={mps.Id}&horizonWeeks=0",
                     $"?mpsSnapshotId={mps.Id}&horizonWeeks=73", $"?mpsSnapshotId={Guid.Empty}&horizonWeeks=4",
                     "?mpsSnapshotId=not-a-guid&horizonWeeks=4", $"?mpsSnapshotId={mps.Id}&horizonWeeks=four"
                 })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(root + query)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(root + "/refresh" + query, null)).StatusCode);
        }
        Assert.Equal(0, _inventoryReads);
    }

    [Fact]
    public async Task Horizon_Beyond_The_Mps_Derived_Maximum_Returns_400_With_The_Maximum()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var mps = Seed(factory, id);
        var root = $"/api/v1/workspaces/{id}/planning-workbook";

        foreach (var request in new Func<Task<HttpResponseMessage>>[]
                 {
                     () => client.GetAsync($"{root}?mpsSnapshotId={mps.Id}&horizonWeeks=13"),
                     () => client.PostAsync($"{root}/refresh?mpsSnapshotId={mps.Id}&horizonWeeks=13", null)
                 })
        {
            using var response = await request();
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(12, problem.GetProperty("maximumHorizonWeeks").GetInt32());
            Assert.True(problem.GetProperty("errors").TryGetProperty("horizonWeeks", out _));
        }
        Assert.Equal(0, _inventoryReads);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{root}?mpsSnapshotId={mps.Id}&horizonWeeks=12")).StatusCode);
    }

    [Fact]
    public async Task Unknown_Workspace_404_And_Mps_Context_409()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var root = $"/api/v1/workspaces/{id}/planning-workbook";

        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/v1/workspaces/{Guid.NewGuid()}/planning-workbook?mpsSnapshotId={Guid.NewGuid()}&horizonWeeks=4")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync($"{root}?mpsSnapshotId={Guid.NewGuid()}&horizonWeeks=4")).StatusCode);
        Seed(factory, id);
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync($"{root}?mpsSnapshotId={Guid.NewGuid()}&horizonWeeks=4")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"{root}/refresh?mpsSnapshotId={Guid.NewGuid()}&horizonWeeks=4", null)).StatusCode);
        Assert.Equal(0, _inventoryReads);
    }

    [Fact]
    public async Task Source_Unavailable_Without_Last_Good_Returns_Sanitized_503()
    {
        _failInventory = true;
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var mps = Seed(factory, id);

        using var response = await client.GetAsync($"/api/v1/workspaces/{id}/planning-workbook?mpsSnapshotId={mps.Id}&horizonWeeks=4");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("qad-secret", body);
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain("P-1", body);
    }

    [Fact]
    public async Task Loaded_Workbook_Serializes_Exact_Decimals_Null_Versus_Zero_Dollars_And_Stale_Refresh()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var mps = Seed(factory, id);
        var root = $"/api/v1/workspaces/{id}/planning-workbook";

        using var response = await client.GetAsync($"{root}?mpsSnapshotId={mps.Id}&horizonWeeks=3");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = (await response.Content.ReadFromJsonAsync<JsonElement>());
        Assert.Equal(mps.Id.ToString(), json.GetProperty("mpsSnapshotId").GetString());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("openOrdersSnapshotId").GetString()));
        Assert.Equal(Today.ToString("yyyy-MM-dd"), json.GetProperty("priceEffectiveDate").GetString());
        Assert.Equal("firstTier", json.GetProperty("priceSelectionPolicy").GetString());
        Assert.Equal(3, json.GetProperty("horizonWeeks").GetInt32());
        Assert.Equal(3, json.GetProperty("weekLabels").GetArrayLength());
        Assert.Equal(12, json.GetProperty("minimumSelectableHorizonWeeks").GetInt32());
        Assert.Equal(12, json.GetProperty("maximumHorizonWeeks").GetInt32());
        Assert.False(json.GetProperty("isStale").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("warning").ValueKind);

        var parts = json.GetProperty("parts");
        Assert.Equal(["P-1", "P-2"], parts.EnumerateArray().Select(p => p.GetProperty("part").GetString()));
        var unpriced = parts[0];
        Assert.False(unpriced.GetProperty("isPriced").GetBoolean());
        Assert.Equal(JsonValueKind.Null, unpriced.GetProperty("selectedPrice").ValueKind);
        Assert.Equal("10.125", unpriced.GetProperty("netQuantityOnHand").GetString());
        var week0 = unpriced.GetProperty("weeks")[0];
        Assert.Equal("weekly", week0.GetProperty("kind").GetString());
        Assert.Equal("12.5", week0.GetProperty("mps").GetString());
        Assert.Equal(JsonValueKind.Null, week0.GetProperty("mpsDollars").ValueKind);
        Assert.Equal("3.5", week0.GetProperty("salesOrderQuantity").GetString());
        Assert.Equal("7.875", week0.GetProperty("salesOrderDollars").GetString()); // 3.5 x 2.25, RA line excluded
        Assert.Equal("4.5", week0.GetProperty("forecast").GetString());              // display-only absolute value
        Assert.Equal("19.125", week0.GetProperty("estimatedOnHand").GetString());    // 10.125 + 12.5 - 3.5
        Assert.Equal("falldown", unpriced.GetProperty("falldown").GetProperty("kind").GetString());
        Assert.Equal(JsonValueKind.Null, unpriced.GetProperty("falldown").GetProperty("weekLabel").ValueKind);

        var zero = parts[1];
        Assert.True(zero.GetProperty("isPriced").GetBoolean());
        Assert.Equal("0", zero.GetProperty("selectedPrice").GetString());
        Assert.Equal(2, zero.GetProperty("priceTiers").GetArrayLength());
        var zeroWeek = zero.GetProperty("weeks")[0];
        Assert.Equal("2", zeroWeek.GetProperty("mps").GetString());
        Assert.Equal("0", zeroWeek.GetProperty("mpsDollars").GetString());
        Assert.Equal("0", zero.GetProperty("undated").GetProperty("salesOrderQuantity").GetString());

        _failInventory = true;
        using var stale = await client.PostAsync($"{root}/refresh?mpsSnapshotId={mps.Id}&horizonWeeks=3", null);
        Assert.Equal(HttpStatusCode.OK, stale.StatusCode);
        var staleJson = await stale.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(staleJson.GetProperty("isStale").GetBoolean());
        Assert.Equal(PlanningWorkbookService.StaleWarning, staleJson.GetProperty("warning").GetString());
        Assert.Equal(json.GetProperty("workbookSnapshotId").GetString(), staleJson.GetProperty("workbookSnapshotId").GetString());
        Assert.DoesNotContain("qad-secret", staleJson.GetRawText());
    }

    [Fact]
    public void Generated_OpenApi_Marks_Required_Query_Parameters_On_Both_Routes()
    {
        var path = FindRepositoryFile("docs/openapi/Kst.Api.json");
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        var paths = document.RootElement.GetProperty("paths");
        foreach (var (route, method) in new[]
                 {
                     ("/api/v1/workspaces/{assignmentId}/planning-workbook", "get"),
                     ("/api/v1/workspaces/{assignmentId}/planning-workbook/refresh", "post")
                 })
        {
            var parameters = paths.GetProperty(route).GetProperty(method).GetProperty("parameters").EnumerateArray().ToList();
            foreach (var name in new[] { "mpsSnapshotId", "horizonWeeks" })
            {
                var parameter = parameters.Single(p => p.GetProperty("name").GetString() == name);
                Assert.Equal("query", parameter.GetProperty("in").GetString());
                Assert.True(parameter.TryGetProperty("required", out var required) && required.GetBoolean(),
                    $"{method.ToUpperInvariant()} {route} must mark {name} required.");
            }
        }
    }

    private static string FindRepositoryFile(string relative)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate)) return candidate;
        }
        throw new FileNotFoundException("Generated OpenAPI document not found.", relative);
    }

    private static async Task<Guid> CreateAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/workspaces",
            new { displayName = (string?)null, site = "SW", parentParts = new[] { "P-1", "P-2" }, isTemporary = false });
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("assignmentId").GetGuid();
    }

    private static MpsSnapshot Seed(KstApiFactory factory, Guid id)
    {
        var due = Today;
        var snapshot = new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, "SW",
            [new MpsResolvedPart("P-1", null), new MpsResolvedPart("P-2", null)],
            [
                new MpsSourceRow("TEST", "SW", "P-1", null, due, null, 12.5m, MpsSupplyType.Supply, "WO-1", MpsWorkOrderState.Planned),
                new MpsSourceRow("TEST", "SW", "P-2", null, due, null, 2m, MpsSupplyType.Supply, "WO-2", MpsWorkOrderState.Planned)
            ]);
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(id, snapshot);
        return snapshot;
    }

    private static OpenOrderLine Line(string salesOrder, decimal quantity, decimal price) => new(
        new("TEST", salesOrder, 1), "P-1", "SW", null, null, 0m,
        new(Today, null, null, null, quantity, price), null, null, null, "SP", null,
        null, "", null, null, null, null, null, null, null, null, null, null,
        null, null, null, null, null, true);
}
