using System.Diagnostics;
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Kst.Domain.Mps;
using Kst.Domain.WorkOrders;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.WorkOrders;

/// <summary>
/// Direct, parameterized QAD adapter for Stage 7/7R work-order summary/card retrieval (accepted
/// contract §7/§13, amended by Stage 7R). Two query shapes share the same card-field +
/// Kitting-count SQL projection:
/// (1) the parent-scoped four-week planning window (Stage 7R), sourced directly from <c>wo_mstr</c>
///     and open to every non-closed work order (Closed and RMABOM excluded at the SQL boundary);
/// (2) a single WOID lookup without the A/F/R eligibility filter, used to resolve a planning-window
///     parent's facts (a Stage 7R depth-1 work order may carry any non-closed status).
/// All shapes exclude RMABOM work orders at the SQL boundary.
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

    /// <summary>
    /// Reads the parent-scoped four-week Work Order planning window (Stage 7R): Due-Date-based
    /// Falldown plus Week 0..3 under the active weekly-bucket basis, for every non-closed,
    /// non-RMABOM work order on the parent part. <paramref name="bucketKind"/>/
    /// <paramref name="bucketWeekStart"/> narrow the result to a single bucket (Falldown, or one
    /// forward week); both null returns the full window. Falldown is always Due-Date based
    /// regardless of <paramref name="dateBasis"/>.
    /// </summary>
    public async Task<IReadOnlyList<WorkOrderSummary>> ReadPlanningWindowAsync(
        string site,
        string parentPart,
        MpsDateBasis dateBasis,
        DateOnly weekStart,
        DateOnly windowEndExclusive,
        MpsBucketKind? bucketKind,
        DateOnly? bucketWeekStart,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("QAD connection is not configured.");

        var domain = QadSiteDomainMap.Resolve(site);
        var stopwatch = Stopwatch.StartNew();

        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);

        var (sql, parameters) = BuildPlanningWindowQuery(
            domain, site, parentPart, dateBasis, weekStart, windowEndExclusive, bucketKind, bucketWeekStart);
        var command = new CommandDefinition(
            sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        var rawRows = (await connection.QueryAsync<QadWorkOrderSummaryRawRow>(command)).ToList();
        var normalized = rawRows.Select(NormalizePlanningWindow).ToList();

        stopwatch.Stop();
        _logger.LogInformation(
            "Planning-window work order read for part {ParentPart} in site {Site} returned {RowCount} rows in {ElapsedMs}ms.",
            parentPart, site, normalized.Count, stopwatch.ElapsedMilliseconds);

        return normalized;
    }

    /// <summary>
    /// Reads one work-order summary by WOID without the A/F/R eligibility filter (Stage 7R). A
    /// planning-window depth-1 work order may carry any non-closed status, so the immediate parent's
    /// facts must resolve for non-A/F/R parents too. Closed and RMABOM work orders are still
    /// excluded at the SQL boundary. Returns null when no such work order exists.
    /// </summary>
    public async Task<WorkOrderSummary?> ReadByWoidAsync(
        string site,
        string woid,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("QAD connection is not configured.");

        var domain = QadSiteDomainMap.Resolve(site);

        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);

        var (sql, parameters) = BuildByWoidQuery(domain, site, woid);
        var command = new CommandDefinition(
            sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        var row = await connection.QueryFirstOrDefaultAsync<QadWorkOrderSummaryRawRow>(command);

        return row is null ? null : NormalizePlanningWindow(row);
    }

    /// <summary>
    /// Builds the parent-scoped four-week planning-window query. Public and pure (no connection) so
    /// SQL/parameter shape is independently testable. The forward-window predicate switches between
    /// <c>wo_due_date</c> (Due basis) and <c>wo_rel_date</c> (Release basis) via the <c>@DateBasis</c>
    /// parameter; the Falldown predicate always uses <c>wo_due_date</c>. <c>@WindowEnd</c> is the
    /// exclusive horizon end (first day of Week 4) so the four-week boundary is parameterized, not
    /// hardcoded.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildPlanningWindowQuery(
        string domain, string site, string parentPart,
        MpsDateBasis dateBasis,
        DateOnly weekStart,
        DateOnly windowEndExclusive,
        MpsBucketKind? bucketKind,
        DateOnly? bucketWeekStart)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("ParentPart", parentPart);
        parameters.Add("WeekStart", weekStart.ToDateTime(TimeOnly.MinValue), DbType.Date);
        parameters.Add("DateBasis", dateBasis == MpsDateBasis.ReleaseDate ? "releaseDate" : "dueDate");

        string predicate;
        if (bucketKind == MpsBucketKind.Falldown)
        {
            // Falldown is always Due-Date based, regardless of the active weekly-bucket basis.
            predicate = "wo.wo_due_date < @WeekStart";
        }
        else if (bucketKind == MpsBucketKind.Weekly)
        {
            if (bucketWeekStart is not { } bucketStart)
                throw new ArgumentException(
                    "A weekly planning-window bucket requires a bucket week start.", nameof(bucketWeekStart));
            var bucketWeekEnd = bucketStart.AddDays(7);
            parameters.Add("BucketWeekStart", bucketStart.ToDateTime(TimeOnly.MinValue), DbType.Date);
            parameters.Add("BucketWeekEnd", bucketWeekEnd.ToDateTime(TimeOnly.MinValue), DbType.Date);
            predicate = """
                ( (@DateBasis = 'releaseDate' AND wo.wo_rel_date >= @BucketWeekStart AND wo.wo_rel_date < @BucketWeekEnd)
                  OR (@DateBasis = 'dueDate' AND wo.wo_due_date >= @BucketWeekStart AND wo.wo_due_date < @BucketWeekEnd) )
                """;
        }
        else
        {
            // Full planning window: Due-Date-based Falldown PLUS Week 0..3 under the active basis.
            // Intentionally not reducible to one date-field predicate in Release mode.
            parameters.Add("WindowEnd", windowEndExclusive.ToDateTime(TimeOnly.MinValue), DbType.Date);
            predicate = """
                ( wo.wo_due_date < @WeekStart
                  OR (@DateBasis = 'releaseDate' AND wo.wo_rel_date >= @WeekStart AND wo.wo_rel_date < @WindowEnd)
                  OR (@DateBasis = 'dueDate' AND wo.wo_due_date >= @WeekStart AND wo.wo_due_date < @WindowEnd) )
                """;
        }

        var sql = $"""
            SELECT
            {CardSelectColumnsSql}
            FROM qadpro2.dbo.wo_mstr AS wo
            {KittingApplySql}
            WHERE wo.wo_domain = @Domain
              AND wo.wo_site = @Site
              AND wo.wo_part = @ParentPart
              AND wo.wo_status <> 'C'
              AND ISNULL(wo.wo_bom_code, '') <> 'RMABOM'
              AND {predicate}
            ORDER BY
                CASE WHEN wo.wo_due_date < @WeekStart THEN 0 ELSE 1 END,
                CASE WHEN @DateBasis = 'releaseDate' THEN wo.wo_rel_date ELSE wo.wo_due_date END,
                wo.wo_lot
            """;

        return (sql, parameters);
    }

    /// <summary>
    /// Builds the single-WOID lookup query (Stage 7R parent resolution). Public and pure for the
    /// same reason as <see cref="BuildPlanningWindowQuery"/>. No A/F/R filter — a planning-window
    /// parent may carry any non-closed status — but Closed and RMABOM are still excluded.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildByWoidQuery(string domain, string site, string woid)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("Woid", woid);

        var sql = $"""
            SELECT
            {CardSelectColumnsSql}
            FROM qadpro2.dbo.wo_mstr AS wo
            {KittingApplySql}
            WHERE wo.wo_domain = @Domain
              AND wo.wo_site = @Site
              AND wo.wo_lot = @Woid
              AND wo.wo_status <> 'C'
              AND ISNULL(wo.wo_bom_code, '') <> 'RMABOM';
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

    /// <summary>Normalizes a planning-window / single-WOID row, passing any non-closed raw status code through (trimmed).</summary>
    public static WorkOrderSummary NormalizePlanningWindow(QadWorkOrderSummaryRawRow raw) =>
        BuildSummary(raw, NormalizePlanningWindowStatus);

    private static WorkOrderSummary BuildSummary(QadWorkOrderSummaryRawRow raw, Func<string, string> statusNormalizer) => new(
        PartNumber: raw.PartNumber,
        Woid: raw.Woid,
        Status: statusNormalizer(raw.Status),
        OrderedQuantity: raw.OrderedQuantity,
        CompletedQuantity: raw.CompletedQuantity,
        ReleaseDate: raw.ReleaseDate.HasValue ? DateOnly.FromDateTime(raw.ReleaseDate.Value) : null,
        DueDate: raw.DueDate.HasValue ? DateOnly.FromDateTime(raw.DueDate.Value) : null,
        Kitting: KittingSummary.Calculate(raw.ApplicableLineCount, raw.FullyIssuedLineCount),
        SalesOrder: string.IsNullOrWhiteSpace(raw.SalesOrder) ? null : raw.SalesOrder.Trim());

    /// <summary>
    /// Normalizes a planning-window / single-WOID status: any non-closed raw code passes through
    /// (trimmed) so a previously unseen non-closed code renders safely instead of failing the read.
    /// Closed ('C') is excluded at the SQL boundary; no defensive drop is applied here so a query
    /// defect would surface rather than silently removing a row.
    /// </summary>
    public static string NormalizePlanningWindowStatus(string status) => status.Trim();
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
