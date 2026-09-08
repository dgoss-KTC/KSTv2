using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.Bom;
using Kst.Application.Mps;
using Kst.Application.Shortages;
using Kst.Application.WorkOrders;
using Kst.Domain.Bom;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.Shortages;
using Kst.Domain.WorkOrders;
using Microsoft.Extensions.DependencyInjection;

namespace Kst.Api.IntegrationTests;

public sealed class WorkOrderImmediateMaterialEndpointTests
{
    private const string Parent = "BUILD";
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now.Date);

    [Fact]
    public async Task GetImmediateMaterial_Maps_Actual_Short_Kss_Context_Without_Recalculation()
    {
        await using var factory = CreateFactory("R", "EA", usable: 2m, po: new("PO-1", Today, 8m, true, "TRACK", true, "O"), isKss: true);
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await CreateAndSeedAsync(factory, client, Parent);

        var response = await client.GetAsync(Route(assignmentId, snapshotId));
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var component = root.GetProperty("components")[0];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("actualWo", component.GetProperty("requirementSource").GetString());
        Assert.Equal("short", component.GetProperty("materialStatus").GetString());
        Assert.Equal("committedSequential", component.GetProperty("allocationMode").GetString());
        Assert.Equal(10m, component.GetProperty("requiredQuantity").GetDecimal());
        Assert.Equal(0m, component.GetProperty("issuedQuantity").GetDecimal());
        Assert.Equal(10m, component.GetProperty("varianceQuantity").GetDecimal());
        Assert.Equal(0m, component.GetProperty("issuedPercent").GetDecimal());
        Assert.Equal(8m, component.GetProperty("shortQuantity").GetDecimal());
        Assert.Equal(0m, component.GetProperty("usableHardAllocationToThisWoComponent").GetDecimal());
        Assert.Equal(0m, component.GetProperty("ownHardCoverage").GetDecimal());
        Assert.Equal(10m, component.GetProperty("uncoveredRequirement").GetDecimal());
        Assert.Equal(2m, component.GetProperty("availableQuantityAtEvaluation").GetDecimal());
        Assert.Equal(2m, component.GetProperty("allocatedQuantity").GetDecimal());
        Assert.True(component.GetProperty("incoming").GetProperty("isKss").GetBoolean());
    }

    [Fact]
    public async Task GetImmediateMaterial_Maps_Projected_OnHand_With_Null_Issue_Presentation()
    {
        await using var factory = CreateFactory("F", "EA", usable: 20m);
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await CreateAndSeedAsync(factory, client, Parent);

        var response = await client.GetAsync(Route(assignmentId, snapshotId, "releaseDate"));
        var component = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("components")[0];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("projectedBom", component.GetProperty("requirementSource").GetString());
        Assert.Equal("onHand", component.GetProperty("materialStatus").GetString());
        Assert.Equal(JsonValueKind.Null, component.GetProperty("issuedQuantity").ValueKind);
        Assert.Equal(JsonValueKind.Null, component.GetProperty("varianceQuantity").ValueKind);
        Assert.Equal(JsonValueKind.Null, component.GetProperty("issuedPercent").ValueKind);
        Assert.Equal(JsonValueKind.Null, component.GetProperty("isOverIssued").ValueKind);
        Assert.Equal(JsonValueKind.Null, component.GetProperty("incoming").ValueKind);
    }

    [Fact]
    public async Task GetImmediateMaterial_Maps_Unknown_Component_As_Success_Without_NoPo()
    {
        await using var factory = CreateFactory("R", null, usable: 0m);
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await CreateAndSeedAsync(factory, client, Parent);

        var response = await client.GetAsync(Route(assignmentId, snapshotId));
        var component = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("components")[0];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("unknown", component.GetProperty("materialStatus").GetString());
        Assert.Equal(JsonValueKind.Null, component.GetProperty("shortQuantity").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, component.GetProperty("diagnostic").ValueKind);
        Assert.Equal(JsonValueKind.Null, component.GetProperty("incoming").ValueKind);
    }

    [Fact]
    public async Task GetImmediateMaterial_Maps_Actual_Missing_MasterPm_As_Unknown_Without_NoPo()
    {
        await using var factory = CreateFactory("R", "EA", usable: 0m);
        factory.WorkOrderMaterialReader = new DelegateWorkOrderMaterialReader((_, _, _) =>
            Task.FromResult<IReadOnlyList<WorkOrderMaterialLine>>([new("COMP", "Component", 10m, 0m, false, "EA", IsMasterPmCodeReliable: false)]));
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await CreateAndSeedAsync(factory, client, Parent);

        var response = await client.GetAsync(Route(assignmentId, snapshotId));
        var component = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("components")[0];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("unknown", component.GetProperty("materialStatus").GetString());
        Assert.Equal(JsonValueKind.Null, component.GetProperty("shortQuantity").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, component.GetProperty("diagnostic").ValueKind);
        Assert.Equal(JsonValueKind.Null, component.GetProperty("availableQuantityAtEvaluation").ValueKind);
        Assert.Equal(JsonValueKind.Null, component.GetProperty("allocatedQuantity").ValueKind);
        Assert.Equal(JsonValueKind.Null, component.GetProperty("incoming").ValueKind);
    }

    [Fact]
    public async Task GetImmediateMaterial_Maps_Every_Component_Field_For_Manufactured_NotApplicable_Row()
    {
        await using var factory = CreateFactory("R", "EA", usable: 20m);
        factory.WorkOrderMaterialReader = new DelegateWorkOrderMaterialReader((_, _, _) =>
            Task.FromResult<IReadOnlyList<WorkOrderMaterialLine>>([new("SUBASSY", "Subassembly", 10m, 0m, true, "EA")]));
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await CreateAndSeedAsync(factory, client, Parent);

        var response = await client.GetAsync(Route(assignmentId, snapshotId));
        var component = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("components")[0];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("SUBASSY", component.GetProperty("componentPart").GetString());
        Assert.Equal("Subassembly", component.GetProperty("description").GetString());
        Assert.True(component.GetProperty("isManufactured").GetBoolean());
        Assert.Equal("EA", component.GetProperty("unitOfMeasure").GetString());
        Assert.Equal("actualWo", component.GetProperty("requirementSource").GetString());
        Assert.Equal("notApplicable", component.GetProperty("materialStatus").GetString());
        Assert.Equal("advisorySharedPool", component.GetProperty("allocationMode").GetString());
        Assert.Equal(10m, component.GetProperty("requiredQuantity").GetDecimal());
        Assert.Equal(0m, component.GetProperty("issuedQuantity").GetDecimal());
        Assert.Equal(10m, component.GetProperty("varianceQuantity").GetDecimal());
        Assert.Equal(0m, component.GetProperty("issuedPercent").GetDecimal());
        Assert.Equal(JsonValueKind.Null, component.GetProperty("remainingRequirement").ValueKind);
        Assert.Equal(0m, component.GetProperty("usableHardAllocationToThisWoComponent").GetDecimal());
        Assert.Equal(0m, component.GetProperty("ownHardCoverage").GetDecimal());
        Assert.Equal(JsonValueKind.Null, component.GetProperty("uncoveredRequirement").ValueKind);
        Assert.Equal(JsonValueKind.Null, component.GetProperty("availableQuantityAtEvaluation").ValueKind);
        Assert.Equal(JsonValueKind.Null, component.GetProperty("allocatedQuantity").ValueKind);
        Assert.Equal(0m, component.GetProperty("usableOnHand").GetDecimal());
        Assert.Equal(JsonValueKind.Null, component.GetProperty("shortQuantity").ValueKind);
        Assert.False(component.GetProperty("isFloorStockOrNonIssued").GetBoolean());
        Assert.False(component.GetProperty("isOverIssued").GetBoolean());
        var inventoryActivity = component.GetProperty("inventoryActivity");
        Assert.Equal(0m, inventoryActivity.GetProperty("transit").GetDecimal());
        Assert.Equal(0m, inventoryActivity.GetProperty("inspection").GetDecimal());
        Assert.Equal(0m, inventoryActivity.GetProperty("nonNet").GetDecimal());
        Assert.Equal(0m, inventoryActivity.GetProperty("mrb").GetDecimal());
        Assert.Equal(0m, inventoryActivity.GetProperty("ncmInspection").GetDecimal());
        Assert.Equal(0m, inventoryActivity.GetProperty("expiredExpiring").GetDecimal());
        Assert.Equal(JsonValueKind.Null, component.GetProperty("incoming").ValueKind);
        Assert.Equal(JsonValueKind.Null, component.GetProperty("diagnostic").ValueKind);
    }

    [Fact]
    public async Task GetImmediateMaterial_Maps_HardAllocation_And_Advisory_Provenance()
    {
        await using var hardFactory = CreateFactory("R", "EA", usable: 10m);
        hardFactory.HardAllocationReader = new DelegateHardAllocationReader((_, _, _, _) =>
            Task.FromResult<IReadOnlyList<HardAllocation>>([new("WO-1", "10", "COMP", "L", "LOT", 10m)]));
        using var hardClient = hardFactory.CreateClient();
        var (hardAssignmentId, hardSnapshotId) = await CreateAndSeedAsync(hardFactory, hardClient, Parent);

        var hardResponse = await hardClient.GetAsync(Route(hardAssignmentId, hardSnapshotId));
        var hardComponent = JsonDocument.Parse(await hardResponse.Content.ReadAsStringAsync()).RootElement.GetProperty("components")[0];

        Assert.Equal("hardAllocatedCommitted", hardComponent.GetProperty("allocationMode").GetString());
        Assert.Equal(10m, hardComponent.GetProperty("usableHardAllocationToThisWoComponent").GetDecimal());
        Assert.Equal(10m, hardComponent.GetProperty("ownHardCoverage").GetDecimal());
        Assert.Equal(0m, hardComponent.GetProperty("uncoveredRequirement").GetDecimal());
        Assert.Equal(0m, hardComponent.GetProperty("availableQuantityAtEvaluation").GetDecimal());
        Assert.Equal(0m, hardComponent.GetProperty("allocatedQuantity").GetDecimal());

        await using var advisoryFactory = CreateFactory("F", "EA", usable: 2m);
        using var advisoryClient = advisoryFactory.CreateClient();
        var (advisoryAssignmentId, advisorySnapshotId) = await CreateAndSeedAsync(advisoryFactory, advisoryClient, Parent);

        var advisoryResponse = await advisoryClient.GetAsync(Route(advisoryAssignmentId, advisorySnapshotId));
        var advisoryComponent = JsonDocument.Parse(await advisoryResponse.Content.ReadAsStringAsync()).RootElement.GetProperty("components")[0];

        Assert.Equal("advisorySharedPool", advisoryComponent.GetProperty("allocationMode").GetString());
        Assert.Equal(0m, advisoryComponent.GetProperty("usableHardAllocationToThisWoComponent").GetDecimal());
        Assert.Equal(0m, advisoryComponent.GetProperty("ownHardCoverage").GetDecimal());
        Assert.Equal(10m, advisoryComponent.GetProperty("uncoveredRequirement").GetDecimal());
        Assert.Equal(2m, advisoryComponent.GetProperty("availableQuantityAtEvaluation").GetDecimal());
        Assert.Equal(2m, advisoryComponent.GetProperty("allocatedQuantity").GetDecimal());
    }

    [Fact]
    public async Task GetImmediateMaterial_Maps_NoPo_And_Analysis_DataIssue()
    {
        await using var factory = CreateFactory("F", "EA", usable: 0m, bom: []);
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await CreateAndSeedAsync(factory, client, Parent);

        var response = await client.GetAsync(Route(assignmentId, snapshotId));
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, root.GetProperty("components").GetArrayLength());
        Assert.NotEqual(JsonValueKind.Null, root.GetProperty("diagnostic").ValueKind);
    }

    [Fact]
    public async Task GetImmediateMaterial_Maps_NoPo_For_Short_Component()
    {
        await using var factory = CreateFactory("R", "EA", usable: 0m);
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await CreateAndSeedAsync(factory, client, Parent);

        var response = await client.GetAsync(Route(assignmentId, snapshotId));
        var incoming = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("components")[0].GetProperty("incoming");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("NO PO", incoming.GetProperty("poState").GetString());
    }

    [Fact]
    public async Task GetImmediateMaterial_Maps_Kss_Without_Conventional_Po_As_Kss_Not_NoPo()
    {
        await using var factory = CreateFactory("R", "EA", usable: 0m, isKss: true);
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await CreateAndSeedAsync(factory, client, Parent);

        var response = await client.GetAsync(Route(assignmentId, snapshotId));
        var incoming = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("components")[0].GetProperty("incoming");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(incoming.GetProperty("isKss").GetBoolean());
        Assert.Equal(JsonValueKind.Null, incoming.GetProperty("poState").ValueKind);
        Assert.Equal(JsonValueKind.Null, incoming.GetProperty("poNumber").ValueKind);
    }

    [Fact]
    public async Task GetImmediateMaterial_Maps_Stale_And_NotInWindow_Outcomes()
    {
        await using var factory = CreateFactory("R", "EA", usable: 20m);
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await CreateAndSeedAsync(factory, client, Parent);
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(assignmentId,
            new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, "SW", [new(Parent, "Description")], []));

        var stale = await client.GetAsync(Route(assignmentId, snapshotId));

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
    }

    [Fact]
    public async Task GetImmediateMaterial_Maps_NotInWindow_And_Unavailable()
    {
        var summary = new WorkOrderSummary(Parent, "WO-1", "R", 10m, 0m, Today, Today, KittingSummary.Calculate(0, 0));
        await using var outsideFactory = new KstApiFactory
        {
            WorkOrderSummaryReader = new DelegateWorkOrderSummaryReader((_, _, _, _, _, _, _, _) => Task.FromResult<IReadOnlyList<WorkOrderSummary>>([]), (_, _, _) => Task.FromResult<WorkOrderSummary?>(summary))
        };
        using var outsideClient = outsideFactory.CreateClient();
        var (outsideAssignmentId, outsideSnapshotId) = await CreateAndSeedAsync(outsideFactory, outsideClient, Parent);
        var outside = await outsideClient.GetAsync(Route(outsideAssignmentId, outsideSnapshotId));
        Assert.Equal(HttpStatusCode.NotFound, outside.StatusCode);

        await using var unavailableFactory = CreateFactory("F", "EA", usable: 20m);
        unavailableFactory.BomSourceReader = new DelegateBomSourceReader((_, _, _, _) => throw new InvalidOperationException("Unavailable"));
        using var unavailableClient = unavailableFactory.CreateClient();
        var (unavailableAssignmentId, unavailableSnapshotId) = await CreateAndSeedAsync(unavailableFactory, unavailableClient, Parent);
        var unavailable = await unavailableClient.GetAsync(Route(unavailableAssignmentId, unavailableSnapshotId));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
    }

    [Fact]
    public async Task GetImmediateMaterial_Allows_Nested_Manufactured_Subassembly_Not_In_TopLevel_Mps_Parents()
    {
        const string subassembly = "SUBASSY";
        var summary = new WorkOrderSummary(subassembly, "WO-1", "R", 10m, 0m, Today, Today, KittingSummary.Calculate(0, 0));
        await using var factory = CreateFactory("R", "EA", usable: 20m);
        factory.WorkOrderSummaryReader = new DelegateWorkOrderSummaryReader((_, _, _, _, _, _, _, _) => Task.FromResult<IReadOnlyList<WorkOrderSummary>>([summary]), (_, _, _) => Task.FromResult<WorkOrderSummary?>(summary));
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await CreateAndSeedAsync(factory, client, Parent);

        var response = await client.GetAsync(Route(assignmentId, snapshotId));
        var workOrder = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("workOrder");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(subassembly, workOrder.GetProperty("buildPart").GetString());
    }

    [Fact]
    public async Task GetImmediateMaterial_Maps_Control_Outcomes_And_Validates_DateBasis()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);

        var notLoaded = await client.GetAsync(Route(assignmentId, Guid.NewGuid()));
        var invalidBasis = await client.GetAsync(Route(assignmentId, Guid.NewGuid(), "invalid"));

        Assert.Equal(HttpStatusCode.Conflict, notLoaded.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidBasis.StatusCode);
    }

    [Fact]
    public async Task GetImmediateMaterial_Invalid_Query_Inputs_Return_ValidationProblemDetails()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);

        var response = await client.GetAsync(
            $"/api/v1/workspaces/{assignmentId}/work-orders/WO-1/immediate-material?snapshotId=invalid&dateBasis=invalid");
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", problem.GetProperty("type").GetString());
        Assert.Equal("One or more validation errors occurred.", problem.GetProperty("title").GetString());
        var errors = problem.GetProperty("errors");
        Assert.Contains("valid GUID", errors.GetProperty("snapshotId")[0].GetString());
        Assert.Contains("dueDate", errors.GetProperty("dateBasis")[0].GetString());
    }

    [Fact]
    public async Task GetImmediateMaterialSummary_Distinguishes_Short_Row_DataIssue_And_Analysis_DataIssue()
    {
        var shortSummary = new WorkOrderSummary(Parent, "WO-SHORT", "R", 10m, 0m, Today, Today, KittingSummary.Calculate(0, 0));
        var rowIssueSummary = new WorkOrderSummary(Parent, "WO-ROW-ISSUE", "R", 10m, 0m, Today, Today, KittingSummary.Calculate(0, 0));
        var analysisIssueSummary = new WorkOrderSummary(Parent, "WO-ANALYSIS-ISSUE", "F", 10m, 0m, Today, Today, KittingSummary.Calculate(0, 0));
        await using var factory = CreateFactory("R", "EA", usable: 0m, bom: []);
        factory.WorkOrderSummaryReader = new DelegateWorkOrderSummaryReader(
            (_, _, _, _, _, _, _, _) => Task.FromResult<IReadOnlyList<WorkOrderSummary>>([shortSummary, rowIssueSummary, analysisIssueSummary]),
            (_, woid, _) => Task.FromResult<WorkOrderSummary?>(new[] { shortSummary, rowIssueSummary, analysisIssueSummary }.SingleOrDefault(summary => summary.Woid == woid)));
        factory.WorkOrderMaterialReader = new DelegateWorkOrderMaterialReader((_, woid, _) =>
            Task.FromResult<IReadOnlyList<WorkOrderMaterialLine>>([new("COMP", "Component", 10m, 0m, false, woid == "WO-ROW-ISSUE" ? null : "EA")]));
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await CreateAndSeedAsync(factory, client, Parent);

        var response = await client.GetAsync(SummaryRoute(assignmentId, snapshotId, Parent));
        var workOrders = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("workOrders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertSummaryIndicators(workOrders, "WO-SHORT", hasShortage: true, hasDataIssue: false);
        AssertSummaryIndicators(workOrders, "WO-ROW-ISSUE", hasShortage: false, hasDataIssue: true);
        AssertSummaryIndicators(workOrders, "WO-ANALYSIS-ISSUE", hasShortage: false, hasDataIssue: true);
    }

    [Fact]
    public async Task GetImmediateMaterialSummary_Returns_WorkspaceWide_Or_ParentScoped_Results()
    {
        const string secondParent = "BUILD-2";
        var first = new WorkOrderSummary(Parent, "WO-1", "R", 10m, 0m, Today, Today, KittingSummary.Calculate(0, 0));
        var second = new WorkOrderSummary(secondParent, "WO-2", "R", 10m, 0m, Today, Today, KittingSummary.Calculate(0, 0));
        await using var factory = CreateFactory("R", "EA", usable: 20m);
        factory.WorkOrderSummaryReader = new DelegateWorkOrderSummaryReader(
            (_, part, _, _, _, _, _, _) => Task.FromResult<IReadOnlyList<WorkOrderSummary>>(part == Parent ? [first] : [second]),
            (_, _, _) => Task.FromResult<WorkOrderSummary?>(first));
        using var client = factory.CreateClient();
        var (assignmentId, snapshotId) = await CreateAndSeedAsync(factory, client, Parent, secondParent);

        var workspaceResponse = await client.GetAsync(SummaryRoute(assignmentId, snapshotId));
        var parentResponse = await client.GetAsync(SummaryRoute(assignmentId, snapshotId, Parent));

        Assert.Equal(HttpStatusCode.OK, workspaceResponse.StatusCode);
        Assert.Equal(2, JsonDocument.Parse(await workspaceResponse.Content.ReadAsStringAsync()).RootElement.GetProperty("workOrders").GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, parentResponse.StatusCode);
        var parentOrders = JsonDocument.Parse(await parentResponse.Content.ReadAsStringAsync()).RootElement.GetProperty("workOrders");
        Assert.Equal("WO-1", Assert.Single(parentOrders.EnumerateArray()).GetProperty("woid").GetString());
    }

    [Fact]
    public async Task GetImmediateMaterialSummary_Maps_MpsNotLoaded_Stale_NotFound_And_Unavailable_Outcomes()
    {
        await using var factory = CreateFactory("F", "EA", usable: 20m);
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, Parent);

        var notLoaded = await client.GetAsync(SummaryRoute(assignmentId, Guid.NewGuid(), Parent));
        var snapshotId = SnapshotId.New();
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(assignmentId,
            new MpsSnapshot(snapshotId, DateTimeOffset.UtcNow, "SW", [new(Parent, "Description")], []));
        var currentSnapshotId = SnapshotId.New();
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(assignmentId,
            new MpsSnapshot(currentSnapshotId, DateTimeOffset.UtcNow, "SW", [new(Parent, "Description")], []));
        var stale = await client.GetAsync(SummaryRoute(assignmentId, snapshotId.Value, Parent));
        var notFound = await client.GetAsync(SummaryRoute(assignmentId, currentSnapshotId.Value, "NOT-IN-SCOPE"));

        await using var unavailableFactory = CreateFactory("F", "EA", usable: 20m);
        unavailableFactory.BomSourceReader = new DelegateBomSourceReader((_, _, _, _) => throw new InvalidOperationException("Unavailable"));
        using var unavailableClient = unavailableFactory.CreateClient();
        var (unavailableAssignmentId, unavailableSnapshotId) = await CreateAndSeedAsync(unavailableFactory, unavailableClient, Parent);
        var unavailable = await unavailableClient.GetAsync(SummaryRoute(unavailableAssignmentId, unavailableSnapshotId, Parent));

        Assert.Equal(HttpStatusCode.Conflict, notLoaded.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
    }

    private static KstApiFactory CreateFactory(string status, string? unitOfMeasure, decimal usable, NextPurchaseOrder? po = null, IReadOnlyList<BomOccurrence>? bom = null, bool isKss = false)
    {
        var summary = new WorkOrderSummary(Parent, "WO-1", status, 10m, 0m, Today, Today, KittingSummary.Calculate(0, 0));
        return new KstApiFactory
        {
            WorkOrderSummaryReader = new DelegateWorkOrderSummaryReader((_, _, _, _, _, _, _, _) => Task.FromResult<IReadOnlyList<WorkOrderSummary>>([summary]), (_, _, _) => Task.FromResult<WorkOrderSummary?>(summary)),
            WorkOrderMaterialReader = new DelegateWorkOrderMaterialReader((_, _, _) => Task.FromResult<IReadOnlyList<WorkOrderMaterialLine>>([new("COMP", "Component", 10m, 0m, false, unitOfMeasure)])),
            BomSourceReader = new DelegateBomSourceReader((_, _, _, _) => Task.FromResult(bom ?? [new BomOccurrence("1/COMP", 1, "COMP", "P", false, "Component", 1m, null, unitOfMeasure, "P")])),
            CommittedWorkOrderPopulationReader = new DelegateCommittedWorkOrderPopulationReader((_, _, _, _, _) => Task.FromResult<IReadOnlyList<CommittedWorkOrderComponent>>([new("WO-1", status, null, Today, Today, "COMP", 10m, 0m, unitOfMeasure)])),
            HardAllocationReader = new DelegateHardAllocationReader((_, _, _, _) => Task.FromResult<IReadOnlyList<HardAllocation>>([])),
            InventoryPositionReader = new DelegateInventoryPositionReader((_, parts, _, _, _) => Task.FromResult<IReadOnlyList<InventoryPosition>>(parts.Select(part => new InventoryPosition(part, new UsableInventoryPosition(usable, new Dictionary<InventoryActivity, decimal>()))).ToList())),
            IssuePolicyReader = new DelegateIssuePolicyReader((_, _, _) => Task.FromResult(true)),
            IssueDaysReader = new DelegateIssueDaysReader((_, _) => Task.FromResult<int?>(7)),
            NextPurchaseOrderReader = new DelegateNextPurchaseOrderReader((_, _, _) => Task.FromResult(po)),
            KssScheduleReader = new DelegateKssScheduleReader((_, _, _, _) => Task.FromResult(isKss))
        };
    }

    private static async Task<(Guid AssignmentId, Guid SnapshotId)> CreateAndSeedAsync(KstApiFactory factory, HttpClient client, params string[] parents)
    {
        var assignmentId = await CreateWorkspaceAsync(client, parents);
        var snapshot = new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, "SW", parents.Select(parent => new MpsResolvedPart(parent, "Description")).ToList(), []);
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(assignmentId, snapshot);
        return (assignmentId, snapshot.Id.Value);
    }

    private static async Task<Guid> CreateWorkspaceAsync(HttpClient client, params string[] parentParts)
    {
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", new { site = "SW", parentParts, isTemporary = false });
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return root.GetProperty("assignmentId").GetGuid();
    }

    private static string Route(Guid assignmentId, Guid snapshotId, string? dateBasis = null) =>
        $"/api/v1/workspaces/{assignmentId}/work-orders/WO-1/immediate-material?snapshotId={snapshotId}" +
        (dateBasis is null ? string.Empty : $"&dateBasis={dateBasis}");

    private static string SummaryRoute(Guid assignmentId, Guid snapshotId, string? parentPart = null) =>
        $"/api/v1/workspaces/{assignmentId}/work-orders/immediate-material-summary?snapshotId={snapshotId}" +
        (parentPart is null ? string.Empty : $"&parentPart={parentPart}");

    private static void AssertSummaryIndicators(JsonElement workOrders, string woid, bool hasShortage, bool hasDataIssue)
    {
        var workOrder = workOrders.EnumerateArray().Single(workOrder => workOrder.GetProperty("woid").GetString() == woid);
        Assert.Equal(hasShortage, workOrder.GetProperty("hasShortage").GetBoolean());
        Assert.Equal(hasDataIssue, workOrder.GetProperty("hasDataIssue").GetBoolean());
    }
}
