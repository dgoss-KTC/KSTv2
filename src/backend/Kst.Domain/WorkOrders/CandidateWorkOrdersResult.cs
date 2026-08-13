namespace Kst.Domain.WorkOrders;

/// <summary>
/// Candidate subassembly work orders for a manufactured component, plus whether more existed beyond
/// the display limit (<see cref="WorkOrderDrilldownPolicy.CandidateResultLimit"/>). Lives in Domain
/// (not the QAD integration project) so <c>Kst.Application</c> can depend on it without referencing
/// <c>Kst.Integrations.Qad</c>.
/// </summary>
public sealed record CandidateWorkOrdersResult(IReadOnlyList<WorkOrderSummary> Candidates, bool IsTruncated);
