using Kst.Domain.Workspaces;

namespace Kst.Application.Workspaces;

public sealed record WorkspaceReorderResult
{
    public IReadOnlyList<WorkspaceAssignment>? Workspaces { get; init; }
    public IReadOnlyList<WorkspaceValidationError>? ValidationErrors { get; init; }

    public bool IsSuccess => Workspaces is not null;

    public static WorkspaceReorderResult Success(IReadOnlyList<WorkspaceAssignment> workspaces) =>
        new() { Workspaces = workspaces };

    public static WorkspaceReorderResult Failure(IReadOnlyList<WorkspaceValidationError> errors) =>
        new() { ValidationErrors = errors };
}
