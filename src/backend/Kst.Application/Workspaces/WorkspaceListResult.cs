using Kst.Domain.Workspaces;

namespace Kst.Application.Workspaces;

public sealed record WorkspaceListResult(
    IReadOnlyList<WorkspaceAssignment> Workspaces,
    string? ConfigurationWarning
);
