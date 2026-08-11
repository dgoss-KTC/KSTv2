using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// QAD is never configured in the test environment (Testing appsettings has no QadDatabase server),
/// so these tests exercise the workspace-not-found / MPS-not-loaded / validation Problem Details
/// paths reachable without a live QAD environment. See <c>MpsEndpointTests</c> for the equivalent
/// pattern this mirrors, and the manual Stage 6D live-QAD validation notes for loaded/stale/missing-
/// part scenarios that require a real database.
/// </summary>
public sealed class PartDetailEndpointTests
{
    private static async Task<Guid> CreateWorkspaceAsync(HttpClient client)
    {
        var request = new { site = "SW", parentParts = new[] { "ABC100" }, isTemporary = false };
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("assignmentId").GetGuid();
    }

    [Fact]
    public async Task GetPartDetail_Returns404_For_Unknown_Workspace()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/workspaces/{Guid.NewGuid()}/part-detail?partNumber=ABC100");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPartDetail_Returns409_When_Mps_Not_Loaded()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/part-detail?partNumber=ABC100");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetPartDetail_Returns400_When_PartNumber_Missing()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/part-detail");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPartDetail_Returns400_When_PartNumber_Blank()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/part-detail?partNumber=%20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
