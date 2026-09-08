using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.Shortages;

/// <summary>Builds the site-wide, non-window-bounded usable QAD hard/detail allocation source for Stage 9.</summary>
public sealed class QadHardAllocationReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadHardAllocationReader> _logger;

    public QadHardAllocationReader(QadConnectionOptions options, ILogger<QadHardAllocationReader> logger) => (_options, _logger) = (options, logger);

    public async Task<IReadOnlyList<QadHardAllocation>> ReadAsync(string site, DateOnly today, int issueDays, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        var (sql, parameters) = BuildQuery(QadSiteDomainMap.Resolve(site), site, today, issueDays);
        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);
        var rows = await connection.QueryAsync<QadHardAllocationRawRow>(new CommandDefinition(sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken));
        var result = rows.Select(Normalize).ToList();
        _logger.LogInformation("Stage 9 hard-allocation read for site {Site} returned {RowCount} rows.", site, result.Count);
        return result;
    }

    public static (string Sql, DynamicParameters Parameters) BuildQuery(string domain, string site, DateOnly today, int issueDays)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("ExpirationCutoff", today.AddDays(issueDays).ToDateTime(TimeOnly.MinValue), DbType.Date);

        const string sql = """
            SELECT
                lad.lad_nbr     AS Woid,
                lad.lad_line    AS OperationNumber,
                lad.lad_part    AS ComponentPart,
                lad.lad_loc     AS Location,
                lad.lad_lot     AS Lot,
                lad.lad_qty_all AS AllocatedQuantity
            FROM qadpro2.dbo.lad_det AS lad
            INNER JOIN qadpro2.dbo.ld_det AS ld
                ON ld.ld_domain = lad.lad_domain
                AND ld.ld_site = lad.lad_site
                AND ld.ld_loc = lad.lad_loc
                AND ld.ld_part = lad.lad_part
                AND ld.ld_lot = lad.lad_lot
            INNER JOIN qadpro2.dbo.loc_mstr AS loc
                ON loc.loc_domain = ld.ld_domain
                AND loc.loc_site = ld.ld_site
                AND loc.loc_loc = ld.ld_loc
            INNER JOIN qadpro2.dbo.is_mstr AS ism
                ON ism.is_domain = loc.loc_domain
                AND ism.is_status = loc.loc_status
            WHERE lad.lad_domain = @Domain
              AND lad.lad_site = @Site
              AND lad.lad_dataset = 'wod_det'
              AND lad.lad_qty_all > 0
              AND ld.ld_qty_oh > 0
              AND ld.ld_lot NOT LIKE 'RA%'
              AND UPPER(ism.is_status) = 'STOCK'
              AND ism.is_nettable = 1
              AND (ld.ld_expire IS NULL OR ld.ld_expire > @ExpirationCutoff);
            """;

        return (sql, parameters);
    }

    public static QadHardAllocation Normalize(QadHardAllocationRawRow raw) =>
        new(raw.Woid, raw.OperationNumber, raw.ComponentPart, raw.Location, raw.Lot, raw.AllocatedQuantity);
}

// QAD's lad_line operation identifier is character data. It is provenance only in Stage 9, so it
// remains a string rather than being coerced to a numeric value at the integration boundary.
public sealed record QadHardAllocation(string Woid, string OperationNumber, string ComponentPart, string Location, string Lot, decimal AllocatedQuantity);
public sealed record QadHardAllocationRawRow(string Woid, string OperationNumber, string ComponentPart, string Location, string Lot, decimal AllocatedQuantity);
