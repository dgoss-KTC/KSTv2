using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kst.Api.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<KstApiFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(KstApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_Returns200()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_Returns_CamelCase_Json()
    {
        var response = await _client.GetAsync("/health");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("status", out _), "Expected camelCase 'status' property.");
        Assert.True(doc.RootElement.TryGetProperty("application", out _), "Expected camelCase 'application' property.");
        Assert.True(doc.RootElement.TryGetProperty("backendVersion", out _), "Expected camelCase 'backendVersion' property.");
        Assert.True(doc.RootElement.TryGetProperty("processId", out _), "Expected camelCase 'processId' property.");
        Assert.True(doc.RootElement.TryGetProperty("instanceId", out _), "Expected camelCase 'instanceId' property.");
        Assert.True(doc.RootElement.TryGetProperty("timestamp", out _), "Expected camelCase 'timestamp' property.");
    }

    [Fact]
    public async Task GetHealth_Status_Is_Healthy()
    {
        var response = await _client.GetAsync("/health");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        Assert.Equal("healthy", doc.RootElement.GetProperty("status").GetString());
    }
}
