using Dapper;
using Microsoft.Extensions.Logging;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.Shortages;

/// <summary>Reads QAD's effective supplier-scheduled relationship independently of conventional PO open-quantity qualification.</summary>
public sealed class QadKssScheduleReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadKssScheduleReader> _logger;

    public QadKssScheduleReader(QadConnectionOptions options, ILogger<QadKssScheduleReader> logger) => (_options, _logger) = (options, logger);

    public async Task<bool> IsKssAsync(string site, string part, DateOnly today, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        var (sql, parameters) = BuildQuery(QadSiteDomainMap.Resolve(site), site, part, today);
        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);
        var isKss = await connection.QueryFirstOrDefaultAsync<bool>(new CommandDefinition(sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken));
        _logger.LogInformation("Stage 9 KSS schedule read completed for part {Part} in site {Site}; found={Found}.", part, site, isKss);
        return isKss;
    }

    public static (string Sql, DynamicParameters Parameters) BuildQuery(string domain, string site, string part, DateOnly today)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("Part", part);
        parameters.Add("Today", today.ToDateTime(TimeOnly.MinValue), System.Data.DbType.Date);
        const string sql = """
            SELECT CAST(CASE WHEN EXISTS
            (
                SELECT 1
                FROM qadpro2.dbo.pod_det AS pod
                INNER JOIN qadpro2.dbo.po_mstr AS po
                    ON po.po_domain = pod.pod_domain
                    AND po.po_nbr = pod.pod_nbr
                WHERE pod.pod_domain = @Domain
                  AND pod.pod_site = @Site
                  AND pod.pod_part = @Part
                  AND (pod.pod_end_eff##1 IS NULL OR pod.pod_end_eff##1 >= @Today)
                  AND po.po_sched = 1
                  AND (po.po_eff_to IS NULL OR po.po_eff_to >= @Today)
            ) THEN 1 ELSE 0 END AS bit) AS IsKss;
            """;
        return (sql, parameters);
    }
}
