namespace Kst.Application.Refresh;

/// <summary>
/// Generic delegate-backed <see cref="IRefreshProvider"/>. Lets the composition root (Kst.Api) adapt
/// integration-specific connectivity checks without Kst.Application referencing those projects.
/// </summary>
public sealed class DelegateRefreshProvider : IRefreshProvider
{
    private readonly Func<CancellationToken, Task<RefreshProviderOutcome>> _refresh;

    public DelegateRefreshProvider(string sourceName, Func<CancellationToken, Task<RefreshProviderOutcome>> refresh)
    {
        SourceName = sourceName;
        _refresh = refresh;
    }

    public string SourceName { get; }

    public Task<RefreshProviderOutcome> RefreshAsync(CancellationToken cancellationToken = default) =>
        _refresh(cancellationToken);
}
