using Kst.Domain.Mps;
using Kst.Domain.PartDetail;

namespace Kst.Domain.PlanningWorkbook;

/// <summary>
/// One display-only MRP forecast fact (QAD <c>mrp_detail = 'FORECAST'</c>) at its own due date.
/// <see cref="Quantity"/> is the raw source quantity; the workbook displays its absolute value and
/// never uses it for supply, demand, projections, revenue, validation or export.
/// </summary>
public sealed record PlanningForecastRow(string Part, DateOnly DueDate, decimal Quantity);

/// <summary>A staged signed adjustment for one part and Due-Date business week (Monday label).</summary>
public sealed record ProposedMpsAdjustment(string Part, DateOnly WeekLabel, decimal Adjustment);

/// <summary>
/// One Falldown or weekly Planning Workbook cell group for a part. Dollar values are null when the
/// part is unpriced (no current price tier); a valid zero price yields zero dollars.
/// Falldown is not adjustable, so its <see cref="Adjustment"/> is always zero.
/// </summary>
public sealed record PlanningBucket(
    MpsBucketKind Kind,
    DateOnly? WeekLabel,
    decimal SalesOrderQuantity,
    decimal SalesOrderDollars,
    decimal Mps,
    decimal? MpsDollars,
    decimal Forecast,
    decimal Adjustment,
    decimal ProposedMps,
    decimal? AdjustedMpsDollars,
    decimal EstimatedOnHand,
    decimal AdjustedOnHand);

/// <summary>
/// Non-RA open sales-order lines without a Due Date. Kept separate: they do not participate in
/// Falldown, weekly or fiscal-quarter totals, or inventory projections.
/// </summary>
public sealed record PlanningUndatedDemand(decimal SalesOrderQuantity, decimal SalesOrderDollars, int LineCount);

/// <summary>One part block of the Planning Workbook in retained MPS part order.</summary>
public sealed record PlanningPartBlock(
    string Part,
    string? Description,
    decimal NetQuantityOnHand,
    IReadOnlyList<PartPriceBreak> PriceTiers,
    decimal? SelectedPrice,
    PlanningBucket Falldown,
    IReadOnlyList<PlanningBucket> Weeks,
    PlanningUndatedDemand Undated)
{
    public bool IsPriced => SelectedPrice is not null;
}

/// <summary>Weekly/Falldown Planning Workbook values. Fiscal-quarter grouping is frontend-owned.</summary>
public sealed record PlanningWorkbook(
    DateOnly Today,
    IReadOnlyList<DateOnly> WeekLabels,
    IReadOnlyList<PlanningPartBlock> Parts);

/// <summary>
/// All facts needed to calculate the workbook. Every resolved part must have a Net QOH value and a
/// price-tier entry (an empty tier list means no current price list); missing entries are rejected
/// rather than silently treated as zero.
/// </summary>
public sealed record PlanningWorkbookInput(
    IReadOnlyList<MpsResolvedPart> ResolvedParts,
    IReadOnlyList<MpsSourceRow> MpsRows,
    IReadOnlyList<OpenOrders.OpenOrderLine> SalesOrderLines,
    IReadOnlyDictionary<string, decimal> NetQuantityOnHand,
    IReadOnlyDictionary<string, IReadOnlyList<PartPriceBreak>> PriceTiers,
    IReadOnlyList<PlanningForecastRow> ForecastRows,
    IReadOnlyList<ProposedMpsAdjustment> Adjustments,
    int HorizonWeeks,
    DateOnly Today);
