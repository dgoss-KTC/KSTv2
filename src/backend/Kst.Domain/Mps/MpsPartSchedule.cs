namespace Kst.Domain.Mps;

/// <summary>
/// One parent-part row of the MPS grid. Site, workspace identity, snapshot identity/time, selected
/// date basis, and horizon are owned by the surrounding snapshot/view, not repeated here.
/// </summary>
public sealed record MpsPartSchedule(
    string ParentPart,
    string? Description,
    IReadOnlyList<MpsBucket> Buckets
);
