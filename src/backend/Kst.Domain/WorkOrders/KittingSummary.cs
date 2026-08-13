namespace Kst.Domain.WorkOrders;

/// <summary>
/// Line-based (not quantity-weighted) Kitting % for one work order (accepted contract §8). Kitting is
/// <c>null</c>/N-A, never 0%, when there are zero applicable material lines.
/// </summary>
public sealed record KittingSummary(
    int ApplicableLineCount,
    int FullyIssuedLineCount,
    decimal? KittingPercent
)
{
    public static KittingSummary Calculate(int applicableLineCount, int fullyIssuedLineCount)
    {
        if (applicableLineCount < 0)
            throw new ArgumentOutOfRangeException(nameof(applicableLineCount), "Applicable line count cannot be negative.");
        if (fullyIssuedLineCount < 0)
            throw new ArgumentOutOfRangeException(nameof(fullyIssuedLineCount), "Fully issued line count cannot be negative.");
        if (fullyIssuedLineCount > applicableLineCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fullyIssuedLineCount), "Fully issued line count cannot exceed applicable line count.");
        }

        var percent = applicableLineCount == 0
            ? (decimal?)null
            : (decimal)fullyIssuedLineCount / applicableLineCount * 100m;

        return new KittingSummary(applicableLineCount, fullyIssuedLineCount, percent);
    }

    /// <summary>
    /// Convenience overload for when full material-line detail (rather than pre-aggregated counts) is
    /// already in hand. Every line here is assumed already-applicable (zero-required lines excluded
    /// upstream); this does not re-filter by <see cref="WorkOrderMaterialLine.RequiredQuantity"/>.
    /// </summary>
    public static KittingSummary FromMaterialLines(IReadOnlyList<WorkOrderMaterialLine> applicableLines) =>
        Calculate(applicableLines.Count, applicableLines.Count(l => l.IsFullyIssued));
}
