using Kst.Domain.Workspaces;

namespace Kst.Application.Workspaces;

public sealed record WorkspaceCreateResult
{
    public WorkspaceAssignment? Workspace { get; init; }
    public IReadOnlyList<WorkspaceValidationError>? ValidationErrors { get; init; }

    public bool IsSuccess => Workspace is not null;

    public static WorkspaceCreateResult Success(WorkspaceAssignment workspace) =>
        new() { Workspace = workspace };

    public static WorkspaceCreateResult Failure(IReadOnlyList<WorkspaceValidationError> errors) =>
        new() { ValidationErrors = errors };
}
