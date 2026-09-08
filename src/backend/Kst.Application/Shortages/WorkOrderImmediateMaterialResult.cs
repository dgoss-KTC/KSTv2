using Kst.Domain.Common;
using Kst.Domain.Shortages;

namespace Kst.Application.Shortages;

public enum WorkOrderImmediateMaterialOutcomeKind { Loaded, MpsNotLoaded, SnapshotChanged, WorkOrderNotInImmediateWindow, Unavailable }

public sealed record WorkOrderImmediateMaterialResult(
    WorkOrderImmediateMaterialOutcomeKind Kind, WorkOrderImmediateMaterialAnalysis? Analysis = null,
    SnapshotId? SnapshotId = null, string? Diagnostic = null)
{
    public static WorkOrderImmediateMaterialResult Loaded(SnapshotId snapshotId, WorkOrderImmediateMaterialAnalysis analysis) => new(WorkOrderImmediateMaterialOutcomeKind.Loaded, analysis, snapshotId);
    public static WorkOrderImmediateMaterialResult MpsNotLoaded { get; } = new(WorkOrderImmediateMaterialOutcomeKind.MpsNotLoaded);
    public static WorkOrderImmediateMaterialResult SnapshotChanged { get; } = new(WorkOrderImmediateMaterialOutcomeKind.SnapshotChanged);
    public static WorkOrderImmediateMaterialResult WorkOrderNotInImmediateWindow { get; } = new(WorkOrderImmediateMaterialOutcomeKind.WorkOrderNotInImmediateWindow);
    public static WorkOrderImmediateMaterialResult Unavailable { get; } = new(WorkOrderImmediateMaterialOutcomeKind.Unavailable);
}

public enum WorkOrderImmediateMaterialSummaryOutcomeKind { Loaded, MpsNotLoaded, SnapshotChanged, PartNotInScope, Unavailable }

public sealed record WorkOrderImmediateMaterialSummaryResult(
    WorkOrderImmediateMaterialSummaryOutcomeKind Kind,
    IReadOnlyList<WorkOrderImmediateMaterialAnalysis>? Analyses = null,
    SnapshotId? SnapshotId = null)
{
    public static WorkOrderImmediateMaterialSummaryResult Loaded(SnapshotId snapshotId, IReadOnlyList<WorkOrderImmediateMaterialAnalysis> analyses) => new(WorkOrderImmediateMaterialSummaryOutcomeKind.Loaded, analyses, snapshotId);
    public static WorkOrderImmediateMaterialSummaryResult MpsNotLoaded { get; } = new(WorkOrderImmediateMaterialSummaryOutcomeKind.MpsNotLoaded);
    public static WorkOrderImmediateMaterialSummaryResult SnapshotChanged { get; } = new(WorkOrderImmediateMaterialSummaryOutcomeKind.SnapshotChanged);
    public static WorkOrderImmediateMaterialSummaryResult PartNotInScope { get; } = new(WorkOrderImmediateMaterialSummaryOutcomeKind.PartNotInScope);
    public static WorkOrderImmediateMaterialSummaryResult Unavailable { get; } = new(WorkOrderImmediateMaterialSummaryOutcomeKind.Unavailable);
}
