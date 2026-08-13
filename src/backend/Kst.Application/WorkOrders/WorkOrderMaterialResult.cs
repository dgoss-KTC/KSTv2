using Kst.Domain.Common;
using Kst.Domain.WorkOrders;

namespace Kst.Application.WorkOrders;

/// <summary>
/// Outcome of a WOID material/kitting-line request, mapped to HTTP semantics by Kst.Api.
/// <see cref="Lines"/>, <see cref="Kitting"/> and <see cref="SnapshotId"/> are populated only for
/// <see cref="Loaded"/>.
/// </summary>
public enum WorkOrderMaterialOutcomeKind
{
    /// <summary>Composed successfully (may be an empty list when the WO has no applicable material lines).</summary>
    Loaded,

    /// <summary>The workspace has no current MPS snapshot; this request must not trigger an MPS load.</summary>
    MpsNotLoaded,

    /// <summary>The caller's requested snapshot id no longer matches the workspace's current MPS snapshot.</summary>
    SnapshotChanged,

    /// <summary>The QAD read failed. Never a cached/stale fallback for Stage 7 — retry is expected.</summary>
    Unavailable
}

public sealed record WorkOrderMaterialResult(
    WorkOrderMaterialOutcomeKind Kind,
    IReadOnlyList<WorkOrderMaterialLine>? Lines = null,
    KittingSummary? Kitting = null,
    SnapshotId? SnapshotId = null)
{
    public static WorkOrderMaterialResult Loaded(SnapshotId snapshotId, IReadOnlyList<WorkOrderMaterialLine> lines) =>
        new(WorkOrderMaterialOutcomeKind.Loaded, lines, KittingSummary.FromMaterialLines(lines), snapshotId);

    public static WorkOrderMaterialResult MpsNotLoaded { get; } = new(WorkOrderMaterialOutcomeKind.MpsNotLoaded);
    public static WorkOrderMaterialResult SnapshotChanged { get; } = new(WorkOrderMaterialOutcomeKind.SnapshotChanged);
    public static WorkOrderMaterialResult Unavailable { get; } = new(WorkOrderMaterialOutcomeKind.Unavailable);
}
