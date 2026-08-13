using Kst.Domain.Common;
using Kst.Domain.WorkOrders;

namespace Kst.Application.WorkOrders;

/// <summary>
/// Outcome of a candidate-subassembly Work Order request, mapped to HTTP semantics by Kst.Api.
/// <see cref="Result"/> and <see cref="SnapshotId"/> are populated only for <see cref="Loaded"/>.
/// </summary>
public enum WorkOrderCandidateOutcomeKind
{
    /// <summary>Composed successfully (may have zero candidates).</summary>
    Loaded,

    /// <summary>The workspace has no current MPS snapshot; this request must not trigger an MPS load.</summary>
    MpsNotLoaded,

    /// <summary>The caller's requested snapshot id no longer matches the workspace's current MPS snapshot.</summary>
    SnapshotChanged,

    /// <summary>The immediate parent WOID could not be resolved to any A/F/R Work Order.</summary>
    WorkOrderNotFound,

    /// <summary>The immediate parent Work Order was found but has no usable Due Date; candidates cannot be bounded.</summary>
    ParentDueDateUnavailable,

    /// <summary>The requested component is not a manufactured (pt_pm_code='M') line on the immediate parent's material list.</summary>
    ComponentNotManufactured,

    /// <summary>The QAD read failed. Never a cached/stale fallback for Stage 7 — retry is expected.</summary>
    Unavailable
}

public sealed record WorkOrderCandidateResult(
    WorkOrderCandidateOutcomeKind Kind,
    CandidateWorkOrdersResult? Result = null,
    SnapshotId? SnapshotId = null)
{
    public static WorkOrderCandidateResult Loaded(SnapshotId snapshotId, CandidateWorkOrdersResult result) =>
        new(WorkOrderCandidateOutcomeKind.Loaded, result, snapshotId);

    public static WorkOrderCandidateResult MpsNotLoaded { get; } = new(WorkOrderCandidateOutcomeKind.MpsNotLoaded);
    public static WorkOrderCandidateResult SnapshotChanged { get; } = new(WorkOrderCandidateOutcomeKind.SnapshotChanged);
    public static WorkOrderCandidateResult WorkOrderNotFound { get; } = new(WorkOrderCandidateOutcomeKind.WorkOrderNotFound);
    public static WorkOrderCandidateResult ParentDueDateUnavailable { get; } = new(WorkOrderCandidateOutcomeKind.ParentDueDateUnavailable);
    public static WorkOrderCandidateResult ComponentNotManufactured { get; } = new(WorkOrderCandidateOutcomeKind.ComponentNotManufactured);
    public static WorkOrderCandidateResult Unavailable { get; } = new(WorkOrderCandidateOutcomeKind.Unavailable);
}
