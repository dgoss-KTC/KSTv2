using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.Bom;
using Kst.Application.Inventory;
using Kst.Application.Mps;
using Kst.Domain.Bom;
using Kst.Domain.Common;
using Kst.Domain.Inventory;
using Kst.Domain.Mps;
using Microsoft.Extensions.DependencyInjection;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// Stage 8D.3 BOM endpoint integration tests. QAD is never configured in the test host, so the
/// BOM/inventory reader bridges are replaced with deterministic fakes through
/// <see cref="KstApiFactory"/> (Amendment 3) and the singleton <see cref="IMpsSnapshotStore"/>
/// is seeded at runtime after the workspace exists — no live QAD is required for any path here.
/// </summary>
public sealed class BomEndpointTests
{
    private const string Parent = "ABC100";

    private static BomOccurrence Occ(string key, int level, string part, string pm) =>
        new(key, level, part, pm, false, $"DESC {part}", 1m, 0m);

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
    public async Task GetBom_Returns404_For_Unknown_Workspace()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/workspaces/{Guid.NewGuid()}/parts/{Parent}/bom");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBom_Returns409_With_Stable_Title_When_Mps_Not_Loaded()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/parts/{Parent}/bom");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var doc = JsonDocument.Parse(body);
        Assert.Equal("MPS data not loaded", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetBom_Returns400_When_Parent_Blank()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/parts/%20/bom");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBom_Returns200_With_Bom_Dto()
    {
        var bom = new FakeBomReader
        {
            Occurrences = new[]
            {
                Occ("k1", 1, "P1", "P"),
                Occ("k2", 2, "P2", "N"),
                Occ("k3", 2, "P3", "M"),
            }
        };
        await using var factory = new KstApiFactory { BomSourceReader = bom, PartInventoryReader = new FakeInventoryReader() };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);
        SeedMps(factory, assignmentId, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/parts/{Parent}/bom");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("SW", root.GetProperty("site").GetString());
        Assert.Equal(Parent, root.GetProperty("parentPart").GetString());
        Assert.Equal(DateOnly.FromDateTime(DateTime.Now.Date).ToString("yyyy-MM-dd"), root.GetProperty("effectiveDate").GetString());
        Assert.False(root.GetProperty("isStale").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("warning").ValueKind);
        Assert.False(string.IsNullOrEmpty(root.GetProperty("loadedAtUtc").GetString()));

        var lines = root.GetProperty("lines").EnumerateArray().ToList();
        Assert.Equal(2, lines.Count);
        Assert.Equal("P1", lines[0].GetProperty("componentPart").GetString());
        Assert.Equal(1, lines[0].GetProperty("level").GetInt32());
        Assert.Equal("k3", lines[1].GetProperty("occurrenceKey").GetString());

        // Shared Site + Part inventory is composed in (RMA 999 is never exposed).
        Assert.Equal(10, lines[0].GetProperty("netQuantityOnHand").GetDecimal());
        Assert.Equal(5, lines[0].GetProperty("nonNetQuantityOnHand").GetDecimal());
        Assert.Equal(1, bom.CallCount);
        Assert.Equal("SW", bom.LastSite);
        Assert.Equal(Parent, bom.LastParent);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Now.Date), bom.LastEffectiveDate);
    }

    [Fact]
    public async Task GetBom_StructuralEmpty_Returns200_EmptyLines()
    {
        var inventory = new FakeInventoryReader();
        await using var factory = new KstApiFactory { BomSourceReader = new FakeBomReader(), PartInventoryReader = inventory };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);
        SeedMps(factory, assignmentId, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/parts/{Parent}/bom");
        var body = await response.Content.ReadAsStringAsync();
        var root = (JsonElement)JsonSerializer.Deserialize(body, typeof(JsonElement))!;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(root.GetProperty("lines").GetArrayLength() == 0);
        Assert.Equal(0, inventory.CallCount);
    }

    [Fact]
    public async Task GetBom_AllHidden_Returns200_EmptyLines()
    {
        var bom = new FakeBomReader
        {
            Occurrences = new[]
            {
                Occ("k1", 1, "P1", "N"),
                Occ("k2", 2, "P2", "S"),
            }
        };
        var inventory = new FakeInventoryReader();
        await using var factory = new KstApiFactory { BomSourceReader = bom, PartInventoryReader = inventory };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);
        SeedMps(factory, assignmentId, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/parts/{Parent}/bom");
        var body = await response.Content.ReadAsStringAsync();
        var root = (JsonElement)JsonSerializer.Deserialize(body, typeof(JsonElement))!;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(root.GetProperty("lines").GetArrayLength() == 0);
        Assert.Equal(0, inventory.CallCount);
    }

    [Fact]
    public async Task GetBom_Preserves_Order_And_OccurrenceKeys()
    {
        var bom = new FakeBomReader
        {
            Occurrences = new[]
            {
                Occ("k1", 1, "B", "P"),
                Occ("k2", 1, "A", "N"),
                Occ("k3", 2, "C", "M"),
                Occ("k4", 1, "D", "P"),
            }
        };
        await using var factory = new KstApiFactory { BomSourceReader = bom, PartInventoryReader = new FakeInventoryReader() };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);
        SeedMps(factory, assignmentId, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/parts/{Parent}/bom");
        var body = await response.Content.ReadAsStringAsync();
        var root = (JsonElement)JsonSerializer.Deserialize(body, typeof(JsonElement))!;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var keys = root.GetProperty("lines").EnumerateArray()
            .Select(l => l.GetProperty("occurrenceKey").GetString()).ToList();
        Assert.Equal(new[] { "k1", "k3", "k4" }, keys);
    }

    [Fact]
    public async Task GetBom_Returns503_When_StructuralReader_Fails()
    {
        var bom = new FakeBomReader { Error = new InvalidOperationException("QAD database connectivity failed.") };
        await using var factory = new KstApiFactory { BomSourceReader = bom, PartInventoryReader = new FakeInventoryReader() };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);
        SeedMps(factory, assignmentId, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/parts/{Parent}/bom");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("BOM information unavailable", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetBom_Returns503_When_InventoryReader_Fails()
    {
        var bom = new FakeBomReader { Occurrences = new[] { Occ("k1", 1, "P1", "P") } };
        var inventory = new FakeInventoryReader { Error = new InvalidOperationException("QAD database connectivity failed.") };
        await using var factory = new KstApiFactory { BomSourceReader = bom, PartInventoryReader = inventory };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);
        SeedMps(factory, assignmentId, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/parts/{Parent}/bom");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("BOM information unavailable", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetBom_Returns404_When_Parent_Out_Of_Scope()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);
        SeedMps(factory, assignmentId, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/parts/ZZZ999/bom");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Part not in workspace scope", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetBom_SerializedLine_Has_Net_And_NonNet_But_No_Rma()
    {
        var bom = new FakeBomReader { Occurrences = new[] { Occ("k1", 1, "P1", "P") } };
        await using var factory = new KstApiFactory { BomSourceReader = bom, PartInventoryReader = new FakeInventoryReader() };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);
        SeedMps(factory, assignmentId, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/parts/{Parent}/bom");
        var body = await response.Content.ReadAsStringAsync();
        var root = (JsonElement)JsonSerializer.Deserialize(body, typeof(JsonElement))!;
        var line = root.GetProperty("lines")[0];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var propertyNames = line.EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains("netQuantityOnHand", propertyNames);
        Assert.Contains("nonNetQuantityOnHand", propertyNames);
        Assert.DoesNotContain(propertyNames, name => name.Contains("rma", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetBom_Returns200Stale_When_Reload_Fails_With_Compatible_Cache()
    {
        var bom = new FakeBomReader { Occurrences = new[] { Occ("k1", 1, "P1", "P") } };
        await using var factory = new KstApiFactory { BomSourceReader = bom, PartInventoryReader = new FakeInventoryReader() };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);
        SeedMps(factory, assignmentId, Parent);

        // Fresh load succeeds and populates the cache.
        var first = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/parts/{Parent}/bom");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // A new MPS generation forces a reload...
        SeedMps(factory, assignmentId, Parent);
        // ...and the reader now fails: the same-site/same-effective-date entry is served stale.
        bom.Error = new InvalidOperationException("QAD database connectivity failed.");
        var second = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/parts/{Parent}/bom");
        var body = await second.Content.ReadAsStringAsync();
        var root = (JsonElement)JsonSerializer.Deserialize(body, typeof(JsonElement))!;

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.True(root.GetProperty("isStale").GetBoolean());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("warning").GetString()));
        Assert.True(root.GetProperty("lines").GetArrayLength() > 0);
    }

    /// <summary>Deterministic <see cref="IBomSourceReader"/> fake for the test host.</summary>
    private sealed class FakeBomReader : IBomSourceReader
    {
        public IReadOnlyList<BomOccurrence> Occurrences { get; init; } = [];
        public Exception? Error { get; set; }
        public int CallCount { get; private set; }
        public string? LastSite { get; private set; }
        public string? LastParent { get; private set; }
        public DateOnly? LastEffectiveDate { get; private set; }

        public Task<IReadOnlyList<BomOccurrence>> ReadAsync(
            string site, string parentPart, DateOnly effectiveDate, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastSite = site;
            LastParent = parentPart;
            LastEffectiveDate = effectiveDate;
            if (Error is not null)
                throw Error;
            return Task.FromResult(Occurrences);
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
