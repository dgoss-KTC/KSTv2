namespace Kst.Application.ComponentDetail;

/// <summary>
/// Per-(workspace, component-part) Component Detail cache. Implementations live in
/// Kst.Infrastructure. In-memory only (no persisted cache), matching the accepted Stage 6/8D.3
/// lazy-detail caches. Not a replacement for
/// <see cref="Kst.Application.Mps.IMpsSnapshotStore"/>, which owns the MPS source-fact snapshot
/// this cache is keyed against for freshness. Site compatibility is a payload concern enforced
/// by <see cref="ComponentDetailService"/>, not a key concern, mirroring <c>IBomCacheStore</c>/
/// <c>IPartDetailCacheStore</c>.
/// </summary>
public interface IComponentDetailCacheStore
{
    ComponentDetailCacheEntry? Get(Guid workspaceId, string componentPart);

    void Set(Guid workspaceId, string componentPart, ComponentDetailCacheEntry entry);
}
