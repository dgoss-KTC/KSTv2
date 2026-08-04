namespace Kst.Application.Workspaces;

/// <summary>
/// Application service for workspace configuration management.
/// </summary>
public interface IWorkspaceConfigurationService
{
    Task<WorkspaceListResult> GetWorkspacesAsync();
    Task<WorkspaceCreateResult> CreateWorkspaceAsync(CreateWorkspaceCommand command);
    Task<WorkspaceUpdateResult> UpdateWorkspaceAsync(Guid assignmentId, CreateWorkspaceCommand command);
    Task<WorkspaceOperationResult> ArchiveWorkspaceAsync(Guid assignmentId);
    Task<WorkspaceOperationResult> RestoreWorkspaceAsync(Guid assignmentId);
    Task<WorkspaceOperationResult> DeleteWorkspaceAsync(Guid assignmentId);
    Task ResetWorkspacesAsync();
    Task<WorkspaceReorderResult> ReorderWorkspacesAsync(ReorderWorkspacesCommand command);
}
