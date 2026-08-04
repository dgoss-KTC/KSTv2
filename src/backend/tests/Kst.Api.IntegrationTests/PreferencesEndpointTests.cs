using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// Each test creates its own factory to ensure isolated in-memory store state.
/// </summary>
public sealed class PreferencesEndpointTests
{
    [Fact]
    public async Task GetPreferences_Returns200_With_Defaults()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/preferences");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var preferences = doc.RootElement.GetProperty("preferences");

        Assert.Equal("system", preferences.GetProperty("theme").GetString());
        Assert.Equal("blue", preferences.GetProperty("accentColor").GetString());
        Assert.Equal("compact", preferences.GetProperty("rowDensity").GetString());
        Assert.True(doc.RootElement.TryGetProperty("configurationWarning", out _));
    }

    [Fact]
    public async Task PutPreferences_Returns200_And_Persists_Values()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { theme = "dark", accentColor = "teal", rowDensity = "comfortable" };
        var putResponse = await client.PutAsJsonAsync("/api/v1/preferences", request);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var getResponse = await client.GetAsync("/api/v1/preferences");
        var json = await getResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var preferences = doc.RootElement.GetProperty("preferences");

        Assert.Equal("dark", preferences.GetProperty("theme").GetString());
        Assert.Equal("teal", preferences.GetProperty("accentColor").GetString());
        Assert.Equal("comfortable", preferences.GetProperty("rowDensity").GetString());
    }

    [Fact]
    public async Task PutPreferences_Returns400_For_Invalid_Theme()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { theme = "rainbow", accentColor = "blue", rowDensity = "compact" };
        var response = await client.PutAsJsonAsync("/api/v1/preferences", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("theme", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PutPreferences_Returns400_For_Invalid_AccentColor()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { theme = "dark", accentColor = "purple", rowDensity = "compact" };
        var response = await client.PutAsJsonAsync("/api/v1/preferences", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutPreferences_Is_CaseInsensitive()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var request = new { theme = "LIGHT", accentColor = "AMBER", rowDensity = "COMPACT" };
        var response = await client.PutAsJsonAsync("/api/v1/preferences", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal("light", doc.RootElement.GetProperty("preferences").GetProperty("theme").GetString());
    }
}
