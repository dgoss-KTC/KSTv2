using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Kst.Domain.WorkOrders;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.WorkOrders;

/// <summary>
/// Direct, parameterized QAD adapter for Stage 7 work-order summary/card retrieval (accepted contract
/// §7/§13). Supports two query shapes that share the same card-field + Kitting-count SQL projection:
/// (1) explicit WOID lookup, for the top-level bucket → Work Orders drill-down, where the caller
/// already knows the relevant WOIDs from the retained MPS snapshot; (2) candidate subassembly lookup
/// by manufactured component part, across all eligible A/F/R work orders regardless of Due Date. Both
/// restrict results to the eligible A/F/R status set and exclude RMABOM work orders at the SQL boundary.
/// </summary>
public sealed class QadWorkOrderSummaryReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadWorkOrderSummaryReader> _logger;

    public QadWorkOrderSummaryReader(QadConnectionOptions options, ILogger<QadWorkOrderSummaryReader> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>Reads Stage 7 work-order summaries for an explicit set of WOIDs (top-level bucket drill-down).</summary>
    public async Task<IReadOnlyList<WorkOrderSummary>> ReadByWoidsAsync(
        string site,
        IReadOnlyList<string> woids,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("QAD connection is not configured.");
        if (woids.Count == 0)
            return [];

        var domain = QadSiteDomainMap.Resolve(site);
        var stopwatch = Stopwatch.StartNew();

        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);

        var (sql, parameters) = BuildByWoidsQuery(domain, site, woids);
        var command = new CommandDefinition(
            sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        var rawRows = await connection.QueryAsync<QadWorkOrderSummaryRawRow>(command);
        var normalized = rawRows.Select(Normalize).ToList();

        stopwatch.Stop();
        _logger.LogInformation(
            "Work order summary read for {WoidCount} WOID(s) in site {Site} returned {RowCount} rows in {ElapsedMs}ms.",
            woids.Count, site, normalized.Count, stopwatch.ElapsedMilliseconds);

        return normalized;
    }

    /// <summary>
    /// Reads candidate subassembly work orders for a manufactured component across all eligible
    /// A/F/R work orders, regardless of Due Date. Fetches one row beyond <paramref name="limit"/> so
    /// truncation can be detected without a second round trip.
    /// </summary>
    public async Task<CandidateWorkOrdersResult> ReadCandidatesAsync(
        string site,
        string componentPart,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("QAD connection is not configured.");
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit), "Candidate limit must be positive.");

        var domain = QadSiteDomainMap.Resolve(site);
        var stopwatch = Stopwatch.StartNew();

        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);

        var (sql, parameters) = BuildCandidateQuery(domain, site, componentPart, limit);
        var command = new CommandDefinition(
            sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        var rawRows = (await connection.QueryAsync<QadWorkOrderSummaryRawRow>(command)).ToList();

        stopwatch.Stop();
        _logger.LogInformation(
            "Candidate work order read for component {ComponentPart} in site {Site} returned {RowCount} rows in {ElapsedMs}ms.",
            componentPart, site, rawRows.Count, stopwatch.ElapsedMilliseconds);

        return ComposeCandidateResult(rawRows, limit);
    }

    /// <summary>Public and pure so the fetch-one-extra / truncation-detection logic is independently testable.</summary>
    public static CandidateWorkOrdersResult ComposeCandidateResult(IReadOnlyList<QadWorkOrderSummaryRawRow> rawRows, int limit)
    {
        var isTruncated = rawRows.Count > limit;
        var candidates = rawRows.Take(limit).Select(Normalize).ToList();
        return new CandidateWorkOrdersResult(candidates, isTruncated);
    }

    /// <summary>
    /// Builds the explicit-WOID lookup query. Public and pure (no connection) so SQL/parameter shape
    /// is independently testable. Uses the same VALUES-table parameterization convention as
    /// <see cref="Mps.QadMpsSourceReader.BuildBatchQuery"/> rather than Dapper's automatic IN-clause
    /// expansion, for consistent, directly-inspectable SQL text.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildByWoidsQuery(
        string domain, string site, IReadOnlyList<string> woids)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);

        var valueRows = new List<string>(woids.Count);
        for (var i = 0; i < woids.Count; i++)
        {
            var paramName = $"Woid{i}";
            parameters.Add(paramName, woids[i]);
            valueRows.Add($"(@{paramName})");
        }

        var sql = $"""
            WITH RequestedWoids (Woid) AS
            (
                SELECT Woid FROM (VALUES {string.Join(", ", valueRows)}) AS W (Woid)
            )
            SELECT
            {CardSelectColumnsSql}
            FROM qadpro2.dbo.wo_mstr AS wo
            INNER JOIN RequestedWoids AS req
                ON req.Woid = wo.wo_lot
            {KittingApplySql}
            WHERE wo.wo_domain = @Domain
              AND wo.wo_site = @Site
              AND wo.wo_status IN ('A', 'F', 'R')
              AND ISNULL(wo.wo_bom_code, '') <> 'RMABOM';
            """;

        return (sql, parameters);
    }

    /// <summary>
    /// Builds the candidate-subassembly query. Public and pure for the same reason as
    /// <see cref="BuildByWoidsQuery"/>. Returns all eligible A/F/R work orders for the component
    /// regardless of Due Date. Ordering matches the accepted rule: most recent Due Date first (Due
    /// Date, then Release Date, both descending), WOID as a deterministic tie-break.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildCandidateQuery(
        string domain, string site, string componentPart, int limit)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("ComponentPart", componentPart);
        parameters.Add("FetchLimit", limit + 1);

        var sql = $"""
            SELECT TOP (@FetchLimit)
            {CardSelectColumnsSql}
            FROM qadpro2.dbo.wo_mstr AS wo
            {KittingApplySql}
            WHERE wo.wo_domain = @Domain
              AND wo.wo_site = @Site
              AND wo.wo_part = @ComponentPart
              AND wo.wo_status IN ('A', 'F', 'R')
              AND ISNULL(wo.wo_bom_code, '') <> 'RMABOM'
            ORDER BY
                wo.wo_due_date DESC,
                wo.wo_rel_date DESC,
                wo.wo_lot ASC;
            """;

        return (sql, parameters);
    }

    // Dapper's record deserializer requires the constructor parameter order to match this column
    // order exactly (positional, not name-based) - keep SalesOrder last to match
    // QadWorkOrderSummaryRawRow's trailing optional parameter position.
    private const string CardSelectColumnsSql = """
                wo.wo_part          AS PartNumber,
                wo.wo_lot           AS Woid,
                wo.wo_status        AS Status,
                wo.wo_qty_ord       AS OrderedQuantity,
                wo.wo_qty_comp      AS CompletedQuantity,
                wo.wo_rel_date      AS ReleaseDate,
                wo.wo_due_date      AS DueDate,
                ISNULL(kit.ApplicableLineCount, 0)  AS ApplicableLineCount,
                ISNULL(kit.FullyIssuedLineCount, 0) AS FullyIssuedLineCount,
                wo.wo_so_job        AS SalesOrder
        """;

    /// <summary>
    /// Counts applicable (<c>wod_qty_req &lt;&gt; 0</c>) and fully-issued (<c>wod_qty_iss &gt;=
    /// wod_qty_req</c>) material lines per work order without materializing the lines themselves.
    /// </summary>
    private const string KittingApplySql = """
        OUTER APPLY (
                SELECT
                    COUNT(*) AS ApplicableLineCount,
                    SUM(CASE WHEN wod.wod_qty_iss >= wod.wod_qty_req THEN 1 ELSE 0 END) AS FullyIssuedLineCount
                FROM qadpro2.dbo.wod_det AS wod
                WHERE wod.wod_domain = wo.wo_domain
                  AND wod.wod_lot = wo.wo_lot
                  AND wod.wod_qty_req <> 0
            ) AS kit
        """;

    public static WorkOrderSummary Normalize(QadWorkOrderSummaryRawRow raw) => new(
        PartNumber: raw.PartNumber,
        Woid: raw.Woid,
        Status: NormalizeStatus(raw.Status),
        OrderedQuantity: raw.OrderedQuantity,
        CompletedQuantity: raw.CompletedQuantity,
        ReleaseDate: raw.ReleaseDate.HasValue ? DateOnly.FromDateTime(raw.ReleaseDate.Value) : null,
        DueDate: raw.DueDate.HasValue ? DateOnly.FromDateTime(raw.DueDate.Value) : null,
        Kitting: KittingSummary.Calculate(raw.ApplicableLineCount, raw.FullyIssuedLineCount),
        SalesOrder: string.IsNullOrWhiteSpace(raw.SalesOrder) ? null : raw.SalesOrder.Trim());

    /// <summary>SQL already restricts to A/F/R; an unexpected value indicates a query/data defect and must fail loudly.</summary>
    public static WorkOrderStatus NormalizeStatus(string status) => status.Trim() switch
    {
        "A" => WorkOrderStatus.Allocating,
        "F" => WorkOrderStatus.Frozen,
        "R" => WorkOrderStatus.Released,
        _ => throw new InvalidOperationException($"Unexpected work order status '{status}' outside the eligible A/F/R set.")
    };
}

/// <summary>QAD-shaped raw Dapper result row. Does not travel past this integration boundary.</summary>
public sealed record QadWorkOrderSummaryRawRow(
    string PartNumber,
    string Woid,
    string Status,
    decimal OrderedQuantity,
    decimal CompletedQuantity,
    DateTime? ReleaseDate,
    DateTime? DueDate,
    int ApplicableLineCount,
    int FullyIssuedLineCount,
    string? SalesOrder = null
);
