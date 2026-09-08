namespace Kst.Domain.Shortages;

/// <summary>Pure Stage 9 requirement, authoritative hard-allocation, and residual-pool calculations.</summary>
public static class ImmediateMaterialCalculator
{
    public static decimal NormalizeQuantity(decimal quantity, string? unitOfMeasure) =>
        string.Equals(unitOfMeasure, "EA", StringComparison.OrdinalIgnoreCase)
            ? decimal.Floor(quantity)
            : quantity;

    public static decimal CalculateRemainingRequirement(decimal adjustedRequiredQuantity, decimal issuedQuantity) =>
        Math.Max(adjustedRequiredQuantity - issuedQuantity, 0m);

    public static decimal CalculateProjectedBuildQuantity(decimal orderedQuantity, decimal completedQuantity, decimal rejectedQuantity) =>
        Math.Max(orderedQuantity - completedQuantity - rejectedQuantity, 0m);

    /// <summary>
    /// Consolidates already-expanded non-phantom BOM path requirements. Structural expansion stays outside
    /// this calculation, while the relationship quantity-per multiplication remains explicit and testable here.
    /// </summary>
    public static decimal CalculateProjectedRequirement(
        IEnumerable<BomRequirementPath> paths,
        decimal projectedBuildQuantity,
        string? unitOfMeasure)
    {
        var quantity = paths
            .Where(path => !path.IsPhantom)
            .Sum(path => path.RelationshipQuantities.Aggregate(1m, (product, quantityPer) => product * quantityPer))
            * projectedBuildQuantity;

        return NormalizeQuantity(quantity, unitOfMeasure);
    }

    public static decimal CalculateOwnHardCoverage(decimal remainingRequirement, decimal usableHardAllocation) =>
        Math.Min(Math.Max(remainingRequirement, 0m), Math.Max(usableHardAllocation, 0m));

    public static decimal CalculateUncoveredRequirement(decimal remainingRequirement, decimal ownHardCoverage) =>
        Math.Max(remainingRequirement - ownHardCoverage, 0m);

    public static decimal CalculateResidualFreeInventory(decimal physicalUsableQuantity, decimal usableHardAllocations) =>
        Math.Max(physicalUsableQuantity - Math.Max(usableHardAllocations, 0m), 0m);

    public static IReadOnlyList<CommittedAllocation> AllocateCommittedSequentially(
        IEnumerable<CommittedAllocationInput> inputs,
        decimal residualFreeInventory)
    {
        var inventoryRemaining = Math.Max(residualFreeInventory, 0m);
        var allocations = new List<CommittedAllocation>();

        foreach (var input in inputs
                     .Where(input => IsCommitted(input.WorkOrder.Status))
                      .OrderBy(input => string.Equals(input.WorkOrder.Status, "R", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                     .ThenBy(input => GetOrderingDate(input.WorkOrder))
                     .ThenBy(input => input.WorkOrder.WoId, StringComparer.Ordinal))
        {
            var allocated = Math.Min(Math.Max(input.UncoveredRequirement, 0m), inventoryRemaining);
            inventoryRemaining -= allocated;
            allocations.Add(new CommittedAllocation(input.WorkOrder.WoId, allocated, inventoryRemaining));
        }

        return allocations;
    }

    public static ComponentRow Evaluate(
        ComponentRequirement requirement,
        UsableInventoryPosition inventoryPosition,
        WorkOrderContext workOrder,
        decimal usableHardAllocationToThisWoComponent,
        decimal availableQuantityAtEvaluation,
        IncomingContext? incoming = null)
    {
        var isActual = requirement.Source == RequirementSource.ActualWo;
        decimal? issuedQuantity = isActual ? requirement.IssuedQuantity : null;
        decimal? varianceQuantity = isActual ? requirement.AdjustedRequiredQuantity - requirement.IssuedQuantity : null;
        decimal? issuedPercent = isActual && requirement.AdjustedRequiredQuantity > 0m
            ? requirement.IssuedQuantity / requirement.AdjustedRequiredQuantity * 100m
            : null;
        bool? isOverIssued = isActual ? requirement.IssuedQuantity > requirement.AdjustedRequiredQuantity : null;

        if (requirement.IsManufactured)
        {
            return new ComponentRow(requirement, inventoryPosition, AllocationMode.AdvisorySharedPool,
                0m, 0m, null, null, null, null, MaterialStatus.NotApplicable, incoming,
                issuedQuantity, varianceQuantity, issuedPercent, isOverIssued,
                null);
        }

        if (!requirement.IsReliable || !inventoryPosition.IsReliable)
        {
            return new ComponentRow(
                requirement, inventoryPosition, AllocationMode.AdvisorySharedPool,
                Math.Max(usableHardAllocationToThisWoComponent, 0m), 0m,
                null, null, null, null, MaterialStatus.Unknown, incoming,
                issuedQuantity, varianceQuantity, issuedPercent, isOverIssued,
                requirement.IsReliable ? "Inventory position data is not reliable." : "Component unit of measure or master P/M classification is unavailable or inconsistent.");
        }

        var remaining = CalculateRemainingRequirement(requirement.AdjustedRequiredQuantity, requirement.IssuedQuantity);
        var hardCoverage = CalculateOwnHardCoverage(remaining, usableHardAllocationToThisWoComponent);
        var uncovered = CalculateUncoveredRequirement(remaining, hardCoverage);
        var available = Math.Max(availableQuantityAtEvaluation, 0m);
        var allocated = Math.Min(uncovered, available);
        var shortQuantity = uncovered - allocated;
        var mode = IsCommitted(workOrder.Status)
            ? AllocationMode.CommittedSequential
            : AllocationMode.AdvisorySharedPool;
        if (hardCoverage > 0m && uncovered == 0m)
            mode = AllocationMode.HardAllocatedCommitted;

        var status = shortQuantity > 0m ? MaterialStatus.Short : MaterialStatus.OnHand;

        return new ComponentRow(
            requirement, inventoryPosition, mode, Math.Max(usableHardAllocationToThisWoComponent, 0m), hardCoverage,
            uncovered, available, allocated, shortQuantity, status, incoming,
            issuedQuantity, varianceQuantity, issuedPercent, isOverIssued, null);
    }

    private static bool IsCommitted(string status) =>
        string.Equals(status, "R", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "A", StringComparison.OrdinalIgnoreCase);

    private static DateOnly GetOrderingDate(WorkOrderContext workOrder) =>
        workOrder.PlanningBucketContext == PlanningBucketContext.ForwardRelease
            ? workOrder.ReleaseDate ?? DateOnly.MaxValue
            : workOrder.DueDate ?? DateOnly.MaxValue;
}

/// <summary>One committed WO/component demand after own authoritative hard coverage.</summary>
public sealed record CommittedAllocationInput(WorkOrderContext WorkOrder, decimal UncoveredRequirement);

/// <summary>One committed sequential-allocation result, including the shared pool left after it.</summary>
public sealed record CommittedAllocation(string WoId, decimal AllocatedQuantity, decimal InventoryRemaining);

/// <summary>One expanded BOM path for a component requirement; each relationship quantity contributes multiplicatively.</summary>
public sealed record BomRequirementPath(IReadOnlyList<decimal> RelationshipQuantities, bool IsPhantom);
