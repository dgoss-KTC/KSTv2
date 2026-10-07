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
    /// Returns the selected unit sales price. An empty tier list is a successful lookup that found no
    /// eligible list price and yields exactly zero (owner-approved amendment 2026-10-07); a failed or
    /// incomplete acquisition is rejected upstream and never reaches this method. A tier with a zero
    /// price is a genuine zero price. Tiers are ordered by MOQ
    /// ascending (stable for equal MOQs, preserving source order); prices are never rounded.
    /// </summary>
    public static decimal Select(
        IReadOnlyList<PartPriceBreak> tiers,
        PlanningPriceSelectionPolicy policy = PlanningPriceSelectionPolicy.FirstTier)
    {
        ArgumentNullException.ThrowIfNull(tiers);
        return policy switch
        {
            PlanningPriceSelectionPolicy.FirstTier => tiers.Count == 0
                ? 0m
                : tiers.OrderBy(t => t.MinimumOrderQuantity).First().UnitPrice,
            _ => throw new ArgumentOutOfRangeException(nameof(policy))
        };
    }
}
