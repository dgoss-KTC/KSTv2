using Kst.Application.Bom;
using Kst.Application.Mps;
using Kst.Application.Shortages;
using Kst.Application.Tests.Mps;
using Kst.Application.WorkOrders;
using Kst.Domain.Bom;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.Shortages;
using Kst.Domain.WorkOrders;
using Kst.Domain.Workspaces;
using Kst.Infrastructure.Mps;
using Kst.Infrastructure.Shortages;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Application.Tests.Shortages;

public sealed class WorkOrderImmediateShortageServiceTests
{
    private static readonly DateOnly Today = new(2026, 9, 3);
    private static readonly WorkspaceAssignment Workspace = new(Guid.NewGuid(), "Test", "SW", null, null, ["BUILD"], false, null, true, 0);

    [Theory]
    [InlineData("R", null, RequirementSource.ActualWo)]
    [InlineData("A", null, RequirementSource.ActualWo)]
    [InlineData("E", "F", RequirementSource.ActualWo)]
    [InlineData("E", null, RequirementSource.ProjectedBom)]
    [InlineData("F", null, RequirementSource.ProjectedBom)]
    [InlineData("P", null, RequirementSource.ProjectedBom)]
    public async Task RequirementAuthority_Follows_Accepted_Status_Rule(string status, string? type, RequirementSource expected)
    {
        var fixture = Fixture(status, type);
        var result = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderImmediateMaterialOutcomeKind.Loaded, result.Kind);
        Assert.Equal(expected, Assert.Single(result.Analysis!.ComponentRows).Requirement.Source);
        Assert.Equal(expected == RequirementSource.ActualWo ? 1 : 0, fixture.MaterialReads);
        Assert.Equal(expected == RequirementSource.ProjectedBom ? 1 : 0, fixture.BomReads);
    }

    [Fact]
    public async Task Lowercase_E_Status_Uses_Projected_Bom_Requirements()
    {
        var fixture = Fixture("e");

        var result = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderImmediateMaterialOutcomeKind.Loaded, result.Kind);
        Assert.Equal(RequirementSource.ProjectedBom, Assert.Single(result.Analysis!.ComponentRows).Requirement.Source);
    }

    [Fact]
    public async Task Unsupported_Unfinished_Status_Returns_Loaded_Analysis_DataIssue()
    {
        var fixture = Fixture("Z");
        var result = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);
        Assert.Equal(WorkOrderImmediateMaterialOutcomeKind.Loaded, result.Kind);
        Assert.NotNull(result.Analysis!.Diagnostic);
    }

    [Fact]
    public async Task Manufactured_Subassembly_WorkOrder_Is_Analyzed_Without_TopLevel_MpsParentMembership()
    {
        var fixture = Fixture("R");
        fixture.Summary = fixture.Summary with { PartNumber = "MANUFACTURED-SUBASSEMBLY" };
        fixture.Window = [fixture.Summary];

        var result = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);

        Assert.DoesNotContain(
            fixture.Store.GetState(Workspace.AssignmentId).Snapshot!.ResolvedParts,
            part => part.ParentPart == "MANUFACTURED-SUBASSEMBLY");
        Assert.Equal(WorkOrderImmediateMaterialOutcomeKind.Loaded, result.Kind);
        Assert.Equal("MANUFACTURED-SUBASSEMBLY", result.Analysis!.WorkOrder.BuildPart);
        var planningWindow = Assert.Single(fixture.PlanningWindowReads);
        Assert.Equal(("SW", "MANUFACTURED-SUBASSEMBLY"), planningWindow);
        Assert.Equal(1, fixture.MaterialReads);
    }

    [Fact]
    public async Task Missing_WorkOrder_And_PlanningWindow_Membership_Are_Rejected()
    {
        var fixture = Fixture("R");
        fixture.Summary = null!;
        var missing = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);
        Assert.Equal(WorkOrderImmediateMaterialOutcomeKind.WorkOrderNotInImmediateWindow, missing.Kind);

        fixture = Fixture("R");
        fixture.Window = [];
        var outsideWindow = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);
        Assert.Equal(WorkOrderImmediateMaterialOutcomeKind.WorkOrderNotInImmediateWindow, outsideWindow.Kind);
    }

    [Fact]
    public async Task Projected_Uses_ReleaseDate_And_Explodes_Through_Phantoms_Then_Consolidates()
    {
        var fixture = Fixture("E", bom: [
            Occ("PH", 1, true, 2m, "EA"), Occ("C1", 2, false, 1.5m, "EA"), Occ("C1", 1, false, 0.5m, "EA")]);
        fixture.Summary = fixture.Summary with { ReleaseDate = new DateOnly(2026, 8, 25), OrderedQuantity = 3m };
        fixture.Window = [fixture.Summary];

        var result = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);

        var row = Assert.Single(result.Analysis!.ComponentRows);
        Assert.Equal("C1", row.Requirement.ComponentPart);
        Assert.Equal("EA", row.Requirement.UnitOfMeasure);
        Assert.Equal(10m, row.Requirement.AdjustedRequiredQuantity); // (2 * 1.5 + .5) * 3, post-consolidation EA floor
        Assert.Equal(new DateOnly(2026, 8, 25), fixture.BomEffectiveDate);
    }

    [Fact]
    public async Task Actual_Ea_Requirements_Are_Floored_Per_Line_Before_Issued_Subtraction()
    {
        var fixture = Fixture("R", usable: 0m);
        fixture.MaterialLines = [
            new("C1", "C", 5.9m, 0.5m, false, "EA"),
            new("C1", "C", 5.9m, 0.5m, false, "EA")];

        var row = Assert.Single((await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today)).Analysis!.ComponentRows);

        Assert.Equal(10m, row.Requirement.AdjustedRequiredQuantity);
        Assert.Equal(1m, row.IssuedQuantity);
        Assert.Equal(9m, row.ShortQuantity);
    }

    [Fact]
    public async Task Actual_Empty_Material_Read_Returns_Loaded_Empty_Analysis_Without_DataIssue()
    {
        var fixture = Fixture("R");
        fixture.MaterialLines = [];

        var result = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderImmediateMaterialOutcomeKind.Loaded, result.Kind);
        Assert.Equal("WO1", result.Analysis!.WorkOrder.WoId);
        Assert.Empty(result.Analysis!.ComponentRows);
        Assert.Null(result.Analysis.Diagnostic);
        Assert.Equal(0, fixture.PurchaseOrderReads);
    }

    [Fact]
    public async Task Actual_ManufacturedOnly_Material_Read_Retains_NotApplicable_Component_Without_Po_Inference()
    {
        var fixture = Fixture("R");
        fixture.MaterialLines = [new("SUBASSY", "Subassembly", 10m, 0m, true, "EA")];

        var result = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderImmediateMaterialOutcomeKind.Loaded, result.Kind);
        var row = Assert.Single(result.Analysis!.ComponentRows);
        Assert.True(row.Requirement.IsManufactured);
        Assert.Equal(MaterialStatus.NotApplicable, row.MaterialStatus);
        Assert.Null(row.ShortQuantity);
        Assert.Null(row.Incoming);
        Assert.Equal(0, fixture.PurchaseOrderReads);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Actual_Missing_MasterPm_Is_Unknown_Without_Allocation_Or_Po_Inference(string? masterPmCode)
    {
        var fixture = Fixture("R", usable: 10m, hard: [new("WO1", "10", "C1", "L", "LOT", 10m)]);
        fixture.MaterialLines = [ActualLineWithMasterPm(masterPmCode)];

        var row = Assert.Single((await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today)).Analysis!.ComponentRows);

        Assert.Equal(MaterialStatus.Unknown, row.MaterialStatus);
        Assert.Equal(10m, row.Requirement.AdjustedRequiredQuantity);
        Assert.Equal(0m, row.IssuedQuantity);
        Assert.Null(row.ShortQuantity);
        Assert.Equal(0m, row.UsableHardAllocationToThisWoComponent);
        Assert.Null(row.AvailableQuantityAtEvaluation);
        Assert.Null(row.AllocatedQuantity);
        Assert.NotNull(row.Diagnostic);
        Assert.Null(row.Incoming);
        Assert.Equal(0, fixture.PurchaseOrderReads);
    }

    [Fact]
    public async Task Projected_NonEa_Fractional_Requirement_Is_Retained()
    {
        var fixture = Fixture("F", bom: [Occ("C1", 1, false, 1.25m, "LB")], usable: 0m);
        fixture.Summary = fixture.Summary with { OrderedQuantity = 3m };
        fixture.Window = [fixture.Summary];

        var row = Assert.Single((await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today)).Analysis!.ComponentRows);

        Assert.Equal(3.75m, row.Requirement.AdjustedRequiredQuantity);
    }

    [Fact]
    public async Task Projected_MasterNonM_Remains_Eligible_When_Stage8_EffectivePm_Is_M()
    {
        var fixture = Fixture("F", bom: [Occ("C1", 1, false, 1m, "EA", pmCode: "M", masterPmCode: "P")], usable: 0m);

        var row = Assert.Single((await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today)).Analysis!.ComponentRows);

        Assert.False(row.Requirement.IsManufactured);
        Assert.Equal(MaterialStatus.Short, row.MaterialStatus);
    }

    [Fact]
    public async Task Projected_MasterM_Is_NotApplicable_When_Stage8_EffectivePm_Is_NonM()
    {
        var fixture = Fixture("F", bom: [Occ("C1", 1, false, 1m, "EA", pmCode: "P", masterPmCode: "M")], usable: 0m);

        var row = Assert.Single((await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today)).Analysis!.ComponentRows);

        Assert.True(row.Requirement.IsManufactured);
        Assert.Equal(MaterialStatus.NotApplicable, row.MaterialStatus);
        Assert.Null(row.ShortQuantity);
        Assert.Null(row.Incoming);
        Assert.Equal(0, fixture.PurchaseOrderReads);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task Projected_Missing_MasterPm_Is_Unknown_Without_Po_Inference(string? masterPmCode)
    {
        var fixture = Fixture("F", bom: [Occ("C1", 1, false, 1m, "EA", pmCode: "P", masterPmCode: masterPmCode)], usable: 0m);

        var row = Assert.Single((await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today)).Analysis!.ComponentRows);

        Assert.False(row.Requirement.IsManufactured);
        Assert.Equal(MaterialStatus.Unknown, row.MaterialStatus);
        Assert.Null(row.ShortQuantity);
        Assert.NotNull(row.Diagnostic);
        Assert.Null(row.Incoming);
        Assert.Equal(0, fixture.PurchaseOrderReads);
    }

    [Fact]
    public async Task Projected_Null_Uom_Is_DataIssue_Without_Po_Read()
    {
        var fixture = Fixture("F", bom: [Occ("C1", 1, false, 1m, null)], usable: 0m);

        var row = Assert.Single((await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today)).Analysis!.ComponentRows);

        Assert.Equal(MaterialStatus.Unknown, row.MaterialStatus);
        Assert.NotNull(row.Diagnostic);
        Assert.Null(row.Incoming);
        Assert.Equal(0, fixture.PurchaseOrderReads);
    }

    [Fact]
    public async Task Projected_Without_Effective_Bom_Returns_Loaded_Analysis_DataIssue()
    {
        var fixture = Fixture("F", bom: []);

        var result = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderImmediateMaterialOutcomeKind.Loaded, result.Kind);
        Assert.Empty(result.Analysis!.ComponentRows);
        Assert.NotNull(result.Analysis.Diagnostic);
    }

    [Fact]
    public async Task Missing_Component_Uom_Returns_Unknown_Row_Without_Po_Inference()
    {
        var fixture = Fixture("R");
        fixture.Service = new WorkOrderImmediateShortageService(new FakeWorkspaceConfigurationService(Workspace), fixture.Store,
            new DelegateWorkOrderSummaryReader((_, _, _, _, _, _, _, _) => Task.FromResult(fixture.Window), (_, _, _) => Task.FromResult<WorkOrderSummary?>(fixture.Summary)),
            new DelegateWorkOrderMaterialReader((_, _, _) => Task.FromResult<IReadOnlyList<WorkOrderMaterialLine>>([new("C1", "C", 10m, 0m, false, null)])),
            new DelegateBomSourceReader((_, _, _, _) => Task.FromResult(fixture.Bom)),
            new DelegateCommittedWorkOrderPopulationReader((_, _, _, _, _) => Task.FromResult<IReadOnlyList<CommittedWorkOrderComponent>>([Committed("WO1", "R", 10m, 0m)])),
            new DelegateHardAllocationReader((_, _, _, _) => Task.FromResult<IReadOnlyList<HardAllocation>>([])),
            new DelegateInventoryPositionReader((_, parts, _, _, _) => Task.FromResult<IReadOnlyList<InventoryPosition>>(parts.Select(p => new InventoryPosition(p, new UsableInventoryPosition(0m, new Dictionary<InventoryActivity, decimal>()))).ToList())),
            new DelegateIssuePolicyReader((_, _, _) => Task.FromResult(true)), new DelegateIssueDaysReader((_, _) => Task.FromResult<int?>(7)),
            new DelegateNextPurchaseOrderReader((_, _, _) => { fixture.PurchaseOrderReads++; return Task.FromResult<NextPurchaseOrder?>(null); }),
            new DelegateKssScheduleReader((_, _, _, _) => Task.FromResult(false)), new InMemoryWorkOrderImmediateMaterialCacheStore(), NullLogger<WorkOrderImmediateShortageService>.Instance);

        var row = Assert.Single((await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today)).Analysis!.ComponentRows);

        Assert.Equal(MaterialStatus.Unknown, row.MaterialStatus);
        Assert.Null(row.ShortQuantity);
        Assert.NotNull(row.Diagnostic);
        Assert.Null(row.Incoming);
        Assert.Equal(0, fixture.PurchaseOrderReads);
    }

    [Fact]
    public async Task Actual_Mixed_Component_Uoms_Return_Unknown_Row_Without_Po_Inference()
    {
        var fixture = Fixture("R");
        fixture.MaterialLines =
        [
            new("C1", "Component", 5.9m, 0m, false, "EA"),
            new("C1", "Component", 5.9m, 0m, false, "LB")
        ];

        var row = Assert.Single((await fixture.Service.GetImmediateMaterialAsync(
            Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today)).Analysis!.ComponentRows);

        Assert.Equal(10.9m, row.Requirement.AdjustedRequiredQuantity);
        Assert.Null(row.Requirement.UnitOfMeasure);
        Assert.Equal(MaterialStatus.Unknown, row.MaterialStatus);
        Assert.Null(row.ShortQuantity);
        Assert.NotNull(row.Diagnostic);
        Assert.Null(row.Incoming);
        Assert.Equal(0, fixture.PurchaseOrderReads);
    }

    [Fact]
    public async Task Projected_Bom_Uses_Supplied_Today_When_ReleaseDate_Is_Missing()
    {
        var fixture = Fixture("F");
        fixture.Summary = fixture.Summary with { ReleaseDate = null };
        fixture.Window = [fixture.Summary];

        await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.ReleaseDate, Today);

        Assert.Equal(Today, fixture.BomEffectiveDate);
    }

    [Fact]
    public async Task HardAllocation_Is_Credited_Once_And_External_OutsideWindow_Reservation_Reduces_FreePool()
    {
        // HardAllocation carries the authoritative lad_det identity directly; no wod_det row is required.
        var fixture = Fixture("R", hard: [new("WO1", "10", "C1", "L", "1", 3m), new("OUTSIDE", "10", "C1", "L", "2", 4m)], usable: 10m, committed: [Committed("WO1", "R", 10m, 0m)]);
        var result = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);

        var row = Assert.Single(result.Analysis!.ComponentRows);
        Assert.Equal(3m, row.OwnHardCoverage);
        Assert.Equal(3m, row.AllocatedQuantity); // free = 10 - all hard 7; selected consumes only its 7 residual
        Assert.Equal(4m, row.ShortQuantity);
    }

    [Fact]
    public async Task HardAllocations_Aggregate_All_Operations_For_The_Same_WorkOrder_And_Reduce_The_AdvisoryPool()
    {
        var fixture = Fixture("R", hard:
        [
            new("WO1", "10", "C1", "L", "1", 2m),
            new("WO1", "20", "C1", "L", "2", 4m),
            new("OUTSIDE", "10", "C1", "L", "3", 4m)
        ], usable: 10m, committed: [Committed("WO1", "R", 10m, 0m)]);

        var row = Assert.Single((await fixture.Service.GetImmediateMaterialAsync(
            Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today)).Analysis!.ComponentRows);

        Assert.Equal(6m, row.UsableHardAllocationToThisWoComponent);
        Assert.Equal(6m, row.OwnHardCoverage);
        Assert.Equal(0m, row.AvailableQuantityAtEvaluation);
        Assert.Equal(0m, row.AllocatedQuantity);
        Assert.Equal(4m, row.ShortQuantity);
    }

    [Fact]
    public async Task Committed_R_Consumes_Before_A_And_ReleaseMode_Orders_By_ReleaseDate_Then_Woid()
    {
        var fixture = Fixture("R", committed:
        [
            Committed("A1", "A", 6m, 0m, due: Today, release: Today),
            Committed("R2", "R", 6m, 0m, due: Today, release: Today.AddDays(2)),
            Committed("R1", "R", 6m, 0m, due: Today, release: Today.AddDays(1)),
        ], usable: 10m);
        fixture.Summary = fixture.Summary with { Woid = "R2", ReleaseDate = Today.AddDays(2) };
        fixture.Window = [fixture.Summary];
        var result = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "R2", MpsDateBasis.ReleaseDate, Today);

        Assert.Equal(4m, Assert.Single(result.Analysis!.ComponentRows).AllocatedQuantity);
    }

    [Fact]
    public async Task Uncommitted_Actual_E_F_Is_Advisory_And_Short_Context_Uses_Kss_Or_NoPo()
    {
        var fixture = Fixture("E", "F", usable: 2m, po: new("PO1", Today, 8m, true, "T", true, "O"), isKss: true);
        var result = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);
        var row = Assert.Single(result.Analysis!.ComponentRows);
        Assert.Equal(AllocationMode.AdvisorySharedPool, row.AllocationMode);
        Assert.True(row.Incoming!.IsKss);
        Assert.NotEqual("NO PO", row.Incoming.PoState);

        fixture = Fixture("E", "F", usable: 2m, po: null);
        result = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);
        Assert.Equal("NO PO", Assert.Single(result.Analysis!.ComponentRows).Incoming!.PoState);
    }

    [Theory]
    [InlineData(false, false, "NO PO")]
    [InlineData(true, false, null)]
    public async Task Short_Purchased_Component_Uses_Independent_Kss_Classification_When_No_Conventional_Po(bool isKss, bool hasPo, string? expectedPoState)
    {
        var fixture = Fixture("R", usable: 0m,
            po: hasPo ? new NextPurchaseOrder("PO1", Today, 8m, true, "T", false, "O") : null,
            isKss: isKss);

        var row = Assert.Single((await fixture.Service.GetImmediateMaterialAsync(
            Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today)).Analysis!.ComponentRows);

        Assert.Equal(isKss, row.Incoming!.IsKss);
        Assert.Equal(expectedPoState, row.Incoming.PoState);
        Assert.Null(row.Incoming.PoNumber);
        Assert.Equal(10m, row.ShortQuantity);
    }

    [Fact]
    public async Task Short_Kss_Component_Retains_Qualifying_Conventional_Po_Context()
    {
        var fixture = Fixture("R", usable: 0m,
            po: new NextPurchaseOrder("PO1", Today, 8m, true, "T", false, "O"), isKss: true);

        var row = Assert.Single((await fixture.Service.GetImmediateMaterialAsync(
            Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today)).Analysis!.ComponentRows);

        Assert.True(row.Incoming!.IsKss);
        Assert.Equal("PO1", row.Incoming.PoNumber);
        Assert.Equal(8m, row.Incoming.PoOpenQuantity);
        Assert.Equal(10m, row.ShortQuantity);
    }

    [Fact]
    public async Task Short_NonKss_Component_Retains_Qualifying_Conventional_Po_Context()
    {
        var fixture = Fixture("R", usable: 0m,
            po: new NextPurchaseOrder("PO1", Today, 8m, true, "T", true, "O"));

        var row = Assert.Single((await fixture.Service.GetImmediateMaterialAsync(
            Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today)).Analysis!.ComponentRows);

        Assert.False(row.Incoming!.IsKss);
        Assert.Equal("PO1", row.Incoming.PoNumber);
        Assert.Equal("O", row.Incoming.PoState);
        Assert.Equal(10m, row.ShortQuantity);
    }

    [Fact]
    public async Task Uncommitted_WorkOrders_Read_The_Same_PostCommitted_AdvisoryPool_Without_Consuming_It()
    {
        var committed = new[] { Committed("R1", "R", 8m, 0m) };
        var first = Fixture("E", "F", committed: committed, usable: 10m);
        var second = Fixture("E", "F", committed: committed, usable: 10m);

        var firstRow = Assert.Single((await first.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, first.Snapshot, "WO1", MpsDateBasis.DueDate, Today)).Analysis!.ComponentRows);
        var secondRow = Assert.Single((await second.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, second.Snapshot, "WO1", MpsDateBasis.DueDate, Today)).Analysis!.ComponentRows);

        Assert.Equal(2m, firstRow.AllocatedQuantity);
        Assert.Equal(2m, secondRow.AllocatedQuantity);
        Assert.Equal(8m, firstRow.ShortQuantity);
        Assert.Equal(8m, secondRow.ShortQuantity);
    }

    [Fact]
    public async Task Uncommitted_WorkOrders_In_The_Same_Analysis_Read_The_Same_AdvisoryPool_Without_Consuming_It()
    {
        var fixture = Fixture("E", "F", committed: [Committed("R1", "R", 8m, 0m)], usable: 10m);
        var second = fixture.Summary with { Woid = "WO2" };
        fixture.Window = [fixture.Summary, second];

        var result = await fixture.Service.GetSummaryAsync(
            Workspace.AssignmentId, fixture.Snapshot, "BUILD", MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderImmediateMaterialSummaryOutcomeKind.Loaded, result.Kind);
        var rows = result.Analyses!.Select(analysis => Assert.Single(analysis.ComponentRows)).ToList();
        Assert.All(rows, row =>
        {
            Assert.Equal(AllocationMode.AdvisorySharedPool, row.AllocationMode);
            Assert.Equal(2m, row.AllocatedQuantity);
            Assert.Equal(8m, row.ShortQuantity);
        });
    }

    [Fact]
    public async Task OnHand_Does_Not_Read_Po_And_FloorStock_Is_Informational()
    {
        var fixture = Fixture("R", usable: 20m, issuePolicy: false);
        var result = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);
        var row = Assert.Single(result.Analysis!.ComponentRows);
        Assert.Equal(MaterialStatus.OnHand, row.MaterialStatus);
        Assert.Null(row.Incoming);
        Assert.True(row.IsFloorStockOrNonIssued);
        Assert.Equal(0, fixture.PurchaseOrderReads);
    }

    [Fact]
    public async Task Source_Failure_Returns_Unavailable()
    {
        var fixture = Fixture("F");
        fixture.Service = new WorkOrderImmediateShortageService(new FakeWorkspaceConfigurationService(Workspace), fixture.Store,
            new DelegateWorkOrderSummaryReader((_, _, _, _, _, _, _, _) => Task.FromResult(fixture.Window), (_, _, _) => Task.FromResult<WorkOrderSummary?>(fixture.Summary)),
            new DelegateWorkOrderMaterialReader((_, _, _) => Task.FromResult<IReadOnlyList<WorkOrderMaterialLine>>([])),
            new DelegateBomSourceReader((_, _, _, _) => throw new InvalidOperationException("QAD unavailable")),
            new DelegateCommittedWorkOrderPopulationReader((_, _, _, _, _) => Task.FromResult<IReadOnlyList<CommittedWorkOrderComponent>>([])),
            new DelegateHardAllocationReader((_, _, _, _) => Task.FromResult<IReadOnlyList<HardAllocation>>([])),
            new DelegateInventoryPositionReader((_, _, _, _, _) => Task.FromResult<IReadOnlyList<InventoryPosition>>([])),
            new DelegateIssuePolicyReader((_, _, _) => Task.FromResult(true)), new DelegateIssueDaysReader((_, _) => Task.FromResult<int?>(7)),
            new DelegateNextPurchaseOrderReader((_, _, _) => Task.FromResult<NextPurchaseOrder?>(null)),
            new DelegateKssScheduleReader((_, _, _, _) => Task.FromResult(false)), new InMemoryWorkOrderImmediateMaterialCacheStore(), NullLogger<WorkOrderImmediateShortageService>.Instance);

        var result = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);

        Assert.Equal(WorkOrderImmediateMaterialOutcomeKind.Unavailable, result.Kind);
    }

    [Fact]
    public async Task Cache_Is_Keyed_By_BusinessDate_And_Snapshot()
    {
        var fixture = Fixture("R");
        await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);
        await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);
        await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today.AddDays(1));
        Assert.Equal(2, fixture.MaterialReads);
        var newSnapshot = Seed(fixture.Store);
        var changed = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);
        Assert.Equal(WorkOrderImmediateMaterialOutcomeKind.SnapshotChanged, changed.Kind);
        await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, newSnapshot, "WO1", MpsDateBasis.DueDate, Today);
        Assert.Equal(3, fixture.MaterialReads);
    }

    [Fact]
    public async Task Cache_Does_Not_Serve_Prior_Business_Day_When_Fresh_Source_Fails()
    {
        var fixture = Fixture("R");
        var first = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);
        fixture.MaterialReadFails = true;

        var second = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today.AddDays(1));

        Assert.Equal(WorkOrderImmediateMaterialOutcomeKind.Loaded, first.Kind);
        Assert.Equal(WorkOrderImmediateMaterialOutcomeKind.Unavailable, second.Kind);
        Assert.Equal(2, fixture.MaterialReads);
    }

    [Fact]
    public async Task Cache_Is_Isolated_Between_Due_And_Release_Bases()
    {
        var fixture = Fixture("R");
        var due = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.DueDate, Today);
        fixture.MaterialReadFails = true;

        var release = await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "WO1", MpsDateBasis.ReleaseDate, Today);

        Assert.Equal(WorkOrderImmediateMaterialOutcomeKind.Loaded, due.Kind);
        Assert.Equal(WorkOrderImmediateMaterialOutcomeKind.Unavailable, release.Kind);
        Assert.Equal(2, fixture.MaterialReads);
    }

    [Fact]
    public async Task Falldown_Is_DueDate_Sorted_In_Release_Basis()
    {
        var fixture = Fixture("R", committed:
        [
            Committed("R2", "R", 6m, 0m, due: Today.AddDays(-5), release: Today.AddDays(-10)),
            Committed("R1", "R", 6m, 0m, due: Today.AddDays(-6), release: Today.AddDays(-1)),
        ], usable: 10m);
        fixture.Summary = fixture.Summary with { Woid = "R2", DueDate = Today.AddDays(-5), ReleaseDate = Today.AddDays(-10) };
        fixture.Window = [fixture.Summary];

        var row = Assert.Single((await fixture.Service.GetImmediateMaterialAsync(Workspace.AssignmentId, fixture.Snapshot, "R2", MpsDateBasis.ReleaseDate, Today)).Analysis!.ComponentRows);

        Assert.Equal(4m, row.AllocatedQuantity);
    }

    private static TestFixture Fixture(string status, string? type = null, IReadOnlyList<BomOccurrence>? bom = null, IReadOnlyList<HardAllocation>? hard = null, IReadOnlyList<CommittedWorkOrderComponent>? committed = null, decimal usable = 20m, bool issuePolicy = true, NextPurchaseOrder? po = null, bool isKss = false)
    {
        var store = new InMemoryMpsSnapshotStore(); var snapshot = Seed(store);
        var fixture = new TestFixture { Store = store, Snapshot = snapshot };
        fixture.Summary = new WorkOrderSummary("BUILD", "WO1", status, 10m, 0m, Today, Today, KittingSummary.Calculate(0, 0), WorkOrderType: type);
        fixture.Window = [fixture.Summary]; fixture.Bom = bom ?? [Occ("C1", 1, false, 1m, "EA")];
        fixture.Service = new WorkOrderImmediateShortageService(new FakeWorkspaceConfigurationService(Workspace), store,
            new DelegateWorkOrderSummaryReader((site, part, _, _, _, _, _, _) => { fixture.PlanningWindowReads.Add((site, part)); return Task.FromResult(fixture.Window); }, (_, _, _) => Task.FromResult<WorkOrderSummary?>(fixture.Summary)),
            new DelegateWorkOrderMaterialReader((_, _, _) =>
            {
                fixture.MaterialReads++;
                if (fixture.MaterialReadFails) throw new InvalidOperationException("QAD unavailable");
                return Task.FromResult(fixture.MaterialLines);
            }),
            new DelegateBomSourceReader((_, _, date, _) => { fixture.BomReads++; fixture.BomEffectiveDate = date; return Task.FromResult(fixture.Bom); }),
            new DelegateCommittedWorkOrderPopulationReader((_, _, _, _, _) => Task.FromResult(committed ?? [Committed("WO1", status is "R" or "A" ? status : "R", 10m, 0m)])),
            new DelegateHardAllocationReader((_, _, _, _) => Task.FromResult(hard ?? [])),
            new DelegateInventoryPositionReader((_, parts, _, _, _) => Task.FromResult<IReadOnlyList<InventoryPosition>>(parts.Select(p => new InventoryPosition(p, new UsableInventoryPosition(usable, new Dictionary<InventoryActivity, decimal>()))).ToList())),
            new DelegateIssuePolicyReader((_, _, _) => Task.FromResult(issuePolicy)), new DelegateIssueDaysReader((_, _) => Task.FromResult<int?>(7)),
            new DelegateNextPurchaseOrderReader((_, _, _) => { fixture.PurchaseOrderReads++; return Task.FromResult(po); }),
            new DelegateKssScheduleReader((_, _, _, _) => Task.FromResult(isKss)), new InMemoryWorkOrderImmediateMaterialCacheStore(), NullLogger<WorkOrderImmediateShortageService>.Instance);
        return fixture;
    }

    private static SnapshotId Seed(IMpsSnapshotStore store) { var snapshot = new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, "SW", [new("BUILD", "")], []); store.SetLoaded(Workspace.AssignmentId, snapshot); return snapshot.Id; }
    private static BomOccurrence Occ(string part, int level, bool phantom, decimal quantity, string? uom, string pmCode = "P", string? masterPmCode = "P") => new($"{level}/{part}", level, part, pmCode, phantom, part, quantity, null, uom, masterPmCode);
    private static WorkOrderMaterialLine ActualLineWithMasterPm(string? masterPmCode) =>
        new("C1", "Component", 10m, 0m, false, "EA", IsMasterPmCodeReliable: !string.IsNullOrWhiteSpace(masterPmCode));
    private static CommittedWorkOrderComponent Committed(string woid, string status, decimal required, decimal issued, DateOnly? due = null, DateOnly? release = null) => new(woid, status, null, due ?? Today, release ?? Today, "C1", required, issued, "EA");

    private sealed class TestFixture { public WorkOrderImmediateShortageService Service { get; set; } = null!; public IMpsSnapshotStore Store { get; init; } = null!; public SnapshotId Snapshot { get; init; } public WorkOrderSummary Summary { get; set; } = null!; public IReadOnlyList<WorkOrderSummary> Window { get; set; } = []; public IReadOnlyList<WorkOrderMaterialLine> MaterialLines { get; set; } = [new("C1", "C", 10m, 0m, false, "EA")]; public bool MaterialReadFails { get; set; } public IReadOnlyList<BomOccurrence> Bom { get; set; } = []; public List<(string Site, string Part)> PlanningWindowReads { get; } = []; public int MaterialReads; public int BomReads; public int PurchaseOrderReads; public DateOnly? BomEffectiveDate; }
}
