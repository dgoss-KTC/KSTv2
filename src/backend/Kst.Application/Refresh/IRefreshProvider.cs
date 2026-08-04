namespace Kst.Application.Refresh;

/// <summary>
/// Truthful outcome of a single provider's participation in a refresh cycle.
/// No provider may report Succeeded without an actual successful data load.
/// </summary>
public enum RefreshProviderOutcome
{
    NotConfigured,
    Succeeded,
    Failed,
    Unavailable
}

/// <summary>
/// A named participant in the Stage 4 refresh lifecycle shell. Concrete adapters (QAD, shortage
/// database, etc.) are wired in Kst.Api so that Kst.Application does not depend on integration
/// projects.
/// </summary>
public interface IRefreshProvider
{
    string SourceName { get; }
    Task<RefreshProviderOutcome> RefreshAsync(CancellationToken cancellationToken = default);
}
