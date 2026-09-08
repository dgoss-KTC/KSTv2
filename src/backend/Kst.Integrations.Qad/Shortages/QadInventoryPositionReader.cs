using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Kst.Domain.Mps;
using Kst.Domain.Shortages;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.Shortages;

/// <summary>Builds the batchable Stage 9 physical inventory-position source: usable quantity plus non-covering activity context.</summary>
public sealed class QadInventoryPositionReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadInventoryPositionReader> _logger;

    public QadInventoryPositionReader(QadConnectionOptions options, ILogger<QadInventoryPositionReader> logger) => (_options, _logger) = (options, logger);

    public async Task<IReadOnlyList<QadInventoryPosition>> ReadAsync(string site, IReadOnlyList<string> partNumbers, DateOnly today, int issueDays, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        if (partNumbers.Count == 0) return [];
        var domain = QadSiteDomainMap.Resolve(site);
        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);
        var result = new List<QadInventoryPosition>();
        foreach (var batch in MpsPartBatcher.Batch(partNumbers, _options.MaxPartBatchSize))
        {
            var (sql, parameters) = BuildBatchQuery(domain, site, batch, today, issueDays);
            var rows = await connection.QueryAsync<QadInventoryPositionRawRow>(new CommandDefinition(sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken));
            result.AddRange(rows.Select(Normalize));
        }
        _logger.LogInformation("Stage 9 inventory-position read for site {Site} returned {RowCount} rows.", site, result.Count);
        return result;
    }

    public static (string Sql, DynamicParameters Parameters) BuildBatchQuery(
        string domain, string site, IReadOnlyList<string> partNumbers, DateOnly today, int issueDays)
    {
        if (partNumbers is null)
            throw new ArgumentNullException(nameof(partNumbers));
        if (partNumbers.Count == 0)
            throw new ArgumentException("At least one part number is required.", nameof(partNumbers));

        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("ExpirationCutoff", today.AddDays(issueDays).ToDateTime(TimeOnly.MinValue), DbType.Date);
        var values = new List<string>(partNumbers.Count);
        for (var index = 0; index < partNumbers.Count; index++)
        {
            var name = $"Part{index}";
            parameters.Add(name, partNumbers[index]);
            values.Add($"(@{name})");
        }

        var sql = $"""
            WITH ScopeParts (PartNumber) AS
            (
                SELECT PartNumber FROM (VALUES {string.Join(", ", values)}) AS Parts (PartNumber)
            ),
            Positions AS
            (
                SELECT
                    ld.ld_part AS PartNumber,
                    SUM(CASE WHEN ld.ld_lot NOT LIKE 'RA%' AND UPPER(ism.is_status) = 'STOCK' AND ism.is_nettable = 1
                                      AND (ld.ld_expire IS NULL OR ld.ld_expire > @ExpirationCutoff)
                             THEN ld.ld_qty_oh ELSE 0 END) AS UsableQuantity,
                    SUM(CASE WHEN UPPER(ism.is_status) = 'TRAN' THEN ld.ld_qty_oh ELSE 0 END) AS TransitQuantity,
                    SUM(CASE WHEN UPPER(ism.is_status) = 'INSPECT' THEN ld.ld_qty_oh ELSE 0 END) AS InspectionQuantity,
                    SUM(CASE WHEN ism.is_nettable = 0 THEN ld.ld_qty_oh ELSE 0 END) AS NonNetQuantity,
                    SUM(CASE WHEN UPPER(ism.is_status) = 'MRB' THEN ld.ld_qty_oh ELSE 0 END) AS MrbQuantity,
                    SUM(CASE WHEN UPPER(ism.is_status) = 'NCMINSP' THEN ld.ld_qty_oh ELSE 0 END) AS NcmInspectionQuantity,
                    SUM(CASE WHEN ism.is_nettable = 1 AND ld.ld_expire <= @ExpirationCutoff THEN ld.ld_qty_oh ELSE 0 END) AS ExpiredExpiringQuantity
                FROM qadpro2.dbo.ld_det AS ld
                INNER JOIN qadpro2.dbo.loc_mstr AS loc
                    ON loc.loc_domain = ld.ld_domain
                    AND loc.loc_site = ld.ld_site
                    AND loc.loc_loc = ld.ld_loc
                INNER JOIN qadpro2.dbo.is_mstr AS ism
                    ON ism.is_domain = loc.loc_domain
                    AND ism.is_status = loc.loc_status
                WHERE ld.ld_domain = @Domain
                  AND ld.ld_site = @Site
                  AND ld.ld_part IN (SELECT PartNumber FROM ScopeParts)
                  AND ld.ld_qty_oh > 0
                GROUP BY ld.ld_part
            )
            SELECT
                scope.PartNumber,
                ISNULL(position.UsableQuantity, 0) AS UsableQuantity,
                ISNULL(position.TransitQuantity, 0) AS TransitQuantity,
                ISNULL(position.InspectionQuantity, 0) AS InspectionQuantity,
                ISNULL(position.NonNetQuantity, 0) AS NonNetQuantity,
                ISNULL(position.MrbQuantity, 0) AS MrbQuantity,
                ISNULL(position.NcmInspectionQuantity, 0) AS NcmInspectionQuantity,
                ISNULL(position.ExpiredExpiringQuantity, 0) AS ExpiredExpiringQuantity
            FROM ScopeParts AS scope
            LEFT JOIN Positions AS position ON position.PartNumber = scope.PartNumber;
            """;
        return (sql, parameters);
    }

    public static QadInventoryPosition Normalize(QadInventoryPositionRawRow raw) => new(
        raw.PartNumber,
        new UsableInventoryPosition(raw.UsableQuantity, new Dictionary<InventoryActivity, decimal>
        {
            [InventoryActivity.Transit] = raw.TransitQuantity,
            [InventoryActivity.Inspection] = raw.InspectionQuantity,
            [InventoryActivity.NonNet] = raw.NonNetQuantity,
            [InventoryActivity.Mrb] = raw.MrbQuantity,
            [InventoryActivity.NcmInspection] = raw.NcmInspectionQuantity,
            [InventoryActivity.ExpiredExpiring] = raw.ExpiredExpiringQuantity
        }));
}

public sealed record QadInventoryPosition(string PartNumber, UsableInventoryPosition Position);
public sealed record QadInventoryPositionRawRow(
    string PartNumber, decimal UsableQuantity, decimal TransitQuantity, decimal InspectionQuantity,
    decimal NonNetQuantity, decimal MrbQuantity, decimal NcmInspectionQuantity, decimal ExpiredExpiringQuantity);
