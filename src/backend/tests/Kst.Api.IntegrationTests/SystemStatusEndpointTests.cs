using System.Net;
using System.Text.Json;

namespace Kst.Api.IntegrationTests;

public sealed class SystemStatusEndpointTests : IClassFixture<KstApiFactory>
{
    private readonly HttpClient _client;

    public SystemStatusEndpointTests(KstApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSystemStatus_Returns200()
    {
        var response = await _client.GetAsync("/api/v1/system/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSystemStatus_Returns_CamelCase_Json()
    {
        var response = await _client.GetAsync("/api/v1/system/status");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("applicationName", out _));
        Assert.True(doc.RootElement.TryGetProperty("applicationVersion", out _));
        Assert.True(doc.RootElement.TryGetProperty("backendFramework", out _));
        Assert.True(doc.RootElement.TryGetProperty("backendInstanceId", out _));
        Assert.True(doc.RootElement.TryGetProperty("startedAt", out _));
        Assert.True(doc.RootElement.TryGetProperty("currentTime", out _));
        Assert.True(doc.RootElement.TryGetProperty("snapshot", out _));
        Assert.True(doc.RootElement.TryGetProperty("dataSources", out _));
    }

    [Fact]
    public async Task GetSystemStatus_ApplicationName_Is_KST()
    {
        var response = await _client.GetAsync("/api/v1/system/status");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        var appName = doc.RootElement.GetProperty("applicationName").GetString();
        Assert.Equal("Keytronic Scheduler's Toolbox", appName);
    }

    [Fact]
    public async Task GetSystemStatus_BackendFramework_Is_DotNet10()
    {
        var response = await _client.GetAsync("/api/v1/system/status");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        Assert.Equal(".NET 10", doc.RootElement.GetProperty("backendFramework").GetString());
    }

    [Fact]
    public async Task GetSystemStatus_Snapshot_Status_Is_NotLoaded()
    {
        var response = await _client.GetAsync("/api/v1/system/status");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        var snapshot = doc.RootElement.GetProperty("snapshot");
        Assert.Equal("notLoaded", snapshot.GetProperty("status").GetString());
        Assert.False(snapshot.GetProperty("available").GetBoolean());
    }

    [Fact]
    public async Task GetSystemStatus_DataSources_Contains_QAD_And_Shortages()
    {
        var response = await _client.GetAsync("/api/v1/system/status");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        var dataSources = doc.RootElement.GetProperty("dataSources").EnumerateArray().ToList();
        Assert.Equal(2, dataSources.Count);

        var names = dataSources.Select(ds => ds.GetProperty("name").GetString()).ToList();
        Assert.Contains("QAD", names);
        Assert.Contains("Shortage Database", names);
    }

    [Fact]
    public async Task GetSystemStatus_DataSources_Status_Is_NotConfigured_Without_Config()
    {
        var response = await _client.GetAsync("/api/v1/system/status");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        var dataSources = doc.RootElement.GetProperty("dataSources").EnumerateArray().ToList();
        foreach (var ds in dataSources)
        {
            Assert.Equal("notConfigured", ds.GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task GetSystemStatus_Timestamps_Are_Valid_DateTimeOffset()
    {
        var response = await _client.GetAsync("/api/v1/system/status");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        var startedAt = doc.RootElement.GetProperty("startedAt").GetString();
        var currentTime = doc.RootElement.GetProperty("currentTime").GetString();

        Assert.True(DateTimeOffset.TryParse(startedAt, out _), "startedAt must be a valid DateTimeOffset.");
        Assert.True(DateTimeOffset.TryParse(currentTime, out _), "currentTime must be a valid DateTimeOffset.");
    }
}
