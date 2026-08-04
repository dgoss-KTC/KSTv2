using Kst.Domain.Workspaces;

namespace Kst.Application.Workspaces;

public sealed record WorkspaceUpdateResult
{
    public WorkspaceAssignment? Workspace { get; init; }
    public IReadOnlyList<WorkspaceValidationError>? ValidationErrors { get; init; }
    public bool NotFound { get; init; }

    public bool IsSuccess => Workspace is not null;

    public static WorkspaceUpdateResult Success(WorkspaceAssignment workspace) =>
        new() { Workspace = workspace };

    public static WorkspaceUpdateResult Failure(IReadOnlyList<WorkspaceValidationError> errors) =>
        new() { ValidationErrors = errors };

    public static WorkspaceUpdateResult NotFoundResult() => new() { NotFound = true };
}
