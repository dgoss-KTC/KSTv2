namespace Kst.Application.ApprovedVendors;

/// <summary>
/// Thrown when Approved Vendors are requested for a workspace id that does not exist in the
/// current workspace configuration. Distinct from a QAD/database failure so the API layer can
/// return 404 rather than an "unavailable" Problem Details response. Mirrors
/// <c>Kst.Application.ComponentDetail.ComponentWorkspaceNotFoundException</c>.
/// </summary>
public sealed class ApprovedVendorWorkspaceNotFoundException(Guid workspaceId)
    : Exception($"Workspace '{workspaceId}' was not found.")
{
    public Guid WorkspaceId { get; } = workspaceId;
}
