using Kst.Domain.Common;
using Kst.Domain.Mps;

namespace Kst.Application.Mps;

/// <summary>
/// The last successfully loaded raw MPS facts for a workspace: resolved parent-part scope plus
/// source rows, before Due/Release-date and horizon projection (which is done locally, per request,
/// via <see cref="Kst.Domain.Mps.MpsScheduleBuilder"/> — never by re-querying QAD).
/// </summary>
public sealed record MpsSnapshot(
    SnapshotId Id,
    DateTimeOffset LoadedAt,
    string Site,
    IReadOnlyList<MpsResolvedPart> ResolvedParts,
    IReadOnlyList<MpsSourceRow> SourceRows
);
