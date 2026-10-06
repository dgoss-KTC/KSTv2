using Kst.Domain.PartDetail;

namespace Kst.Domain.PlanningWorkbook;

/// <summary>Planning price-tier selection policy. Only the owner-confirmed default exists.</summary>
public enum PlanningPriceSelectionPolicy
{
    /// <summary>First tier of the MOQ-ascending accepted Part Info tier list.</summary>
    FirstTier
}

public static class PlanningPriceSelection
{
    /// <summary>
    /// Returns the selected unit sales price, or null when the part is unpriced (no current tiers).
    /// A tier with a zero price is a valid zero price, not unpriced. Tiers are ordered by MOQ
    /// ascending (stable for equal MOQs, preserving source order); prices are never rounded.
    /// </summary>
    public static decimal? Select(
        IReadOnlyList<PartPriceBreak> tiers,
        PlanningPriceSelectionPolicy policy = PlanningPriceSelectionPolicy.FirstTier)
    {
        ArgumentNullException.ThrowIfNull(tiers);
        return policy switch
        {
            PlanningPriceSelectionPolicy.FirstTier => tiers.Count == 0
                ? null
                : tiers.OrderBy(t => t.MinimumOrderQuantity).First().UnitPrice,
            _ => throw new ArgumentOutOfRangeException(nameof(policy))
        };
    }
}
