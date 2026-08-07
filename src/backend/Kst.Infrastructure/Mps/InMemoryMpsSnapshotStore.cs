using System.Collections.Concurrent;
using Kst.Application.Mps;
using Kst.Domain.Snapshots;

namespace Kst.Infrastructure.Mps;

/// <summary>
/// In-memory, thread-safe per-workspace MPS state store. State does not persist across process
/// restarts (MPS facts are always reloaded from QAD on first access after a restart, same as the
/// system-wide snapshot store).
/// </summary>
public sealed class InMemoryMpsSnapshotStore : IMpsSnapshotStore
{
    private readonly ConcurrentDictionary<Guid, MpsWorkspaceState> _states = new();

    public MpsWorkspaceState GetState(Guid workspaceId) =>
        _states.GetOrAdd(workspaceId, _ => MpsWorkspaceState.Initial);

    public void SetRefreshing(Guid workspaceId)
    {
        _states.AddOrUpdate(
            workspaceId,
            _ => MpsWorkspaceState.Initial with { IsRefreshInProgress = true },
            (_, existing) => existing with { IsRefreshInProgress = true });
    }

    public void SetLoaded(Guid workspaceId, MpsSnapshot snapshot)
    {
        _states[workspaceId] = new MpsWorkspaceState(
            SnapshotStatus.Current,
            snapshot,
            ErrorMessage: null,
            LastAttemptAt: snapshot.LoadedAt,
            IsRefreshInProgress: false);
    }

    public void SetFailed(Guid workspaceId, string errorMessage)
    {
        var existing = GetState(workspaceId);
        var status = existing.Snapshot is not null ? SnapshotStatus.Stale : SnapshotStatus.Failed;

        _states[workspaceId] = existing with
        {
            Status = status,
            ErrorMessage = errorMessage,
            LastAttemptAt = DateTimeOffset.UtcNow,
            IsRefreshInProgress = false
        };
    }
}
