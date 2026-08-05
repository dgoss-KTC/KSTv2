namespace Kst.Domain.Workspaces;

/// <summary>
/// A scheduler-managed set of parent-level parts at a specific site, scoped by product line,
/// explicit parent parts, or both. Customer number is not an authoritative scope field.
/// </summary>
public sealed record WorkspaceAssignment(
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
