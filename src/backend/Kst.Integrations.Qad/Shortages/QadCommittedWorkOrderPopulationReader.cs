using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Kst.Domain.Mps;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.Shortages;

/// <summary>Builds the site-wide, window-bounded R/A source population for Stage 9's later residual allocation pass.</summary>
public sealed class QadCommittedWorkOrderPopulationReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadCommittedWorkOrderPopulationReader> _logger;

    public QadCommittedWorkOrderPopulationReader(QadConnectionOptions options, ILogger<QadCommittedWorkOrderPopulationReader> logger) =>
        (_options, _logger) = (options, logger);

    public async Task<IReadOnlyList<QadCommittedWorkOrderComponent>> ReadAsync(string site, MpsDateBasis dateBasis, DateOnly weekStart, DateOnly windowEndExclusive, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        var (sql, parameters) = BuildQuery(QadSiteDomainMap.Resolve(site), site, dateBasis, weekStart, windowEndExclusive);
        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);
        var rows = await connection.QueryAsync<QadCommittedWorkOrderComponentRawRow>(new CommandDefinition(sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken));
        var result = rows.Select(Normalize).ToList();
        _logger.LogInformation("Stage 9 committed work-order population read for site {Site} returned {RowCount} rows.", site, result.Count);
        return result;
    }

    public static (string Sql, DynamicParameters Parameters) BuildQuery(
        string domain, string site, MpsDateBasis dateBasis, DateOnly weekStart, DateOnly windowEndExclusive)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("WeekStart", weekStart.ToDateTime(TimeOnly.MinValue), DbType.Date);
        parameters.Add("WindowEnd", windowEndExclusive.ToDateTime(TimeOnly.MinValue), DbType.Date);
        parameters.Add("DateBasis", dateBasis == MpsDateBasis.ReleaseDate ? "releaseDate" : "dueDate");

        const string sql = """
            SELECT
                wo.wo_lot      AS Woid,
                wo.wo_status   AS Status,
                wo.wo_type     AS WorkOrderType,
                wo.wo_due_date AS DueDate,
                wo.wo_rel_date AS ReleaseDate,
                wod.wod_part   AS ComponentPart,
                wod.wod_qty_req AS RequiredQuantity,
                wod.wod_qty_iss AS IssuedQuantity,
                pt.pt_um       AS UnitOfMeasure
            FROM qadpro2.dbo.wo_mstr AS wo
            INNER JOIN qadpro2.dbo.wod_det AS wod
                ON wod.wod_domain = wo.wo_domain
                AND wod.wod_lot = wo.wo_lot
            LEFT JOIN qadpro2.dbo.pt_mstr AS pt
                ON pt.pt_domain = wod.wod_domain
                AND pt.pt_part = wod.wod_part
            WHERE wo.wo_domain = @Domain
              AND wo.wo_site = @Site
               AND UPPER(wo.wo_status) IN ('R', 'A')
              AND ISNULL(wo.wo_bom_code, '') <> 'RMABOM'
              AND wod.wod_qty_req <> 0
              AND (
                    wo.wo_due_date < @WeekStart
                    OR (@DateBasis = 'releaseDate' AND wo.wo_rel_date >= @WeekStart AND wo.wo_rel_date < @WindowEnd)
                    OR (@DateBasis = 'dueDate' AND wo.wo_due_date >= @WeekStart AND wo.wo_due_date < @WindowEnd)
                  );
            """;

        return (sql, parameters);
    }

    public static QadCommittedWorkOrderComponent Normalize(QadCommittedWorkOrderComponentRawRow raw) => new(
        raw.Woid, raw.Status.Trim(), string.IsNullOrWhiteSpace(raw.WorkOrderType) ? null : raw.WorkOrderType.Trim(),
        raw.DueDate.HasValue ? DateOnly.FromDateTime(raw.DueDate.Value) : null,
        raw.ReleaseDate.HasValue ? DateOnly.FromDateTime(raw.ReleaseDate.Value) : null,
        raw.ComponentPart, raw.RequiredQuantity, raw.IssuedQuantity,
        string.IsNullOrWhiteSpace(raw.UnitOfMeasure) ? null : raw.UnitOfMeasure.Trim());
}

public sealed record QadCommittedWorkOrderComponent(
    string Woid, string Status, string? WorkOrderType, DateOnly? DueDate, DateOnly? ReleaseDate,
    string ComponentPart, decimal RequiredQuantity, decimal IssuedQuantity, string? UnitOfMeasure);

public sealed record QadCommittedWorkOrderComponentRawRow(
    string Woid, string Status, string? WorkOrderType, DateTime? DueDate, DateTime? ReleaseDate,
    string ComponentPart, decimal RequiredQuantity, decimal IssuedQuantity, string? UnitOfMeasure);
