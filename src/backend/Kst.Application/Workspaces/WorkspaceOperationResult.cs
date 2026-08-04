using Kst.Domain.Workspaces;

namespace Kst.Application.Workspaces;

/// <summary>
/// Result of an archive, restore, or delete operation on a single workspace assignment.
/// </summary>
public sealed record WorkspaceOperationResult
{
    public WorkspaceAssignment? Workspace { get; init; }
    public bool NotFound { get; init; }

    public bool IsSuccess => !NotFound;

    public static WorkspaceOperationResult Success(WorkspaceAssignment? workspace = null) =>
        new() { Workspace = workspace };

    public static WorkspaceOperationResult NotFoundResult() => new() { NotFound = true };
}
