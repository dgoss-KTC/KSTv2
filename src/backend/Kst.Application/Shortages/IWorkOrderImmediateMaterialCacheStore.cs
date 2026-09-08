using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.Shortages;

namespace Kst.Application.Shortages;

public interface IWorkOrderImmediateMaterialCacheStore
{
    WorkOrderImmediateMaterialCacheEntry? Get(Guid workspaceId, SnapshotId snapshotId, string woid, MpsDateBasis dateBasis, DateOnly businessDate);
    void Set(WorkOrderImmediateMaterialCacheEntry entry);
    WorkOrderImmediateMaterialBatchCacheEntry? GetBatch(Guid workspaceId, SnapshotId snapshotId, string buildPart, MpsDateBasis dateBasis, DateOnly businessDate);
    void SetBatch(WorkOrderImmediateMaterialBatchCacheEntry entry);
}

public sealed record WorkOrderImmediateMaterialCacheEntry(
    Guid WorkspaceId, SnapshotId SnapshotId, string Woid, MpsDateBasis DateBasis, DateOnly BusinessDate,
    WorkOrderImmediateMaterialAnalysis Analysis);

/// <summary>Shared calculation result for one parent planning window. Detail enrichment is intentionally excluded.</summary>
public sealed record WorkOrderImmediateMaterialBatchCacheEntry(
    Guid WorkspaceId, SnapshotId SnapshotId, string BuildPart, MpsDateBasis DateBasis, DateOnly BusinessDate,
    IReadOnlyList<WorkOrderImmediateMaterialAnalysis> Analyses);
