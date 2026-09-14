using System.Diagnostics;
using Dapper;
using Microsoft.Extensions.Logging;
using Kst.Domain.ComponentOrders;
using Kst.Domain.Mps;
using Kst.Integrations.Qad.Inventory;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.ComponentOrders;

/// <summary>
/// Direct, parameterized QAD adapter for the Stage 10 Component Orders population: every
/// qualifying conventional open PO line for a bounded set of workspace-scoped component parts at
/// one site. Owns SQL text/parameters, lookup-key normalization/deduplication (reusing the shared
/// <see cref="QadPartInventoryReader.NormalizePartNumbers"/> convention), part-list batching via
/// <see cref="MpsPartBatcher"/>, the QAD-shaped raw result, and raw-to-normalized mapping. Does not
/// know about workspaces, MPS snapshots, BOMs, caching, or presentation.
///
/// Qualification is the accepted Stage 9 conventional open-PO rule verbatim: positive calculated
/// open quantity (<c>pod_qty_ord - pod_qty_rcvd &gt; 0</c>) and case-insensitive C/X status
/// exclusion. No <c>po_mstr.po_stat</c> predicate exists or may be added (Stage 9 deferred evidence
/// item). Missing part-master rows are legitimate returned data: the master joins are LEFT JOINs,
/// so a line whose part has no <c>pt_mstr</c> row still returns with null Description and null
/// master-fallback values — never a silent zero-row result.
///
/// The KSS indicator is the accepted Stage 9 independent effective supplier-schedule relationship
/// (effective <c>pod_end_eff##1</c>, <c>po_sched = 1</c>, effective <c>po_eff_to</c>) evaluated as a
/// scope-limited existence set; it annotates rows already admitted by the conventional rule and
/// never admits a row on its own.
/// </summary>
public sealed class QadComponentOrderReader
{
    public const int MaxComponentOrderBatchSize = 250;

    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadComponentOrderReader> _logger;

    public QadComponentOrderReader(QadConnectionOptions options, ILogger<QadComponentOrderReader> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Reads every qualifying conventional open PO line for the requested component parts at one
    /// site. Domain is derived from <paramref name="site"/> via <see cref="QadSiteDomainMap"/> here,
    /// at the QAD integration boundary, so callers never need QAD-specific domain knowledge. Requested
    /// part numbers are normalized and deduplicated case-insensitively in C# before SQL batching; an
    /// empty input returns an empty result without opening a connection. A QAD/query failure
    /// propagates as an exception — it is never converted to an empty list (an empty successful
    /// result means no scoped component has qualifying conventional open PO lines).
    /// </summary>
    public async Task<IReadOnlyList<ComponentOrderLine>> ReadAsync(
        string site,
        IReadOnlyList<string> componentParts,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        if (!_options.IsConfigured)
            throw new InvalidOperationException("QAD connection is not configured.");

        var domain = QadSiteDomainMap.Resolve(site);
        var lookupKeys = QadPartInventoryReader.NormalizePartNumbers(componentParts);
        if (lookupKeys.Count == 0)
        {
            _logger.LogInformation("Stage 10 component-order reader completed. NormalizedComponentCount=0 BatchCount=0 ReturnedRowCount=0 ElapsedMs={ElapsedMs}", stopwatch.ElapsedMilliseconds);
            return [];
        }

        var batches = MpsPartBatcher.Batch(lookupKeys, MaxComponentOrderBatchSize);
        _logger.LogInformation(
            "Stage 10 component-order reader starting. NormalizedComponentCount={NormalizedComponentCount} BatchCount={BatchCount} ElapsedMs={ElapsedMs}",
            lookupKeys.Count, batches.Count, stopwatch.ElapsedMilliseconds);
        _logger.LogInformation("Stage 10 component-order connection open started. ElapsedMs={ElapsedMs}", stopwatch.ElapsedMilliseconds);
        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);
        _logger.LogInformation("Stage 10 component-order connection open completed. ElapsedMs={ElapsedMs}", stopwatch.ElapsedMilliseconds);

        var results = new List<ComponentOrderLine>(lookupKeys.Count);
        for (var i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            var batchStopwatch = Stopwatch.StartNew();
            _logger.LogInformation(
                "Stage 10 component-order batch started. BatchIndex={BatchIndex} BatchCount={BatchCount} ScopedComponentCount={ScopedComponentCount} ElapsedMs={ElapsedMs}",
                i + 1, batches.Count, batch.Count, stopwatch.ElapsedMilliseconds);
            var (sql, parameters) = BuildBatchQuery(domain, site, batch, today);
            var command = new CommandDefinition(
                sql,
                parameters,
                commandTimeout: _options.CommandTimeoutSeconds,
                cancellationToken: cancellationToken);

            var rawRows = await connection.QueryAsync<QadComponentOrderLineRawRow>(command);
            var normalized = rawRows.Select(Normalize).ToList();
            batchStopwatch.Stop();

            _logger.LogInformation(
                "Stage 10 component-order batch completed. BatchIndex={BatchIndex} BatchCount={BatchCount} ScopedComponentCount={ScopedComponentCount} ReturnedRowCount={ReturnedRowCount} BatchElapsedMs={BatchElapsedMs} ElapsedMs={ElapsedMs}",
                i + 1, batches.Count, batch.Count, normalized.Count, batchStopwatch.ElapsedMilliseconds, stopwatch.ElapsedMilliseconds);

            results.AddRange(normalized);
        }

        _logger.LogInformation(
            "Stage 10 component-order reader completed. NormalizedComponentCount={NormalizedComponentCount} BatchCount={BatchCount} ReturnedRowCount={ReturnedRowCount} ElapsedMs={ElapsedMs}",
            lookupKeys.Count, batches.Count, results.Count, stopwatch.ElapsedMilliseconds);
        return results;
    }

