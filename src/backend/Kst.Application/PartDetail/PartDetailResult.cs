namespace Kst.Application.PartDetail;

/// <summary>
/// Outcome of a PartDetail request, mapped to HTTP semantics by Kst.Api (see the accepted Stage 6
/// contract, section 15). <see cref="Detail"/> is populated only for <see cref="Loaded"/>.
/// </summary>
public enum PartDetailOutcomeKind
{
    /// <summary>Composed successfully; may be fresh or stale-last-good (<see cref="PartDetail.IsStale"/>).</summary>
    Loaded,

    /// <summary>The workspace has no current MPS snapshot; PartDetail must not trigger an MPS load.</summary>
    MpsNotLoaded,

    /// <summary>The requested part is not in the workspace's current resolved MPS parent scope.</summary>
    OutOfScope,

    /// <summary>The part is in scope, but no <c>pt_mstr</c> row exists for it.</summary>
    MissingPart,

    /// <summary>QAD read failed and no usable cached PartDetail exists for this workspace/part.</summary>
    Unavailable
}

public sealed record PartDetailResult(PartDetailOutcomeKind Kind, PartDetail? Detail = null)
{
    public static PartDetailResult Loaded(PartDetail detail) => new(PartDetailOutcomeKind.Loaded, detail);
    public static PartDetailResult MpsNotLoaded { get; } = new(PartDetailOutcomeKind.MpsNotLoaded);
    public static PartDetailResult OutOfScope { get; } = new(PartDetailOutcomeKind.OutOfScope);
    public static PartDetailResult MissingPart { get; } = new(PartDetailOutcomeKind.MissingPart);
    public static PartDetailResult Unavailable { get; } = new(PartDetailOutcomeKind.Unavailable);
}
