using Kst.Domain.Shortages;

namespace Kst.Domain.Tests.Shortages;

public sealed class ImmediateMaterialCalculatorTests
{
    private static readonly ComponentRequirement Requirement = new("C1", "Component", RequirementSource.ActualWo, 10m, 2m, "EA");
    private static readonly UsableInventoryPosition Position = new(20m, new Dictionary<InventoryActivity, decimal>());

    [Theory]
    [InlineData(2.9, "EA", 2)]
    [InlineData(2.9, "FT", 2.9)]
    [InlineData(-2.9, "EA", -3)]
    public void NormalizeQuantity_Floors_Only_Ea(decimal quantity, string unitOfMeasure, decimal expected) =>
        Assert.Equal(expected, ImmediateMaterialCalculator.NormalizeQuantity(quantity, unitOfMeasure));

    [Fact]
    public void RemainingRequirement_Clamps_OverIssue_To_Zero()
    {
        Assert.Equal(0m, ImmediateMaterialCalculator.CalculateRemainingRequirement(10m, 12m));
        Assert.Equal(8m, ImmediateMaterialCalculator.CalculateRemainingRequirement(10m, 2m));
    }

    [Fact]
    public void ProjectedBuildQuantity_Subtracts_Completed_And_Rejected_Then_Clamps()
    {
        Assert.Equal(7m, ImmediateMaterialCalculator.CalculateProjectedBuildQuantity(12m, 3m, 2m));
        Assert.Equal(0m, ImmediateMaterialCalculator.CalculateProjectedBuildQuantity(12m, 9m, 4m));
    }

    [Fact]
    public void ProjectedRequirement_Consolidates_NonPhantom_Paths_Extends_Then_EaRounds()
    {
        var result = ImmediateMaterialCalculator.CalculateProjectedRequirement(
            [
                new BomRequirementPath([1.25m], false),
                new BomRequirementPath([0.5m, 0.5m], false),
                new BomRequirementPath([100m], true)
            ],
            projectedBuildQuantity: 2.5m,
            unitOfMeasure: "EA");

        Assert.Equal(3m, result);
    }

    [Fact]
    public void OwnHardCoverage_Is_Clamped_To_RemainingRequirement_And_Excluded_From_FreePool()
    {
        var remaining = ImmediateMaterialCalculator.CalculateRemainingRequirement(10m, 2m);

        Assert.Equal(8m, ImmediateMaterialCalculator.CalculateOwnHardCoverage(remaining, 20m));
        Assert.Equal(0m, ImmediateMaterialCalculator.CalculateUncoveredRequirement(remaining, 8m));
        Assert.Equal(12m, ImmediateMaterialCalculator.CalculateResidualFreeInventory(20m, 8m));
        Assert.Equal(0m, ImmediateMaterialCalculator.CalculateResidualFreeInventory(5m, 8m));
    }

    [Fact]
    public void CommittedAllocation_Uses_R_Before_A_And_DueDate_For_Falldown_And_ForwardDue()
    {
        var allocations = ImmediateMaterialCalculator.AllocateCommittedSequentially(
        [
            Input("A-late", "A", PlanningBucketContext.ForwardDue, due: new(2026, 9, 10)),
            Input("R-late", "R", PlanningBucketContext.Falldown, due: new(2026, 9, 5)),
            Input("R-early", "R", PlanningBucketContext.ForwardDue, due: new(2026, 9, 1)),
        ], residualFreeInventory: 10m);

        Assert.Equal(["R-early", "R-late", "A-late"], allocations.Select(a => a.WoId));
        Assert.Equal([6m, 4m, 0m], allocations.Select(a => a.AllocatedQuantity));
    }

    [Fact]
    public void CommittedAllocation_Uses_ReleaseDate_In_ForwardRelease_Context_And_Woid_ToBreak_Ties()
    {
        var allocations = ImmediateMaterialCalculator.AllocateCommittedSequentially(
        [
            Input("WO2", "R", PlanningBucketContext.ForwardRelease, due: new(2026, 9, 1), release: new(2026, 9, 2)),
            Input("WO1", "R", PlanningBucketContext.ForwardRelease, due: new(2026, 8, 1), release: new(2026, 9, 2)),
        ], residualFreeInventory: 6m);

        Assert.Equal(["WO1", "WO2"], allocations.Select(a => a.WoId));
        Assert.Equal([6m, 0m], allocations.Select(a => a.AllocatedQuantity));
    }

    [Fact]
    public void CommittedAllocation_Excludes_Uncommitted_WorkOrders()
    {
        var allocations = ImmediateMaterialCalculator.AllocateCommittedSequentially(
            [Input("E1", "E", PlanningBucketContext.ForwardDue), Input("F1", "F", PlanningBucketContext.ForwardDue)], 10m);

        Assert.Empty(allocations);
    }

    [Fact]
    public void CommittedAllocation_Treats_HumanEntered_Status_Case_Insensitively()
    {
        var allocations = ImmediateMaterialCalculator.AllocateCommittedSequentially(
            [Input("a-late", "a", PlanningBucketContext.ForwardDue, due: new(2026, 9, 3)), Input("r-first", "r", PlanningBucketContext.ForwardDue, due: new(2026, 9, 4))], 6m);

        Assert.Equal(["r-first", "a-late"], allocations.Select(allocation => allocation.WoId));
    }

