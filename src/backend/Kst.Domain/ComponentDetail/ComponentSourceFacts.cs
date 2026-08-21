namespace Kst.Domain.ComponentDetail;

/// <summary>
/// Raw QAD-sourced Stage 8D.5 facts for one component part at one site, before cache/freshness
/// composition (which is Application-owned; see <c>Kst.Application.ComponentDetail.ComponentDetail</c>).
/// Analogous to <see cref="Kst.Domain.PartDetail.PartDetailSourceFacts"/>: the minimum mechanical
/// wrapper needed to cross the <c>Kst.Integrations.Qad</c> → <c>Kst.Application</c> boundary. Does
/// not carry inventory (composed separately by the Application service from the shared
/// <c>IPartInventoryReader</c>) or cache/freshness metadata. Never exposed past Kst.Application.
/// </summary>
public sealed record ComponentSourceFacts(
    string ComponentPart,
    string? Description,
    string? PartStatusCode,
    string? IosCode,
    decimal? StandardCost,
    decimal? Qctc,
    int? TimeFence,
    decimal? SafetyTime,
    decimal? SafetyStock,
    string? BuyerPlanner,
    int? PurchaseLeadTimeDays,
    int? InspectionLeadTimeDays,
    int? CumulativeLeadTimeDays,
    decimal? MinimumOrderQuantity,
    decimal? OrderMultiple);
