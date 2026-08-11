namespace Kst.Domain.PartDetail;

/// <summary>
/// One current MOQ/price tier for a part, sourced from <c>pi_mstr</c> + <c>pid_det</c>. Most parts
/// have exactly one tier; a handful have several, ordered by <see cref="MinimumOrderQuantity"/>
/// ascending for stable presentation.
/// </summary>
public sealed record PartPriceBreak(
    decimal MinimumOrderQuantity,
    decimal UnitPrice
);
