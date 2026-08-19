using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Kst.Domain.Inventory;
using Kst.Domain.PartDetail;
using Kst.Integrations.Qad.Inventory;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.PartDetail;

/// <summary>
/// Direct, parameterized QAD adapter for Stage 6 PartDetail: one part-master + site-specific
/// planning-parameter lookup (<c>pt_mstr</c> LEFT JOIN <c>ptp_det</c>), one inventory aggregation
/// (the shared accepted Stage 6 classification, built by
/// <see cref="Kst.Integrations.Qad.Inventory.QadPartInventoryReader.BuildBatchQuery"/> as a
/// one-part batch and executed on this reader's single connection), and one current-price-tier
/// lookup. Owns SQL text/parameters and raw-to-normalized mapping; does not know about caching,
/// MPS scope, or workspace state (see
/// <see cref="Kst.Application.PartDetail.PartDetailService"/> for that orchestration).
/// </summary>
public sealed class QadPartDetailReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadPartDetailReader> _logger;

    public QadPartDetailReader(QadConnectionOptions options, ILogger<QadPartDetailReader> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Reads PartDetail source facts for one part. Domain is derived from <paramref name="site"/> via
    /// <see cref="QadSiteDomainMap"/>, at the QAD integration boundary. Returns null when no
    /// <c>pt_mstr</c> row exists for the part/domain (a true missing-part state).
    /// </summary>
    public async Task<PartDetailSourceFacts?> ReadAsync(
        string site,
        string partNumber,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("QAD connection is not configured.");

        var domain = QadSiteDomainMap.Resolve(site);
        var stopwatch = Stopwatch.StartNew();

        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);

        var (partSql, partParameters) = BuildPartMasterQuery(domain, site, partNumber);
        var partCommand = new CommandDefinition(
            partSql, partParameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        var partRow = await connection.QuerySingleOrDefaultAsync<QadPartMasterRawRow>(partCommand);

        if (partRow is null)
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "PartDetail part-master lookup found no pt_mstr row for part {PartNumber} in domain {Domain}.",
                partNumber, domain);
            return null;
        }

        var (inventorySql, inventoryParameters) = QadPartInventoryReader.BuildBatchQuery(domain, site, [partNumber]);
        var inventoryCommand = new CommandDefinition(
            inventorySql, inventoryParameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        var inventoryRow = await connection.QuerySingleOrDefaultAsync<QadPartInventoryRawRow>(inventoryCommand);
        var inventorySummary = inventoryRow is null ? null : QadPartInventoryReader.Normalize(inventoryRow);

        var (priceSql, priceParameters) = BuildPriceQuery(domain, partNumber, today);
        var priceCommand = new CommandDefinition(
            priceSql, priceParameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        var priceRows = await connection.QueryAsync<QadPartPriceRawRow>(priceCommand);

        stopwatch.Stop();
        _logger.LogInformation(
            "PartDetail read for part {PartNumber} in domain {Domain} completed in {ElapsedMs}ms.",
            partNumber, domain, stopwatch.ElapsedMilliseconds);

        return Normalize(partRow, inventorySummary, priceRows.ToList());
    }

    /// <summary>
    /// Builds the part-master + site-specific planning-parameter lookup query. Public and pure so
    /// SQL/parameter shape is independently testable. Part master identity is domain + part
    /// (<c>pt_mstr</c>); site-specific planning parameters (manufacturing lead time, safety time,
    /// current revision, safety stock) come from the <c>ptp_det</c> row for the selected site, joined
    /// via <c>ptp_domain</c>/<c>ptp_part</c> (NOT <c>pt_mstr.pt_site</c> — per-domain SME guidance,
    /// <c>pt_mstr</c> is master-level/not reliably site-scoped; <c>ptp_det</c> owns site-specific
    /// values). A <c>LEFT JOIN</c> with the site filter in the join condition (not <c>WHERE</c>)
    /// preserves the accepted "blank QAD data is allowed" rule: a missing <c>ptp_det</c> row for the
    /// selected site still returns the <c>pt_mstr</c>-sourced fields, with the four site-specific
    /// fields left null.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildPartMasterQuery(string domain, string site, string partNumber)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("Part", partNumber);

        const string sql = """
            SELECT TOP (1)
                pt.pt_part      AS PartNumber,
                pt.pt_buyer     AS PlannerCode,
                ptp.ptp_mfg_lead AS ManufacturingLeadTimeDays,
                ptp.ptp_sfty_tme AS SafetyTimeDays,
                pt.pt_status    AS PartStatusCode,
                pt.pt_rev       AS CurrentRevision,
                pt.pt_desc1     AS Description,
                pt.pt_warr_cd   AS IosCode,
                ptp.ptp_sfty_stk AS SafetyStockQuantity
            FROM qadpro2.dbo.pt_mstr AS pt
            LEFT JOIN qadpro2.dbo.ptp_det AS ptp
                ON ptp.ptp_domain = pt.pt_domain
                AND ptp.ptp_part = pt.pt_part
                AND ptp.ptp_site = @Site
            WHERE pt.pt_domain = @Domain
              AND pt.pt_part = @Part;
            """;

        return (sql, parameters);
    }

    /// <summary>
    /// Builds the current-price-tier query: latest <c>pi_mstr</c> row with <c>pi_start &lt;= today</c>
    /// wins, joined to its <c>pid_det</c> tiers ordered by MOQ ascending.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildPriceQuery(string domain, string partNumber, DateOnly today)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Part", partNumber);
        parameters.Add("Today", today.ToDateTime(TimeOnly.MinValue));

        const string sql = """
            WITH CurrentPriceList AS
            (
                SELECT TOP (1) pi_list_id
                FROM qadpro2.dbo.pi_mstr
                WHERE pi_domain = @Domain
                  AND pi_part_code = @Part
                  AND pi_start <= @Today
                ORDER BY pi_start DESC
            )
            SELECT
                pid.pid_qty AS MinimumOrderQuantity,
                pid.pid_amt AS UnitPrice
            FROM CurrentPriceList AS cpl
            INNER JOIN qadpro2.dbo.pid_det AS pid
                ON pid.pid_domain = @Domain
                AND pid.pid_list_id = cpl.pi_list_id
            ORDER BY pid.pid_qty ASC;
            """;

        return (sql, parameters);
    }

    public static PartDetailSourceFacts Normalize(
        QadPartMasterRawRow part,
        PartInventorySummary? inventory,
        IReadOnlyList<QadPartPriceRawRow> priceRows) => new(
        PartNumber: part.PartNumber,
        PlannerCode: part.PlannerCode,
        ManufacturingLeadTimeDays: part.ManufacturingLeadTimeDays,
        SafetyTimeDays: part.SafetyTimeDays,
        PartStatusCode: part.PartStatusCode,
        CurrentRevision: part.CurrentRevision,
        Description: part.Description,
        IosCode: part.IosCode,
        SafetyStockQuantity: part.SafetyStockQuantity,
        QuantityOnHand: inventory?.NetQuantityOnHand ?? 0m,
        QuantityNonNet: inventory?.NonNetQuantityOnHand ?? 0m,
        QuantityRmaOnHand: inventory?.RmaQuantityOnHand ?? 0m,
        PriceBreaks: priceRows
            .Select(r => new PartPriceBreak(r.MinimumOrderQuantity, r.UnitPrice))
            .OrderBy(b => b.MinimumOrderQuantity)
            .ToList());
}

/// <summary>QAD-shaped raw part-master Dapper result row. Does not travel past this integration boundary.</summary>
public sealed record QadPartMasterRawRow(
    string PartNumber,
    string? PlannerCode,
    decimal? ManufacturingLeadTimeDays,
    decimal? SafetyTimeDays,
    string? PartStatusCode,
    string? CurrentRevision,
    string? Description,
    string? IosCode,
    decimal? SafetyStockQuantity
);

/// <summary>QAD-shaped raw price-tier Dapper result row.</summary>
public sealed record QadPartPriceRawRow(decimal MinimumOrderQuantity, decimal UnitPrice);
