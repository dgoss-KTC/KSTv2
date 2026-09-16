using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.Bom;
using Kst.Application.LongTermShortages;
using Kst.Application.Mps;
using Kst.Domain.Bom;
using Kst.Domain.Common;
using Kst.Domain.LongTermShortages;
using Kst.Domain.Mps;
using Microsoft.Extensions.DependencyInjection;

namespace Kst.Api.IntegrationTests;

public sealed class LongTermShortagesEndpointTests
{
    [Fact]
    public async Task Get_ReturnsValidationAndNotFoundProblems()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var invalid = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/long-term-shortages?snapshotId=invalid");
        var missing = await client.GetAsync($"/api/v1/workspaces/{Guid.NewGuid()}/long-term-shortages?snapshotId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsConflictWhenMpsIsNotLoadedOrSnapshotChanged()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var notLoaded = await client.GetAsync(Route(assignmentId, Guid.NewGuid()));
        SeedSnapshot(factory, assignmentId);
        var changed = await client.GetAsync(Route(assignmentId, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Conflict, notLoaded.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsUnavailableWhenInitialSourceReadFails()
    {
        await using var factory = CreateFactory((_, _, _, _, _, _) => throw new InvalidOperationException("QAD unavailable"));
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await SeedWorkspaceAsync(factory, client);

        var response = await client.GetAsync(Route(assignmentId, snapshotId));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsProjectionAndExportUsesOnlyCachedDisplayedComponents()
    {
        await using var factory = CreateFactory((_, _, _, _, _, _) => Task.FromResult<IReadOnlyList<LongTermShortageInput>>([Input()]));
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await SeedWorkspaceAsync(factory, client);

        var loaded = await client.GetAsync(Route(assignmentId, snapshotId));
        var root = JsonDocument.Parse(await loaded.Content.ReadAsStringAsync()).RootElement;
        var export = await client.PostAsJsonAsync($"/api/v1/workspaces/{assignmentId}/long-term-shortages/export", new { snapshotId, componentParts = new[] { "COMP" } });
        var invalidExport = await client.PostAsJsonAsync($"/api/v1/workspaces/{assignmentId}/long-term-shortages/export", new { snapshotId, componentParts = new[] { "OTHER" } });

        Assert.Equal(HttpStatusCode.OK, loaded.StatusCode);
        Assert.Equal("COMP", root.GetProperty("rows")[0].GetProperty("componentPart").GetString());
        Assert.Equal("EA", root.GetProperty("rows")[0].GetProperty("unitOfMeasure").GetString());
        Assert.Equal(10m, root.GetProperty("rows")[0].GetProperty("displayOpeningQoh").GetDecimal());
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", export.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, invalidExport.StatusCode);
    }

    [Fact]
    public async Task Get_MapsPopulationOptionQueryParametersToTheProjection()
    {
        await using var factory = CreateFactory(EchoSource(), ClassifiedOccurrences());
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await SeedWorkspaceAsync(factory, client);

        var defaultResponse = await client.GetAsync(Route(assignmentId, snapshotId));
        var manufacturedResponse = await client.GetAsync($"{Route(assignmentId, snapshotId)}&includeManufacturedParts=true");
        var phantomResponse = await client.GetAsync($"{Route(assignmentId, snapshotId)}&includePhantoms=true");
        var bothResponse = await client.GetAsync($"{Route(assignmentId, snapshotId)}&includeManufacturedParts=true&includePhantoms=true");

        Assert.Equal(HttpStatusCode.OK, defaultResponse.StatusCode);
        Assert.Equal(["NORMAL"], await PartsAsync(defaultResponse));
        Assert.Equal(["MFG", "NORMAL"], await PartsAsync(manufacturedResponse));
        Assert.Equal(["NORMAL", "PHANTOM"], await PartsAsync(phantomResponse));
        Assert.Equal(["BOTH", "MFG", "NORMAL", "PHANTOM"], await PartsAsync(bothResponse));
    }

    [Fact]
    public async Task Export_MapsPopulationOptionRequestFields_AndRejectsAMismatchedProjection()
    {
        await using var factory = CreateFactory(EchoSource(), ClassifiedOccurrences());
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await SeedWorkspaceAsync(factory, client);

        // Load both option sets so a mismatch is a projection-content rejection, not a missing cache entry.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Route(assignmentId, snapshotId))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Route(assignmentId, snapshotId)}&includeManufacturedParts=true")).StatusCode);

        // MFG only exists in the manufactured projection; exporting it under the default options must fail.
        var mismatched = await client.PostAsJsonAsync($"/api/v1/workspaces/{assignmentId}/long-term-shortages/export", new { snapshotId, componentParts = new[] { "MFG" } });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, mismatched.StatusCode);

        // The same part exports under the option set that produced it.
        var matched = await client.PostAsJsonAsync($"/api/v1/workspaces/{assignmentId}/long-term-shortages/export", new { snapshotId, componentParts = new[] { "MFG" }, includeManufacturedParts = true });
        Assert.Equal(HttpStatusCode.OK, matched.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", matched.Content.Headers.ContentType!.MediaType);
    }

    private static KstApiFactory CreateFactory(
        Func<string, IReadOnlyDictionary<string, IReadOnlyList<string>>, DateOnly, DateOnly,
            IReadOnlySet<string>, CancellationToken, Task<IReadOnlyList<LongTermShortageInput>>>? read = null,
        IReadOnlyList<BomOccurrence>? bomOccurrences = null) => new()
        {
            BomSourceReader = new DelegateBomSourceReader((_, _, _, _) =>
                Task.FromResult(bomOccurrences ?? [new("1/COMP", 1, "COMP", "P", false, null, 1m, null)])),
            LongTermShortageSourceReader = new DelegateLongTermShortageSourceReader(read ??
            ((_, _, _, _, _, _) => Task.FromResult<IReadOnlyList<LongTermShortageInput>>([]))),
        };

    private static Func<string, IReadOnlyDictionary<string, IReadOnlyList<string>>, DateOnly, DateOnly, IReadOnlySet<string>, CancellationToken, Task<IReadOnlyList<LongTermShortageInput>>> EchoSource() =>
        (_, componentParents, _, _, _, _) => Task.FromResult<IReadOnlyList<LongTermShortageInput>>(componentParents.Keys.Select(part => new LongTermShortageInput(part, "EA", "P", "Component", false, 14, "Planner", "BP", 10m,
            SafetyStockState.Resolved, 5m, ["PARENT"], [], [], [])).ToList());

    private static IReadOnlyList<BomOccurrence> ClassifiedOccurrences() =>
    [
        new("1/NORMAL", 1, "NORMAL", "P", false, null, 1m, null, null, "P"),
        new("2/MFG", 1, "MFG", "M", false, null, 1m, null, null, "M"),
        new("3/PHANTOM", 1, "PHANTOM", "P", true, null, 1m, null, null, "P"),
        new("4/BOTH", 1, "BOTH", "M", true, null, 1m, null, null, "M"),
    ];

    private static async Task<string[]> PartsAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("rows").EnumerateArray().Select(element => element.GetProperty("componentPart").GetString()!).ToList()
            .OrderBy(value => value).ToArray();

    private static async Task<(Guid AssignmentId, Guid SnapshotId)> SeedWorkspaceAsync(KstApiFactory factory, HttpClient client)
    {
        var assignmentId = await CreateWorkspaceAsync(client);
        SeedSnapshot(factory, assignmentId);
        return (assignmentId, factory.Services.GetRequiredService<IMpsSnapshotStore>().GetState(assignmentId).Snapshot!.Id.Value);
    }

    private static void SeedSnapshot(KstApiFactory factory, Guid assignmentId) => factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(assignmentId,
        new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, "SW", [new MpsResolvedPart("PARENT", "")], []));

    private static async Task<Guid> CreateWorkspaceAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", new { site = "SW", parentParts = new[] { "PARENT" }, isTemporary = false });
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("assignmentId").GetGuid();
    }

    private static string Route(Guid assignmentId, Guid snapshotId) => $"/api/v1/workspaces/{assignmentId}/long-term-shortages?snapshotId={snapshotId}";

        private static LongTermShortageInput Input() => new("COMP", "EA", "P", "Component", false, 14, "Planner", "BP", 10m,
        SafetyStockState.Resolved, 5m, ["PARENT"], [], [], []);
}
