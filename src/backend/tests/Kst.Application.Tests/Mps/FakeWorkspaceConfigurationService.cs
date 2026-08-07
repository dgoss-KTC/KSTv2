using Kst.Application.Workspaces;
using Kst.Domain.Workspaces;

namespace Kst.Application.Tests.Mps;

/// <summary>Minimal fake exposing only the single workspace the MPS orchestrator needs.</summary>
internal sealed class FakeWorkspaceConfigurationService : IWorkspaceConfigurationService
{
    private readonly WorkspaceAssignment _workspace;

    public FakeWorkspaceConfigurationService(WorkspaceAssignment workspace) => _workspace = workspace;

    public Task<WorkspaceListResult> GetWorkspacesAsync() =>
        Task.FromResult(new WorkspaceListResult([_workspace], ConfigurationWarning: null));

    public Task<WorkspaceCreateResult> CreateWorkspaceAsync(CreateWorkspaceCommand command) =>
        throw new NotImplementedException();

    public Task<WorkspaceUpdateResult> UpdateWorkspaceAsync(Guid assignmentId, CreateWorkspaceCommand command) =>
        throw new NotImplementedException();

    public Task<WorkspaceOperationResult> ArchiveWorkspaceAsync(Guid assignmentId) =>
        throw new NotImplementedException();

    public Task<WorkspaceOperationResult> RestoreWorkspaceAsync(Guid assignmentId) =>
        throw new NotImplementedException();

    public Task<WorkspaceOperationResult> DeleteWorkspaceAsync(Guid assignmentId) =>
        throw new NotImplementedException();

    public Task ResetWorkspacesAsync() => throw new NotImplementedException();

    public Task<WorkspaceReorderResult> ReorderWorkspacesAsync(ReorderWorkspacesCommand command) =>
        throw new NotImplementedException();
}
