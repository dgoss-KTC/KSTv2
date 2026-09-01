namespace Kst.Domain.WorkOrders;

/// <summary>
/// Stage 7R four-week Work Order planning window. The forward window spans the first four MPS
/// business weeks (Week 0..3) after the current business week; Falldown is the separate,
/// always-Due-Date-based overdue bucket (see <see cref="Mps.MpsBusinessCalendar.IsFalldown"/>). The
/// four-week horizon is the Work Order drill-down eligibility horizon and is distinct from the MPS
/// grid's own display horizon, which is never truncated or altered by it.
/// </summary>
public static class WorkOrderPlanningWindow
{
    /// <summary>Number of forward MPS business weeks in the Work Order planning window (Week 0..3).</summary>
    public const int ForwardWeekCount = 4;

    /// <summary>Exclusive end of the forward planning window, given the current business-week start.</summary>
    public static DateOnly GetWindowEndExclusive(DateOnly currentWeekStart) =>
        currentWeekStart.AddDays(7 * ForwardWeekCount);
}
