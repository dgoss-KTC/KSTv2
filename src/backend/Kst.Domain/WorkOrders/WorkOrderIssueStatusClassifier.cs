namespace Kst.Domain.WorkOrders;

/// <summary>
/// Classifies a material line's Issued % into a <see cref="WorkOrderIssueStatus"/> using the accepted
/// Stage 7 thresholds (contract §10): 95%/105% boundaries are inclusive to their exception side, so an
/// exact 95% or 105% reading is an exception, not "within expected range".
/// </summary>
public static class WorkOrderIssueStatusClassifier
{
    public static WorkOrderIssueStatus Classify(decimal issuedPercent) => issuedPercent switch
    {
        <= 95m => WorkOrderIssueStatus.UnderIssuedException,
        >= 105m => WorkOrderIssueStatus.OverIssuedException,
        _ => WorkOrderIssueStatus.WithinExpectedRange
    };
}
