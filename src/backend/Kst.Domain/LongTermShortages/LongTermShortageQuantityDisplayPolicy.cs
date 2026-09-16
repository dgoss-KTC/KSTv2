namespace Kst.Domain.LongTermShortages;

/// <summary>Stage 11-A display/export rounding only; projection values remain raw.</summary>
public static class LongTermShortageQuantityDisplayPolicy
{
    public static decimal Round(decimal quantity, string? unitOfMeasure)
    {
        if (string.Equals(unitOfMeasure?.Trim(), "EA", StringComparison.OrdinalIgnoreCase)
            || string.Equals(unitOfMeasure?.Trim(), "EACH", StringComparison.OrdinalIgnoreCase))
            return decimal.Round(quantity, 0, MidpointRounding.AwayFromZero);

        return decimal.Floor(quantity * 10_000m) / 10_000m;
    }
}
