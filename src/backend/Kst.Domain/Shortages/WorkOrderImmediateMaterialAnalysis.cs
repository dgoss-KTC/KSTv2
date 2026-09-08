namespace Kst.Domain.Shortages;

/// <summary>Complete Stage 9 immediate material analysis for one work order.</summary>
public sealed record WorkOrderImmediateMaterialAnalysis(
    WorkOrderContext WorkOrder,
    IReadOnlyList<ComponentRow> ComponentRows,
    string? Diagnostic = null);
