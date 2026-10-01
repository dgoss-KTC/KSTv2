using Kst.Domain.Common;
using Kst.Domain.OpenOrders;
using Kst.Application.Workspaces;

namespace Kst.Application.OpenOrders;

public sealed class OpenOrdersDraftService(IOpenOrdersDraftStore store, OpenOrdersService reports, IClock clock,
    IWorkspaceConfigurationService workspaces)
{
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct) =>
        (await workspaces.GetWorkspacesAsync()).Workspaces.Any(w => w.AssignmentId == id) &&
        await store.ExistsAsync(id, ct);

    public async Task<OpenOrdersDraftResult> RestoreAsync(Guid id, SnapshotId mpsId, CancellationToken ct)
    {
        OpenOrdersDraft? draft;
        try { draft = await store.LoadAsync(id, ct); }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidDataException or NotSupportedException)
            { return new(null, null, [], "Saved draft is corrupt and was retained for recovery; it was not restored."); }
        if (draft is null) return new(null, null, [], null);
        if (!(await workspaces.GetWorkspacesAsync()).Workspaces.Any(w => w.AssignmentId == id))
            return new(null, null, [], null);
        // Always acquire a NEW report; a cached GET cannot authorize restoration.
        var fresh = await reports.RefreshAsync(id, mpsId, ct);
        if (fresh.Kind != OpenOrdersOutcomeKind.Loaded || fresh.Report?.IsStale != false)
            return new(draft, null, draft.Proposals.Select(p => new OpenOrderReconciliation(p,
                ["Fresh report unavailable; proposal has not been restored."])).ToArray(),
                "Draft retained. A fresh workspace report is required before restoration.");
        var report = fresh.Report;
        var rows = Reconcile(draft.Proposals, report);
        if (draft.MpsSnapshotId != mpsId.ToString())
            rows = rows.Select(row => row with { Issues = [.. row.Issues, "MPS snapshot changed; review workspace scope."] }).ToArray();
        if (draft.Site != report.Snapshot.Site)
            rows = rows.Select(row => row with { Issues = [.. row.Issues, "Workspace site changed."] }).ToArray();
        return new(draft, report, rows, null);
    }

    public async Task<OpenOrdersDraftResult> SaveAsync(Guid id, SnapshotId mpsId, SnapshotId reportId,
        IReadOnlyList<OpenOrderProposal> proposals, CancellationToken ct)
    {
        var result = await reports.GetCachedForReportExportAsync(id, mpsId, reportId, ct);
        if (result.Kind != OpenOrdersOutcomeKind.Loaded || result.Report?.IsStale != false)
            return new(null, null, [], "Current workspace report required before saving a draft.");
        var report = result.Report;
        if (proposals.Select(p => p.Key).Distinct().Count() != proposals.Count)
            return new(null, null, [], "Duplicate source line identities are not allowed.");
        var draft = new OpenOrdersDraft(id, report.Snapshot.Site, mpsId.ToString(), reportId.ToString(),
            clock.UtcNow, proposals.Where(OpenOrderPlanning.Changed).ToArray());
        await store.SaveAsync(draft, ct);
        return new(draft, report, Reconcile(draft.Proposals, report), null);
    }

    public Task DeleteAsync(Guid id, CancellationToken ct) => store.DeleteAsync(id, ct);

    public static IReadOnlyList<OpenOrderReconciliation> Reconcile(IReadOnlyList<OpenOrderProposal> proposals, OpenOrdersReport report)
    {
        var lines = report.Snapshot.Lines.ToDictionary(l => l.Key);
        return proposals.Select(p =>
        {
            lines.TryGetValue(p.Key, out var current);
            var issues = OpenOrderPlanning.Issues(p, current).ToList();
            if (report.IsStale) issues.Add("Report is stale.");
            return new OpenOrderReconciliation(p, issues);
        }).ToArray();
    }
}
