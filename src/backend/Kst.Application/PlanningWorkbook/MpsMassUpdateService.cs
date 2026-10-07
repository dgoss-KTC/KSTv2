using Kst.Application.Mps;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.PlanningWorkbook;

namespace Kst.Application.PlanningWorkbook;

public enum MpsMassUpdateOutcome
{
    Ready,
    InvalidRequest,
    UnknownWorkspace,
    MpsNotLoaded,
    SnapshotChanged,
    SiteChanged,
    Rejected
}

/// <summary>
/// Result of preparing a Mass Updater file. Only issue codes and counts are exposed on failure. On
/// <see cref="MpsMassUpdateOutcome.Ready"/> the rows are ordered by retained workspace part order, then week.
/// </summary>
public sealed record MpsMassUpdateResult(
    MpsMassUpdateOutcome Outcome,
    string? IssueCode = null,
    int AffectedRowCount = 0,
    IReadOnlyList<MpsMassUpdateRow>? Rows = null,
    string? WorkspaceLabel = null,
    DateOnly? BusinessDate = null,
    SnapshotId? MpsSnapshotId = null);

/// <summary>
/// Stage 14.6 Mass Updater export validation (ledger §5.8–§5.9, §7, §8.6). Validates every staged
/// adjustment against the workspace's complete currently loaded MPS snapshot with the accepted Domain
/// <see cref="MpsAdjustmentValidator"/>, independent of any visible horizon, View or filter, and never
/// substitutes an unavailable baseline with zero. The request must name the current workspace, site and
/// MPS snapshot; any invalid entry rejects the whole export and no rows are produced. Workbook staleness
/// (Open Orders, inventory, prices, forecast) is irrelevant and is not read. This service has no QAD
/// dependency, performs no MPS refresh, no database write, no submission and no import.
/// </summary>
public sealed class MpsMassUpdateService(
    IWorkspaceConfigurationService workspaces,
    IMpsSnapshotStore mps,
    IClock clock)
{
    public const int MaxEntries = 10_000;
    public const int MaxPartLength = 100;

    public async Task<MpsMassUpdateResult> PrepareAsync(
        Guid assignmentId, string site, SnapshotId mpsSnapshotId, IReadOnlyList<MpsAdjustmentEntry> entries, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (string.IsNullOrWhiteSpace(site) || entries.Count > MaxEntries ||
            entries.Any(e => e is null || string.IsNullOrWhiteSpace(e.Part) || e.Part.Length > MaxPartLength))
            return new(MpsMassUpdateOutcome.InvalidRequest, "invalid-request");

        var workspace = (await workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == assignmentId);
        ct.ThrowIfCancellationRequested();
        if (workspace is null) return new(MpsMassUpdateOutcome.UnknownWorkspace, "workspace-missing");
        if (!string.Equals(site, workspace.Site, StringComparison.Ordinal))
            return new(MpsMassUpdateOutcome.SiteChanged, "site-changed");

        var snapshot = mps.GetState(assignmentId).Snapshot;
        if (snapshot is null) return new(MpsMassUpdateOutcome.MpsNotLoaded, "mps-not-loaded");
        if (snapshot.Id != mpsSnapshotId) return new(MpsMassUpdateOutcome.SnapshotChanged, "snapshot-changed");
        if (!string.Equals(snapshot.Site, workspace.Site, StringComparison.Ordinal))
            return new(MpsMassUpdateOutcome.SiteChanged, "site-changed");

        var today = DateOnly.FromDateTime(clock.LocalNow.Date);
        var context = new MpsAdjustmentContext(snapshot.Id.Value, snapshot.ResolvedParts, snapshot.SourceRows, today);
        var result = MpsAdjustmentValidator.Validate(new MpsAdjustmentRequest(snapshot.Id.Value, entries), context);
        if (!result.IsValid)
            return new(MpsMassUpdateOutcome.Rejected, result.Issue!.Value.ToCode(), result.AffectedRowCount);
        if (result.Rows.Count == 0)
            return new(MpsMassUpdateOutcome.Rejected, "no-adjustments");

        return new(MpsMassUpdateOutcome.Ready, Rows: result.Rows,
            WorkspaceLabel: string.IsNullOrWhiteSpace(workspace.DisplayName) ? workspace.Site : workspace.DisplayName,
            BusinessDate: today, MpsSnapshotId: snapshot.Id);
    }
}
