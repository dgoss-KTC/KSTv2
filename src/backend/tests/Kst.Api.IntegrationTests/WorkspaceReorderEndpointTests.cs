using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// Each test creates its own factory to ensure isolated in-memory store state.
/// </summary>
public sealed class WorkspaceReorderEndpointTests
{
    private static async Task<Guid> CreateWorkspaceAsync(HttpClient client, string site, string parentPart)
    {
        var response = await client.PostAsJsonAsync("/api/v1/workspaces",
            new { site, parentParts = new[] { parentPart }, isTemporary = false });
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("assignmentId").GetGuid();
    }

    [Fact]
    public async Task ReorderWorkspaces_Returns200_And_Applies_New_Order()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var a = await CreateWorkspaceAsync(client, "AA", "11111111");
        var b = await CreateWorkspaceAsync(client, "BB", "22222222");

        var response = await client.PutAsJsonAsync("/api/v1/workspaces/order",
            new { assignmentIds = new[] { b, a } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var workspaces = doc.RootElement.GetProperty("workspaces").EnumerateArray()
            .OrderBy(w => w.GetProperty("sortOrder").GetInt32())
            .ToList();

        Assert.Equal(b, workspaces[0].GetProperty("assignmentId").GetGuid());
        Assert.Equal(a, workspaces[1].GetProperty("assignmentId").GetGuid());
    }

    [Fact]
    public async Task ReorderWorkspaces_Returns400_For_Duplicate_Ids()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var a = await CreateWorkspaceAsync(client, "AA", "11111111");

        var response = await client.PutAsJsonAsync("/api/v1/workspaces/order",
            new { assignmentIds = new[] { a, a } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReorderWorkspaces_Returns400_For_Mismatched_Id_Set()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        await CreateWorkspaceAsync(client, "AA", "11111111");
        await CreateWorkspaceAsync(client, "BB", "22222222");

        var response = await client.PutAsJsonAsync("/api/v1/workspaces/order",
            new { assignmentIds = new[] { Guid.NewGuid() } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
