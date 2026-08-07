using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// QAD is never configured in the test environment (Testing appsettings has no QadDatabase server),
/// so these tests exercise the "database unavailable" Problem Details path rather than real QAD data
/// — real-data validation requires a live QAD environment this test suite does not have.
/// </summary>
public sealed class MpsEndpointTests
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
    public async Task GetMpsDashboard_Returns404_For_Unknown_Workspace()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/workspaces/{Guid.NewGuid()}/mps");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMpsDashboard_Returns503_With_Friendly_Detail_When_Qad_Not_Configured()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/mps");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal(
            "Database currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT.",
            doc.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task RefreshMpsDashboard_Returns503_When_Qad_Not_Configured()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.PostAsync($"/api/v1/workspaces/{assignmentId}/mps/refresh", content: null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task GetMpsDashboard_Returns400_For_Invalid_DateBasis()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/mps?dateBasis=notARealBasis");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(73)]
    [InlineData(-1)]
    public async Task GetMpsDashboard_Returns400_For_HorizonWeeks_Out_Of_Range(int horizonWeeks)
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/mps?horizonWeeks={horizonWeeks}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
