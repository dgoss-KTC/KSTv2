namespace Kst.Domain.Workspaces;

public sealed record WorkspaceAssignment(
    Guid AssignmentId,
    string? DisplayName,
    string Site,
    string? CustomerNumber,
    string? ProductLineFrom,
    string? ProductLineTo,
    bool IsTemporary,
    DateOnly? CoverageEndsOn,
    bool IsEnabled,
    int SortOrder
);
