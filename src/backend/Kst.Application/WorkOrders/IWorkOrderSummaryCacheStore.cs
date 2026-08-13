using Kst.Domain.Common;
using Kst.Domain.WorkOrders;

namespace Kst.Application.WorkOrders;

/// <summary>
/// Per-(workspace, MPS snapshot generation, WOID) Work Order summary cache. Implementations live in
/// Kst.Infrastructure. Keying on the MPS snapshot id (not just workspace+WOID) is deliberate: unlike
/// Stage 6 PartDetail, Stage 7 investigation data must NOT survive a successful MPS refresh as a
/// stale fallback — a lookup against a superseded snapshot id is a plain cache miss, never reused.
/// </summary>
public interface IWorkOrderSummaryCacheStore
{
    WorkOrderSummaryCacheEntry? Get(Guid workspaceId, SnapshotId mpsSnapshotId, string woid);

    void Set(Guid workspaceId, SnapshotId mpsSnapshotId, string woid, WorkOrderSummaryCacheEntry entry);
}

public sealed record WorkOrderSummaryCacheEntry(
    Guid WorkspaceId,
    SnapshotId MpsSnapshotId,
    string Woid,
    WorkOrderSummary Summary
);
