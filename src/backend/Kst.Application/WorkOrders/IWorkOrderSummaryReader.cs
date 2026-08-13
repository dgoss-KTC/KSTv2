using Kst.Domain.WorkOrders;

namespace Kst.Application.WorkOrders;

/// <summary>
/// Reads Stage 7 work-order summary/card facts. Implementations live in Kst.Integrations.Qad;
/// Kst.Api bridges the concrete adapter into this interface via <see cref="DelegateWorkOrderSummaryReader"/>
/// so Kst.Application never references Kst.Integrations.Qad.
/// </summary>
public interface IWorkOrderSummaryReader
{
    /// <summary>Reads work-order summaries for an explicit set of WOIDs (top-level bucket drill-down).</summary>
    Task<IReadOnlyList<WorkOrderSummary>> ReadByWoidsAsync(
        string site,
        IReadOnlyList<string> woids,
        CancellationToken cancellationToken = default);

    /// <summary>Reads candidate subassembly work orders for a manufactured component.</summary>
    Task<CandidateWorkOrdersResult> ReadCandidatesAsync(
        string site,
        string componentPart,
        int limit,
        CancellationToken cancellationToken = default);
}
