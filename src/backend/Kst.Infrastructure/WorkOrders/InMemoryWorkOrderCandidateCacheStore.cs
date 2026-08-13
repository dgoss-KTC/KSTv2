using System.Collections.Concurrent;
using Kst.Application.WorkOrders;
using Kst.Domain.Common;

namespace Kst.Infrastructure.WorkOrders;

/// <summary>
/// Thread-safe in-memory <see cref="IWorkOrderCandidateCacheStore"/>. No persistence across process
/// restart. Mirrors <c>InMemoryPartDetailCacheStore</c>.
/// </summary>
public sealed class InMemoryWorkOrderCandidateCacheStore : IWorkOrderCandidateCacheStore
{
    private readonly ConcurrentDictionary<
        (Guid WorkspaceId, SnapshotId MpsSnapshotId, string ImmediateParentWoid, string ComponentPart, int TargetDepth),
        WorkOrderCandidateCacheEntry> _entries = new();

    public WorkOrderCandidateCacheEntry? Get(
        Guid workspaceId, SnapshotId mpsSnapshotId, string immediateParentWoid, string componentPart, int targetDepth) =>
        _entries.TryGetValue(Key(workspaceId, mpsSnapshotId, immediateParentWoid, componentPart, targetDepth), out var entry)
            ? entry
            : null;

    public void Set(
        Guid workspaceId, SnapshotId mpsSnapshotId, string immediateParentWoid, string componentPart, int targetDepth,
        WorkOrderCandidateCacheEntry entry) =>
        _entries[Key(workspaceId, mpsSnapshotId, immediateParentWoid, componentPart, targetDepth)] = entry;

    private static (Guid, SnapshotId, string, string, int) Key(
        Guid workspaceId, SnapshotId mpsSnapshotId, string immediateParentWoid, string componentPart, int targetDepth) =>
        (workspaceId, mpsSnapshotId, immediateParentWoid.Trim().ToUpperInvariant(), componentPart.Trim().ToUpperInvariant(), targetDepth);
}
