using Kst.Domain.Common;
using Kst.Domain.WorkOrders;

namespace Kst.Application.WorkOrders;

/// <summary>
/// Per-(workspace, MPS snapshot generation, WOID) material/kitting-line cache. Implementations live
/// in Kst.Infrastructure. See <see cref="IWorkOrderSummaryCacheStore"/> for why the snapshot id is
/// part of the key rather than a separate staleness check.
/// </summary>
public interface IWorkOrderMaterialCacheStore
{
    WorkOrderMaterialCacheEntry? Get(Guid workspaceId, SnapshotId mpsSnapshotId, string woid);

    void Set(Guid workspaceId, SnapshotId mpsSnapshotId, string woid, WorkOrderMaterialCacheEntry entry);
}

public sealed record WorkOrderMaterialCacheEntry(
    Guid WorkspaceId,
    SnapshotId MpsSnapshotId,
    string Woid,
    IReadOnlyList<WorkOrderMaterialLine> Lines
);
