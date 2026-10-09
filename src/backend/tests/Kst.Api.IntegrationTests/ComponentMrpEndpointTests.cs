using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.ComponentMrp;
using Kst.Domain.ComponentMrp;
using Kst.Domain.LongTermShortages;

namespace Kst.Api.IntegrationTests;

public class ComponentMrpEndpointTests
{
    [Fact]
    public async Task Mocked_direct_part_generates_real_workbook_and_fresh_preparation_without_workspace()
    {
        var calls = 0;
        await using var factory = new KstApiFactory
        {
            ComponentMrpReader = new DelegateComponentMrpReader((site, part, _, _) =>
        {
            calls++; Assert.Equal("KV", site); Assert.Equal("direct", part);
            return Task.FromResult<ComponentMrpSource?>(new(new(part, "KG", "A", "Synthetic", null, "site", 12.5m,
                SafetyStockState.Resolved, null, [], [])
            { SitePlanningPresent = false }, new(null, null, null, null, null, 0), DateTimeOffset.UtcNow));
        })
        };
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-KST-Report-Session", Guid.NewGuid().ToString());
        var request = new { site = "KV", part = "direct", horizonWeeks = 13, includeUnconfirmed = false };
        var first = await client.PostAsJsonAsync("/api/v1/reports/component-mrp", request);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var body = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("includeUnconfirmed").GetBoolean()); Assert.Equal("Week", body.GetProperty("period").GetString());
        var bytes = await client.GetByteArrayAsync($"/api/v1/reports/component-mrp/preparations/{body.GetProperty("preparationId").GetString()}/bytes");
        Assert.Equal((byte)'P', bytes[0]); Assert.Equal((byte)'K', bytes[1]);
        Assert.Contains("Selected-site", body.GetProperty("warning").GetString());
        var second = await client.PostAsJsonAsync("/api/v1/reports/component-mrp", request);
        var next = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(body.GetProperty("preparationId").GetString(), next.GetProperty("preparationId").GetString());
        Assert.Equal(2, calls);
        var defaults = await client.PostAsJsonAsync("/api/v1/reports/component-mrp", new { site = "KV", part = "direct", horizonWeeks = 13 });
        var defaultBody = await defaults.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(defaultBody.GetProperty("includeUnconfirmed").GetBoolean()); Assert.Equal("Week", defaultBody.GetProperty("period").GetString());
        var month = await client.PostAsJsonAsync("/api/v1/reports/component-mrp", new { site = "KV", part = "direct", horizonWeeks = 13, period = "Month" });
        var monthBody = await month.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("Month", monthBody.GetProperty("period").GetString());
        Assert.Equal(defaultBody.GetProperty("forecastEndExclusive").GetString(), monthBody.GetProperty("forecastEndExclusive").GetString());
    }

    [Fact]
    public async Task Missing_invalid_and_failed_acquisition_are_distinct_and_sanitized()
    {
        await using var factory = new KstApiFactory
        {
            ComponentMrpReader = new DelegateComponentMrpReader((_, part, _, _) =>
            part == "missing" ? Task.FromResult<ComponentMrpSource?>(null) : throw new Exception("secret connection details"))
        };
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-KST-Report-Session", Guid.NewGuid().ToString());
        async Task<HttpResponseMessage> Generate(string site, string part) => await client.PostAsJsonAsync("/api/v1/reports/component-mrp", new { site, part, horizonWeeks = 26 });
        Assert.Equal(HttpStatusCode.BadRequest, (await Generate("KS", "missing")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Generate("SW", "missing")).StatusCode);
        var failure = await Generate("SW", "error"); Assert.Equal(HttpStatusCode.ServiceUnavailable, failure.StatusCode);
        Assert.DoesNotContain("secret", await failure.Content.ReadAsStringAsync());
    }
}
