namespace Kst.Application.Workspaces;

/// <summary>
/// Requests a new persisted order for the currently enabled (active) workspace assignments.
/// The provided list must exactly match the set of currently enabled assignment ids, with no
/// duplicates. Archived workspaces are not part of this operation.
/// </summary>
public sealed record ReorderWorkspacesCommand(IReadOnlyList<Guid> AssignmentIds);
