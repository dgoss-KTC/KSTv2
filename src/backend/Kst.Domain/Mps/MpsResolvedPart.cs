namespace Kst.Domain.Mps;

/// <summary>
/// One workspace-resolved parent-level part (from explicit configuration and/or product-line
/// discovery) and its <c>pt_desc1</c> description, independent of whether it currently has any
/// qualifying MPS source facts.
/// </summary>
public sealed record MpsResolvedPart(
    string ParentPart,
    string? Description
);
