using System.Net;
using System.Text.Json;

namespace Kst.Api.IntegrationTests;

public sealed class ReadyEndpointTests : IClassFixture<KstApiFactory>
{
    private readonly HttpClient _client;

    public ReadyEndpointTests(KstApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetReady_Returns200()
    {
        var response = await _client.GetAsync("/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetReady_Status_Is_Ready()
    {
        var response = await _client.GetAsync("/ready");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        Assert.Equal("ready", doc.RootElement.GetProperty("status").GetString());
        Assert.True(doc.RootElement.GetProperty("initialized").GetBoolean());
    }

    [Fact]
    public async Task GetReady_SnapshotAvailable_Is_False_Initially()
    {
        var response = await _client.GetAsync("/ready");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        Assert.False(doc.RootElement.GetProperty("snapshotAvailable").GetBoolean());
    }
}
