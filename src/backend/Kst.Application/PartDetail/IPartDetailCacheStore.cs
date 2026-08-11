namespace Kst.Application.PartDetail;

/// <summary>
/// Per-(workspace, parent-part) PartDetail cache. Implementations live in Kst.Infrastructure.
/// In-memory only initially (no persisted/offline PartDetail cache), matching the accepted Stage 6
/// contract. Not a replacement for <see cref="Kst.Application.Mps.IMpsSnapshotStore"/>, which owns the
/// unrelated MPS source-fact snapshot this cache is keyed against.
/// </summary>
public interface IPartDetailCacheStore
{
    PartDetailCacheEntry? Get(Guid workspaceId, string parentPart);

    void Set(Guid workspaceId, string parentPart, PartDetailCacheEntry entry);
}
