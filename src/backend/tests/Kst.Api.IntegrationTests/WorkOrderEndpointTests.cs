using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// QAD is never configured in the test environment (Testing appsettings has no QadDatabase server),
/// so a workspace's MPS snapshot can never actually load here. These tests exercise the
/// workspace-not-found / MPS-not-loaded / validation Problem Details paths reachable without a live
/// QAD environment. See <c>PartDetailEndpointTests</c> for the equivalent pattern this mirrors.
/// </summary>
public sealed class WorkOrderEndpointTests
{
    private static async Task<Guid> CreateWorkspaceAsync(HttpClient client)
    {
        var request = new { site = "SW", parentParts = new[] { "ABC100" }, isTemporary = false };
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("assignmentId").GetGuid();
    }

    // -- Bucket work orders ------------------------------------------------

    [Fact]
    public async Task GetBucketWorkOrders_Returns404_For_Unknown_Workspace()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/v1/workspaces/{Guid.NewGuid()}/work-orders/bucket?snapshotId={Guid.NewGuid()}&parentPart=ABC100&bucketKind=weekly");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBucketWorkOrders_Returns409_When_Mps_Not_Loaded()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.GetAsync(
            $"/api/v1/workspaces/{assignmentId}/work-orders/bucket?snapshotId={Guid.NewGuid()}&parentPart=ABC100&bucketKind=weekly");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetBucketWorkOrders_Returns400_When_SnapshotId_Missing()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.GetAsync(
            $"/api/v1/workspaces/{assignmentId}/work-orders/bucket?parentPart=ABC100&bucketKind=weekly");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBucketWorkOrders_Returns400_When_ParentPart_Missing()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.GetAsync(
            $"/api/v1/workspaces/{assignmentId}/work-orders/bucket?snapshotId={Guid.NewGuid()}&bucketKind=weekly");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBucketWorkOrders_Returns400_When_BucketKind_Invalid()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.GetAsync(
            $"/api/v1/workspaces/{assignmentId}/work-orders/bucket?snapshotId={Guid.NewGuid()}&parentPart=ABC100&bucketKind=notARealBucket");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(73)]
    public async Task GetBucketWorkOrders_Returns400_For_HorizonWeeks_Out_Of_Range(int horizonWeeks)
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.GetAsync(
            $"/api/v1/workspaces/{assignmentId}/work-orders/bucket?snapshotId={Guid.NewGuid()}&parentPart=ABC100&bucketKind=weekly&horizonWeeks={horizonWeeks}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -- Material lines ------------------------------------------------

    [Fact]
    public async Task GetWorkOrderMaterialLines_Returns404_For_Unknown_Workspace()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/v1/workspaces/{Guid.NewGuid()}/work-orders/WO-1001/material?snapshotId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkOrderMaterialLines_Returns409_When_Mps_Not_Loaded()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.GetAsync(
            $"/api/v1/workspaces/{assignmentId}/work-orders/WO-1001/material?snapshotId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkOrderMaterialLines_Returns400_When_SnapshotId_Missing()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/work-orders/WO-1001/material");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkOrderMaterialLines_Returns400_When_SnapshotId_Not_A_Guid()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.GetAsync(
            $"/api/v1/workspaces/{assignmentId}/work-orders/WO-1001/material?snapshotId=not-a-guid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -- Candidates ------------------------------------------------

    [Fact]
    public async Task GetWorkOrderCandidates_Returns404_For_Unknown_Workspace()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/v1/workspaces/{Guid.NewGuid()}/work-orders/candidates" +
            $"?snapshotId={Guid.NewGuid()}&immediateParentWoid=WO-PARENT&componentPart=COMP1&targetDepth=2");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkOrderCandidates_Returns409_When_Mps_Not_Loaded()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.GetAsync(
            $"/api/v1/workspaces/{assignmentId}/work-orders/candidates" +
            $"?snapshotId={Guid.NewGuid()}&immediateParentWoid=WO-PARENT&componentPart=COMP1&targetDepth=2");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkOrderCandidates_Returns400_When_ComponentPart_Missing()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client);

        var response = await client.GetAsync(
            $"/api/v1/workspaces/{assignmentId}/work-orders/candidates" +
            $"?snapshotId={Guid.NewGuid()}&immediateParentWoid=WO-PARENT&targetDepth=2");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task GetWorkOrderCandidates_Returns400_For_Depth_Outside_Level_2_And_3(int targetDepth)
    {
        // Depth is validated before workspace resolution, so an unknown workspace still yields 400
        // (not 404) for an out-of-range depth.
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/v1/workspaces/{Guid.NewGuid()}/work-orders/candidates" +
            $"?snapshotId={Guid.NewGuid()}&immediateParentWoid=WO-PARENT&componentPart=COMP1&targetDepth={targetDepth}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -- Contract serialization ---------------------------------------------
    //
    // QAD is never configured in this test environment (see the class-level remark above), so the
    // 200 OK / Loaded outcome for these endpoints cannot be reached through a live HTTP round trip
    // here — that happy-path wire-format confirmation is covered by Checkpoint 7D.11 Live-QAD
    // Validation instead. What CAN be verified without QAD is that the response DTOs serialize with
    // the same camelCase policy the running app actually applies to every response (pulled from the
    // host's own configured `JsonOptions`, not a duplicated assumption of it).

    [Fact]
    public async Task WorkOrder_Response_Dtos_Serialize_With_CamelCase_Property_Names()
    {
        await using var factory = new KstApiFactory();
        var serializerOptions = factory.Services
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()
            .Value.SerializerOptions;

        var bucketJson = JsonSerializer.Serialize(
            new Kst.Api.Dtos.WorkOrderBucketResponseDto(
                Guid.NewGuid().ToString(),
                [
                    new Kst.Api.Dtos.WorkOrderSummaryDto(
                        "ABC100", "1001", "released", 100m, 40m, 60m,
                        new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 15), "SO-4521",
                        new Kst.Api.Dtos.KittingSummaryDto(4, 2, 50m))
                ]),
            serializerOptions);
        AssertHasProperties(bucketJson, "snapshotId", "workOrders");
        var woJson = JsonDocument.Parse(bucketJson).RootElement.GetProperty("workOrders")[0];
        AssertHasProperties(
            woJson, "partNumber", "woid", "status", "orderedQuantity", "completedQuantity",
            "openQuantity", "releaseDate", "dueDate", "salesOrder", "kitting");
        AssertHasProperties(woJson.GetProperty("kitting"), "applicableLineCount", "fullyIssuedLineCount", "kittingPercent");

        var materialJson = JsonSerializer.Serialize(
            new Kst.Api.Dtos.WorkOrderMaterialResponseDto(
                Guid.NewGuid().ToString(), "1001",
                new Kst.Api.Dtos.KittingSummaryDto(4, 2, 50m),
                [
                    new Kst.Api.Dtos.WorkOrderMaterialLineDto(
                        "COMP1", "Widget", 10m, 5m, -5m, 50m, "underIssuedException", false, false)
                ]),
            serializerOptions);
        AssertHasProperties(materialJson, "snapshotId", "woid", "kitting", "lines");
        var lineJson = JsonDocument.Parse(materialJson).RootElement.GetProperty("lines")[0];
        AssertHasProperties(
            lineJson, "componentPart", "componentDescription", "requiredQuantity", "issuedQuantity",
            "varianceQuantity", "issuedPercent", "issueStatus", "isManufactured", "isFullyIssued");

        var candidateJson = JsonSerializer.Serialize(
            new Kst.Api.Dtos.WorkOrderCandidateResponseDto(
                Guid.NewGuid().ToString(),
                [
                    new Kst.Api.Dtos.WorkOrderSummaryDto(
                        "SUBASSY", "2001", "allocating", 10m, 0m, 10m, null,
                        new DateOnly(2026, 8, 10), null, new Kst.Api.Dtos.KittingSummaryDto(0, 0, null))
                ],
                false),
            serializerOptions);
        AssertHasProperties(candidateJson, "snapshotId", "candidates", "isTruncated");
    }

    private static void AssertHasProperties(string json, params string[] propertyNames) =>
        AssertHasProperties(JsonDocument.Parse(json).RootElement, propertyNames);

    private static void AssertHasProperties(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
            Assert.True(element.TryGetProperty(name, out _), $"Expected camelCase '{name}' property.");
    }
}