    /// <summary>
    /// Builds the parameterized batch query for one bounded set of component parts. The PoLines CTE
    /// selects every qualifying conventional open PO line in scope (accepted Stage 9 rule, no master
    /// status predicate) and resolves per-line facts: part-master description with site-first /
    /// master-second purchasing lead time and buyer-code fallback; the KssComponents CTE holds the
    /// distinct scoped parts carrying an effective supplier-schedule relationship as of
    /// <paramref name="today"/>. The outer query joins the workspace-domain vendor display
    /// (<c>vd_addr</c> + domain, display value <c>vd_sort</c>, raw supplier code when no row matches)
    /// and the domain-scoped buyer resolution (<c>code_fldname</c> discriminator plus
    /// <c>code_value</c>, display <c>code_user1</c>; never cross-domain). Public and pure (no
    /// connection) so SQL/parameter shape is independently testable.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildBatchQuery(
        string domain,
        string site,
        IReadOnlyList<string> componentParts,
        DateOnly today)
    {
        if (componentParts is null)
            throw new ArgumentNullException(nameof(componentParts));
        if (componentParts.Count == 0)
            throw new ArgumentException("At least one component part is required.", nameof(componentParts));

        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("Today", today.ToDateTime(TimeOnly.MinValue), System.Data.DbType.Date);

        var valueRows = new List<string>(componentParts.Count);
        for (var i = 0; i < componentParts.Count; i++)
        {
            var paramName = $"Part{i}";
            parameters.Add(paramName, componentParts[i]);
            valueRows.Add($"(@{paramName})");
        }

        var sql = $"""
            WITH ScopeParts (PartNumber) AS
            (
                SELECT PartNumber FROM (VALUES {string.Join(", ", valueRows)}) AS Parts (PartNumber)
            ),
            PoLines AS
            (
                SELECT
                    pod.pod_part AS ComponentPart,
                    pm.pt_desc1 AS Description,
                    ISNULL(ptp.ptp_pur_lead, pm.pt_pur_lead) AS LeadTimeDays,
                    po.po_nbr AS PoNumber,
                    pod.pod_line AS PoLine,
                    pod.pod_due_date AS DueDate,
                    pod.pod_qty_ord - pod.pod_qty_rcvd AS OpenQuantity,
                    pod.pod__log01 AS IsConfirmed,
                    po.po_vend AS VendorCode,
                    CASE WHEN LTRIM(RTRIM(ISNULL(ptp.ptp_buyer, ''))) <> '' THEN ptp.ptp_buyer ELSE pm.pt_buyer END AS BuyerCode,
                    CASE WHEN LTRIM(RTRIM(ISNULL(ptp.ptp_buyer, ''))) <> '' THEN 'ptp_buyer' ELSE 'pt_buyer' END AS BuyerField,
                    pod.pod_vpart AS ManufacturerItem,
                    pod.pod__chr06 AS TrackingInfo
                FROM qadpro2.dbo.pod_det AS pod
                INNER JOIN qadpro2.dbo.po_mstr AS po
                    ON po.po_domain = pod.pod_domain
                    AND po.po_nbr = pod.pod_nbr
                LEFT JOIN qadpro2.dbo.ptp_det AS ptp
                    ON ptp.ptp_domain = pod.pod_domain
                    AND ptp.ptp_site = pod.pod_site
                    AND ptp.ptp_part = pod.pod_part
                LEFT JOIN qadpro2.dbo.pt_mstr AS pm
                    ON pm.pt_domain = pod.pod_domain
                    AND pm.pt_part = pod.pod_part
                WHERE pod.pod_domain = @Domain
                  AND pod.pod_site = @Site
                  AND pod.pod_part IN (SELECT PartNumber FROM ScopeParts)
                  AND LOWER(ISNULL(pod.pod_status, '')) NOT IN ('c', 'x')
                  AND pod.pod_qty_ord - pod.pod_qty_rcvd > 0
            ),
            KssComponents AS
            (
                SELECT DISTINCT pod2.pod_part AS ComponentPart
                FROM qadpro2.dbo.pod_det AS pod2
                INNER JOIN qadpro2.dbo.po_mstr AS po2
                    ON po2.po_domain = pod2.pod_domain
                    AND po2.po_nbr = pod2.pod_nbr
                WHERE pod2.pod_domain = @Domain
                  AND pod2.pod_site = @Site
                  AND pod2.pod_part IN (SELECT PartNumber FROM ScopeParts)
                  AND (pod2.pod_end_eff##1 IS NULL OR pod2.pod_end_eff##1 >= @Today)
                  AND po2.po_sched = 1
                  AND (po2.po_eff_to IS NULL OR po2.po_eff_to >= @Today)
            )
            SELECT
                pl.ComponentPart,
                pl.Description,
                pl.LeadTimeDays,
                pl.PoNumber,
                pl.PoLine,
                pl.DueDate,
                pl.OpenQuantity,
                pl.IsConfirmed,
                ISNULL(vd.vd_sort, pl.VendorCode) AS SupplierDisplay,
                ISNULL(vd.vd_sort, pl.VendorCode) AS SupplierIdentifier,
                cm.code_user1 AS BuyerDisplay,
                pl.ManufacturerItem,
                CAST(CASE WHEN kc.ComponentPart IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS IsKss,
                pl.TrackingInfo
            FROM PoLines AS pl
            LEFT JOIN qadpro2.dbo.vd_mstr AS vd
                ON vd.vd_addr = pl.VendorCode
                AND vd.vd_domain = @Domain
            LEFT JOIN qadpro2.dbo.code_mstr AS cm
                ON cm.code_domain = @Domain
                AND cm.code_fldname = pl.BuyerField
                AND cm.code_value = pl.BuyerCode
            LEFT JOIN KssComponents AS kc
                ON kc.ComponentPart = pl.ComponentPart
            ORDER BY pl.ComponentPart, pl.DueDate, pl.PoNumber, pl.PoLine;
            """;

        return (sql, parameters);
    }

    /// <summary>Maps the QAD-shaped raw row to the domain line. Date-only normalization and trim/empty-to-null text handling follow the established reader conventions.</summary>
    public static ComponentOrderLine Normalize(QadComponentOrderLineRawRow raw) => new(
        raw.ComponentPart,
        NormalizeText(raw.Description),
        raw.LeadTimeDays,
        raw.PoNumber,
        raw.PoLine,
        raw.DueDate.HasValue ? DateOnly.FromDateTime(raw.DueDate.Value) : null,
        raw.OpenQuantity,
        raw.IsConfirmed,
        NormalizeText(raw.SupplierDisplay),
        NormalizeText(raw.BuyerDisplay),
        NormalizeText(raw.ManufacturerItem),
        raw.IsKss,
        NormalizeText(raw.TrackingInfo),
        NormalizeText(raw.SupplierIdentifier));

    private static string? NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>
/// QAD-shaped raw Dapper result row: one qualifying conventional open PO line. Does not travel past
/// this integration boundary. Deliberately a settable-property class (not a positional record): with
/// 13 columns spanning direct passthrough, computed (<c>CASE</c>/<c>ISNULL</c>), and joined values,
/// Dapper's strict constructor-matching materialization proved fragile against the live QAD column
/// shape (observed production failure: "A parameterless default constructor or one matching
/// signature ... is required" once real data reached this reader), even though the SQL/record shape
/// passed unit and integration tests against synthetic data. The property-based mapper binds each
/// column by name independent of order and applies Dapper's full value-coercion path, which is the
/// robust choice for a wide multi-join projection like this one.
/// </summary>
public sealed class QadComponentOrderLineRawRow
{
    public string ComponentPart { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? LeadTimeDays { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public int PoLine { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal OpenQuantity { get; set; }
    public bool? IsConfirmed { get; set; }
    public string? SupplierDisplay { get; set; }
    public string? BuyerDisplay { get; set; }
    public string? ManufacturerItem { get; set; }
    public bool IsKss { get; set; }
    public string? TrackingInfo { get; set; }
    public string? SupplierIdentifier { get; set; }
}
