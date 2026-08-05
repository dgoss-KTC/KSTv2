namespace Kst.Api.Dtos;

public sealed record CreateWorkspaceRequestDto(
    string? DisplayName,
    string? Site,
    string? ProductLineFrom,
    string? ProductLineTo,
    IReadOnlyList<string>? ParentParts,
    bool IsTemporary,
    DateOnly? CoverageEndsOn
);

public sealed record WorkspaceAssignmentDto(
    Guid AssignmentId,
    string? DisplayName,
    string Site,
    string? ProductLineFrom,
    string? ProductLineTo,
    IReadOnlyList<string> ParentParts,
    bool IsTemporary,
    DateOnly? CoverageEndsOn,
    bool IsEnabled,
    int SortOrder
);

public sealed record WorkspaceListResponseDto(
    IReadOnlyList<WorkspaceAssignmentDto> Workspaces,
    string? ConfigurationWarning
);

public sealed record ReorderWorkspacesRequestDto(
    IReadOnlyList<Guid> AssignmentIds
);
