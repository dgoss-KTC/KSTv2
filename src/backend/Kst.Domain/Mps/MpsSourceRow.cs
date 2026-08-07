namespace Kst.Domain.Mps;

/// <summary>
/// One normalized, qualifying MRP supply fact (<c>mrp_dataset = 'wo_mstr'</c>) safely associated to
/// its work order, before weekly/Falldown aggregation. This is the application-facing contract
/// produced by the QAD adapter; QAD-specific raw strings must not travel past this boundary.
/// </summary>
public sealed record MpsSourceRow(
    string Domain,
    string Site,
    string ParentPart,
    string? Description,
    DateOnly DueDate,
    DateOnly? ReleaseDate,
    decimal Quantity,
    MpsSupplyType SupplyType,
    string WorkOrderId,
    MpsWorkOrderState WorkOrderState
);
