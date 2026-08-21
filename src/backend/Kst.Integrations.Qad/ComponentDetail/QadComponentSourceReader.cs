using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Kst.Domain.ComponentDetail;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.ComponentDetail;

/// <summary>
/// Direct, parameterized QAD adapter for Stage 8D.5 Component Detail: one part-master +
/// selected-site planning-parameter lookup (<c>pt_mstr</c> LEFT JOIN <c>ptp_det</c>), one
/// Standard Cost lookup (<c>sct_det</c>, owner-accepted <c>sct_sim = 'Standard'</c> filter), and
/// one QCTC lookup (<c>Analysis.dbo.in_price</c>, owner-accepted <c>inp_source = 'qtbom_det'</c>
/// filter) — three sequential queries on one connection, matching
/// <see cref="Kst.Integrations.Qad.PartDetail.QadPartDetailReader"/>'s shape. Deliberately does
/// not touch inventory (composed separately by
/// <see cref="Kst.Application.ComponentDetail.ComponentDetailService"/> from the shared
/// <c>IPartInventoryReader</c>). Owns SQL text/parameters and raw-to-normalized mapping; does not
/// know about caching, MPS scope, or workspace state.
/// </summary>
public sealed class QadComponentSourceReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadComponentSourceReader> _logger;

    public QadComponentSourceReader(QadConnectionOptions options, ILogger<QadComponentSourceReader> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Reads Component Detail source facts for one part. Domain is derived from
    /// <paramref name="site"/> via <see cref="QadSiteDomainMap"/>, at the QAD integration
    /// boundary. Returns null when no <c>pt_mstr</c> row exists for the part/domain (a true
    /// missing-component state) without issuing the Standard Cost/QCTC queries.
    /// </summary>
    public async Task<ComponentSourceFacts?> ReadAsync(
        string site,
        string componentPart,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("QAD connection is not configured.");

        var domain = QadSiteDomainMap.Resolve(site);
        var stopwatch = Stopwatch.StartNew();

        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);

        var (masterSql, masterParameters) = BuildMasterPlanningQuery(domain, site, componentPart);
        var masterCommand = new CommandDefinition(
            masterSql, masterParameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        var masterRow = await connection.QuerySingleOrDefaultAsync<QadComponentMasterRawRow>(masterCommand);

        if (masterRow is null)
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "Component Detail master lookup found no pt_mstr row for part {ComponentPart} in domain {Domain}.",
                componentPart, domain);
            return null;
        }

        var (costSql, costParameters) = BuildStandardCostQuery(domain, site, componentPart);
        var costCommand = new CommandDefinition(
            costSql, costParameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        var costRow = await connection.QuerySingleOrDefaultAsync<QadStandardCostRawRow>(costCommand);

        var (qctcSql, qctcParameters) = BuildQctcQuery(domain, site, componentPart);
        var qctcCommand = new CommandDefinition(
            qctcSql, qctcParameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        var qctcRow = await connection.QuerySingleOrDefaultAsync<QadQctcRawRow>(qctcCommand);

        stopwatch.Stop();
        _logger.LogInformation(
            "Component Detail read for part {ComponentPart} in domain {Domain} completed in {ElapsedMs}ms.",
            componentPart, domain, stopwatch.ElapsedMilliseconds);

        return Normalize(masterRow, costRow, qctcRow);
    }

    /// <summary>
    /// Builds the part-master + selected-site planning-parameter lookup query. Public and pure so
    /// SQL/parameter shape is independently testable. Part master identity is domain + part
    /// (<c>pt_mstr</c>); site-specific planning/lead-time/ordering values come from the
    /// <c>ptp_det</c> row for the selected site, joined via <c>ptp_domain</c>/<c>ptp_part</c>
    /// (NOT <c>pt_mstr.pt_site</c> — the Stage 8 BOM site/global P/M fallback is a P/M-specific
    /// rule and is never generalized here). A <c>LEFT JOIN</c> with the site filter in the join
    /// condition (not <c>WHERE</c>) preserves the accepted "missing selected-site data is valid"
    /// rule: a missing <c>ptp_det</c> row for the selected site still returns the
    /// <c>pt_mstr</c>-sourced fields, with all nine planning/lead-time/ordering fields left null.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildMasterPlanningQuery(string domain, string site, string componentPart)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("Part", componentPart);

        const string sql = """
            SELECT TOP (1)
                pt.pt_part AS ComponentPart,
                pt.pt_desc1 AS Description1,
                pt.pt_desc2 AS Description2,
                pt.pt_status AS PartStatusCode,
                pt.pt_warr_cd AS IosCode,

                ptp.ptp_timefnce AS TimeFence,
                ptp.ptp_sfty_tme AS SafetyTime,
                ptp.ptp_sfty_stk AS SafetyStock,
                ptp.ptp_buyer AS BuyerPlanner,
                ptp.ptp_pur_lead AS PurchaseLeadTimeDays,
                ptp.ptp_ins_lead AS InspectionLeadTimeDays,
                ptp.ptp_cum_lead AS CumulativeLeadTimeDays,
                ptp.ptp_ord_min AS MinimumOrderQuantity,
                ptp.ptp_ord_mult AS OrderMultiple

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
    /// Builds the Standard Cost query: owner-accepted <c>sct_sim = 'Standard'</c> filter, with
    /// <c>latest sct_cst_date</c> as a defensive tie-break (live validation found the filter alone
    /// yields a naturally unique row per part). Never selects across other simulations
    /// (Current/KPI/PurCst).
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildStandardCostQuery(string domain, string site, string componentPart)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("Part", componentPart);

        const string sql = """
            SELECT TOP (1)
                sct_cst_tot AS StandardCost,
                sct_cst_date AS StandardCostDate
            FROM qadpro2.dbo.sct_det
            WHERE sct_domain = @Domain
              AND sct_site = @Site
              AND sct_part = @Part
              AND sct_sim = 'Standard'
            ORDER BY sct_cst_date DESC;
            """;

        return (sql, parameters);
    }

    /// <summary>
    /// Builds the QCTC query: owner-accepted <c>inp_source = 'qtbom_det'</c> filter (the only
    /// source with meaningful non-zero QCTC in the validated environment — <c>idh_hist</c> and
    /// <c>pid_det</c> rows always carry <c>inp_qctc = 0</c> and can share the same latest date as
    /// a real <c>qtbom_det</c> row), with latest <c>inp_start_date</c> winning. Queries the
    /// <c>Analysis</c> database via the existing QAD connection/security context — no new
    /// connection or credentials are required.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildQctcQuery(string domain, string site, string componentPart)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("Part", componentPart);

        const string sql = """
            SELECT TOP (1)
                inp_qctc AS Qctc,
                inp_start_date AS QctcStartDate
            FROM Analysis.dbo.in_price
            WHERE inp_domain = @Domain
              AND inp_site = @Site
              AND inp_part = @Part
              AND inp_source = 'qtbom_det'
            ORDER BY inp_start_date DESC;
            """;

        return (sql, parameters);
    }

    public static ComponentSourceFacts Normalize(
        QadComponentMasterRawRow master,
        QadStandardCostRawRow? cost,
        QadQctcRawRow? qctc) => new(
        ComponentPart: master.ComponentPart,
        Description: CombineDescription(master.Description1, master.Description2),
        PartStatusCode: master.PartStatusCode,
        IosCode: master.IosCode,
        StandardCost: cost?.StandardCost,
        Qctc: qctc?.Qctc,
        TimeFence: master.TimeFence,
        SafetyTime: master.SafetyTime,
        SafetyStock: master.SafetyStock,
        BuyerPlanner: master.BuyerPlanner,
        PurchaseLeadTimeDays: master.PurchaseLeadTimeDays,
        InspectionLeadTimeDays: master.InspectionLeadTimeDays,
        CumulativeLeadTimeDays: master.CumulativeLeadTimeDays,
        MinimumOrderQuantity: master.MinimumOrderQuantity,
        OrderMultiple: master.OrderMultiple);

    /// <summary>
    /// Combines the part-master description segments null-safely: each segment is trimmed,
    /// NULL/whitespace-only segments are dropped, and the remaining segments are joined with a
    /// single space. Same convention as <c>QadBomReader.CombineDescription</c> (an independent
    /// copy, not a cross-feature reference — each QAD reader owns its own normalization). Pure
    /// and testable.
    /// </summary>
    public static string? CombineDescription(string? description1, string? description2)
    {
        var first = description1?.Trim();
        var second = description2?.Trim();

        if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(second))
            return null;
        if (string.IsNullOrEmpty(first))
            return second;
        if (string.IsNullOrEmpty(second))
            return first;

        return $"{first} {second}";
    }
}

/// <summary>QAD-shaped raw part-master + selected-site planning Dapper result row. Does not travel past this integration boundary.</summary>
public sealed record QadComponentMasterRawRow(
    string ComponentPart,
    string? Description1,
    string? Description2,
    string? PartStatusCode,
    string? IosCode,
    int? TimeFence,
    decimal? SafetyTime,
    decimal? SafetyStock,
    string? BuyerPlanner,
    int? PurchaseLeadTimeDays,
    int? InspectionLeadTimeDays,
    int? CumulativeLeadTimeDays,
    decimal? MinimumOrderQuantity,
    decimal? OrderMultiple);

/// <summary>QAD-shaped raw Standard Cost Dapper result row. <see cref="StandardCostDate"/> is selection evidence only, never exposed past this boundary.</summary>
public sealed record QadStandardCostRawRow(decimal? StandardCost, DateTime? StandardCostDate);

/// <summary>QAD-shaped raw QCTC Dapper result row. <see cref="QctcStartDate"/> is selection evidence only, never exposed past this boundary.</summary>
public sealed record QadQctcRawRow(decimal? Qctc, DateTime? QctcStartDate);
