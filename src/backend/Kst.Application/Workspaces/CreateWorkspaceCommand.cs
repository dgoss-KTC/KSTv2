namespace Kst.Application.Workspaces;

public sealed record CreateWorkspaceCommand(
    string? DisplayName,
    string? Site,
    string? ProductLineFrom,
    string? ProductLineTo,
    IReadOnlyList<string>? ParentParts,
    bool IsTemporary,
    DateOnly? CoverageEndsOn
);
