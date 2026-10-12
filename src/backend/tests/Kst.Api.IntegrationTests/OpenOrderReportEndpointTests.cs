using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.OpenOrderReport;
using Kst.Domain.OpenOrderReport;

namespace Kst.Api.IntegrationTests;

public class OpenOrderReportEndpointTests
{
    [Fact]
    public void Preparation_clones_bytes_retains_limits_and_discard_is_kind_owned()
    {
        var store = new ComponentMrpPreparations(); var bytes = new byte[] { 1, 2 };
        var start = DateTimeOffset.UtcNow; var p = store.Add("owner", bytes, "OpenOrderReport"); bytes[0] = 99;
        Assert.Equal(1, store.Get(p.Id, "owner", "OpenOrderReport")![0]);
        Assert.InRange(p.Expires, start.AddMinutes(30), DateTimeOffset.UtcNow.AddMinutes(30));
        store.Discard("owner", "Sop"); Assert.NotNull(store.Get(p.Id, "owner", "OpenOrderReport"));
        store.Discard("owner", "OpenOrderReport"); Assert.Null(store.Get(p.Id, "owner", "OpenOrderReport"));
        Assert.Throws<InvalidOperationException>(() => store.Add("owner", new byte[8 * 1024 * 1024 + 1], "OpenOrderReport"));
    }

    [Fact]
    public async Task Fresh_prepared_empty_error_validation_and_kind_owner_isolation()
    {
        var calls = 0;
        await using var factory = new KstApiFactory
        {
            OpenOrderReportReader = new DelegateOpenOrderReportReader((f, _) =>
        {
            calls++;
            if (f.Site == "MS") throw new Exception("secret source details");
            if (f.Site == "MN") return Task.FromResult<IReadOnlyList<OpenOrderReportRow>>([new([])]);
            if (f.Site == "NW") return Task.FromResult<IReadOnlyList<OpenOrderReportRow>>(Enumerable.Repeat(new OpenOrderReportRow(Enumerable.Repeat(new OpenOrderReportCell(), 34).ToArray()), 10001).ToArray());
            if (f.Site == "AR") return Task.FromResult<IReadOnlyList<OpenOrderReportRow>>([]);
            return Task.FromResult<IReadOnlyList<OpenOrderReportRow>>([new(Enumerable.Repeat(new OpenOrderReportCell(), 34).ToArray())]);
        })
        };
        using var client = factory.CreateClient(); var owner = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-KST-Report-Session", owner);
        Task<HttpResponseMessage> Generate(string? site) => client.PostAsJsonAsync("/api/v1/reports/open-orders", new { site, optionalFields = Array.Empty<string>() });
        var body = await (await Generate("SW")).Content.ReadFromJsonAsync<JsonElement>(); var id = body.GetProperty("preparationId").GetString();
        Assert.StartsWith("Open-Orders_", body.GetProperty("fileName").GetString());
        var bytes = await client.GetByteArrayAsync($"/api/v1/reports/open-orders/preparations/{id}/bytes"); Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/api/v1/reports/shipments-to-go/preparations/{id}/bytes")).StatusCode);
        client.DefaultRequestHeaders.Remove("X-KST-Report-Session"); client.DefaultRequestHeaders.Add("X-KST-Report-Session", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/api/v1/reports/open-orders/preparations/{id}/bytes")).StatusCode);
        client.DefaultRequestHeaders.Remove("X-KST-Report-Session"); client.DefaultRequestHeaders.Add("X-KST-Report-Session", owner);
        await Generate("SW"); Assert.Equal(2, calls);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/api/v1/reports/open-orders/preparations/{id}/bytes")).StatusCode);
        var latest = await (await Generate("SW")).Content.ReadFromJsonAsync<JsonElement>();
        var latestId = latest.GetProperty("preparationId").GetString();
        var empty = await (await Generate("AR")).Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("noData", empty.GetProperty("outcome").GetString());
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/api/v1/reports/open-orders/preparations/{latestId}/bytes")).StatusCode);
        Assert.Equal(JsonValueKind.Null, empty.GetProperty("preparationId").ValueKind);
        foreach (var s in new[] { "MS", "MN", "NW" }) { var failed = await Generate(s); Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode); Assert.DoesNotContain("secret", await failed.Content.ReadAsStringAsync()); }
        foreach (var s in new string?[] { "KS", null }) Assert.Equal(HttpStatusCode.BadRequest, (await Generate(s)).StatusCode);
        client.DefaultRequestHeaders.Remove("X-KST-Report-Session"); Assert.Equal(HttpStatusCode.BadRequest, (await Generate("SW")).StatusCode);
    }
}
