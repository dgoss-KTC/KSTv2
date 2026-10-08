using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.FinishedGoods;
using Kst.Application.Mps;
using Kst.Domain.Common;
using Kst.Domain.FinishedGoods;
using Kst.Domain.Mps;
using Microsoft.Extensions.DependencyInjection;

namespace Kst.Api.IntegrationTests;

public sealed class FinishedGoodsEndpointTests
{
    [Fact]
    public async Task ExactStringsRowsCacheAndStaleErrorsUseRealEndpointWithoutCompanySources()
    {
        var calls = 0;
        await using var factory = new KstApiFactory
        {
            FinishedGoodsSourceReader = new DelegateFinishedGoodsSourceReader((_, _, _) =>
            ++calls == 2 ? throw new Exception("private database details") : Task.FromResult<IReadOnlyList<FinishedGoodsSourceRow>>([
                new("KTC", "SW", "P", "ship", "ra1", "R1", 3.00001m, 1.23456m, false),
                new("KTC", "SW", "P", "ship", "ra1", "R2", 3.00001m, 1.23456m, false)]))
        };
        using var client = factory.CreateClient();
        var id = await Create(client);
        var snapshot = Seed(factory, id);
        var path = $"/api/v1/workspaces/{id}/finished-goods?mpsSnapshotId={snapshot}";
        var first = await client.GetFromJsonAsync<JsonElement>(path);
        Assert.Equal("USD", first.GetProperty("currency").GetString());
        var rows = first.GetProperty("rows"); Assert.Equal(2, rows.GetArrayLength());
        Assert.NotEqual(rows[0].GetProperty("rowId").GetString(), rows[1].GetProperty("rowId").GetString());
        Assert.Equal("3.00001", rows[0].GetProperty("onHand").GetString());
        Assert.Equal("3.7036923456", rows[0].GetProperty("value").GetString());
        Assert.Equal("ship", rows[0].GetProperty("sourceLocation").GetString());
        Assert.Equal("SHIP", rows[0].GetProperty("location").GetString());
        Assert.True(rows[0].GetProperty("isRma").GetBoolean());
        await client.GetAsync(path); Assert.Equal(1, calls);
        using var refresh = await client.PostAsync($"/api/v1/workspaces/{id}/finished-goods/refresh?mpsSnapshotId={snapshot}", null);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var stale = await refresh.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(stale.GetProperty("isStale").GetBoolean());
        Assert.Equal(first.GetProperty("acquiredAtUtc").GetString(), stale.GetProperty("acquiredAtUtc").GetString());
        Assert.Equal(first.GetProperty("finishedGoodsSnapshotId").GetString(), stale.GetProperty("finishedGoodsSnapshotId").GetString());
        Assert.DoesNotContain("private", stale.GetProperty("warning").GetString());
        Seed(factory, id);
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task RequiredScopeUnavailableAndEmptyHaveDistinctOutcomes()
    {
        await using var factory = new KstApiFactory(); // QAD forcibly unconfigured, throwing production delegate.
        using var client = factory.CreateClient(); var id = await Create(client);
        var root = $"/api/v1/workspaces/{id}/finished-goods";
        foreach (var query in new[] { "", "?mpsSnapshotId=bad", $"?mpsSnapshotId={Guid.Empty}" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(root + query)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync(root + $"?mpsSnapshotId={Guid.NewGuid()}")).StatusCode);
        var snapshot = Seed(factory, id);
        var failed = await client.GetAsync(root + $"?mpsSnapshotId={snapshot}");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.DoesNotContain("connection", await failed.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        snapshot = Seed(factory, id, empty: true);
        var empty = await client.GetFromJsonAsync<JsonElement>(root + $"?mpsSnapshotId={snapshot}");
        Assert.Equal(0, empty.GetProperty("rows").GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/workspaces/{Guid.NewGuid()}/finished-goods?mpsSnapshotId={snapshot}")).StatusCode);
    }

    private static async Task<Guid> Create(HttpClient client)
    {
        var result = await client.PostAsJsonAsync("/api/v1/workspaces", new { site = "SW", parentParts = new[] { "P" }, isTemporary = false });
        result.EnsureSuccessStatusCode();
        return (await result.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("assignmentId").GetGuid();
    }
    private static SnapshotId Seed(KstApiFactory factory, Guid id, bool empty = false)
    {
        var snapshot = new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, "SW", empty ? [] : [new MpsResolvedPart("P", null)], []);
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(id, snapshot); return snapshot.Id;
    }
}
