namespace Kst.Application.Mps;

/// <summary>
/// Thrown when an MPS operation is requested for a workspace id that does not exist in the current
/// workspace configuration. Distinct from a QAD/database failure so the API layer can return 404
/// rather than the "database unavailable" Problem Details response.
/// </summary>
public sealed class MpsWorkspaceNotFoundException(Guid workspaceId)
    : Exception($"Workspace '{workspaceId}' was not found.")
{
    public Guid WorkspaceId { get; } = workspaceId;
}
