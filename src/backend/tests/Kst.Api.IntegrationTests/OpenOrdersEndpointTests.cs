using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.Mps;
using Kst.Application.OpenOrders;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.OpenOrders;
using Microsoft.Extensions.DependencyInjection;

namespace Kst.Api.IntegrationTests;

public sealed class OpenOrdersEndpointTests
{
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
        using var hitJson = JsonDocument.Parse(await client.GetStringAsync(url));
        Assert.True(hitJson.RootElement.GetProperty("isStale").GetBoolean());
        var retried = await client.PostAsync(root + $"/refresh?mpsSnapshotId={snapshot.Id}", null);
        using var retriedJson = JsonDocument.Parse(await retried.Content.ReadAsStringAsync());
        Assert.False(retriedJson.RootElement.GetProperty("isStale").GetBoolean());
        Assert.NotEqual(goodId, retriedJson.RootElement.GetProperty("openOrdersSnapshotId").GetString());
        Assert.Equal(4, calls);
    }

    private static async Task<Guid> CreateAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/workspaces",
            new { site = "SW", parentParts = new[] { "P-1" }, isTemporary = false });
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
