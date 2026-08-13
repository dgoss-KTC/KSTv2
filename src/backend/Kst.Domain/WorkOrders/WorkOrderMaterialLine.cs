namespace Kst.Domain.WorkOrders;

/// <summary>
/// One applicable Stage 7 work-order material (<c>wod_det</c>) line (accepted contract §9/§11).
/// Zero-required (<c>wod_qty_req = 0</c>) rows are excluded before this model is constructed; do not
/// construct one for such a row. <see cref="IsManufactured"/> is normalized from <c>pt_pm_code = 'M'</c>
/// at the QAD integration boundary; PM Code itself never travels past that boundary.
/// </summary>
public sealed record WorkOrderMaterialLine(
    string ComponentPart,
    string? ComponentDescription,
    decimal RequiredQuantity,
    decimal IssuedQuantity,
    bool IsManufactured
)
{
    public decimal VarianceQuantity => IssuedQuantity - RequiredQuantity;

    /// <summary>Null when <see cref="RequiredQuantity"/> is zero (defensive; such lines should not reach this model).</summary>
    public decimal? IssuedPercent => RequiredQuantity == 0m ? null : IssuedQuantity / RequiredQuantity * 100m;

    public WorkOrderIssueStatus? IssueStatus => IssuedPercent is { } percent
        ? WorkOrderIssueStatusClassifier.Classify(percent)
        : null;

    /// <summary>Exact-100% and over-issued lines both count as fully issued (accepted contract §8).</summary>
    public bool IsFullyIssued => RequiredQuantity != 0m && IssuedQuantity >= RequiredQuantity;
}
