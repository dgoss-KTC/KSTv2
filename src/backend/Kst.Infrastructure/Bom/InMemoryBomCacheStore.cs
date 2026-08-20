using System.Collections.Concurrent;
using Kst.Application.Bom;

namespace Kst.Infrastructure.Bom;

/// <summary>
/// Thread-safe in-memory <see cref="IBomCacheStore"/>. No persistence across process restart,
/// matching the accepted Stage 6/7 lazy-detail caches. Keyed by (WorkspaceId, ParentPart) — a
/// structural mirror of <c>InMemoryPartDetailCacheStore</c>; the Site/EffectiveDate
/// compatibility fields of the accepted BOM business identity live on the entry and are
/// enforced by <c>BomService</c>, not the key.
/// </summary>
public sealed class InMemoryBomCacheStore : IBomCacheStore
{
    private readonly ConcurrentDictionary<(Guid WorkspaceId, string ParentPart), BomCacheEntry> _entries = new();

    public BomCacheEntry? Get(Guid workspaceId, string parentPart) =>
        _entries.TryGetValue((workspaceId, Key(parentPart)), out var entry) ? entry : null;

    public void Set(Guid workspaceId, string parentPart, BomCacheEntry entry) =>
        _entries[(workspaceId, Key(parentPart))] = entry;

    private static string Key(string parentPart) => parentPart.Trim().ToUpperInvariant();
}
