namespace Kst.Application.PartDetail;

/// <summary>
/// Thrown when PartDetail is requested for a workspace id that does not exist in the current
/// workspace configuration. Distinct from a QAD/database failure so the API layer can return 404
/// rather than an "unavailable" Problem Details response. Mirrors <c>Kst.Application.Mps.MpsWorkspaceNotFoundException</c>.
/// </summary>
public sealed class PartDetailWorkspaceNotFoundException(Guid workspaceId)
    : Exception($"Workspace '{workspaceId}' was not found.")
{
    public Guid WorkspaceId { get; } = workspaceId;
}
