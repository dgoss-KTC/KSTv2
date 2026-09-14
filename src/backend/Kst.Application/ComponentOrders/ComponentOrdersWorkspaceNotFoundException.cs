namespace Kst.Application.ComponentOrders;

/// <summary>
/// Thrown when a Stage 10 Component Orders request is made for a workspace id that does not exist
/// in the current workspace configuration. Mirrors
/// <c>Kst.Application.WorkOrders.WorkOrderDrilldownWorkspaceNotFoundException</c>.
/// </summary>
public sealed class ComponentOrdersWorkspaceNotFoundException(Guid workspaceId)
    : Exception($"Workspace '{workspaceId}' was not found.")
{
    public Guid WorkspaceId { get; } = workspaceId;
}
