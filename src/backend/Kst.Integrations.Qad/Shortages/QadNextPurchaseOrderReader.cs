using Dapper;
using Microsoft.Extensions.Logging;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.Shortages;

/// <summary>Builds the earliest qualifying PO-line context read; it supplies context only, never shortage coverage.</summary>
public sealed class QadNextPurchaseOrderReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadNextPurchaseOrderReader> _logger;

    public QadNextPurchaseOrderReader(QadConnectionOptions options, ILogger<QadNextPurchaseOrderReader> logger) => (_options, _logger) = (options, logger);

    public async Task<QadNextPurchaseOrder?> ReadAsync(string site, string part, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        var (sql, parameters) = BuildQuery(QadSiteDomainMap.Resolve(site), site, part);
        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);
        var row = await connection.QueryFirstOrDefaultAsync<QadNextPurchaseOrderRawRow>(new CommandDefinition(sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken));
        _logger.LogInformation("Stage 9 next-PO read completed for part {Part} in site {Site}; found={Found}.", part, site, row is not null);
        return row is null ? null : Normalize(row);
    }

    public static (string Sql, DynamicParameters Parameters) BuildQuery(string domain, string site, string part)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("Part", part);
        const string sql = """
            SELECT TOP (1)
                po.po_nbr AS PoNumber,
                pod.pod_due_date AS DueDate,
                pod.pod_qty_ord - pod.pod_qty_rcvd AS OpenQuantity,
                po.po_confirm AS IsConfirmed,
                pod.pod__chr06 AS TrackingInfo,
                CAST(CASE WHEN po.po_sched = 1 OR pod.pod_sched = 1 THEN 1 ELSE 0 END AS bit) AS IsKss,
                po.po_stat AS PoState
            FROM qadpro2.dbo.pod_det AS pod
            INNER JOIN qadpro2.dbo.po_mstr AS po
                ON po.po_domain = pod.pod_domain
                AND po.po_nbr = pod.pod_nbr
            WHERE pod.pod_domain = @Domain
              AND pod.pod_site = @Site
              AND pod.pod_part = @Part
              AND LOWER(ISNULL(pod.pod_status, '')) NOT IN ('c', 'x')
              AND pod.pod_qty_ord - pod.pod_qty_rcvd > 0
            ORDER BY pod.pod_due_date, po.po_nbr, pod.pod_line;
            """;
        return (sql, parameters);
    }

    public static QadNextPurchaseOrder Normalize(QadNextPurchaseOrderRawRow raw) => new(
        raw.PoNumber, raw.DueDate.HasValue ? DateOnly.FromDateTime(raw.DueDate.Value) : null,
        raw.OpenQuantity, raw.IsConfirmed, string.IsNullOrWhiteSpace(raw.TrackingInfo) ? null : raw.TrackingInfo.Trim(),
        raw.IsKss, string.IsNullOrWhiteSpace(raw.PoState) ? null : raw.PoState.Trim());
}

public sealed record QadNextPurchaseOrder(
    string PoNumber, DateOnly? DueDate, decimal OpenQuantity, bool IsConfirmed, string? TrackingInfo, bool IsKss, string? PoState);
public sealed record QadNextPurchaseOrderRawRow(
    string PoNumber, DateTime? DueDate, decimal OpenQuantity, bool IsConfirmed, string? TrackingInfo, bool IsKss, string? PoState);
