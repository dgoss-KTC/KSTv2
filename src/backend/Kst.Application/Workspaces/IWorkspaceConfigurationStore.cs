using Kst.Domain.Workspaces;

namespace Kst.Application.Workspaces;

/// <summary>
/// Persistence contract for workspace configuration. Implemented in Kst.Infrastructure.
/// </summary>
public interface IWorkspaceConfigurationStore
{
    Task<WorkspaceLoadResult> LoadAsync();
    Task SaveAsync(IReadOnlyList<WorkspaceAssignment> workspaces);
}
