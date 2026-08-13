using Kst.Domain.Common;
using Kst.Domain.WorkOrders;

namespace Kst.Application.WorkOrders;

/// <summary>
/// Per-(workspace, MPS snapshot generation, immediate parent WOID, manufactured component part,
/// target depth) candidate Work Order cache. Implementations live in Kst.Infrastructure. Keyed on the
/// immediate parent WOID (not just the component part) so the same manufactured component drilled
/// from two different parent work orders is never accidentally aliased to one cached result.
/// </summary>
public interface IWorkOrderCandidateCacheStore
{
    WorkOrderCandidateCacheEntry? Get(
        Guid workspaceId, SnapshotId mpsSnapshotId, string immediateParentWoid, string componentPart, int targetDepth);

    void Set(
        Guid workspaceId, SnapshotId mpsSnapshotId, string immediateParentWoid, string componentPart, int targetDepth,
        WorkOrderCandidateCacheEntry entry);
}

public sealed record WorkOrderCandidateCacheEntry(
    Guid WorkspaceId,
    SnapshotId MpsSnapshotId,
    string ImmediateParentWoid,
    string ComponentPart,
    int TargetDepth,
    CandidateWorkOrdersResult Result
);
