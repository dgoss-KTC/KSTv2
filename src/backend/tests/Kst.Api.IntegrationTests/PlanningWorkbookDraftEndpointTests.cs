using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.Mps;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Microsoft.Extensions.DependencyInjection;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// Stage 14.5 Planning draft endpoint tests. Drafts use an in-memory store; no QAD reader is invoked
/// by any draft route (all readers are left unconfigured and would throw).
/// </summary>
public sealed class PlanningWorkbookDraftEndpointTests
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
    private static DateOnly WeekLabel(int index)
    {
        var sunday = Today.AddDays(-(int)Today.DayOfWeek);
        return sunday.AddDays(1 + 7 * index);
    }

    [Fact]
    public async Task Save_Load_Presence_And_Delete_Round_Trip_With_Exact_Text_And_Unavailable_Baselines()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var mps = Seed(factory, id);
        var root = $"/api/v1/workspaces/{id}/planning-workbook/draft";

        Assert.False((await client.GetFromJsonAsync<JsonElement>($"{root}/presence")).GetProperty("exists").GetBoolean());
        var none = await client.GetFromJsonAsync<JsonElement>($"{root}?mpsSnapshotId={mps.Id}");
        Assert.False(none.GetProperty("exists").GetBoolean());

        using var saved = await client.PutAsJsonAsync(root, new
        {
            mpsSnapshotId = mps.Id.Value,
            entries = new object[]
            {
                new { part = "P-1", weekLabel = WeekLabel(0), adjustment = "-12" },
                new { part = "P-2", weekLabel = WeekLabel(0), adjustment = "-3" },
                new { part = "P-2", weekLabel = WeekLabel(1), adjustment = "0" },
                new { part = "P-1", weekLabel = WeekLabel(-1), adjustment = "4" }
            }
        });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var json = await saved.Content.ReadFromJsonAsync<JsonElement>();
        var rows = json.GetProperty("rows");
        Assert.Equal(3, rows.GetArrayLength()); // the zero entry is a no-op and is not saved
        Assert.Equal("12.5", rows[0].GetProperty("currentMps").GetString());
        Assert.Equal("0.5", rows[0].GetProperty("proposedMps").GetString());
        Assert.Equal("-1", rows[1].GetProperty("proposedMps").GetString());
        Assert.Equal("negative-result", rows[1].GetProperty("issueCode").GetString());
        Assert.Equal(JsonValueKind.Null, rows[2].GetProperty("savedMpsQuantity").ValueKind);
        Assert.Equal(JsonValueKind.Null, rows[2].GetProperty("currentMps").ValueKind);
        Assert.Equal("week-passed", rows[2].GetProperty("issueCode").GetString());
        Assert.Equal(mps.Id.ToString(), json.GetProperty("savedMpsSnapshotId").GetString());

        Assert.True((await client.GetFromJsonAsync<JsonElement>($"{root}/presence")).GetProperty("exists").GetBoolean());
        var loaded = await client.GetFromJsonAsync<JsonElement>($"{root}?mpsSnapshotId={mps.Id}");
        Assert.True(loaded.GetProperty("exists").GetBoolean());
        Assert.Equal("-12", loaded.GetProperty("rows")[0].GetProperty("adjustment").GetString());
        Assert.False(loaded.GetProperty("rows")[0].GetProperty("baselineChanged").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(root)).StatusCode);
        Assert.False((await client.GetFromJsonAsync<JsonElement>($"{root}/presence")).GetProperty("exists").GetBoolean());
    }

    [Fact]
    public async Task Invalid_Requests_Unknown_Workspace_Mps_Conflicts_And_Corrupt_Drafts_Are_Sanitized()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var root = $"/api/v1/workspaces/{id}/planning-workbook/draft";
        var entry = new[] { new { part = "P-1", weekLabel = WeekLabel(0), adjustment = "5" } };

        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(root, new { mpsSnapshotId = Guid.NewGuid(), entries = entry })).StatusCode);
        var mps = Seed(factory, id);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(root, new { mpsSnapshotId = Guid.NewGuid(), entries = entry })).StatusCode);
        (await client.PutAsJsonAsync(root, new { mpsSnapshotId = mps.Id.Value, entries = entry })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync($"{root}?mpsSnapshotId={Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"{root}?mpsSnapshotId={Guid.Empty}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(root)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync($"/api/v1/workspaces/{Guid.NewGuid()}/planning-workbook/draft", new { mpsSnapshotId = mps.Id.Value, entries = entry })).StatusCode);

        using var invalid = await client.PutAsJsonAsync(root, new { mpsSnapshotId = mps.Id.Value, entries = new[] { new { part = "P-1", weekLabel = WeekLabel(0), adjustment = "+5" } } });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("invalid-number", (await invalid.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        factory.PlanningDraftStore.Corrupt.Add(id);
        Assert.True((await client.GetFromJsonAsync<JsonElement>($"{root}/presence")).GetProperty("exists").GetBoolean());
        var corrupt = await client.GetFromJsonAsync<JsonElement>($"{root}?mpsSnapshotId={mps.Id}");
        Assert.True(corrupt.GetProperty("isCorrupt").GetBoolean());
        Assert.Equal(0, corrupt.GetProperty("rows").GetArrayLength());
        using var overwrite = await client.PutAsJsonAsync(root, new { mpsSnapshotId = mps.Id.Value, entries = entry });
        Assert.Equal(HttpStatusCode.Conflict, overwrite.StatusCode);
        Assert.Equal("draft-corrupt", (await overwrite.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Contains(id, factory.PlanningDraftStore.Corrupt);
    }

    [Fact]
    public async Task Workspace_Delete_And_Reset_Remove_Planning_Drafts_While_Archive_Preserves_Them()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var archived = await CreateAsync(client);
        var deleted = await CreateAsync(client, "P-3");
        foreach (var id in new[] { archived, deleted })
        {
            var mps = Seed(factory, id);
            (await client.PutAsJsonAsync($"/api/v1/workspaces/{id}/planning-workbook/draft",
                new { mpsSnapshotId = mps.Id.Value, entries = new[] { new { part = "P-1", weekLabel = WeekLabel(0), adjustment = "5" } } }))
                .EnsureSuccessStatusCode();
        }

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/workspaces/{archived}/archive", null)).StatusCode);
        Assert.NotNull(factory.PlanningDraftStore.Get(archived));
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/workspaces/{deleted}")).StatusCode);
        Assert.Null(factory.PlanningDraftStore.Get(deleted));
        Assert.NotNull(factory.PlanningDraftStore.Get(archived));
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/v1/workspaces")).StatusCode);
        Assert.Null(factory.PlanningDraftStore.Get(archived));
    }

    private static async Task<Guid> CreateAsync(HttpClient client, string extraPart = "P-2")
    {
        var response = await client.PostAsJsonAsync("/api/v1/workspaces",
            new { displayName = (string?)null, site = "SW", parentParts = new[] { "P-1", extraPart }, isTemporary = false });
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("assignmentId").GetGuid();
    }

    private static MpsSnapshot Seed(KstApiFactory factory, Guid id)
    {
        var snapshot = new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, "SW",
            [new MpsResolvedPart("P-1", null), new MpsResolvedPart("P-2", null)],
            [
                new MpsSourceRow("TEST", "SW", "P-1", null, Today, null, 12.5m, MpsSupplyType.Supply, "WO-1", MpsWorkOrderState.Planned),
                new MpsSourceRow("TEST", "SW", "P-2", null, Today, null, 2m, MpsSupplyType.Supply, "WO-2", MpsWorkOrderState.Planned)
            ]);
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(id, snapshot);
        return snapshot;
    }
}
