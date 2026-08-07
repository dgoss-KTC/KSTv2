using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.Connectivity;

/// <summary>
/// Real QAD connectivity check: opens a read-only, Windows-integrated SQL Server connection and
/// runs a trivial round trip (no business query). Never logs credentials or the full connection
/// string.
/// </summary>
public sealed class SqlServerQadConnectivityCheck : IQadConnectivityCheck
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<SqlServerQadConnectivityCheck> _logger;

    public SqlServerQadConnectivityCheck(QadConnectionOptions options, ILogger<SqlServerQadConnectivityCheck> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<ConnectivityResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            return new ConnectivityResult(ConnectivityStatus.NotConfigured);

        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandType = CommandType.Text;
            command.CommandTimeout = _options.ConnectTimeoutSeconds;
            await command.ExecuteScalarAsync(cancellationToken);

            _logger.LogInformation(
                "QAD connectivity check succeeded. ElapsedMs={ElapsedMs}",
                (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds);

            return new ConnectivityResult(ConnectivityStatus.Succeeded);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("QAD connectivity check timed out or was cancelled.");
            return new ConnectivityResult(ConnectivityStatus.TimedOut, "QAD connectivity check timed out.");
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(ex, "QAD connectivity check failed with a SQL error. Category={ErrorCode}", ex.Number);
            return new ConnectivityResult(ConnectivityStatus.Failed, "QAD database connectivity failed.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "QAD connectivity check failed unexpectedly.");
            return new ConnectivityResult(ConnectivityStatus.Failed, "QAD database connectivity failed.");
        }
    }
}
