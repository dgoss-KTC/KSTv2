using Kst.Domain.Mps;
using Kst.Domain.Workspaces;

namespace Kst.Application.Mps;

/// <summary>
/// Resolves a workspace's parent-level MPS part scope (product-line discovery unioned with
/// explicitly configured parts). Implementations live in Kst.Integrations.Qad; Kst.Api bridges the
/// concrete adapter into this interface via <see cref="DelegateMpsScopeResolver"/>.
/// </summary>
public interface IMpsScopeResolver
{
    Task<IReadOnlyList<MpsResolvedPart>> ResolveAsync(
        WorkspaceAssignment workspace,
        CancellationToken cancellationToken = default);
}
