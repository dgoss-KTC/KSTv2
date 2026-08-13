namespace Kst.Domain.WorkOrders;

/// <summary>
/// Semantic classification of a material line's Issued % (accepted contract §10). The backend owns
/// this classification; the frontend owns font/color presentation and must not duplicate the
/// threshold calculation.
/// </summary>
public enum WorkOrderIssueStatus
{
    UnderIssuedException,
    WithinExpectedRange,
    OverIssuedException
}
