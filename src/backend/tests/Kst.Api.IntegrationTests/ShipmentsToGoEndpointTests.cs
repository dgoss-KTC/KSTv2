using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.ShipmentsToGo;
using Kst.Domain.ShipmentsToGo;

namespace Kst.Api.IntegrationTests;

public class ShipmentsToGoEndpointTests
{
    [Fact]
    public async Task Independent_fresh_workbook_empty_failure_and_kind_ownership()
    {
        var calls = 0; var shippedCalls = 0;
        await using var factory = new KstApiFactory
        {
            ShipmentsToGoReader = new DelegateShipmentsToGoReader((sites, next, _) =>
            {
                calls++; Assert.True(next);
                if (sites[0] == "MS") throw new Exception("secret source details");
                return Task.FromResult<IReadOnlyList<ShipmentToGoLine>>(sites[0] == "AR" ? [] : [new(new(2026, 10, 9), "0001", null, "Standard", 1, null, "PART", null, "SW", "SW", 2, null, 0, 0, "Name", 200, "CRC", -200, "")]);
            }, (_, _) => { shippedCalls++; return Task.FromResult<IReadOnlyDictionary<string, double>>(new Dictionary<string, double> { { "SW", 0 } }); })
        };
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-KST-Report-Session", Guid.NewGuid().ToString());
        async Task<HttpResponseMessage> Generate(string site) => await client.PostAsJsonAsync("/api/v1/reports/shipments-to-go", new { sites = new[] { site }, includeNextWeek = true });
        var first = await Generate("SW"); Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var body = await first.Content.ReadFromJsonAsync<JsonElement>(); var id = body.GetProperty("preparationId").GetString();
        var bytes = await client.GetByteArrayAsync($"/api/v1/reports/shipments-to-go/preparations/{id}/bytes"); Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/api/v1/reports/component-mrp/preparations/{id}/bytes")).StatusCode);
        await Generate("SW"); Assert.Equal(2, calls); Assert.Equal(2, shippedCalls);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/api/v1/reports/shipments-to-go/preparations/{id}/bytes")).StatusCode);
        var empty = await (await Generate("AR")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("noData", empty.GetProperty("outcome").GetString()); Assert.Equal(JsonValueKind.Null, empty.GetProperty("preparationId").ValueKind); Assert.Equal(2, shippedCalls);
        var error = await Generate("MS"); Assert.Equal(HttpStatusCode.ServiceUnavailable, error.StatusCode); Assert.DoesNotContain("secret", await error.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await Generate("KS")).StatusCode);
    }
}
