using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// Each test creates its own factory to ensure isolated in-memory store state.
/// </summary>
public sealed class WorkspaceEndpointTests
{
    // --- List ---

    [Fact]
    public async Task ListWorkspaces_Returns200_With_Empty_List()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/workspaces");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("workspaces", out var arr));
        Assert.Equal(JsonValueKind.Array, arr.ValueKind);
        Assert.Equal(0, arr.GetArrayLength());
    }

    [Fact]
    public async Task ListWorkspaces_Returns_CamelCase_Json()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/workspaces");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("workspaces", out _));
        Assert.True(doc.RootElement.TryGetProperty("configurationWarning", out _));
    }

    // --- Create ---

    [Fact]
    public async Task CreateWorkspace_Returns201_For_Valid_Customer_Request()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { site = "NW", customerNumber = "12345678", isTemporary = false };
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkspace_Returns201_For_Valid_ProductLine_Request()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { site = "SW", productLineFrom = "0040", isTemporary = false };
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkspace_Returns201_For_Valid_ProductLine_Range()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { site = "AR", productLineFrom = "0040", productLineTo = "0045", isTemporary = false };
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkspace_Response_Contains_AssignmentId()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { site = "NW", customerNumber = "12345678", isTemporary = false };
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("assignmentId", out var idProp));
        Assert.Equal(JsonValueKind.String, idProp.ValueKind);
        Assert.True(Guid.TryParse(idProp.GetString(), out _));
    }

    [Fact]
    public async Task CreateWorkspace_Response_Is_CamelCase()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { site = "NW", customerNumber = "12345678", isTemporary = false };
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("assignmentId", out _));
        Assert.True(doc.RootElement.TryGetProperty("displayName", out _));
        Assert.True(doc.RootElement.TryGetProperty("site", out _));
        Assert.True(doc.RootElement.TryGetProperty("customerNumber", out _));
        Assert.True(doc.RootElement.TryGetProperty("isEnabled", out _));
        Assert.True(doc.RootElement.TryGetProperty("sortOrder", out _));
    }

    [Fact]
    public async Task CreateWorkspace_Normalizes_Site_To_Uppercase()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { site = "nw", customerNumber = "12345678", isTemporary = false };
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal("NW", doc.RootElement.GetProperty("site").GetString());
    }

    // --- Validation errors (Problem Details) ---

    [Fact]
    public async Task CreateWorkspace_Returns400_For_SiteOnly()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { site = "NW", isTemporary = false };
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkspace_Returns400_For_Missing_Site()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { customerNumber = "12345678", isTemporary = false };
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkspace_Returns400_For_Invalid_CustomerNumber()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { site = "NW", customerNumber = "123", isTemporary = false };
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkspace_Returns_ProblemDetails_With_Errors()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { site = "N", isTemporary = false };
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task CreateWorkspace_Returns400_For_ProductLineTo_Without_ProductLineFrom()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { site = "NW", productLineTo = "0045", isTemporary = false };
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Created workspace appears in list ---

    [Fact]
    public async Task Created_Workspace_Appears_In_List()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { site = "NW", customerNumber = "99999999", isTemporary = false };
        await client.PostAsJsonAsync("/api/v1/workspaces", request);

        var listResponse = await client.GetAsync("/api/v1/workspaces");
        var json = await listResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement.GetProperty("workspaces").EnumerateArray().ToList();
        Assert.Single(arr);
        Assert.Equal("99999999", arr[0].GetProperty("customerNumber").GetString());
    }
}

