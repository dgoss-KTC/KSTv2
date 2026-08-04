namespace Kst.Application.SystemStatus;

/// <summary>
/// Holds the current stateful status of each external data source. Implementations live in
/// Kst.Infrastructure. Distinct from snapshot status and backend-connection status (see
/// docs/architecture/BACKEND_PROJECT_BOUNDARIES.md).
/// </summary>
public interface IDataSourceStatusStore
{
    IReadOnlyList<DataSourceSummary> GetAll();
    void ReplaceAll(IReadOnlyList<DataSourceSummary> sources);
}
