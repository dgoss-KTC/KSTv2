namespace Kst.Domain.ApprovedVendors;

/// <summary>
/// Raw QAD-sourced Stage 8D.7 Approved Vendor List (AVL) relationship fact for one component part.
/// Business grain is Domain + Component Part (not Site-specific, no effective date). Supplier is
/// the accepted primary source ordering; source row multiplicity/duplicates are preserved as-is
/// (no dedup). <see cref="SupplierItem"/> and <see cref="ManufacturerPart"/> may legitimately be
/// null/blank. Deliberately excludes the component Part/Description (already known by the caller)
/// and any cost/PO/lead-time/risk/shortage data — those belong to other capabilities.
/// </summary>
public sealed record ApprovedVendor(
    string Supplier,
    string? VendorName,
    string? SupplierItem,
    string? ManufacturerPart);
