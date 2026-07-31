using System.Net;

namespace Kst.Api.IntegrationTests;

public sealed class CorsPolicyTests : IClassFixture<KstApiFactory>
{
    private readonly HttpClient _client;

    public CorsPolicyTests(KstApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_WithAllowedOrigin_ReturnsCorsHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "http://localhost:1420");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal("http://localhost:1420", values.Single());
    }

    [Fact]
    public async Task GetHealth_WithPackagedTauriOrigin_ReturnsCorsHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "http://tauri.localhost");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal("http://tauri.localhost", values.Single());
    }
}
