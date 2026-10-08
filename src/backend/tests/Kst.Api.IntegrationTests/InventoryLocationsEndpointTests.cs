using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.InventoryLocations;
using Kst.Application.Mps;
using Kst.Domain.Common;
using Kst.Domain.Inventory;
using Microsoft.Extensions.DependencyInjection;

namespace Kst.Api.IntegrationTests;

public sealed class InventoryLocationsEndpointTests
{
    [Fact]
    public async Task ContractPreservesExactNullAndDuplicateRowsAndRefreshFailureIsIndependent()
    {
        var calls = 0;
        await using var factory = new KstApiFactory
        {
            InventoryLocationReader = new DelegateInventoryLocationReader((site, part, _) =>
        {
            Assert.Equal("SW", site); Assert.Equal("COMP", part);
            if (++calls > 1) throw new Exception("private source details");
            return Task.FromResult<IReadOnlyList<InventoryLocationRow>>([
                new("0", "MRB", "RA", "HOLD", "-999999999999999999.1234567890"),
                new("1", "MRB", "RA", "HOLD", "-999999999999999999.1234567890"),
                new("2", null, null, null, null), new("3", "", "", "", "0.0000000000")]);
        })
        };
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/workspaces", new { site = "SW", parentParts = new[] { "PARENT" }, isTemporary = false });
        created.EnsureSuccessStatusCode();
        using var workspace = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = workspace.RootElement.GetProperty("assignmentId").GetGuid();
        var snapshot = new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, "SW", [], []);
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(id, snapshot);
        var path = $"/api/v1/workspaces/{id}/components/COMP/inventory-locations?mpsSnapshotId={snapshot.Id}";
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var rows = body.RootElement.GetProperty("rows");
        Assert.Equal(4, rows.GetArrayLength());
        Assert.Equal(JsonValueKind.String, rows[0].GetProperty("quantityOnHand").ValueKind);
        Assert.Equal("-999999999999999999.1234567890", rows[0].GetProperty("quantityOnHand").GetString());
        Assert.Equal(JsonValueKind.Null, rows[2].GetProperty("quantityOnHand").ValueKind);
        using var stale = JsonDocument.Parse(await (await client.GetAsync(path)).Content.ReadAsStringAsync());
        Assert.True(stale.RootElement.GetProperty("isStale").GetBoolean());
        Assert.Equal(body.RootElement.GetProperty("acquiredAtUtc").GetString(), stale.RootElement.GetProperty("acquiredAtUtc").GetString());
        // Component information is separate (no fake master configured); inventory success never requires it.
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync($"/api/v1/workspaces/{id}/components/COMP")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync(path.Replace(snapshot.Id.ToString(), Guid.NewGuid().ToString()))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(path.Replace("COMP", "%20"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path.Replace(id.ToString(), Guid.NewGuid().ToString()))).StatusCode);
    }

    [Fact]
    public async Task UnconfiguredAcquisitionIsSanitizedFailureNotEmpty()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/workspaces", new { site = "SW", parentParts = new[] { "PARENT" }, isTemporary = false });
        using var workspace = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = workspace.RootElement.GetProperty("assignmentId").GetGuid();
        var snapshot = new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, "SW", [], []);
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(id, snapshot);
        var response = await client.GetAsync($"/api/v1/workspaces/{id}/components/COMP/inventory-locations?mpsSnapshotId={snapshot.Id}");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("configured", await response.Content.ReadAsStringAsync());
    }
}
