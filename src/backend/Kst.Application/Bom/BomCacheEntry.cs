using Kst.Domain.Common;

namespace Kst.Application.Bom;

/// <summary>
/// Cached complete successful BOM composition for one workspace/parent-part, tagged with the
/// MPS snapshot identity it was loaded against (see <c>Kst.Application.Mps.MpsSnapshot.Id</c>).
/// <see cref="Site"/> and <see cref="EffectiveDate"/> are explicit compatibility fields of the
/// accepted business identity (Site + ParentPart + EffectiveDate): a cached entry is usable —
/// as a fresh hit or as a stale last-good fallback — only when the workspace's current Site and
/// the current effective date both match. A cached BOM from another Site or another effective
/// date is NEVER returned.
/// </summary>
public sealed record BomCacheEntry(
    Guid WorkspaceId,
    string Site,
    string ParentPart,
    DateOnly EffectiveDate,
    SnapshotId LoadedAgainstMpsSnapshotId,
    Bom Bom);
