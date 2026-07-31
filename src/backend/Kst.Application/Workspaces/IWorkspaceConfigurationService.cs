namespace Kst.Application.Workspaces;

/// <summary>
/// Application service for workspace configuration management.
/// </summary>
public interface IWorkspaceConfigurationService
{
    Task<WorkspaceListResult> GetWorkspacesAsync();
    Task<WorkspaceCreateResult> CreateWorkspaceAsync(CreateWorkspaceCommand command);
}
