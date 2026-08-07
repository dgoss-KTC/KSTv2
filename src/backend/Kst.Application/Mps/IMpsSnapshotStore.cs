namespace Kst.Application.Mps;

/// <summary>
/// Per-workspace store for the current MPS load state. Implementations live in Kst.Infrastructure.
/// Not a replacement for <see cref="Kst.Application.Snapshots.ISnapshotStore"/>, which tracks the
/// unrelated system-wide connectivity snapshot.
/// </summary>
public interface IMpsSnapshotStore
{
    MpsWorkspaceState GetState(Guid workspaceId);

    /// <summary>Marks a load as in progress. Does not clear any existing good snapshot.</summary>
    void SetRefreshing(Guid workspaceId);

    /// <summary>Records a successful load: Status becomes Current, error is cleared.</summary>
    void SetLoaded(Guid workspaceId, MpsSnapshot snapshot);

    /// <summary>
    /// Records a failed load attempt. Status becomes Stale if a prior good snapshot exists
    /// (retained, shown alongside the error), otherwise Failed.
    /// </summary>
    void SetFailed(Guid workspaceId, string errorMessage);
}

