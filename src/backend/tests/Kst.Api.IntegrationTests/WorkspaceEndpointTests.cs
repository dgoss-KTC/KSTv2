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

    [Fact]
    public async Task CreateWorkspace_Returns400_For_Duplicate_Scope_Among_Active_Workspaces()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { site = "NW", customerNumber = "12345678", isTemporary = false };
        await client.PostAsJsonAsync("/api/v1/workspaces", request);

        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("scope", json, StringComparison.OrdinalIgnoreCase);
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

    // --- Update ---

    private static async Task<Guid> CreateWorkspaceAsync(HttpClient client, object? request = null)
    {
        request ??= new { site = "NW", customerNumber = "12345678", isTemporary = false };
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("assignmentId").GetGuid();
    }

    [Fact]
    public async Task UpdateWorkspace_Returns200_And_Updates_Fields()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var id = await CreateWorkspaceAsync(client);

        var update = new { site = "SW", customerNumber = "87654321", isTemporary = false };
        var response = await client.PutAsJsonAsync($"/api/v1/workspaces/{id}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal("SW", doc.RootElement.GetProperty("site").GetString());
        Assert.Equal("87654321", doc.RootElement.GetProperty("customerNumber").GetString());
        Assert.Equal(id, doc.RootElement.GetProperty("assignmentId").GetGuid());
    }

    [Fact]
    public async Task UpdateWorkspace_Returns400_For_Invalid_Site()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var id = await CreateWorkspaceAsync(client);

        var update = new { site = "N", customerNumber = "87654321", isTemporary = false };
        var response = await client.PutAsJsonAsync($"/api/v1/workspaces/{id}", update);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task UpdateWorkspace_Returns404_For_Unknown_AssignmentId()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var update = new { site = "SW", customerNumber = "87654321", isTemporary = false };
        var response = await client.PutAsJsonAsync($"/api/v1/workspaces/{Guid.NewGuid()}", update);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Archive ---

    [Fact]
    public async Task ArchiveWorkspace_Returns200_And_Sets_IsEnabled_False()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var id = await CreateWorkspaceAsync(client);

        var response = await client.PostAsync($"/api/v1/workspaces/{id}/archive", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("isEnabled").GetBoolean());
    }

    [Fact]
    public async Task ArchiveWorkspace_Returns404_For_Unknown_AssignmentId()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/v1/workspaces/{Guid.NewGuid()}/archive", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Restore ---

    [Fact]
    public async Task RestoreWorkspace_Returns200_And_Sets_IsEnabled_True()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var id = await CreateWorkspaceAsync(client);
        await client.PostAsync($"/api/v1/workspaces/{id}/archive", null);

        var response = await client.PostAsync($"/api/v1/workspaces/{id}/restore", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("isEnabled").GetBoolean());
    }

    [Fact]
    public async Task RestoreWorkspace_Returns404_For_Unknown_AssignmentId()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/v1/workspaces/{Guid.NewGuid()}/restore", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Delete ---

    [Fact]
    public async Task DeleteWorkspace_Returns204_And_Removes_From_List()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var id = await CreateWorkspaceAsync(client);

        var response = await client.DeleteAsync($"/api/v1/workspaces/{id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var listResponse = await client.GetAsync("/api/v1/workspaces");
        var json = await listResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal(0, doc.RootElement.GetProperty("workspaces").GetArrayLength());
    }

    [Fact]
    public async Task DeleteWorkspace_Returns404_For_Unknown_AssignmentId()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/workspaces/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Reset ---

    [Fact]
    public async Task ResetWorkspaces_Returns204_And_Clears_All_Workspaces()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        await CreateWorkspaceAsync(client, new { site = "NW", customerNumber = "11111111", isTemporary = false });
        await CreateWorkspaceAsync(client, new { site = "SW", customerNumber = "22222222", isTemporary = false });

        var response = await client.DeleteAsync("/api/v1/workspaces");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var listResponse = await client.GetAsync("/api/v1/workspaces");
        var json = await listResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal(0, doc.RootElement.GetProperty("workspaces").GetArrayLength());
    }

    [Fact]
    public async Task ResetWorkspaces_On_Empty_Configuration_Returns204()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/v1/workspaces");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}

