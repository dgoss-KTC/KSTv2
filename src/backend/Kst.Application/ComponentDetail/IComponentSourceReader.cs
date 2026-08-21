using Kst.Domain.ComponentDetail;

namespace Kst.Application.ComponentDetail;

/// <summary>
/// Reads Stage 8D.5 Component Detail source facts (master, selected-site planning, Standard
/// Cost, QCTC) for one component part at one site. Implementations live in Kst.Integrations.Qad;
/// Kst.Api bridges the concrete adapter into this interface via
/// <see cref="DelegateComponentSourceReader"/> so Kst.Application never references
/// Kst.Integrations.Qad. Returns null when <c>pt_mstr</c> does not exist for the part/domain (a
/// true missing-component state, distinct from a query failure). Does not read inventory — the
/// Application service composes that separately from the shared <c>IPartInventoryReader</c>.
/// </summary>
public interface IComponentSourceReader
{
    Task<ComponentSourceFacts?> ReadAsync(
        string site,
        string componentPart,
        CancellationToken cancellationToken = default);
}
