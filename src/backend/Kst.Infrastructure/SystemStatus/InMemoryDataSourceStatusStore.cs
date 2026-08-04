using Kst.Application.SystemStatus;

namespace Kst.Infrastructure.SystemStatus;

/// <summary>
/// Thread-safe in-memory store for the current per-source status list.
/// </summary>
public sealed class InMemoryDataSourceStatusStore : IDataSourceStatusStore
{
    private readonly Lock _lock = new();
    private IReadOnlyList<DataSourceSummary> _current;

    public InMemoryDataSourceStatusStore(IReadOnlyList<DataSourceSummary>? initial = null)
    {
        _current = initial ?? [];
    }

    public IReadOnlyList<DataSourceSummary> GetAll()
    {
        lock (_lock)
        {
            return _current;
        }
    }

    public void ReplaceAll(IReadOnlyList<DataSourceSummary> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        lock (_lock)
        {
            _current = sources;
        }
    }
}
