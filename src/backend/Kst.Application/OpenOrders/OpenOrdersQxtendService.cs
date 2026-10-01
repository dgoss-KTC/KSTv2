using Kst.Application.Mps;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.OpenOrders;
using Microsoft.Extensions.Logging;

namespace Kst.Application.OpenOrders;

public interface IOpenOrderCurrentLineReader
{
    Task<IReadOnlyList<OpenOrderCurrentLine>> ReadAsync(string site, IReadOnlyList<OpenOrderLineKey> keys, CancellationToken ct);
}

public sealed class DelegateOpenOrderCurrentLineReader(
    Func<string, IReadOnlyList<OpenOrderLineKey>, CancellationToken, Task<IReadOnlyList<OpenOrderCurrentLine>>> read)
    : IOpenOrderCurrentLineReader
{
    public Task<IReadOnlyList<OpenOrderCurrentLine>> ReadAsync(string site, IReadOnlyList<OpenOrderLineKey> keys, CancellationToken ct) => read(site, keys, ct);
}

public enum QxtendOutcome { Ready, Invalid, Missing, Conflict, Unavailable }
public sealed record QxtendResult(QxtendOutcome Outcome, IReadOnlyList<OpenOrderProposal>? Changes = null,
    string? IssueCode = null, int AffectedRowCount = 0);

/// <summary>One fail-closed fresh reread of only changed line identities before any CSV bytes exist.</summary>
public sealed class OpenOrdersQxtendService(IWorkspaceConfigurationService workspaces, IMpsSnapshotStore mps,
    IOpenOrdersSnapshotStore reports, IOpenOrderCurrentLineReader reader, ILogger<OpenOrdersQxtendService> logger)
{
    public async Task<QxtendResult> ValidateAsync(Guid assignmentId, SnapshotId mpsId, SnapshotId reportId,
        IReadOnlyList<OpenOrderProposal> proposals, CancellationToken ct)
    {
        if (proposals.Count == 0 || proposals.Select(p => p.Key).Distinct().Count() != proposals.Count ||
            proposals.Any(p => !OpenOrderPlanning.Changed(p) || !OpenOrderPlanning.ReasonCodes.Contains(p.ReasonCode ?? "")))
            return new(QxtendOutcome.Invalid, IssueCode: "invalid-proposals", AffectedRowCount: proposals.Count);

        var workspace = (await workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == assignmentId);
        if (workspace is null) return new(QxtendOutcome.Missing, IssueCode: "workspace-missing", AffectedRowCount: proposals.Count);
        var snapshot = mps.GetState(assignmentId).Snapshot;
        var cached = reports.Get(assignmentId);
        if (snapshot is null || snapshot.Id != mpsId || snapshot.Site != workspace.Site ||
            cached is null || cached.IsStale || cached.Snapshot.Id != reportId ||
            cached.Snapshot.MpsSnapshotId != mpsId || cached.Snapshot.Site != workspace.Site)
            return new(QxtendOutcome.Conflict, IssueCode: "snapshot-changed", AffectedRowCount: proposals.Count);

        var parents = snapshot.ResolvedParts.Select(p => p.ParentPart).ToHashSet(StringComparer.Ordinal);
        if (snapshot.ResolvedParts.Count == 0) return new(QxtendOutcome.Conflict, IssueCode: "scope-changed", AffectedRowCount: proposals.Count);
        if (cached.Snapshot.Lines.Select(l => l.Key).Distinct().Count() != cached.Snapshot.Lines.Count)
            return new(QxtendOutcome.Conflict, IssueCode: "duplicate-source", AffectedRowCount: proposals.Count);
        var cachedLines = cached.Snapshot.Lines.ToDictionary(l => l.Key);
        var cachedConflicts = proposals.Count(p => !cachedLines.TryGetValue(p.Key, out var line) ||
            line.Site != workspace.Site || p.Site != line.Site || p.ItemNumber != line.ItemNumber ||
            !parents.Contains(p.ItemNumber) || p.Original != line.SourceValues);
        if (cachedConflicts > 0)
            return new(QxtendOutcome.Conflict, IssueCode: "report-conflict", AffectedRowCount: cachedConflicts);

        IReadOnlyList<OpenOrderCurrentLine> current;
        try { current = await reader.ReadAsync(workspace.Site, proposals.Select(p => p.Key).ToArray(), ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning("Open Orders targeted validation unavailable: {ExceptionType}", ex.GetType().Name);
            return new(QxtendOutcome.Unavailable, IssueCode: "source-unavailable", AffectedRowCount: proposals.Count);
        }
        ct.ThrowIfCancellationRequested();
        // Scope may change while the read is in flight. Never authorize from the former scope.
        var latestWorkspace = (await workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == assignmentId);
        var latest = mps.GetState(assignmentId).Snapshot;
        if (latestWorkspace is null || latestWorkspace.Site != workspace.Site ||
            latestWorkspace.ProductLineFrom != workspace.ProductLineFrom ||
            latestWorkspace.ProductLineTo != workspace.ProductLineTo ||
            !latestWorkspace.ParentParts.SequenceEqual(workspace.ParentParts, StringComparer.Ordinal) ||
            latest is null || latest.Id != mpsId ||
            latest.Site != workspace.Site || !latest.ResolvedParts.Select(p => p.ParentPart).ToHashSet(StringComparer.Ordinal).SetEquals(parents) ||
            reports.Get(assignmentId) is not { IsStale: false } latestReport || latestReport.Snapshot.Id != reportId ||
            latestReport.Snapshot.MpsSnapshotId != mpsId || latestReport.Snapshot.Site != workspace.Site)
            return new(QxtendOutcome.Conflict, IssueCode: "snapshot-changed", AffectedRowCount: proposals.Count);
        if (current.Count > proposals.Count || current.Select(l => l.Key).Distinct().Count() != current.Count)
            return new(QxtendOutcome.Conflict, IssueCode: "duplicate-source", AffectedRowCount: proposals.Count);
        var byKey = current.ToDictionary(l => l.Key);
        var missing = proposals.Count(p => !byKey.ContainsKey(p.Key));
        if (missing > 0) return new(QxtendOutcome.Missing, IssueCode: "line-missing-or-closed", AffectedRowCount: missing);
        var sourceConflicts = proposals.Count(p =>
        {
            var row = byKey[p.Key];
            return row.Site != workspace.Site || row.ItemNumber != p.ItemNumber || !parents.Contains(row.ItemNumber) ||
                row.Values != p.Original || row.Values.OrderQty - row.ShippedQty <= 0;
        });
        if (sourceConflicts > 0) return new(QxtendOutcome.Conflict, IssueCode: "source-changed", AffectedRowCount: sourceConflicts);
        var belowShipped = proposals.Count(p => p.Proposed.OrderQty < byKey[p.Key].ShippedQty);
        if (belowShipped > 0) return new(QxtendOutcome.Conflict, IssueCode: "below-shipped", AffectedRowCount: belowShipped);
        return new(QxtendOutcome.Ready, proposals);
    }
}
