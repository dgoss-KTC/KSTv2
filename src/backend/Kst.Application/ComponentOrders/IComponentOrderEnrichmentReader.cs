namespace Kst.Application.ComponentOrders;

/// <summary>
/// Reads optional source enrichment for already-qualified Component Orders facts. Identifiers are
/// source keys and must be passed through unchanged; an absent dictionary entry is a valid no-match.
/// </summary>
public interface IComponentOrderEnrichmentReader
{
    Task<ComponentOrderEnrichmentResult> ReadAsync(
        ComponentOrderEnrichmentRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Adapter for composition roots and deterministic tests without integration references.</summary>
public sealed class DelegateComponentOrderEnrichmentReader : IComponentOrderEnrichmentReader
{
    private readonly Func<ComponentOrderEnrichmentRequest, CancellationToken, Task<ComponentOrderEnrichmentResult>> _read;

    public DelegateComponentOrderEnrichmentReader(Func<ComponentOrderEnrichmentRequest, CancellationToken, Task<ComponentOrderEnrichmentResult>> read) =>
        _read = read;

    public Task<ComponentOrderEnrichmentResult> ReadAsync(ComponentOrderEnrichmentRequest request, CancellationToken cancellationToken = default) =>
        _read(request, cancellationToken);
}

/// <summary>Exact source-key scopes for optional Component Orders enrichment.</summary>
public sealed record ComponentOrderEnrichmentRequest(
    string Site,
    IReadOnlyList<string> ComponentIdentifiers,
    IReadOnlyList<string> SupplierIdentifiers);

/// <summary>Optional enrichment keyed by the exact source identifiers supplied in the request.</summary>
public sealed record ComponentOrderEnrichmentResult(
    IReadOnlyDictionary<string, string?> CommentsByComponent,
    IReadOnlyDictionary<string, ComponentOrderSupplierRisk> RisksBySupplier)
{
    public static ComponentOrderEnrichmentResult Empty { get; } = new(
        new Dictionary<string, string?>(StringComparer.Ordinal),
        new Dictionary<string, ComponentOrderSupplierRisk>(StringComparer.Ordinal));
}

/// <summary>Supplier risk flags from the Shortages source.</summary>
public sealed record ComponentOrderSupplierRisk(bool IsCreditHold, bool IsCia);
