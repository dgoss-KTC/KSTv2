namespace Kst.Application.WorkOrders;

/// <summary>
/// Thrown when a Stage 7 Work Order drill-down operation is requested for a workspace id that does
/// not exist in the current workspace configuration. Mirrors <c>Kst.Application.Mps.MpsWorkspaceNotFoundException</c>
/// / <c>Kst.Application.PartDetail.PartDetailWorkspaceNotFoundException</c>.
/// </summary>
public sealed class WorkOrderDrilldownWorkspaceNotFoundException(Guid workspaceId)
    : Exception($"Workspace '{workspaceId}' was not found.")
{
    public Guid WorkspaceId { get; } = workspaceId;
}
