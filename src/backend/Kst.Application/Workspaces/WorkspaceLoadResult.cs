using Kst.Domain.Workspaces;

namespace Kst.Application.Workspaces;

public sealed record WorkspaceLoadResult(
    IReadOnlyList<WorkspaceAssignment> Workspaces,
    string? ConfigurationWarning
);
