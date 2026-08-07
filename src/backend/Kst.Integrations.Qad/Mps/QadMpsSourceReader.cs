using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Kst.Domain.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.Mps;

/// <summary>
/// Direct, parameterized QAD adapter query for the initial MPS source read. Owns SQL text, SQL
/// parameters, part-list batching, the QAD-shaped raw result, and raw-to-normalized mapping. Does not
/// pivot weeks, aggregate weekly quantities, or de-duplicate; returns row-oriented facts.
/// </summary>
public sealed class QadMpsSourceReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadMpsSourceReader> _logger;

    public QadMpsSourceReader(QadConnectionOptions options, ILogger<QadMpsSourceReader> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Reads MPS source facts for a workspace's resolved parent parts. Domain is derived from
    /// <paramref name="site"/> via <see cref="QadSiteDomainMap"/> here, at the QAD integration
    /// boundary, so callers (including Kst.Application) never need QAD-specific domain knowledge.
    /// </summary>
    public async Task<IReadOnlyList<MpsSourceRow>> ReadAsync(
        string site,
        IReadOnlyList<string> parentParts,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("QAD connection is not configured.");

        var domain = QadSiteDomainMap.Resolve(site);
        var batches = MpsPartBatcher.Batch(parentParts, _options.MaxPartBatchSize);
        if (batches.Count == 0)
            return [];

        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);

        var results = new List<MpsSourceRow>();
        for (var i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            var stopwatch = Stopwatch.StartNew();
            var (sql, parameters) = BuildBatchQuery(domain, site, batch);
            var command = new CommandDefinition(
                sql,
                parameters,
                commandTimeout: _options.CommandTimeoutSeconds,
                cancellationToken: cancellationToken);

            var rawRows = await connection.QueryAsync<QadMpsRawRow>(command);
            var normalized = rawRows.Select(Normalize).ToList();
            stopwatch.Stop();

            _logger.LogInformation(
                "MPS source batch {BatchIndex}/{BatchCount} ({PartCount} parts) returned {RowCount} rows in {ElapsedMs}ms.",
                i + 1, batches.Count, batch.Count, normalized.Count, stopwatch.ElapsedMilliseconds);

            results.AddRange(normalized);
        }

        return results;
    }

    /// <summary>
    /// Builds the parameterized scope-table query for one batch of parent parts. Public and pure
    /// (no connection) so SQL/parameter shape is independently testable.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildBatchQuery(
        string domain,
        string site,
        IReadOnlyList<string> parts)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);

        var valueRows = new List<string>(parts.Count);
        for (var i = 0; i < parts.Count; i++)
        {
            var paramName = $"Part{i}";
            parameters.Add(paramName, parts[i]);
            valueRows.Add($"(@{paramName})");
        }

        var sql = $"""
            WITH ScopeParts (ParentPart) AS
            (
                SELECT ParentPart FROM (VALUES {string.Join(", ", valueRows)}) AS Parts (ParentPart)
            )
            SELECT
                mrp.mrp_domain      AS Domain,
                UPPER(mrp.mrp_site) AS Site,
                mrp.mrp_part        AS ParentPart,
                pt.pt_desc1         AS Description,
                mrp.mrp_due_date    AS DueDate,
                mrp.mrp_rel_date    AS ReleaseDate,
                mrp.mrp_qty         AS Quantity,
                mrp.mrp_type        AS MrpType,
                mrp.mrp_line        AS WorkOrderId,
                wo.wo_status        AS WorkOrderStatus
            FROM ScopeParts AS scope
            INNER JOIN qadpro2.dbo.mrp_det AS mrp
                ON mrp.mrp_part = scope.ParentPart
                AND mrp.mrp_domain = @Domain
                AND mrp.mrp_site = @Site
            INNER JOIN qadpro2.dbo.wo_mstr AS wo
                ON wo.wo_nbr = mrp.mrp_nbr
                AND wo.wo_lot = mrp.mrp_line
                AND wo.wo_domain = mrp.mrp_domain
                AND wo.wo_site = mrp.mrp_site
                AND wo.wo_part = mrp.mrp_part
            LEFT JOIN qadpro2.dbo.pt_mstr AS pt
                ON pt.pt_part = mrp.mrp_part
                AND pt.pt_domain = mrp.mrp_domain
            WHERE
                mrp.mrp_dataset = 'wo_mstr'
                AND LOWER(mrp.mrp_type) IN ('supply', 'supplyf', 'supplyp')
                AND wo.wo_status <> 'C'
                AND ISNULL(wo.wo_bom_code, '') <> 'RMABOM'
            ORDER BY
                mrp.mrp_part,
                mrp.mrp_due_date,
                mrp.mrp_line;
            """;

        return (sql, parameters);
    }

    public static MpsSourceRow Normalize(QadMpsRawRow raw) => new(
        Domain: raw.Domain,
        Site: raw.Site,
        ParentPart: raw.ParentPart,
        Description: raw.Description,
        DueDate: DateOnly.FromDateTime(raw.DueDate),
        ReleaseDate: raw.ReleaseDate.HasValue ? DateOnly.FromDateTime(raw.ReleaseDate.Value) : null,
        Quantity: raw.Quantity,
        SupplyType: NormalizeSupplyType(raw.MrpType),
        WorkOrderId: raw.WorkOrderId,
        WorkOrderState: NormalizeWorkOrderState(raw.WorkOrderStatus));

    /// <summary>SQL already restricts to supply/supplyf/supplyp; the default case is defensive only.</summary>
    public static MpsSupplyType NormalizeSupplyType(string mrpType) => mrpType.Trim().ToLowerInvariant() switch
    {
        "supply" => MpsSupplyType.Supply,
        "supplyf" => MpsSupplyType.SupplyF,
        "supplyp" => MpsSupplyType.SupplyP,
        _ => MpsSupplyType.Supply
    };

    /// <summary>SQL already excludes 'C'; any other unexpected value normalizes to Unknown rather than failing.</summary>
    public static MpsWorkOrderState NormalizeWorkOrderState(string woStatus) => woStatus.Trim() switch
    {
        "A" => MpsWorkOrderState.Allocating,
        "F" => MpsWorkOrderState.Frozen,
        "R" => MpsWorkOrderState.Released,
        "P" => MpsWorkOrderState.Planned,
        "e" => MpsWorkOrderState.ExplicitlyScheduled,
        _ => MpsWorkOrderState.Unknown
    };
}

/// <summary>QAD-shaped raw Dapper result row. Does not travel past this integration boundary.</summary>
public sealed record QadMpsRawRow(
    string Domain,
    string Site,
    string ParentPart,
    string? Description,
    DateTime DueDate,
    DateTime? ReleaseDate,
    decimal Quantity,
    string MrpType,
    string WorkOrderId,
    string WorkOrderStatus
);
