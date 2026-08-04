using System.Net;
using System.Text.Json;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// Each test creates its own factory to ensure isolated in-memory store state.
/// </summary>
public sealed class SystemRefreshEndpointTests
{
    [Fact]
    public async Task PostRefresh_Returns200_With_SystemStatusResponse()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/v1/system/refresh", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("snapshot", out _));
        Assert.True(doc.RootElement.TryGetProperty("dataSources", out _));
    }

    [Fact]
    public async Task PostRefresh_Sets_LastRefreshAttemptAt()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var before = await client.GetAsync("/api/v1/system/status");
        var beforeDoc = JsonDocument.Parse(await before.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, beforeDoc.RootElement.GetProperty("lastRefreshAttemptAt").ValueKind);

        var refreshResponse = await client.PostAsync("/api/v1/system/refresh", content: null);
        var afterDoc = JsonDocument.Parse(await refreshResponse.Content.ReadAsStringAsync());

        Assert.NotEqual(JsonValueKind.Null, afterDoc.RootElement.GetProperty("lastRefreshAttemptAt").ValueKind);
    }

    [Fact]
    public async Task PostRefresh_With_NotConfigured_Sources_Yields_NotLoaded_Snapshot_And_Null_LastSuccess()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/v1/system/refresh", content: null);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        Assert.Equal("notLoaded", doc.RootElement.GetProperty("snapshot").GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("lastSuccessfulRefreshAt").ValueKind);

        foreach (var ds in doc.RootElement.GetProperty("dataSources").EnumerateArray())
            Assert.Equal("notConfigured", ds.GetProperty("status").GetString());
    }
}
