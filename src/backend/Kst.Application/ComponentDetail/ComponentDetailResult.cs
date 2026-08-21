namespace Kst.Application.ComponentDetail;

/// <summary>
/// Outcome of a Component Detail request, mapped to HTTP semantics by Kst.Api (accepted Stage
/// 8D.5 contract §14/§17). <see cref="Detail"/> is populated only for <see cref="Loaded"/>.
///
/// Deliberately absent (unlike Bom/PartDetail): an <c>OutOfScope</c> outcome. Component identity
/// is never based on BOM occurrence or the workspace's resolved MPS parent list — any component
/// with a <c>pt_mstr</c> row in the resolved domain is servable regardless of BOM membership.
/// </summary>
public enum ComponentDetailOutcomeKind
{
    /// <summary>Composed successfully; may be fresh or same-site stale-last-good (<see cref="ComponentDetail.IsStale"/>).</summary>
    Loaded,

    /// <summary>The workspace has no current MPS snapshot; Component Detail must not trigger an MPS load.</summary>
    MpsNotLoaded,

    /// <summary>No <c>pt_mstr</c> row exists for the requested component part in the resolved domain.</summary>
    NotFound,

    /// <summary>A source/inventory read failed and no compatible (same-site) cached Component Detail exists.</summary>
    Unavailable
}

public sealed record ComponentDetailResult(ComponentDetailOutcomeKind Kind, ComponentDetail? Detail = null)
{
    public static ComponentDetailResult Loaded(ComponentDetail detail) => new(ComponentDetailOutcomeKind.Loaded, detail);

    public static ComponentDetailResult MpsNotLoaded { get; } = new(ComponentDetailOutcomeKind.MpsNotLoaded);

    public static ComponentDetailResult NotFound { get; } = new(ComponentDetailOutcomeKind.NotFound);

    public static ComponentDetailResult Unavailable { get; } = new(ComponentDetailOutcomeKind.Unavailable);
}
