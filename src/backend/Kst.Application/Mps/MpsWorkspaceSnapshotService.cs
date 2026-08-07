using System.Collections.Concurrent;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.Snapshots;
using Kst.Domain.Workspaces;
using Kst.Application.Workspaces;
using Microsoft.Extensions.Logging;

namespace Kst.Application.Mps;

/// <summary>
/// Orchestrates per-workspace MPS load/refresh. Auto-loads on first read and funnels explicit
/// Refresh requests through the same guarded load path, so there is exactly one code path that talks
/// to QAD. Due/Release-date and horizon projection happens locally from the retained snapshot via
/// <see cref="MpsScheduleBuilder"/> — reading or changing the horizon/date-basis never re-queries QAD.
/// A per-workspace guard prevents two concurrent loads for the same workspace; a concurrent caller
/// simply observes the in-progress state rather than starting a second load.
/// </summary>
public sealed class MpsWorkspaceSnapshotService
{
    private readonly IWorkspaceConfigurationService _workspaces;
    private readonly IMpsScopeResolver _scopeResolver;
    private readonly IMpsSourceReader _sourceReader;
    private readonly IMpsSnapshotStore _store;
    private readonly ILogger<MpsWorkspaceSnapshotService> _logger;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _refreshGates = new();

    public MpsWorkspaceSnapshotService(
        IWorkspaceConfigurationService workspaces,
        IMpsScopeResolver scopeResolver,
        IMpsSourceReader sourceReader,
        IMpsSnapshotStore store,
        ILogger<MpsWorkspaceSnapshotService> logger)
    {
        _workspaces = workspaces;
        _scopeResolver = scopeResolver;
        _sourceReader = sourceReader;
        _store = store;
        _logger = logger;
    }

    public async Task<MpsDashboardResult> GetDashboardAsync(
        Guid workspaceId,
        MpsDateBasis dateBasis,
        int horizonWeeks,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var workspace = await FindWorkspaceAsync(workspaceId, cancellationToken);
        var state = _store.GetState(workspaceId);
        if (state.Snapshot is null && !state.IsRefreshInProgress)
            state = await LoadAsync(workspace, cancellationToken);

        return Project(state, dateBasis, horizonWeeks, today);
    }

    public async Task<MpsDashboardResult> RefreshAsync(
        Guid workspaceId,
        MpsDateBasis dateBasis,
        int horizonWeeks,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var workspace = await FindWorkspaceAsync(workspaceId, cancellationToken);
        var state = await LoadAsync(workspace, cancellationToken);
        return Project(state, dateBasis, horizonWeeks, today);
    }

    private async Task<WorkspaceAssignment> FindWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workspaces = await _workspaces.GetWorkspacesAsync();
        return workspaces.Workspaces.FirstOrDefault(w => w.AssignmentId == workspaceId)
            ?? throw new MpsWorkspaceNotFoundException(workspaceId);
    }

    private async Task<MpsWorkspaceState> LoadAsync(WorkspaceAssignment workspace, CancellationToken cancellationToken)
    {
        var workspaceId = workspace.AssignmentId;
        var gate = _refreshGates.GetOrAdd(workspaceId, _ => new SemaphoreSlim(1, 1));
        if (!await gate.WaitAsync(0, cancellationToken))
        {
            // A load is already running for this workspace; do not start a second concurrent one.
            return _store.GetState(workspaceId);
        }

        try
        {
            _store.SetRefreshing(workspaceId);

            var resolvedParts = await _scopeResolver.ResolveAsync(workspace, cancellationToken);
            var parentParts = resolvedParts.Select(p => p.ParentPart).ToList();
            var sourceRows = await _sourceReader.ReadAsync(workspace.Site, parentParts, cancellationToken);

            _store.SetLoaded(workspaceId, new MpsSnapshot(
                SnapshotId.New(), DateTimeOffset.UtcNow, workspace.Site, resolvedParts, sourceRows));
        }
        catch (Exception ex)
        {
            // User-facing behavior stays a generic "unavailable" message (see MpsEndpoints.ToResult);
            // log the real exception here so the cause is still diagnosable server-side.
            _logger.LogError(ex, "MPS load failed for workspace {WorkspaceId} ({Site}).", workspaceId, workspace.Site);
            _store.SetFailed(workspaceId, ex.Message);
        }
        finally
        {
            gate.Release();
        }

        return _store.GetState(workspaceId);
    }

    private static MpsDashboardResult Project(
        MpsWorkspaceState state, MpsDateBasis dateBasis, int horizonWeeks, DateOnly today)
    {
        if (state.Snapshot is null)
        {
            return new MpsDashboardResult(
                state.Status,
                state.ErrorMessage,
                state.IsRefreshInProgress,
                Snapshot: null,
                Schedules: []);
        }

        var schedules = MpsScheduleBuilder.Build(
            state.Snapshot.ResolvedParts, state.Snapshot.SourceRows, dateBasis, horizonWeeks, today);

        return new MpsDashboardResult(
            state.Status,
            state.ErrorMessage,
            state.IsRefreshInProgress,
            state.Snapshot,
            schedules);
    }
}
