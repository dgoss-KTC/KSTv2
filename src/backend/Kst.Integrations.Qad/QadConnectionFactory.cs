using Microsoft.Data.SqlClient;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad;

/// <summary>
/// Opens QAD SQL connections and immediately applies <c>READ UNCOMMITTED</c> isolation, matching the
/// legacy KST reporting-query behavior of avoiding lock contention with production QAD transactional
/// activity. Used only for read-only reporting queries; KST never writes to QAD through this path.
/// </summary>
public static class QadConnectionFactory
{
    public static async Task<SqlConnection> OpenAsync(
        QadConnectionOptions options, CancellationToken cancellationToken = default)
    {
        var connectionString = QadConnectionStringFactory.Build(options);
        var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);

            await using var setIsolationLevel = connection.CreateCommand();
            setIsolationLevel.CommandText = "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;";
            await setIsolationLevel.ExecuteNonQueryAsync(cancellationToken);

            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
