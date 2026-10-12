using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.Sop;
using Kst.Domain.Sop;

namespace Kst.Api.IntegrationTests;

public class SopEndpointTests
{
    [Fact]
    public async Task Fresh_acquire_prepare_coverage_owner_kind_discard_empty_and_failure()
    {
        var calls = 0;
        await using var factory = new KstApiFactory
        {
            SopReader = new DelegateSopReader((site, ranges, ct) =>
        {
            calls++; ct.ThrowIfCancellationRequested();
            if (site == "MS") throw new Exception("secret");
            return Task.FromResult(new SopSource(["Customer", "Part", "St", "Planr", "Prod", "type", "Time Std", " Falldown"], [new(2026, 10, 12)],
                site == "AR" ? [] : ranges.Select(_ => new SopRow([new(Text: "C"), new(Text: "P"), new(Text: "C"), new(Text: "Planner"), new(Number: 2140), new(Text: "SUPPLY"), new(Number: 1), new(Number: 10)], [new(Number: 7)])).ToArray()));
        })
        };
        using var client = factory.CreateClient(); var owner = Guid.NewGuid().ToString(); client.DefaultRequestHeaders.Add("X-KST-Report-Session", owner);
        async Task<HttpResponseMessage> Acquire(string site) => await client.PostAsJsonAsync("/api/v1/reports/sop/acquisitions", new { site, groupBy = "planr", ranges = new[] { new { from = "2142", to = "2140" }, new { from = "2142", to = "2140" } } });
        var acquired = await (await Acquire("SW")).Content.ReadFromJsonAsync<JsonElement>(); var id = acquired.GetProperty("acquisitionId").GetString();
        var descriptor = new { anchorFiscalYear = 2026, anchorStartDate = "2025-06-29", exceptions = Array.Empty<object>(), periods = new[] { new { fiscalYear = 2027, period = 4, quarter = 2, startSunday = "2026-09-27", weekCount = 4, weekLabels = new[] { "2026-10-12" } } } };
        using var wrong = factory.CreateClient(); wrong.DefaultRequestHeaders.Add("X-KST-Report-Session", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Gone, (await wrong.PostAsJsonAsync("/api/v1/reports/sop/preparations", new { acquisitionId = id, presentation = descriptor })).StatusCode);
        var response = await client.PostAsJsonAsync("/api/v1/reports/sop/preparations", new { acquisitionId = id, presentation = descriptor }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var prepared = await response.Content.ReadFromJsonAsync<JsonElement>(); var pid = prepared.GetProperty("preparationId").GetString();
        Assert.Equal((byte)'P', (await client.GetByteArrayAsync($"/api/v1/reports/sop/preparations/{pid}/bytes"))[0]);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/api/v1/reports/shipments-to-go/preparations/{pid}/bytes")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync("/api/v1/reports/sop/preparations", new { acquisitionId = id, presentation = descriptor })).StatusCode);
        Assert.Equal(1, calls);
        var second = await (await Acquire("SW")).Content.ReadFromJsonAsync<JsonElement>(); var secondId = second.GetProperty("acquisitionId").GetString();
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/reports/sop/acquisitions/{secondId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync("/api/v1/reports/sop/preparations", new { acquisitionId = secondId, presentation = descriptor })).StatusCode);
        Assert.Equal("noData", (await (await Acquire("AR")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("outcome").GetString());
        var failure = await Acquire("MS"); Assert.Equal(HttpStatusCode.ServiceUnavailable, failure.StatusCode); Assert.DoesNotContain("secret", await failure.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await Acquire("KS")).StatusCode);
        var third = await (await Acquire("SW")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/reports/sop/preparations", new { acquisitionId = third.GetProperty("acquisitionId").GetString(), presentation = new { anchorFiscalYear = 2026, anchorStartDate = "2025-06-29", exceptions = Array.Empty<object>(), periods = Array.Empty<object>() } })).StatusCode);
    }
}
