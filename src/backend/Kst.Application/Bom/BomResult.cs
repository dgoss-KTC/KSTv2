namespace Kst.Application.Bom;

/// <summary>
/// Outcome of a BOM request, mapped to HTTP semantics by Kst.Api. <see cref="Bom"/> is
/// populated only for <see cref="BomOutcomeKind.Loaded"/>.
///
/// Deliberately absent (unlike PartDetail): a missing-parent outcome. A valid in-scope parent
/// with no effective structural rows — or with no scheduler-visible P/M rows — is
/// <see cref="BomOutcomeKind.Loaded"/> with empty <see cref="Bom.Lines"/> (200), never a 404;
/// the workspace's resolved MPS scope is the authoritative parent-scope check.
/// </summary>
public enum BomOutcomeKind
{
    /// <summary>Composed successfully; may be fresh or same-site/same-effective-date stale-last-good (<see cref="Bom.IsStale"/>).</summary>
    Loaded,

    /// <summary>The workspace has no current MPS snapshot; BOM must not trigger an MPS load.</summary>
    MpsNotLoaded,

    /// <summary>The requested parent is not in the workspace's current resolved MPS parent scope.</summary>
    OutOfScope,

    /// <summary>A BOM/inventory load failed and no compatible (same site + same effective date) cached BOM exists.</summary>
    Unavailable
}

public sealed record BomResult(BomOutcomeKind Kind, Bom? Bom = null)
{
    public static BomResult Loaded(Bom bom) => new(BomOutcomeKind.Loaded, bom);

    public static BomResult MpsNotLoaded { get; } = new(BomOutcomeKind.MpsNotLoaded);

    public static BomResult OutOfScope { get; } = new(BomOutcomeKind.OutOfScope);

    public static BomResult Unavailable { get; } = new(BomOutcomeKind.Unavailable);
}
