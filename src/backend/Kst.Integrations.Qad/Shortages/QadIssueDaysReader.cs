using Dapper;
using Microsoft.Extensions.Logging;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.Shortages;

/// <summary>Builds the selected-site QAD Inventory Control issue-days lookup used for Stage 9 expiration context.</summary>
public sealed class QadIssueDaysReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadIssueDaysReader> _logger;

    public QadIssueDaysReader(QadConnectionOptions options, ILogger<QadIssueDaysReader> logger) => (_options, _logger) = (options, logger);

    public async Task<int?> ReadAsync(string site, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        var (sql, parameters) = BuildQuery(QadSiteDomainMap.Resolve(site), site);
        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);
        var result = await connection.QueryFirstOrDefaultAsync<int?>(new CommandDefinition(sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken));
        _logger.LogInformation("Stage 9 issue-days read completed for site {Site}; found={Found}.", site, result.HasValue);
        return result;
    }

    public static (string Sql, DynamicParameters Parameters) BuildQuery(string domain, string site)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        const string sql = """
            SELECT icc.icc_iss_days AS IssueDays
            FROM qadpro2.dbo.icc_ctrl AS icc
            WHERE icc.icc_domain = @Domain
              AND icc.icc_site = @Site;
            """;
        return (sql, parameters);
    }
}
