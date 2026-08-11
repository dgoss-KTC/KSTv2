using Kst.Domain.Common;

namespace Kst.Application.PartDetail;

/// <summary>
/// Cached PartDetail for one workspace/parent-part, tagged with the MPS snapshot identity it was
/// loaded against (see <c>Kst.Application.Mps.MpsSnapshot.Id</c>). A cache lookup that finds an entry
/// whose <see cref="LoadedAgainstMpsSnapshotId"/> no longer matches the workspace's current MPS
/// snapshot is still retained and used as a stale last-good fallback if a fresh QAD read fails.
/// </summary>
public sealed record PartDetailCacheEntry(
    Guid WorkspaceId,
    string ParentPart,
    SnapshotId LoadedAgainstMpsSnapshotId,
    PartDetail Detail
);
