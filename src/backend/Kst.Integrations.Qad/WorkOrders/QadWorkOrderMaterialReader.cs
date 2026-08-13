using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Kst.Domain.WorkOrders;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.WorkOrders;

/// <summary>
/// Direct, parameterized QAD adapter for Stage 7 lazy work-order material/kitting detail (accepted
/// contract §9/§11). Joins <c>wo_mstr</c> to <c>wod_det</c> by domain + WOID (<c>wo_lot</c>), then to
/// <c>pt_mstr</c> for component description and manufactured-component identity. Zero-required
/// (<c>wod_qty_req = 0</c>) rows are excluded at the SQL boundary; component rows are never
/// deduplicated (the accepted Stage 7 source-behavior validation covers repeated material rows).
/// </summary>
public sealed class QadWorkOrderMaterialReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadWorkOrderMaterialReader> _logger;

    public QadWorkOrderMaterialReader(QadConnectionOptions options, ILogger<QadWorkOrderMaterialReader> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WorkOrderMaterialLine>> ReadAsync(
        string site,
        string woid,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("QAD connection is not configured.");

        var domain = QadSiteDomainMap.Resolve(site);
        var stopwatch = Stopwatch.StartNew();

        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);

        var (sql, parameters) = BuildQuery(domain, site, woid);
        var command = new CommandDefinition(
            sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        var rawRows = await connection.QueryAsync<QadWorkOrderMaterialRawRow>(command);
        var normalized = rawRows.Select(Normalize).ToList();

        stopwatch.Stop();
        _logger.LogInformation(
            "Work order material read for WOID {Woid} in site {Site} returned {RowCount} rows in {ElapsedMs}ms.",
            woid, site, normalized.Count, stopwatch.ElapsedMilliseconds);

        return normalized;
    }

    /// <summary>Builds the material-line query. Public and pure (no connection) so SQL/parameter shape is independently testable.</summary>
    public static (string Sql, DynamicParameters Parameters) BuildQuery(string domain, string site, string woid)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("Woid", woid);

        const string sql = """
            SELECT
                wod.wod_part     AS ComponentPart,
                pt.pt_desc1      AS ComponentDescription,
                wod.wod_qty_req  AS RequiredQuantity,
                wod.wod_qty_iss  AS IssuedQuantity,
                pt.pt_pm_code    AS ComponentPmCode
            FROM qadpro2.dbo.wo_mstr AS wo
            INNER JOIN qadpro2.dbo.wod_det AS wod
                ON wod.wod_domain = wo.wo_domain
                AND wod.wod_lot = wo.wo_lot
            LEFT JOIN qadpro2.dbo.pt_mstr AS pt
                ON pt.pt_domain = wod.wod_domain
                AND pt.pt_part = wod.wod_part
            WHERE wo.wo_domain = @Domain
              AND wo.wo_site = @Site
              AND wo.wo_lot = @Woid
              AND wod.wod_qty_req <> 0
            ORDER BY wod.wod_part;
            """;

        return (sql, parameters);
    }

    public static WorkOrderMaterialLine Normalize(QadWorkOrderMaterialRawRow raw) => new(
        ComponentPart: raw.ComponentPart,
        ComponentDescription: raw.ComponentDescription,
        RequiredQuantity: raw.RequiredQuantity,
        IssuedQuantity: raw.IssuedQuantity,
        IsManufactured: string.Equals(raw.ComponentPmCode?.Trim(), "M", StringComparison.OrdinalIgnoreCase));
}

/// <summary>QAD-shaped raw Dapper result row. Does not travel past this integration boundary.</summary>
public sealed record QadWorkOrderMaterialRawRow(
    string ComponentPart,
    string? ComponentDescription,
    decimal RequiredQuantity,
    decimal IssuedQuantity,
    string? ComponentPmCode
);
