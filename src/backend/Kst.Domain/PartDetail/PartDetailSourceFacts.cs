namespace Kst.Domain.PartDetail;

/// <summary>
/// Raw QAD-sourced Stage 6 facts for one part, before Part Status description normalization and
/// cache/freshness composition (which is Application-owned; see <c>Kst.Application.PartDetail.PartDetail</c>).
/// Analogous to <see cref="Kst.Domain.Mps.MpsSourceRow"/>: the minimum mechanical wrapper needed to
/// cross the <c>Kst.Integrations.Qad</c> → <c>Kst.Application</c> boundary. Never exposed past
/// Kst.Application; not part of the accepted Stage 6 API contract shape.
/// </summary>
public sealed record PartDetailSourceFacts(
    string PartNumber,
    string? PlannerCode,
    decimal? ManufacturingLeadTimeDays,
    decimal? SafetyTimeDays,
    string? PartStatusCode,
    string? CurrentRevision,
    string? Description,
    string? IosCode,
    decimal? SafetyStockQuantity,
    decimal QuantityOnHand,
    decimal QuantityNonNet,
    decimal QuantityRmaOnHand,
    IReadOnlyList<PartPriceBreak> PriceBreaks
);
