using System.Collections.Concurrent;
using Kst.Application.PlanningWorkbook;

namespace Kst.Infrastructure.PlanningWorkbook;

/// <summary>
/// In-memory, thread-safe per-workspace (assignment ID) Planning Workbook source store. Not
/// persisted across process restarts; the workbook is reacquired on first access after a restart.
/// </summary>
public sealed class InMemoryPlanningWorkbookSnapshotStore : IPlanningWorkbookSnapshotStore
{
    private readonly ConcurrentDictionary<Guid, PlanningWorkbookEntry> _entries = new();

    public PlanningWorkbookEntry? Get(Guid workspaceId) =>
        _entries.TryGetValue(workspaceId, out var entry) ? entry : null;

    public void Set(Guid workspaceId, PlanningWorkbookEntry entry) => _entries[workspaceId] = entry;
}