    [Fact]
    public void Evaluate_Credits_HardAllocation_Before_Residual_And_Does_Not_Create_Inventory_Credit()
    {
        var row = ImmediateMaterialCalculator.Evaluate(Requirement, Position, Context("R"), usableHardAllocationToThisWoComponent: 20m, availableQuantityAtEvaluation: 12m);

        Assert.Equal(8m, row.OwnHardCoverage);
        Assert.Equal(0m, row.UncoveredRequirement);
        Assert.Equal(0m, row.AllocatedQuantity);
        Assert.Equal(0m, row.ShortQuantity);
        Assert.Equal(MaterialStatus.OnHand, row.MaterialStatus);
        Assert.Equal(AllocationMode.HardAllocatedCommitted, row.AllocationMode);
    }

    [Fact]
    public void Evaluate_Uses_AdvisoryPool_Without_Changing_It_And_Classifies_Short()
    {
        var row = ImmediateMaterialCalculator.Evaluate(Requirement, Position, Context("F"), usableHardAllocationToThisWoComponent: 3m, availableQuantityAtEvaluation: 2m);

        Assert.Equal(5m, row.UncoveredRequirement);
        Assert.Equal(2m, row.AllocatedQuantity);
        Assert.Equal(3m, row.ShortQuantity);
        Assert.Equal(AllocationMode.AdvisorySharedPool, row.AllocationMode);
        Assert.Equal(MaterialStatus.Short, row.MaterialStatus);
    }

    [Fact]
    public void Evaluate_Leaves_Manufactured_Component_Visible_But_Excludes_It_From_Shortage_Analysis()
    {
        var row = ImmediateMaterialCalculator.Evaluate(Requirement with { IsManufactured = true }, Position, Context("R"), 0m, 0m);

        Assert.Equal(MaterialStatus.NotApplicable, row.MaterialStatus);
        Assert.Null(row.ShortQuantity);
        Assert.Null(row.Diagnostic);
    }

    [Fact]
    public void Evaluate_Leaves_NonManufactured_Component_Eligible_Regardless_Of_Its_PmCode_Category()
    {
        // IsManufactured is true only for the authoritative M P/M code; non-M codes remain in scope.
        var row = ImmediateMaterialCalculator.Evaluate(Requirement with { IsManufactured = false }, Position, Context("R"), 0m, 0m);

        Assert.Equal(MaterialStatus.Short, row.MaterialStatus);
        Assert.Equal(8m, row.ShortQuantity);
    }

    [Fact]
    public void Evaluate_Gives_Unknown_Precedence_When_Requirement_Or_Position_Is_Unreliable()
    {
        var unreliableRequirement = Requirement with { IsReliable = false };
        var unreliablePosition = Position with { IsReliable = false };

        Assert.Equal(MaterialStatus.Unknown, ImmediateMaterialCalculator.Evaluate(unreliableRequirement, Position, Context("R"), 0m, 0m).MaterialStatus);
        Assert.Equal(MaterialStatus.Unknown, ImmediateMaterialCalculator.Evaluate(Requirement, unreliablePosition, Context("R"), 0m, 0m).MaterialStatus);
    }

    [Fact]
    public void Evaluate_Exposes_Actual_Issue_Presentation_And_Projected_Null_Semantics()
    {
        var actual = ImmediateMaterialCalculator.Evaluate(Requirement with { IssuedQuantity = 12m }, Position, Context("R"), 0m, 0m);
        var projected = ImmediateMaterialCalculator.Evaluate(Requirement with { Source = RequirementSource.ProjectedBom }, Position, Context("F"), 0m, 0m);

        Assert.Equal(12m, actual.IssuedQuantity);
        Assert.Equal(-2m, actual.VarianceQuantity);
        Assert.Equal(120m, actual.IssuedPercent);
        Assert.True(actual.IsOverIssued);
        Assert.Equal(0m, actual.UncoveredRequirement);
        Assert.Null(projected.IssuedQuantity);
        Assert.Null(projected.VarianceQuantity);
        Assert.Null(projected.IssuedPercent);
        Assert.Null(projected.IsOverIssued);
    }

    [Fact]
    public void Evaluate_Uses_Null_Shortage_For_Unreliable_Component()
    {
        var row = ImmediateMaterialCalculator.Evaluate(Requirement with { IsReliable = false }, Position, Context("R"), 0m, 0m);

        Assert.Equal(MaterialStatus.Unknown, row.MaterialStatus);
        Assert.Null(row.ShortQuantity);
        Assert.Null(row.Incoming);
    }

    [Fact]
    public void Evaluate_Uses_Null_IssuedPercent_When_Actual_Normalized_Requirement_Is_Zero()
    {
        var row = ImmediateMaterialCalculator.Evaluate(
            Requirement with { AdjustedRequiredQuantity = 0m, IssuedQuantity = 1m },
            Position, Context("R"), 0m, 0m);

        Assert.Equal(1m, row.IssuedQuantity);
        Assert.Equal(-1m, row.VarianceQuantity);
        Assert.Null(row.IssuedPercent);
        Assert.True(row.IsOverIssued);
        Assert.Equal(0m, row.UncoveredRequirement);
    }

    private static CommittedAllocationInput Input(string id, string status, PlanningBucketContext context, DateOnly? due = null, DateOnly? release = null) =>
        new(Context(status, id, context, due, release), 6m);

    private static WorkOrderContext Context(string status, string id = "WO1", PlanningBucketContext context = PlanningBucketContext.ForwardDue, DateOnly? due = null, DateOnly? release = null) =>
        new(id, "BUILD", status, null, due, release, 10m, context);
}
