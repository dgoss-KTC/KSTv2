using Kst.Domain.Mps;
using Kst.Domain.Workspaces;

namespace Kst.Application.Mps;

/// <summary>
/// Generic delegate-backed <see cref="IMpsScopeResolver"/>. Lets the composition root (Kst.Api) adapt
/// the concrete QAD adapter without Kst.Application referencing Kst.Integrations.Qad.
/// </summary>
public sealed class DelegateMpsScopeResolver : IMpsScopeResolver
{
    private readonly Func<WorkspaceAssignment, CancellationToken, Task<IReadOnlyList<MpsResolvedPart>>> _resolve;

    public DelegateMpsScopeResolver(
        Func<WorkspaceAssignment, CancellationToken, Task<IReadOnlyList<MpsResolvedPart>>> resolve)
    {
        _resolve = resolve;
    }

    public Task<IReadOnlyList<MpsResolvedPart>> ResolveAsync(
        WorkspaceAssignment workspace,
        CancellationToken cancellationToken = default) => _resolve(workspace, cancellationToken);
}
