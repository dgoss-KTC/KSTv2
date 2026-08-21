using Kst.Domain.ApprovedVendors;

namespace Kst.Application.ApprovedVendors;

/// <summary>
/// Outcome of an Approved Vendors request, mapped to HTTP semantics by Kst.Api. Deliberately
/// smaller than <c>ComponentDetailResult</c>: there is no MPS-gating outcome (AVL is reference
/// data, not an MPS-derived snapshot) and no explicit not-found outcome (a nonexistent component
/// part naturally yields <see cref="Loaded"/> with an empty collection, per the accepted Stage
/// 8D.7 grain/existence decision).
/// </summary>
public enum ApprovedVendorOutcomeKind
{
    /// <summary>Read successfully; may be an empty collection (zero AVL rows is valid success).</summary>
    Loaded,

    /// <summary>A source read failed.</summary>
    Unavailable
}

public sealed record ApprovedVendorResult(ApprovedVendorOutcomeKind Kind, IReadOnlyList<ApprovedVendor>? Vendors = null)
{
    public static ApprovedVendorResult Loaded(IReadOnlyList<ApprovedVendor> vendors) =>
        new(ApprovedVendorOutcomeKind.Loaded, vendors);

    public static ApprovedVendorResult Unavailable { get; } = new(ApprovedVendorOutcomeKind.Unavailable);
}
