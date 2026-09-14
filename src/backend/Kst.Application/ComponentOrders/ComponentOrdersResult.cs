using Kst.Domain.Common;
using Kst.Domain.ComponentOrders;

namespace Kst.Application.ComponentOrders;

/// <summary>Outcome of a Stage 10 Component Orders request, mapped to HTTP semantics by Kst.Api.</summary>
public enum ComponentOrdersOutcomeKind
{
    /// <summary>Composed successfully (may be an empty group list when no scoped component has qualifying conventional open PO lines).</summary>
    Loaded,

    /// <summary>The workspace has no current MPS snapshot; this request must not trigger an MPS load.</summary>
    MpsNotLoaded,

    /// <summary>The caller's requested snapshot id no longer matches the workspace's current MPS snapshot.</summary>
    SnapshotChanged,

    /// <summary>A QAD read failed. Never a cached/stale fallback and never an empty list — retry is expected.</summary>
    Unavailable
}

/// <summary>Whether optional Shortages enrichment was successfully read for this result.</summary>
public enum ComponentOrdersEnrichmentAvailability
{
    Available,
    Unavailable
}

/// <summary>
/// Outcome of a Stage 10 Component Orders request, mapped to HTTP semantics by Kst.Api.
/// <see cref="Groups"/> and <see cref="SnapshotId"/> are populated only for <see cref="ComponentOrdersOutcomeKind.Loaded"/>.
/// </summary>
public sealed record ComponentOrdersResult(
    ComponentOrdersOutcomeKind Kind,
    IReadOnlyList<ComponentOrderGroup>? Groups = null,
    SnapshotId? SnapshotId = null,
    ComponentOrdersEnrichmentAvailability EnrichmentAvailability = ComponentOrdersEnrichmentAvailability.Available)
{
    public static ComponentOrdersResult Loaded(
        SnapshotId snapshotId,
        IReadOnlyList<ComponentOrderGroup> groups,
        ComponentOrdersEnrichmentAvailability enrichmentAvailability = ComponentOrdersEnrichmentAvailability.Available) =>
        new(ComponentOrdersOutcomeKind.Loaded, groups, snapshotId, enrichmentAvailability);

    public static ComponentOrdersResult MpsNotLoaded { get; } = new(ComponentOrdersOutcomeKind.MpsNotLoaded);
    public static ComponentOrdersResult SnapshotChanged { get; } = new(ComponentOrdersOutcomeKind.SnapshotChanged);
    public static ComponentOrdersResult Unavailable { get; } = new(ComponentOrdersOutcomeKind.Unavailable);
}
