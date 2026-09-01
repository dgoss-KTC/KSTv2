using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.WorkOrders;

namespace Kst.Application.WorkOrders;

/// <summary>
/// Per-(workspace, MPS snapshot generation, parent, date basis, bucket) Stage 7R planning-window
/// population cache. Implementations live in Kst.Infrastructure. Keying on the MPS snapshot id
/// (not just workspace+parent) is deliberate: a lookup against a superseded snapshot id is a plain
/// cache miss, never a stale fallback (a new successful MPS refresh must invalidate prior Stage 7
/// investigation data outright). The date basis and bucket are part of the key because the planning
/// population genuinely differs between Due/Release bases and between the full window and a single
/// bucket.
/// </summary>
public interface IWorkOrderPlanningWindowCacheStore
{
    WorkOrderPlanningWindowCacheEntry? Get(
        Guid workspaceId,
        SnapshotId mpsSnapshotId,
        string parentPart,
        MpsDateBasis dateBasis,
        MpsBucketKind? bucketKind,
        DateOnly? weekLabel);

    void Set(
        Guid workspaceId,
        SnapshotId mpsSnapshotId,
        string parentPart,
        MpsDateBasis dateBasis,
        MpsBucketKind? bucketKind,
        DateOnly? weekLabel,
        WorkOrderPlanningWindowCacheEntry entry);
}

public sealed record WorkOrderPlanningWindowCacheEntry(
    Guid WorkspaceId,
    SnapshotId MpsSnapshotId,
    string ParentPart,
    MpsDateBasis DateBasis,
    MpsBucketKind? BucketKind,
    DateOnly? WeekLabel,
    IReadOnlyList<WorkOrderSummary> WorkOrders);
