using Kst.Domain.Common;
using Kst.Domain.ComponentOrders;

namespace Kst.Application.ComponentOrders;

/// <summary>
/// Per-(workspace, MPS snapshot generation, business date) Stage 10 Component Orders cache.
/// Implementations live in Kst.Infrastructure. Keying on the MPS snapshot id (not just workspace)
/// is deliberate: a lookup against a superseded snapshot id is a plain cache miss, never a stale
/// fallback — a new successful MPS refresh must invalidate prior Component Orders results outright
/// and never serve a prior snapshot's result as fresh. The business date is part of the key because
/// the effective-KSS relationship (and any future date-sensitive source facts) genuinely differ
/// between days within the same snapshot generation.
/// </summary>
public interface IComponentOrdersCacheStore
{
    ComponentOrdersCacheEntry? Get(Guid workspaceId, SnapshotId mpsSnapshotId, DateOnly businessDate);

    void Set(ComponentOrdersCacheEntry entry);
}

public sealed record ComponentOrdersCacheEntry(
    Guid WorkspaceId,
    SnapshotId MpsSnapshotId,
    DateOnly BusinessDate,
    IReadOnlyList<ComponentOrderGroup> Groups);
