namespace Kst.Domain.Shortages;

/// <summary>One Stage 9 analytical result at the binding Work Order + Component grain.</summary>
public sealed record ComponentRow(
    ComponentRequirement Requirement,
    UsableInventoryPosition InventoryPosition,
    AllocationMode AllocationMode,
    decimal UsableHardAllocationToThisWoComponent,
    decimal OwnHardCoverage,
    decimal? UncoveredRequirement,
    decimal? AvailableQuantityAtEvaluation,
    decimal? AllocatedQuantity,
    decimal? ShortQuantity,
    MaterialStatus MaterialStatus,
    IncomingContext? Incoming,
    decimal? IssuedQuantity,
    decimal? VarianceQuantity,
    decimal? IssuedPercent,
    bool? IsOverIssued,
    string? Diagnostic)
{
    /// <summary>Issue-policy context only; it never changes Stage 9 coverage or status arithmetic.</summary>
    public bool IsFloorStockOrNonIssued { get; init; }
}
