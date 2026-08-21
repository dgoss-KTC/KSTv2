using System.Collections.Concurrent;
using Kst.Application.ComponentDetail;

namespace Kst.Infrastructure.ComponentDetail;

/// <summary>
/// Thread-safe in-memory <see cref="IComponentDetailCacheStore"/>. No persistence across process
/// restart, matching the accepted Stage 6/8D.3 lazy-detail caches. Keyed by
/// (WorkspaceId, ComponentPart) — a structural mirror of <c>InMemoryBomCacheStore</c>/
/// <c>InMemoryPartDetailCacheStore</c>; the Site compatibility field of the accepted Component
/// Detail business identity lives on the entry and is enforced by <c>ComponentDetailService</c>,
/// not the key.
/// </summary>
public sealed class InMemoryComponentDetailCacheStore : IComponentDetailCacheStore
{
    private readonly ConcurrentDictionary<(Guid WorkspaceId, string ComponentPart), ComponentDetailCacheEntry> _entries = new();

    public ComponentDetailCacheEntry? Get(Guid workspaceId, string componentPart) =>
        _entries.TryGetValue((workspaceId, Key(componentPart)), out var entry) ? entry : null;

    public void Set(Guid workspaceId, string componentPart, ComponentDetailCacheEntry entry) =>
        _entries[(workspaceId, Key(componentPart))] = entry;

    private static string Key(string componentPart) => componentPart.Trim().ToUpperInvariant();
}
