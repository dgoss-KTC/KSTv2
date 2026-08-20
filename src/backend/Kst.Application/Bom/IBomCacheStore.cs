namespace Kst.Application.Bom;

/// <summary>
/// Per-(workspace, parent-part) BOM cache. Implementations live in Kst.Infrastructure.
/// In-memory only (no persisted BOM cache), matching the accepted Stage 6/7 lazy-detail
/// caches. Not a replacement for <see cref="Kst.Application.Mps.IMpsSnapshotStore"/>, which
/// owns the MPS source-fact snapshot this cache is keyed against for freshness. Site and
/// effective-date compatibility is a payload concern enforced by <see cref="BomService"/>, not
/// a key concern, mirroring <c>IPartDetailCacheStore</c>.
/// </summary>
public interface IBomCacheStore
{
    BomCacheEntry? Get(Guid workspaceId, string parentPart);

    void Set(Guid workspaceId, string parentPart, BomCacheEntry entry);
}
