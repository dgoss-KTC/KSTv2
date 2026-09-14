namespace Kst.Domain.ComponentOrders;

/// <summary>
/// One qualifying conventional open PO line for one component in the active workspace's
/// component scope (Stage 10 Component Orders). Represents scheduling facts, not QAD rows: no
/// SQL column names cross this boundary.
///
/// <see cref="Description"/> is null when the part has no part-master row — a legitimate data
/// state that composes to the "Missing master data" presentation; it never becomes a silent
/// zero-row result.
///
/// <see cref="LeadTimeDays"/> carries the resolved purchasing lead time in whole days (selected-site
/// value with part-master fallback); null or zero renders as "-" and any positive value renders
/// as ceil(days / 7) weeks — that display calculation is presentation-owned, not done here.
///
/// <see cref="OpenQuantity"/> is the raw database open quantity (ordered minus received); no UOM
/// conversion is applied anywhere in this capability.
///
/// <see cref="Confirmed"/> is the line-level confirmation fact; null renders blank (no NULLs were
/// observed in the qualifying open population, but the source column is nullable). It must never
/// be substituted with a PO-master confirmation fact.
///
/// <see cref="SupplierDisplay"/> is the vendor display name resolved through the workspace-domain
/// vendor join; when no vendor master row matches it carries the raw supplier code (the supplier
/// identity itself). It is null only when neither a vendor master match nor a supplier code exists;
/// the value is always truthful and never fabricated.
///
/// <see cref="BuyerDisplay"/> is the buyer resolved to a user in the active workspace domain with
/// the validated buyer code-field discriminator; null means no buyer code exists anywhere for the
/// part (blank display). No cross-domain fallback ever occurs.
///
/// <see cref="IsKss"/> is the accepted independent effective supplier-schedule indicator applied
/// to a row already admitted by the conventional open-PO rule; it never admits a row on its own.
/// </summary>
public sealed record ComponentOrderLine(
    string ComponentPart,
    string? Description,
    int? LeadTimeDays,
    string PoNumber,
    int PoLine,
    DateOnly? DueDate,
    decimal OpenQuantity,
    bool? Confirmed,
    string? SupplierDisplay,
    string? BuyerDisplay,
    string? ManufacturerItem,
    bool IsKss,
    string? TrackingInfo,
    string? SupplierIdentifier = null,
    bool? IsCreditHold = null,
    bool? IsCia = null,
    string? CurrentComments = null);
