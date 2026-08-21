using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.ComponentDetail;
using Kst.Application.Inventory;
using Kst.Application.Mps;
using Kst.Domain.ComponentDetail;
using Kst.Domain.Common;
using Kst.Domain.Inventory;
using Kst.Domain.Mps;
using Microsoft.Extensions.DependencyInjection;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// Stage 8D.5 Component Detail endpoint integration tests. QAD is never configured in the test
/// host, so the source/inventory reader bridges are replaced with deterministic fakes through
/// <see cref="KstApiFactory"/> and the singleton <see cref="IMpsSnapshotStore"/> is seeded at
/// runtime after the workspace exists — no live QAD is required for any path here.
/// </summary>
public sealed class ComponentDetailEndpointTests
{
    private const string Parent = "ABC100";
    private const string Component = "COMP1";

    private static ComponentSourceFacts Facts(string part = Component) => new(
        ComponentPart: part,
        Description: "WIDGET",
        PartStatusCode: "C",
        IosCode: "1234",
        StandardCost: 1.5m,
        Qctc: 2.5m,
        TimeFence: 14,
        SafetyTime: 3m,
        SafetyStock: 100m,
        BuyerPlanner: "JSMITH",
        PurchaseLeadTimeDays: 21,
        InspectionLeadTimeDays: 2,
        CumulativeLeadTimeDays: 30,
        MinimumOrderQuantity: 500m,
        OrderMultiple: 100m);

    private static async Task<Guid> CreateWorkspaceAsync(HttpClient client, params string[] parentParts)
    {
        var request = new { site = "SW", parentParts, isTemporary = false };
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("assignmentId").GetGuid();
    }

