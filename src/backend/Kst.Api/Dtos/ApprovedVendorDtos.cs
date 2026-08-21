namespace Kst.Api.Dtos;

/// <summary>
/// Stage 8D.7 Approved Vendor List (AVL) row for one component part. Grain is Domain + Component
/// Part; the component part/description are not repeated here (the endpoint identity and the
/// Component Information modal already supply them). All values are semantic/typed (no formatted
/// strings) — frontend formatting/presentation belongs to the modal.
/// </summary>
public sealed record ApprovedVendorDto(
    string Supplier,
    string? VendorName,
    string? SupplierItem,
    string? ManufacturerPart);
