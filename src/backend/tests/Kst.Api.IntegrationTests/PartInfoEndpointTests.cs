using System.Net;
using System.Text.Json;
using Kst.Application.ApprovedVendors;
using Kst.Application.ComponentDetail;
using Kst.Application.ComponentOrders;
using Kst.Application.Inventory;
using Kst.Application.InventoryLocations;
using Kst.Application.PartInfo;
using Kst.Domain.ApprovedVendors;
using Kst.Domain.ComponentDetail;
using Kst.Domain.ComponentOrders;
using Kst.Domain.Inventory;

namespace Kst.Api.IntegrationTests;

public sealed class PartInfoEndpointTests
{
    [Fact]
    public async Task InventoryRefreshIsStandaloneAndDoesNotNeedMasterOrOtherSources()
    {
        await using var factory = new KstApiFactory
        {
            InventoryLocationReader = new DelegateInventoryLocationReader((site, part, _) =>
            {
                Assert.Equal("KV", site); Assert.Equal("EXACT", part);
                return Task.FromResult<IReadOnlyList<InventoryLocationRow>>([new("1", "L", null, null, "-1.123456789")]);
            })
        };
        using var client = factory.CreateClient();
        using var doc = JsonDocument.Parse(await client.GetStringAsync("/api/v1/reports/part-info/inventory-locations?site=KV&part=EXACT"));
        Assert.Equal("-1.123456789", doc.RootElement.GetProperty("locations").GetProperty("value")[0].GetProperty("quantityOnHand").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/reports/part-info/inventory-locations?site=KS&part=EXACT")).StatusCode);
    }
    private static ComponentSourceFacts Facts(string part) => new(part, "Synthetic exact lookup", "AC", "A",
        0m, null, null, null, null, null, null, null, null, null, null);
    private static ComponentOrderLine Po(string supplier, int line) => new("EXACT", null, null, "P1", line,
        new DateOnly(2026, 10, 14), 99999999999999999.123456789m, true, supplier, null, null, false, null, supplier);

    private static KstApiFactory Fixture(bool noOrders = false, bool enrichmentFailure = false, bool locationsFailure = false) => new()
    {
        ComponentSourceReader = new DelegateComponentSourceReader((site, part, _) =>
        {
            Assert.Contains(site, new[] { "SW", "KV" });
            return Task.FromResult<ComponentSourceFacts?>(part == "MISSING" ? null : Facts(part));
        }),
        PartInventoryReader = new DelegatePartInventoryReader((site, parts, _) =>
        {
            Assert.Equal(new[] { "EXACT" }, parts);
            return Task.FromResult<IReadOnlyList<PartInventorySummary>>([new(site, "EXACT", 0m, 12.123456789m, 0m)]);
        }),
        InventoryLocationReader = new DelegateInventoryLocationReader((site, part, _) =>
        {
            Assert.Equal("EXACT", part);
            if (locationsFailure) throw new Exception("private connection details");
            return Task.FromResult<IReadOnlyList<InventoryLocationRow>>([]);
        }),
        ApprovedVendorSourceReader = new DelegateApprovedVendorSourceReader((_, _, _) => Task.FromResult<IReadOnlyList<ApprovedVendor>>([])),
        PartInfoContextReader = new DelegatePartInfoContextReader((_, _, _) => Task.FromResult(new PartInfoContext("MFG-EXACT", "M", 3))),
        ComponentOrderSourceReader = new DelegateComponentOrderSourceReader((_, parts, _, _) =>
        {
            Assert.Equal(new[] { "EXACT" }, parts);
            return Task.FromResult<IReadOnlyList<ComponentOrderLine>>(noOrders ? [] : [Po("Supplier A", 1), Po("Supplier B", 2), Po("supplier a", 3)]);
        }),
        ComponentOrderEnrichmentReader = new DelegateComponentOrderEnrichmentReader((request, _) =>
        {
            if (request.ComponentIdentifiers.Count > 0)
            {
                Assert.Empty(request.SupplierIdentifiers);
                Assert.Equal(new[] { "EXACT" }, request.ComponentIdentifiers);
                return Task.FromResult(new ComponentOrderEnrichmentResult(new Dictionary<string, BuyerComment> { ["EXACT"] = new("Buyer comment\nsecond line", new(2022, 1, 31, 13, 30, 0), "jdoe") }, new Dictionary<string, ComponentOrderSupplierRisk>()));
            }
            Assert.Equal(new[] { "Supplier A", "Supplier B", "supplier a" }, request.SupplierIdentifiers);
            if (enrichmentFailure) throw new Exception("private shortage details");
            // Deliberately insensitive dictionary: composition must still match exact keys.
            return Task.FromResult(new ComponentOrderEnrichmentResult(new Dictionary<string, BuyerComment>(),
                new Dictionary<string, ComponentOrderSupplierRisk>(StringComparer.OrdinalIgnoreCase)
                { ["Supplier A"] = new(true, false), ["Supplier B"] = new(false, true) }));
        })
    };

    [Theory]
    [InlineData("SW")]
    [InlineData("KV")]
    public async Task StandaloneWithoutWorkspacePreservesExactValuesAndSupplierIdentities(string site)
    {
        await using var factory = Fixture(); using var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/reports/part-info?site={site}&part=EXACT");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var r = doc.RootElement;
        Assert.Equal(site, r.GetProperty("site").GetString());
        Assert.Equal("2022-01-31T13:30:00", r.GetProperty("commentRecordedAt").GetString());
        Assert.Equal("jdoe", r.GetProperty("commentAuthor").GetString());
        Assert.NotEqual(r.GetProperty("commentRecordedAt").GetString(), r.GetProperty("comment").GetProperty("acquiredAtUtc").GetString());
        Assert.DoesNotContain("commentHistory", r.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("0", r.GetProperty("master").GetProperty("standardCost").GetString());
        Assert.Equal(JsonValueKind.Null, r.GetProperty("master").GetProperty("qctc").ValueKind);
        Assert.Equal(JsonValueKind.Null, r.GetProperty("master").GetProperty("safetyStock").ValueKind);
        Assert.Equal("12.123456789", r.GetProperty("inventory").GetProperty("value").GetProperty("nonNetQuantityOnHand").GetString());
        Assert.Empty(r.GetProperty("locations").GetProperty("value").EnumerateArray());
        Assert.Empty(r.GetProperty("alternates").GetProperty("value").EnumerateArray());
        Assert.Equal("99999999999999999.123456789", r.GetProperty("orders").GetProperty("value")[0].GetProperty("openQuantity").GetString());
        var suppliers = r.GetProperty("suppliers").GetProperty("value");
        Assert.Equal(3, suppliers.GetArrayLength());
        Assert.True(suppliers[0].GetProperty("isCreditHold").GetBoolean()); Assert.False(suppliers[0].GetProperty("isCia").GetBoolean());
        Assert.False(suppliers[1].GetProperty("isCreditHold").GetBoolean()); Assert.True(suppliers[1].GetProperty("isCia").GetBoolean());
        Assert.Equal(JsonValueKind.Null, suppliers[2].GetProperty("isCreditHold").ValueKind);
        Assert.Contains("second line", r.GetProperty("comment").GetProperty("value").GetString());
    }

    [Fact]
    public async Task EmptyOrdersStillAcquireCommentsAndHaveNoSupplierCoverage()
    {
        await using var factory = Fixture(noOrders: true); using var client = factory.CreateClient();
        using var doc = JsonDocument.Parse(await client.GetStringAsync("/api/v1/reports/part-info?site=SW&part=EXACT"));
        Assert.Empty(doc.RootElement.GetProperty("suppliers").GetProperty("value").EnumerateArray());
        Assert.Contains("Buyer comment", doc.RootElement.GetProperty("comment").GetProperty("value").GetString());
    }

    [Fact]
    public async Task OptionalFailuresKeepOtherSectionsAndUnknownSupplierFlags()
    {
        await using var factory = Fixture(enrichmentFailure: true, locationsFailure: true); using var client = factory.CreateClient();
        var text = await client.GetStringAsync("/api/v1/reports/part-info?site=SW&part=EXACT");
        Assert.DoesNotContain("private", text);
        using var doc = JsonDocument.Parse(text); var r = doc.RootElement;
        Assert.Equal(3, r.GetProperty("orders").GetProperty("value").GetArrayLength());
        Assert.NotNull(r.GetProperty("locations").GetProperty("error").GetString());
        Assert.NotNull(r.GetProperty("suppliers").GetProperty("error").GetString());
        Assert.Equal(JsonValueKind.Null, r.GetProperty("suppliers").GetProperty("value")[0].GetProperty("isCia").ValueKind);
        Assert.Equal(JsonValueKind.Null, r.GetProperty("inventory").GetProperty("error").ValueKind);
        Assert.Contains("Buyer comment", r.GetProperty("comment").GetProperty("value").GetString());
    }

    [Theory]
    [InlineData("KS", "EXACT")]
    [InlineData("KTC", "EXACT")]
    [InlineData("SW", " ")]
    [InlineData("SW", "A\nB")]
    public async Task RejectsInvalidInputs(string site, string part)
    {
        await using var factory = Fixture(); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/reports/part-info?site={site}&part={Uri.EscapeDataString(part)}")).StatusCode);
    }

    [Fact]
    public async Task MissingMasterOversizedInputAndUnavailableAreDistinct()
    {
        await using var factory = Fixture(); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/reports/part-info?site=SW&part=MISSING")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/reports/part-info?site=SW&part={new string('A', 257)}")).StatusCode);
        await using var unavailable = new KstApiFactory(); using var offline = unavailable.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await offline.GetAsync("/api/v1/reports/part-info?site=SW&part=EXACT")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/workspaces/{Guid.NewGuid()}/components/EXACT")).StatusCode);
    }
}