    private static void SeedMps(KstApiFactory factory, Guid assignmentId, params string[] parentParts)
    {
        var store = factory.Services.GetRequiredService<IMpsSnapshotStore>();
        var resolved = parentParts.Select(p => new MpsResolvedPart(p, "Description")).ToList();
        var snapshot = new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, "SW", resolved, []);
        store.SetLoaded(assignmentId, snapshot);
    }

    [Fact]
    public async Task GetComponentDetail_Returns404_For_Unknown_Workspace()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/workspaces/{Guid.NewGuid()}/components/{Component}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetComponentDetail_Returns409_With_Stable_Title_When_Mps_Not_Loaded()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/components/{Component}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var doc = JsonDocument.Parse(body);
        Assert.Equal("MPS data not loaded", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetComponentDetail_Returns400_When_ComponentPart_Blank()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/components/%20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetComponentDetail_Returns404_When_Source_Reader_Has_No_PtMstr_Row()
    {
        var source = new FakeComponentSourceReader { Facts = null };
        await using var factory = new KstApiFactory { ComponentSourceReader = source, PartInventoryReader = new FakeInventoryReader() };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);
        SeedMps(factory, assignmentId, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/components/{Component}");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Component not found", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetComponentDetail_Returns200_With_Component_Detail_Dto()
    {
        var source = new FakeComponentSourceReader { Facts = Facts() };
        await using var factory = new KstApiFactory { ComponentSourceReader = source, PartInventoryReader = new FakeInventoryReader() };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);
        SeedMps(factory, assignmentId, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/components/{Component}");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("SW", root.GetProperty("site").GetString());
        Assert.Equal(Component, root.GetProperty("componentPart").GetString());
        Assert.Equal("WIDGET", root.GetProperty("description").GetString());
        Assert.Equal("C", root.GetProperty("partStatusCode").GetString());
        Assert.Equal("CURRENT", root.GetProperty("partStatusDescription").GetString());
        Assert.Equal(10m, root.GetProperty("netQuantityOnHand").GetDecimal());
        Assert.Equal(5m, root.GetProperty("nonNetQuantityOnHand").GetDecimal());
        Assert.Equal(1.5m, root.GetProperty("standardCost").GetDecimal());
        Assert.Equal(2.5m, root.GetProperty("qctc").GetDecimal());
        Assert.False(root.GetProperty("isStale").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("warning").ValueKind);
        Assert.False(string.IsNullOrEmpty(root.GetProperty("loadedAtUtc").GetString()));
    }

    [Fact]
    public async Task GetComponentDetail_SerializedDto_Has_No_Rma_Field()
    {
        var source = new FakeComponentSourceReader { Facts = Facts() };
        await using var factory = new KstApiFactory { ComponentSourceReader = source, PartInventoryReader = new FakeInventoryReader() };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);
        SeedMps(factory, assignmentId, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/components/{Component}");
        var body = await response.Content.ReadAsStringAsync();
        var root = (JsonElement)JsonSerializer.Deserialize(body, typeof(JsonElement))!;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var propertyNames = root.EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains("netQuantityOnHand", propertyNames);
        Assert.Contains("nonNetQuantityOnHand", propertyNames);
        Assert.DoesNotContain(propertyNames, name => name.Contains("rma", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetComponentDetail_Returns503_When_SourceReader_Fails()
    {
        var source = new FakeComponentSourceReader { Error = new InvalidOperationException("QAD database connectivity failed.") };
        await using var factory = new KstApiFactory { ComponentSourceReader = source, PartInventoryReader = new FakeInventoryReader() };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);
        SeedMps(factory, assignmentId, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/components/{Component}");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Component information unavailable", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetComponentDetail_Returns503_When_InventoryReader_Fails()
    {
        var source = new FakeComponentSourceReader { Facts = Facts() };
        var inventory = new FakeInventoryReader { Error = new InvalidOperationException("QAD database connectivity failed.") };
        await using var factory = new KstApiFactory { ComponentSourceReader = source, PartInventoryReader = inventory };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);
        SeedMps(factory, assignmentId, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/components/{Component}");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Component information unavailable", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetComponentDetail_Returns200Stale_When_Reload_Fails_With_Compatible_Cache()
    {
        var source = new FakeComponentSourceReader { Facts = Facts() };
        await using var factory = new KstApiFactory { ComponentSourceReader = source, PartInventoryReader = new FakeInventoryReader() };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);
        SeedMps(factory, assignmentId, Parent);

        // Fresh load succeeds and populates the cache.
        var first = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/components/{Component}");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // A new MPS generation forces a reload...
        SeedMps(factory, assignmentId, Parent);
        // ...and the reader now fails: the same-site entry is served stale.
        source.Error = new InvalidOperationException("QAD database connectivity failed.");
        var second = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/components/{Component}");
        var body = await second.Content.ReadAsStringAsync();
        var root = (JsonElement)JsonSerializer.Deserialize(body, typeof(JsonElement))!;

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.True(root.GetProperty("isStale").GetBoolean());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("warning").GetString()));
        Assert.Equal(Component, root.GetProperty("componentPart").GetString());
    }

    /// <summary>Deterministic <see cref="IComponentSourceReader"/> fake for the test host.</summary>
    private sealed class FakeComponentSourceReader : IComponentSourceReader
    {
        public ComponentSourceFacts? Facts { get; set; }
        public Exception? Error { get; set; }
        public int CallCount { get; private set; }
        public string? LastSite { get; private set; }
        public string? LastComponentPart { get; private set; }

        public Task<ComponentSourceFacts?> ReadAsync(
            string site, string componentPart, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastSite = site;
            LastComponentPart = componentPart;
            if (Error is not null)
                throw Error;
            return Task.FromResult(Facts);
        }
    }

    /// <summary>
    /// Deterministic <see cref="IPartInventoryReader"/> fake: one summary per requested part
    /// (net 10 / non-net 5 / RMA 999) — RMA is present in the summary precisely to prove the
    /// endpoint contract never exposes it.
    /// </summary>
    private sealed class FakeInventoryReader : IPartInventoryReader
    {
        public Exception? Error { get; set; }
        public int CallCount { get; private set; }
        public IReadOnlyList<string>? LastPartNumbers { get; private set; }

        public Task<IReadOnlyList<PartInventorySummary>> ReadSummariesAsync(
            string site, IReadOnlyList<string> partNumbers, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastPartNumbers = partNumbers;
            if (Error is not null)
                throw Error;
            var summaries = partNumbers
                .Select(p => new PartInventorySummary(site, p, 10m, 5m, 999m))
                .ToList();
            return Task.FromResult<IReadOnlyList<PartInventorySummary>>(summaries);
        }
    }
}
