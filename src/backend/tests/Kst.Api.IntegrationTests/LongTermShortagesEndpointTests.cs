using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.Bom;
using Kst.Application.LongTermShortages;
using Kst.Application.Mps;
using Kst.Domain.Bom;
using Kst.Domain.LongTermShortages;
using Kst.Domain.Mps;
using Microsoft.Extensions.DependencyInjection;
namespace Kst.Api.IntegrationTests;
public sealed class LongTermShortagesEndpointTests
{
    [Fact]
    public async Task Get_ExposesRawProjectionAndCachedExport()
    {
        await using var factory = new KstApiFactory { BomSourceReader = new DelegateBomSourceReader((_, _, _, _) => Task.FromResult<IReadOnlyList<BomOccurrence>>([new("1", 1, "COMP", "P", false, null, 1m, null)])), LongTermShortageSourceReader = new DelegateLongTermShortageSourceReader((_, _, _, _) => Task.FromResult<IReadOnlyList<LongTermShortageInput>>([new("COMP", "EA", "P", "Component", null, null, 10m, SafetyStockState.Resolved, 0m, ["PARENT"], [])])) };
        using var client = factory.CreateClient(); var workspace = JsonDocument.Parse(await (await client.PostAsJsonAsync("/api/v1/workspaces", new { site = "SW", parentParts = new[] { "PARENT" }, isTemporary = false })).Content.ReadAsStringAsync()).RootElement.GetProperty("assignmentId").GetGuid();
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(workspace, new MpsSnapshot(Kst.Domain.Common.SnapshotId.New(), DateTimeOffset.UtcNow, "SW", [new MpsResolvedPart("PARENT", "")], [])); var snapshot = factory.Services.GetRequiredService<IMpsSnapshotStore>().GetState(workspace).Snapshot!.Id.Value;
        var response = await client.GetAsync($"/api/v1/workspaces/{workspace}/long-term-shortages?snapshotId={snapshot}"); var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var export = await client.PostAsJsonAsync($"/api/v1/workspaces/{workspace}/long-term-shortages/export", new { snapshotId = snapshot, componentParts = new[] { "COMP" } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal(24, body.GetProperty("rows")[0].GetProperty("weeks").GetArrayLength()); Assert.True(body.GetProperty("rows")[0].TryGetProperty("past", out _)); Assert.Equal(HttpStatusCode.OK, export.StatusCode);
    }
}
