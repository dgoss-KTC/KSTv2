namespace Kst.Application.Workspaces;

public sealed record CreateWorkspaceCommand(
    string? DisplayName,
    string? Site,
    string? CustomerNumber,
    string? ProductLineFrom,
    string? ProductLineTo,
    bool IsTemporary,
    DateOnly? CoverageEndsOn
);
