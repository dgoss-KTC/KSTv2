using Dapper;
using Microsoft.Extensions.Logging;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.Shortages;

/// <summary>Builds the selected-site issue-policy source with the accepted site-first, master-second, true-default precedence.</summary>
public sealed class QadIssuePolicyReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadIssuePolicyReader> _logger;

    public QadIssuePolicyReader(QadConnectionOptions options, ILogger<QadIssuePolicyReader> logger) => (_options, _logger) = (options, logger);

    public async Task<bool> ReadAsync(string site, string part, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        var (sql, parameters) = BuildQuery(QadSiteDomainMap.Resolve(site), site, part);
        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);
        var policy = await connection.QueryFirstOrDefaultAsync<bool>(new CommandDefinition(sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken));
        _logger.LogInformation("Stage 9 issue-policy read completed for part {Part} in site {Site}.", part, site);
        return policy;
    }

    public static (string Sql, DynamicParameters Parameters) BuildQuery(string domain, string site, string part)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("Part", part);

        const string sql = """
            SELECT CAST(COALESCE(ptp.ptp_iss_pol, pt.pt_iss_pol, 1) AS bit) AS IssuePolicy
            FROM (VALUES (@Part)) AS scope (PartNumber)
            LEFT JOIN qadpro2.dbo.pt_mstr AS pt
                ON pt.pt_domain = @Domain
                AND pt.pt_part = scope.PartNumber
            LEFT JOIN qadpro2.dbo.ptp_det AS ptp
                ON ptp.ptp_domain = @Domain
                AND ptp.ptp_part = scope.PartNumber
                AND ptp.ptp_site = @Site;
            """;
        return (sql, parameters);
    }
}
