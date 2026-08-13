using Kst.Domain.Common;
using Kst.Domain.WorkOrders;

namespace Kst.Application.WorkOrders;

/// <summary>
/// Outcome of a top-level bucket Work Order request, mapped to HTTP semantics by Kst.Api (Stage 7
/// contract). <see cref="WorkOrders"/> and <see cref="SnapshotId"/> are populated only for <see cref="Loaded"/>.
/// </summary>
public enum WorkOrderBucketOutcomeKind
{
    /// <summary>Composed successfully (may be an empty list when the bucket has no A/F/R work orders).</summary>
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

public sealed record WorkOrderBucketResult(
    WorkOrderBucketOutcomeKind Kind,
    IReadOnlyList<WorkOrderSummary>? WorkOrders = null,
    SnapshotId? SnapshotId = null)
{
    public static WorkOrderBucketResult Loaded(SnapshotId snapshotId, IReadOnlyList<WorkOrderSummary> workOrders) =>
        new(WorkOrderBucketOutcomeKind.Loaded, workOrders, snapshotId);

    public static WorkOrderBucketResult MpsNotLoaded { get; } = new(WorkOrderBucketOutcomeKind.MpsNotLoaded);
    public static WorkOrderBucketResult SnapshotChanged { get; } = new(WorkOrderBucketOutcomeKind.SnapshotChanged);
    public static WorkOrderBucketResult PartNotInScope { get; } = new(WorkOrderBucketOutcomeKind.PartNotInScope);
    public static WorkOrderBucketResult BucketNotFound { get; } = new(WorkOrderBucketOutcomeKind.BucketNotFound);
    public static WorkOrderBucketResult Unavailable { get; } = new(WorkOrderBucketOutcomeKind.Unavailable);
}
