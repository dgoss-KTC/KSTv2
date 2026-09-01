using Kst.Domain.Common;
using Kst.Domain.WorkOrders;

namespace Kst.Application.WorkOrders;

/// <summary>
/// Outcome of a Stage 7R planning-window Work Order request, mapped to HTTP semantics by Kst.Api.
/// <see cref="WorkOrders"/> and <see cref="SnapshotId"/> are populated only for <see cref="Loaded"/>.
/// </summary>
public enum WorkOrderPlanningWindowOutcomeKind
{
    /// <summary>Composed successfully (may be an empty list when the window has no eligible work orders).</summary>
    Loaded,

    /// <summary>The workspace has no current MPS snapshot; this request must not trigger an MPS load.</summary>
    MpsNotLoaded,

    /// <summary>The caller's requested snapshot id no longer matches the workspace's current MPS snapshot.</summary>
    SnapshotChanged,

    /// <summary>The requested parent part is not in the workspace's current resolved MPS scope.</summary>
    PartNotInScope,

    /// <summary>No bucket matches the requested kind/week within the projected schedule.</summary>
    BucketNotFound,

    /// <summary>The QAD read failed. Never a cached/stale fallback for Stage 7 — retry is expected.</summary>
    Unavailable
}

public sealed record WorkOrderPlanningWindowResult(
    WorkOrderPlanningWindowOutcomeKind Kind,
    IReadOnlyList<WorkOrderSummary>? WorkOrders = null,
    SnapshotId? SnapshotId = null)
{
    public static WorkOrderPlanningWindowResult Loaded(SnapshotId snapshotId, IReadOnlyList<WorkOrderSummary> workOrders) =>
        new(WorkOrderPlanningWindowOutcomeKind.Loaded, workOrders, snapshotId);

    public static WorkOrderPlanningWindowResult MpsNotLoaded { get; } = new(WorkOrderPlanningWindowOutcomeKind.MpsNotLoaded);
    public static WorkOrderPlanningWindowResult SnapshotChanged { get; } = new(WorkOrderPlanningWindowOutcomeKind.SnapshotChanged);
    public static WorkOrderPlanningWindowResult PartNotInScope { get; } = new(WorkOrderPlanningWindowOutcomeKind.PartNotInScope);
    public static WorkOrderPlanningWindowResult BucketNotFound { get; } = new(WorkOrderPlanningWindowOutcomeKind.BucketNotFound);
    public static WorkOrderPlanningWindowResult Unavailable { get; } = new(WorkOrderPlanningWindowOutcomeKind.Unavailable);
}
