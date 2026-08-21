using Kst.Domain.Common;

namespace Kst.Application.ComponentDetail;

/// <summary>
/// Cached complete successful Component Detail composition for one workspace/component-part,
/// tagged with the MPS snapshot identity it was loaded against (see
/// <c>Kst.Application.Mps.MpsSnapshot.Id</c>) — the only workspace freshness-generation identity
/// in the repository (see <see cref="ComponentDetailService"/>). <see cref="Site"/> is the
/// explicit compatibility field of the accepted business identity (Site + ComponentPart): a
/// cached entry is usable — as a fresh hit or as a stale last-good fallback — only when the
/// workspace's current Site matches. No <see cref="EffectiveDate"/>-equivalent field exists;
/// unlike BOM, Component Detail facts are not scoped to a caller-relative effective date.
/// </summary>
public sealed record ComponentDetailCacheEntry(
    Guid WorkspaceId,
    string Site,
    string ComponentPart,
    SnapshotId LoadedAgainstMpsSnapshotId,
    ComponentDetail Detail);
