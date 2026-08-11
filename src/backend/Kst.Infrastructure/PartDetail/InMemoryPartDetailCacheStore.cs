using System.Collections.Concurrent;
using Kst.Application.PartDetail;

namespace Kst.Infrastructure.PartDetail;

/// <summary>
/// Thread-safe in-memory <see cref="IPartDetailCacheStore"/>. No persistence across process restart,
/// matching the accepted Stage 6 contract (in-memory only). Mirrors <c>InMemoryMpsSnapshotStore</c>.
/// </summary>
public sealed class InMemoryPartDetailCacheStore : IPartDetailCacheStore
{
    private readonly ConcurrentDictionary<(Guid WorkspaceId, string ParentPart), PartDetailCacheEntry> _entries = new();

    public PartDetailCacheEntry? Get(Guid workspaceId, string parentPart) =>
        _entries.TryGetValue((workspaceId, Key(parentPart)), out var entry) ? entry : null;

    public void Set(Guid workspaceId, string parentPart, PartDetailCacheEntry entry) =>
        _entries[(workspaceId, Key(parentPart))] = entry;

    private static string Key(string parentPart) => parentPart.Trim().ToUpperInvariant();
}
