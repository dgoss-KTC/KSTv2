using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.Bom;
using Kst.Application.ComponentOrders;
using Kst.Application.Mps;
using Kst.Domain.Bom;
using Kst.Domain.Common;
using Kst.Domain.ComponentOrders;
using Kst.Domain.Mps;
using Microsoft.Extensions.DependencyInjection;

namespace Kst.Api.IntegrationTests;

public sealed class ComponentOrdersEndpointTests
{
    private const string Parent = "BUILD";

    [Fact]
    public async Task GetComponentOrders_Returns_400ValidationProblem_WhenSnapshotIdMissingOrInvalid()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);

        var missing = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/component-orders");
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Contains("snapshotId", await missing.Content.ReadAsStringAsync());

        var invalid = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/component-orders?snapshotId=not-a-guid");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task GetComponentOrders_Returns_404_ForUnknownWorkspace()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/workspaces/{Guid.NewGuid()}/component-orders?snapshotId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetComponentOrders_Returns_409MpsNotLoaded_WhenNoSnapshotExists()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/component-orders?snapshotId={Guid.NewGuid()}");
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("MPS data not loaded", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetComponentOrders_Returns_409SnapshotChanged_ForSupersededId()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);
        SeedSnapshot(factory, assignmentId, Parent); // current snapshot differs from the requested id

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/component-orders?snapshotId={Guid.NewGuid()}");
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Snapshot changed", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetComponentOrders_Returns_503Unavailable_WhenReaderFails()
    {
        await using var factory = CreateFactory(poLines: (_, _, _) => throw new InvalidOperationException("QAD down"));
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await SeedWorkspaceAsync(factory, client);

        var response = await client.GetAsync(Route(assignmentId, snapshotId));
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Component orders unavailable", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetComponentOrders_Returns_GroupedResult_With_AcceptedFieldMapping()
    {
        await using var factory = CreateFactory(poLines: (_, _, _) => Task.FromResult<IReadOnlyList<ComponentOrderLine>>(new[]
        {
            new ComponentOrderLine("COMP", "Hex bolt M8", 14, "2076185", 3, new DateOnly(2026, 9, 30), 12.5m, true, "ACME Industrial", "jharroun@KTC", "VB-8841", false, "PO2076185-3"),
            new ComponentOrderLine("COMP", "Hex bolt M8", 14, "2076186", 1, null, 4m, null, "ACME Industrial", null, null, true, null), // missing due → earliest under accepted ordering
        }));
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await SeedWorkspaceAsync(factory, client);

        var response = await client.GetAsync(Route(assignmentId, snapshotId));
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(snapshotId.ToString(), root.GetProperty("snapshotId").GetString());
        var group = root.GetProperty("groups")[0];
        Assert.Equal("COMP", group.GetProperty("componentPart").GetString());

        // The missing-due line is the display (earliest) line under the accepted ordering.
        var displayLine = group.GetProperty("displayLine");
        Assert.Equal(JsonValueKind.Null, displayLine.GetProperty("dueDate").ValueKind);
        Assert.True(displayLine.GetProperty("isKss").GetBoolean());
        Assert.Equal(JsonValueKind.Null, displayLine.GetProperty("confirmed").ValueKind); // null stays null — never fabricated

        var additional = group.GetProperty("additionalLines")[0];
        Assert.Equal("2076185", additional.GetProperty("poNumber").GetString());
        Assert.Equal(3, additional.GetProperty("poLine").GetInt32());
        Assert.Equal("2026-09-30", additional.GetProperty("dueDate").GetString());
        Assert.Equal(12.5m, additional.GetProperty("openQuantity").GetDecimal()); // raw value — no UOM conversion
        Assert.True(additional.GetProperty("confirmed").GetBoolean());
        Assert.Equal("ACME Industrial", additional.GetProperty("supplierDisplay").GetString());
        Assert.Equal("jharroun@KTC", additional.GetProperty("buyerDisplay").GetString());
        Assert.Equal("VB-8841", additional.GetProperty("manufacturerItem").GetString());
        Assert.Equal(14, additional.GetProperty("leadTimeDays").GetInt32()); // raw days — weeks display is frontend-owned
    }

    [Fact]
    public async Task GetComponentOrders_Exposes_AvailableEnrichmentFacts_WithoutSourceMetadata()
    {
        await using var factory = CreateFactory(
            poLines: (_, _, _) => Task.FromResult<IReadOnlyList<ComponentOrderLine>>([
                new("COMP", "Hex bolt M8", 14, "2076185", 3, null, 12.5m, true, "ACME", null, null, false, null, "V-1"),
                new("COMP", "Hex bolt M8", 14, "2076186", 4, null, 4m, true, "Other", null, null, false, null, "V-2"),
                new("comp2", "Other", null, "2076187", 1, null, 1m, null, null, null, null, false, null, "V-3")
            ]),
            enrichment: (_, _) => Task.FromResult(new ComponentOrderEnrichmentResult(
                new Dictionary<string, string?>(StringComparer.Ordinal) { ["COMP"] = "First line\nSecond line" },
                new Dictionary<string, ComponentOrderSupplierRisk>(StringComparer.Ordinal)
                {
                    ["V-1"] = new(true, false),
                    ["V-2"] = new(false, true)
                })));
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await SeedWorkspaceAsync(factory, client, Parent, "BUILD2");

        var response = await client.GetAsync(Route(assignmentId, snapshotId));
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var display = root.GetProperty("groups")[0].GetProperty("displayLine");
        var additional = root.GetProperty("groups")[0].GetProperty("additionalLines")[0];
        var noMatch = root.GetProperty("groups")[1].GetProperty("displayLine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Available", root.GetProperty("enrichmentAvailability").GetString());
        Assert.True(display.GetProperty("isCreditHold").GetBoolean());
        Assert.False(display.GetProperty("isCia").GetBoolean());
        Assert.Equal("First line\nSecond line", display.GetProperty("currentComments").GetString());
        Assert.False(additional.GetProperty("isCreditHold").GetBoolean());
        Assert.True(additional.GetProperty("isCia").GetBoolean());
        Assert.Equal("First line\nSecond line", additional.GetProperty("currentComments").GetString());
        Assert.Equal(JsonValueKind.Null, noMatch.GetProperty("isCreditHold").ValueKind);
        Assert.Equal(JsonValueKind.Null, noMatch.GetProperty("isCia").ValueKind);
        Assert.Equal(JsonValueKind.Null, noMatch.GetProperty("currentComments").ValueKind);
        Assert.False(root.ToString().Contains("supplierIdentifier", StringComparison.OrdinalIgnoreCase));
        Assert.False(root.ToString().Contains("shortages", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetComponentOrders_Returns_QadGroups_With_UnavailableEnrichment()
    {
        await using var factory = CreateFactory(
            poLines: (_, _, _) => Task.FromResult<IReadOnlyList<ComponentOrderLine>>([
                new("COMP", "Hex bolt M8", 14, "2076185", 3, null, 12.5m, true, "ACME", null, null, false, null, "V-1")
            ]),
            enrichment: (_, _) => throw new InvalidOperationException("Shortages unavailable"));
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await SeedWorkspaceAsync(factory, client);

        var response = await client.GetAsync(Route(assignmentId, snapshotId));
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var line = root.GetProperty("groups")[0].GetProperty("displayLine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Unavailable", root.GetProperty("enrichmentAvailability").GetString());
        Assert.Equal("2076185", line.GetProperty("poNumber").GetString());
        Assert.Equal(JsonValueKind.Null, line.GetProperty("isCreditHold").ValueKind);
        Assert.Equal(JsonValueKind.Null, line.GetProperty("isCia").ValueKind);
        Assert.Equal(JsonValueKind.Null, line.GetProperty("currentComments").ValueKind);
    }

    [Fact]
    public async Task GetComponentOrders_Returns_EmptyGroups_WhenNoQualifyingLines()
    {
        await using var factory = CreateFactory(poLines: (_, _, _) => Task.FromResult<IReadOnlyList<ComponentOrderLine>>([]));
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await SeedWorkspaceAsync(factory, client);

        var response = await client.GetAsync(Route(assignmentId, snapshotId));
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(root.GetProperty("groups").EnumerateArray());
    }

    [Fact]
    public async Task GetComponentOrders_Passes_BomDerivedComponentScope_ToTheReader()
    {
        var seenComponents = new List<IReadOnlyList<string>>();
        await using var factory = CreateFactory(poLines: (_, components, _) =>
        {
            seenComponents.Add(components);
            return Task.FromResult<IReadOnlyList<ComponentOrderLine>>([]);
        });
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await SeedWorkspaceAsync(factory, client, Parent, "BUILD2");

        var response = await client.GetAsync(Route(assignmentId, snapshotId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // The BOM of every resolved parent is exploded; components deduplicated case-insensitively.
        var components = Assert.Single(seenComponents);
        Assert.Equal(["COMP", "comp2"], components.Select(c => c).ToList());
    }

    private static KstApiFactory CreateFactory(
        Func<string, IReadOnlyList<string>, DateOnly, Task<IReadOnlyList<ComponentOrderLine>>>? poLines = null,
        Func<ComponentOrderEnrichmentRequest, CancellationToken, Task<ComponentOrderEnrichmentResult>>? enrichment = null)
    {
        var bomReader = new DelegateBomSourceReader((_, parentPart, _, _) =>
            Task.FromResult<IReadOnlyList<BomOccurrence>>(parentPart == Parent
                ? [new("1/COMP", 1, "COMP", "P", false, "Component", 1m, null)]
                : [new("1/comp2", 1, "comp2", "P", false, "Other", 1m, null)]));

        Func<string, IReadOnlyList<string>, DateOnly, Task<IReadOnlyList<ComponentOrderLine>>> defaultPoLines =
            (_, _, _) => Task.FromResult<IReadOnlyList<ComponentOrderLine>>([]);
        var poReader = new DelegateComponentOrderSourceReader((site, components, today, _) =>
            (poLines ?? defaultPoLines)(site, components, today));
        var enrichmentReader = new DelegateComponentOrderEnrichmentReader(
            enrichment ?? ((_, _) => Task.FromResult(ComponentOrderEnrichmentResult.Empty)));

        return new KstApiFactory
        {
            BomSourceReader = bomReader,
            ComponentOrderSourceReader = poReader,
            ComponentOrderEnrichmentReader = enrichmentReader,
        };
    }

    private static string Route(Guid assignmentId, Guid snapshotId) =>
        $"/api/v1/workspaces/{assignmentId}/component-orders?snapshotId={snapshotId}";

    private static void SeedSnapshot(KstApiFactory factory, Guid assignmentId, params string[] parents)
    {
        var snapshot = new MpsSnapshot(
            SnapshotId.New(), DateTimeOffset.UtcNow, "SW",
            parents.Select(parent => new MpsResolvedPart(parent, "Description")).ToList(), []);
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(assignmentId, snapshot);
    }

    private static async Task<(Guid AssignmentId, Guid SnapshotId)> SeedWorkspaceAsync(KstApiFactory factory, HttpClient client, params string[] parents)
    {
        var effective = parents.Length == 0 ? new[] { Parent } : parents;
        var assignmentId = await CreateWorkspaceAsync(client, effective);
        SeedSnapshot(factory, assignmentId, effective);
        return (assignmentId, factory.Services.GetRequiredService<IMpsSnapshotStore>().GetState(assignmentId).Snapshot!.Id.Value);
    }

    private static async Task<Guid> CreateWorkspaceAsync(HttpClient client, params string[] parentParts)
    {
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", new { site = "SW", parentParts, isTemporary = false });
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return root.GetProperty("assignmentId").GetGuid();
    }
}
