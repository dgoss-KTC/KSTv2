using System.Collections.Concurrent;
using Kst.Application.Inventory;
using Kst.Application.Mps;
using Kst.Application.OpenOrders;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.OpenOrders;
using Kst.Domain.PartDetail;
using Kst.Domain.PlanningWorkbook;
using Kst.Domain.Workspaces;
using Microsoft.Extensions.Logging;

namespace Kst.Application.PlanningWorkbook;

/// <summary>
/// Composes the accepted Stage 14 Domain workbook (<see cref="PlanningWorkbookBuilder"/>) from the
/// workspace's current loaded MPS snapshot (never acquired or refreshed here), Stage 13 Open Orders via
/// <see cref="OpenOrdersService"/>, batched Net QOH, batched current price tiers and display-only
/// forecast facts. Source facts are retained per workspace assignment ID; the projection is
/// calculated per request for the requested horizon and today's business week.
/// <para>
/// Publication rules: a new snapshot is stored only after every source read succeeds and the MPS
/// context is rechecked as unchanged. A source failure (including a stale Stage 13 report) keeps the
/// compatible last-good snapshot, marked stale with a sanitized warning, or yields Unavailable when
/// there is none. Failed sources are never replaced by zeros, unpriced results or an empty forecast.
/// </para>
/// </summary>
public sealed class PlanningWorkbookService(
    IWorkspaceConfigurationService workspaces,
    IMpsSnapshotStore mps,
    OpenOrdersService openOrders,
    IPartInventoryReader inventory,
    IPlanningPriceReader prices,
    IPlanningForecastReader forecasts,
    IPlanningWorkbookSnapshotStore store,
    IClock clock,
    ILogger<PlanningWorkbookService> logger)
{
    public const string StaleWarning =
        "Showing the last known Planning Workbook data. A newer refresh could not be completed.";

    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _gates = new();

    /// <summary>Returns a compatible cached workbook, acquiring source facts when none is available.</summary>
    public Task<PlanningWorkbookResult> GetAsync(
        Guid assignmentId, SnapshotId mpsSnapshotId, int horizonWeeks, CancellationToken ct) =>
        ResolveAsync(assignmentId, mpsSnapshotId, horizonWeeks, false, ct);

    /// <summary>Reacquires Open Orders (through Stage 13), inventory, prices and forecast. Never refreshes MPS.</summary>
    public Task<PlanningWorkbookResult> RefreshAsync(
        Guid assignmentId, SnapshotId mpsSnapshotId, int horizonWeeks, CancellationToken ct) =>
        ResolveAsync(assignmentId, mpsSnapshotId, horizonWeeks, true, ct);

    private async Task<PlanningWorkbookResult> ResolveAsync(
        Guid id, SnapshotId expected, int horizonWeeks, bool refresh, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(horizonWeeks, 1);
        ct.ThrowIfCancellationRequested();

        var assignments = await workspaces.GetWorkspacesAsync();
        var workspace = assignments.Workspaces.FirstOrDefault(w => w.AssignmentId == id);
        if (workspace is null) return new(PlanningWorkbookOutcomeKind.UnknownWorkspace);
        if (CheckMps(id, expected, workspace) is { } mpsProblem) return new(mpsProblem);

        var today = DateOnly.FromDateTime(clock.LocalNow.Date);
        // Owner-confirmed 14.4 range: derived from the already-loaded MPS snapshot only (no QAD read).
        var loadedMps = mps.GetState(id).Snapshot!;
        var maxHorizon = PlanningHorizonRange.MaximumWeeks(loadedMps.ResolvedParts, loadedMps.SourceRows, today);
        if (horizonWeeks > maxHorizon)
            return new(PlanningWorkbookOutcomeKind.HorizonOutOfRange, MaxHorizonWeeks: maxHorizon);

        if (!refresh && Reusable(store.Get(id), id, expected, workspace, today) is { } hit)
            return Project(hit, id, expected, workspace, horizonWeeks, today);

        var gate = _gates.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            // The assignment may have been deleted or re-sited while waiting for the gate.
            if (await CheckContextAsync(id, expected, workspace, ct) is { } changed) return new(changed);
            var cached = store.Get(id);
            if (!refresh && Reusable(cached, id, expected, workspace, today) is { } reused)
                return Project(reused, id, expected, workspace, horizonWeeks, today);

            var mpsSnapshot = mps.GetState(id).Snapshot!;
            var parts = mpsSnapshot.ResolvedParts.Select(p => p.ParentPart).ToArray();
            try
            {
                var report = refresh
                    ? await openOrders.RefreshAsync(id, expected, ct)
                    : await openOrders.GetAsync(id, expected, ct);
                switch (report.Kind)
                {
                    case OpenOrdersOutcomeKind.UnknownWorkspace: return new(PlanningWorkbookOutcomeKind.UnknownWorkspace);
                    case OpenOrdersOutcomeKind.MpsNotLoaded: return new(PlanningWorkbookOutcomeKind.MpsNotLoaded);
                    case OpenOrdersOutcomeKind.MpsSnapshotChanged: return new(PlanningWorkbookOutcomeKind.MpsSnapshotChanged);
                    case OpenOrdersOutcomeKind.Loaded when report.Report is { IsStale: false }:
                        break;
                    default:
                        // Unavailable, or a stale Stage 13 report: never published as a current workbook.
                        return await FailAsync(id, expected, workspace, cached, horizonWeeks, today, "OpenOrdersUnavailable", ct);
                }

                var orders = report.Report!.Snapshot;
                if (orders.MpsSnapshotId != expected || orders.Site != workspace.Site || orders.WorkspaceId != id)
                    return new(PlanningWorkbookOutcomeKind.MpsSnapshotChanged);

                var netQoh = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                var tiers = new Dictionary<string, IReadOnlyList<PartPriceBreak>>(StringComparer.OrdinalIgnoreCase);
                IReadOnlyList<PlanningForecastRow> forecastRows = [];
                if (parts.Length > 0)
                {
                    var summaries = await inventory.ReadSummariesAsync(workspace.Site, parts, ct);
                    foreach (var summary in summaries) netQoh[summary.PartNumber] = summary.NetQuantityOnHand;
                    var priceResults = await prices.ReadAsync(workspace.Site, parts, today, ct);
                    foreach (var price in priceResults) tiers[price.Part] = price.Tiers;
                    forecastRows = await forecasts.ReadAsync(workspace.Site, parts, ct);
                    if (parts.Any(p => !netQoh.ContainsKey(p)))
                        throw new InvalidDataException("Inventory source omitted a requested part.");
                    if (parts.Any(p => !tiers.ContainsKey(p)))
                        throw new InvalidDataException("Price source omitted a requested part.");
                    if (forecastRows.Any(r => r is null || !parts.Contains(r.Part, StringComparer.OrdinalIgnoreCase)))
                        throw new InvalidDataException("Forecast source returned an out-of-scope row.");
                }

                ct.ThrowIfCancellationRequested();
                if (await CheckContextAsync(id, expected, workspace, ct) is { } movedDuringRead) return new(movedDuringRead);

                var snapshot = new PlanningWorkbookSnapshot(
                    SnapshotId.New(), id, workspace.Site, expected, orders.Id, clock.UtcNow, today,
                    orders.Lines, netQoh, tiers, forecastRows);
                var entry = new PlanningWorkbookEntry(snapshot, false, null);
                var workbook = Build(entry, mpsSnapshot, horizonWeeks, today);

                if (await CheckContextAsync(id, expected, workspace, ct) is { } movedBeforePublish) return new(movedBeforePublish);
                store.Set(id, entry);
                return new(PlanningWorkbookOutcomeKind.Loaded, entry, workbook, horizonWeeks,
                    PlanningHorizonRange.MaximumWeeks(mpsSnapshot.ResolvedParts, mpsSnapshot.SourceRows, today));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                return await FailAsync(id, expected, workspace, cached, horizonWeeks, today, ex.GetType().Name, ct);
            }
        }
        finally { gate.Release(); }
    }

    private async Task<PlanningWorkbookResult> FailAsync(
        Guid id, SnapshotId expected, WorkspaceAssignment workspace, PlanningWorkbookEntry? cached,
        int horizonWeeks, DateOnly today, string reason, CancellationToken ct)
    {
        logger.LogWarning("Planning Workbook acquisition failed for workspace {WorkspaceId}: {Reason}", id, reason);
        if (await CheckContextAsync(id, expected, workspace, ct) is { } changed) return new(changed);
        if (cached is null || !Compatible(cached, id, expected, workspace))
            return new(PlanningWorkbookOutcomeKind.Unavailable);

        var stale = cached with { IsStale = true, Warning = StaleWarning };
        store.Set(id, stale);
        return Project(stale, id, expected, workspace, horizonWeeks, today);
    }

    /// <summary>
    /// Re-reads the current assignment and MPS state. A deleted assignment yields UnknownWorkspace
    /// (404); a site that differs from the one this acquisition used, or that no longer matches the
    /// loaded MPS snapshot, yields MpsSnapshotChanged (409). Either prevents publication and stale
    /// fallback for the previous context.
    /// </summary>
    private async Task<PlanningWorkbookOutcomeKind?> CheckContextAsync(
        Guid id, SnapshotId expected, WorkspaceAssignment acquiredFor, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var current = (await workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == id);
        if (current is null) return PlanningWorkbookOutcomeKind.UnknownWorkspace;
        if (current.Site != acquiredFor.Site) return PlanningWorkbookOutcomeKind.MpsSnapshotChanged;
        return CheckMps(id, expected, current);
    }

    private PlanningWorkbookOutcomeKind? CheckMps(Guid id, SnapshotId expected, WorkspaceAssignment workspace)
    {
        var current = mps.GetState(id).Snapshot;
        if (current is null) return PlanningWorkbookOutcomeKind.MpsNotLoaded;
        return current.Id != expected || current.Site != workspace.Site
            ? PlanningWorkbookOutcomeKind.MpsSnapshotChanged
            : null;
    }

    private static bool Compatible(PlanningWorkbookEntry entry, Guid id, SnapshotId expected, WorkspaceAssignment workspace) =>
        entry.Snapshot.WorkspaceId == id && entry.Snapshot.MpsSnapshotId == expected && entry.Snapshot.Site == workspace.Site;

    /// <summary>
    /// A cached snapshot is reused only for the same workspace, site and MPS snapshot, and only on the
    /// same local date as its price effective date (a later date may select a different price list).
    /// </summary>
    private static PlanningWorkbookEntry? Reusable(
        PlanningWorkbookEntry? entry, Guid id, SnapshotId expected, WorkspaceAssignment workspace, DateOnly today) =>
        entry is not null && Compatible(entry, id, expected, workspace) && entry.Snapshot.PriceEffectiveDate == today
            ? entry
            : null;

    private PlanningWorkbookResult Project(
        PlanningWorkbookEntry entry, Guid id, SnapshotId expected, WorkspaceAssignment workspace, int horizonWeeks, DateOnly today)
    {
        var mpsSnapshot = mps.GetState(id).Snapshot;
        if (mpsSnapshot is null || mpsSnapshot.Id != expected || mpsSnapshot.Site != workspace.Site)
            return new(PlanningWorkbookOutcomeKind.MpsSnapshotChanged);
        return new(PlanningWorkbookOutcomeKind.Loaded, entry, Build(entry, mpsSnapshot, horizonWeeks, today), horizonWeeks,
            PlanningHorizonRange.MaximumWeeks(mpsSnapshot.ResolvedParts, mpsSnapshot.SourceRows, today));
    }

    private static Kst.Domain.PlanningWorkbook.PlanningWorkbook Build(
        PlanningWorkbookEntry entry, MpsSnapshot mpsSnapshot, int horizonWeeks, DateOnly today) =>
        PlanningWorkbookBuilder.Build(new PlanningWorkbookInput(
            mpsSnapshot.ResolvedParts,
            mpsSnapshot.SourceRows,
            entry.Snapshot.SalesOrderLines,
            entry.Snapshot.NetQuantityOnHand,
            entry.Snapshot.PriceTiers,
            entry.Snapshot.ForecastRows,
            [],
            horizonWeeks,
            today));
}
