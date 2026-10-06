using Kst.Domain.Common;
using Kst.Domain.OpenOrders;
using Kst.Domain.PartDetail;
using Kst.Domain.PlanningWorkbook;

namespace Kst.Application.PlanningWorkbook;

/// <summary>Price tiers for one requested part. An empty tier list means unpriced (no current list or tiers).</summary>
public sealed record PlanningPartPrice(string Part, IReadOnlyList<PartPriceBreak> Tiers);

/// <summary>
/// Batched current-price-tier reader (accepted Part Info rule, Stage 14 ledger §2.4). Implementations
/// live in Kst.Integrations.Qad and are bridged by <see cref="DelegatePlanningPriceReader"/>. Must
/// return exactly one entry per distinct requested part; query failures propagate.
/// </summary>
public interface IPlanningPriceReader
{
    Task<IReadOnlyList<PlanningPartPrice>> ReadAsync(
        string site, IReadOnlyList<string> parts, DateOnly effectiveDate, CancellationToken cancellationToken);
}

public sealed class DelegatePlanningPriceReader(
    Func<string, IReadOnlyList<string>, DateOnly, CancellationToken, Task<IReadOnlyList<PlanningPartPrice>>> read)
    : IPlanningPriceReader
{
    public Task<IReadOnlyList<PlanningPartPrice>> ReadAsync(
        string site, IReadOnlyList<string> parts, DateOnly effectiveDate, CancellationToken cancellationToken) =>
        read(site, parts, effectiveDate, cancellationToken);
}

/// <summary>
/// Batched display-only MRP forecast reader (ledger §2.5). Successful absence of rows is an empty
/// list; failures propagate.
/// </summary>
public interface IPlanningForecastReader
{
    Task<IReadOnlyList<PlanningForecastRow>> ReadAsync(
        string site, IReadOnlyList<string> parts, CancellationToken cancellationToken);
}

public sealed class DelegatePlanningForecastReader(
    Func<string, IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<PlanningForecastRow>>> read)
    : IPlanningForecastReader
{
    public Task<IReadOnlyList<PlanningForecastRow>> ReadAsync(
        string site, IReadOnlyList<string> parts, CancellationToken cancellationToken) =>
        read(site, parts, cancellationToken);
}

/// <summary>
/// Retained Planning Workbook source facts for one workspace (ledger §8.2). Projections are calculated
/// per request from these facts plus the current MPS snapshot, so a horizon change never re-reads
/// QAD and a cached projection can never be returned for the wrong horizon or business week.
/// </summary>
public sealed record PlanningWorkbookSnapshot(
    SnapshotId Id,
    Guid WorkspaceId,
    string Site,
    SnapshotId MpsSnapshotId,
    SnapshotId OpenOrdersSnapshotId,
    DateTimeOffset AcquiredAtUtc,
    DateOnly PriceEffectiveDate,
    IReadOnlyList<OpenOrderLine> SalesOrderLines,
    IReadOnlyDictionary<string, decimal> NetQuantityOnHand,
    IReadOnlyDictionary<string, IReadOnlyList<PartPriceBreak>> PriceTiers,
    IReadOnlyList<PlanningForecastRow> ForecastRows);

/// <summary>Stored workbook state: the last good snapshot and whether it is now stale.</summary>
public sealed record PlanningWorkbookEntry(PlanningWorkbookSnapshot Snapshot, bool IsStale, string? Warning);

/// <summary>Per-workspace (assignment ID) store for the last good Planning Workbook source snapshot.</summary>
public interface IPlanningWorkbookSnapshotStore
{
    PlanningWorkbookEntry? Get(Guid workspaceId);
    void Set(Guid workspaceId, PlanningWorkbookEntry entry);
}

public enum PlanningWorkbookOutcomeKind { Loaded, UnknownWorkspace, MpsNotLoaded, MpsSnapshotChanged, Unavailable, HorizonOutOfRange }

/// <summary>
/// Result of a workbook request. When <see cref="Kind"/> is <see cref="PlanningWorkbookOutcomeKind.Loaded"/>,
/// <see cref="Entry"/> carries the source snapshot (possibly stale) and <see cref="Workbook"/> its
/// projection for the requested horizon and today's business week. <see cref="MaxHorizonWeeks"/> is the
/// owner-confirmed maximum (<see cref="PlanningHorizonRange"/>) for the loaded MPS snapshot and today's
/// business week; it is also returned with <see cref="PlanningWorkbookOutcomeKind.HorizonOutOfRange"/>.
/// </summary>
public sealed record PlanningWorkbookResult(
    PlanningWorkbookOutcomeKind Kind,
    PlanningWorkbookEntry? Entry = null,
    Kst.Domain.PlanningWorkbook.PlanningWorkbook? Workbook = null,
    int HorizonWeeks = 0,
    int MaxHorizonWeeks = 0);
