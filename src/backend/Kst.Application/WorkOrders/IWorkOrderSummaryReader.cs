using Kst.Domain.Mps;
using Kst.Domain.WorkOrders;

namespace Kst.Application.WorkOrders;

/// <summary>
/// Reads Stage 7/7R work-order summary/card facts. Implementations live in Kst.Integrations.Qad;
/// Kst.Api bridges the concrete adapter into this interface via <see cref="DelegateWorkOrderSummaryReader"/>
/// so Kst.Application never references Kst.Integrations.Qad.
/// </summary>
public interface IWorkOrderSummaryReader
{
    /// <summary>
    /// Reads the parent-scoped four-week Work Order planning window (Stage 7R): Due-Date-based
    /// Falldown plus Week 0..3 under the active weekly-bucket basis, for every non-closed,
    /// non-RMABOM work order on the parent part. <paramref name="bucketKind"/>/
    /// <paramref name="bucketWeekStart"/> narrow the result to one bucket; both null returns the
    /// full window. Falldown is always Due-Date based regardless of <paramref name="dateBasis"/>.
    /// </summary>
    Task<IReadOnlyList<WorkOrderSummary>> ReadPlanningWindowAsync(
        string site,
        string parentPart,
        MpsDateBasis dateBasis,
        DateOnly weekStart,
        DateOnly windowEndExclusive,
        MpsBucketKind? bucketKind,
        DateOnly? bucketWeekStart,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads one work-order summary by WOID without the A/F/R eligibility filter (Stage 7R parent
    /// resolution). Returns null when no non-closed, non-RMABOM work order exists for the WOID.
    /// </summary>
    Task<WorkOrderSummary?> ReadByWoidAsync(
        string site,
        string woid,
        CancellationToken cancellationToken = default);

}
