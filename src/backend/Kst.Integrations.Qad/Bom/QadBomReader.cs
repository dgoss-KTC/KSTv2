using System.Diagnostics;
using System.Globalization;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Kst.Domain.Bom;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.Bom;

/// <summary>
/// Direct, parameterized QAD adapter for the Stage 8 current-effective multi-level BOM structure:
/// reproduces the proven <c>dbo.sp_QAD_ktbmpsrp</c> traversal semantics as KST-owned read-only SQL —
/// complete recursive traversal of effective <c>ps_mstr</c> relationships (no P/M, phantom, or
/// operation filter; hidden rows do not stop recursion), an identity-preserving closure reduction,
/// a SQL-assigned sibling rank (database collation: Component → Reference → OID), and part-master /
/// selected-site enrichment. Owns SQL text/parameters, the QAD-shaped raw result, and the
/// deterministic depth-first expansion (structural Level, path-based <c>OccurrenceKey</c>, cycle
/// guard, effective P/M fallback, null-safe description combination). Does not know about
/// PartDetail, inventory, caching, or workspace state; does not filter to scheduler-visible P/M
/// (Application-owned, Stage 8D.3) and does not enrich with inventory.
/// </summary>
public sealed class QadBomReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadBomReader> _logger;

    public QadBomReader(QadConnectionOptions options, ILogger<QadBomReader> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Reads the complete current-effective multi-level BOM structure for one parent at one site.
    /// Domain is derived from <paramref name="site"/> via <see cref="QadSiteDomainMap"/>
    /// here, at the QAD integration boundary, so callers never need QAD-specific domain knowledge.
    /// <paramref name="effectiveDate"/> is an explicit input (the application clock is wired in
    /// Stage 8D.3); the query never reads system time. Returns the occurrences in proven depth-first
    /// traversal order with actual structural levels — every legitimate occurrence is preserved
    /// (repeated components, diamonds, hidden/non-P/M intermediates), nothing is consolidated or
    /// DISTINCTed. A parent with no effective relationships returns a successful empty collection;
    /// a database/query failure or cancellation propagates as an exception — never a faked empty BOM.
    /// </summary>
    public async Task<IReadOnlyList<BomOccurrence>> ReadAsync(
        string site,
        string parentPart,
        DateOnly effectiveDate,
        CancellationToken cancellationToken = default)
    {
        if (parentPart is null)
            throw new ArgumentNullException(nameof(parentPart));

        var normalizedParent = parentPart.Trim();
        if (normalizedParent.Length == 0)
            throw new ArgumentException("Parent part must not be blank.", nameof(parentPart));

        if (!_options.IsConfigured)
            throw new InvalidOperationException("QAD connection is not configured.");

        var domain = QadSiteDomainMap.Resolve(site);
        var stopwatch = Stopwatch.StartNew();

        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);

        var (sql, parameters) = BuildQuery(domain, site, normalizedParent, effectiveDate);
        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _options.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        var rawRows = (await connection.QueryAsync<QadBomStructuralRawRow>(command)).ToList();
        var occurrences = TraverseDepthFirst(normalizedParent, rawRows);
        stopwatch.Stop();

        _logger.LogInformation(
            "BOM structural read for parent {ParentPart} in domain {Domain} (effective {EffectiveDate}) returned {OccurrenceCount} occurrences in {ElapsedMs}ms.",
            normalizedParent, domain, effectiveDate, occurrences.Count, stopwatch.ElapsedMilliseconds);

        return occurrences;
    }

    /// <summary>
    /// Builds the parameterized BOM structure query for one parent. The recursive CTE
    /// <c>BomStructure</c> reproduces the proven traversal: the anchor is the parent's effective
    /// level-1 relationships and the recursive member is the effective children of each frontier
    /// component — the only conditions are domain and the effective-date predicate (no P/M,
    /// phantom, or <c>ps_op</c> operation filter, so hidden rows never stop recursion). The outer
    /// query reduces the closure to unique physical relationships (the approved closure DISTINCT —
    /// see its comment), LEFT JOINs <c>pt_mstr</c> (domain + part) and selected-site
    /// <c>ptp_det</c> (<c>ptp_domain</c> + <c>ptp_part</c> + <c>ptp_site</c>, never
    /// <c>pt_mstr.pt_site</c>), and assigns the sibling rank with an outer
    /// <c>ROW_NUMBER()</c> so database collation owns the Component → Reference → OID ordering.
    /// <c>OPTION (MAXRECURSION 100)</c> is a protective ceiling: exceeding it fails the statement
    /// (a cyclical/pathological BOM) instead of silently truncating. Public and pure (no
    /// connection) so SQL/parameter shape is independently testable.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildQuery(
        string domain,
        string site,
        string parentPart,
        DateOnly effectiveDate)
    {
        if (parentPart is null)
            throw new ArgumentNullException(nameof(parentPart));

        var normalizedParent = parentPart.Trim();
        if (normalizedParent.Length == 0)
            throw new ArgumentException("Parent part must not be blank.", nameof(parentPart));

        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("ParentPart", normalizedParent);
        parameters.Add("EffectiveDate", effectiveDate.ToDateTime(TimeOnly.MinValue));

        const string sql = """
            WITH BomStructure AS
            (
                SELECT
                    ps.oid_ps_mstr AS OidPsMstr,
                    ps.ps_par      AS ParentPart,
                    ps.ps_comp     AS ComponentPart,
                    ps.ps_ref      AS Reference,
                    ps.ps_qty_per  AS QuantityPer,
                    ps.ps_scrp_pct AS ScrapPercentage
                FROM qadpro2.dbo.ps_mstr AS ps
                WHERE ps.ps_domain = @Domain
                  AND ps.ps_par = @ParentPart
                  AND (ps.ps_start IS NULL OR ps.ps_start <= @EffectiveDate)
                  AND (ps.ps_end IS NULL OR ps.ps_end >= @EffectiveDate)

                UNION ALL

                SELECT
                    ch.oid_ps_mstr AS OidPsMstr,
                    ch.ps_par      AS ParentPart,
                    ch.ps_comp     AS ComponentPart,
                    ch.ps_ref      AS Reference,
                    ch.ps_qty_per  AS QuantityPer,
                    ch.ps_scrp_pct AS ScrapPercentage
                FROM qadpro2.dbo.ps_mstr AS ch
                INNER JOIN BomStructure AS frontier
                    ON frontier.ComponentPart = ch.ps_par
                WHERE ch.ps_domain = @Domain
                  AND (ch.ps_start IS NULL OR ch.ps_start <= @EffectiveDate)
                  AND (ch.ps_end IS NULL OR ch.ps_end >= @EffectiveDate)
            )
            SELECT
                u.OidPsMstr       AS OidPsMstr,
                u.ParentPart      AS ParentPart,
                u.ComponentPart   AS ComponentPart,
                u.Reference       AS Reference,
                u.QuantityPer     AS QuantityPer,
                u.ScrapPercentage AS ScrapPercentage,
                pt.pt_desc1       AS Description1,
                pt.pt_desc2       AS Description2,
                pt.pt_phantom     AS Phantom,
                ptp.ptp_pm_code   AS SitePmCode,
                pt.pt_pm_code     AS GlobalPmCode,
                ROW_NUMBER() OVER (
                    PARTITION BY u.ParentPart
                    ORDER BY u.ComponentPart, u.Reference, u.OidPsMstr
                ) AS SiblingOrder
            FROM (
                -- APPROVED 8D.2 CLOSURE DISTINCT (owner amendment): collapses only duplicate
                -- PATH COPIES of the same physical ps_mstr relationship — in the frontier-join
                -- recursion a shared relationship row is emitted once per path that reaches its
                -- parent. The selected identity includes oid_ps_mstr, so two distinct
                -- relationships can never be merged. This is NOT business-level BOM
                -- deduplication: the C# structural expansion (TraverseDepthFirst) recreates
                -- every legitimate expanded occurrence, and the returned BomOccurrence result
                -- is never DISTINCTed.
                SELECT DISTINCT
                    b.OidPsMstr,
                    b.ParentPart,
                    b.ComponentPart,
                    b.Reference,
                    b.QuantityPer,
                    b.ScrapPercentage
                FROM BomStructure AS b
            ) AS u
            LEFT JOIN qadpro2.dbo.pt_mstr AS pt
                ON pt.pt_domain = @Domain
                AND pt.pt_part = u.ComponentPart
            LEFT JOIN qadpro2.dbo.ptp_det AS ptp
                ON ptp.ptp_domain = @Domain
                AND ptp.ptp_part = u.ComponentPart
                AND ptp.ptp_site = @Site
            OPTION (MAXRECURSION 100);
            """;

        return (sql, parameters);
    }

    /// <summary>
    /// Performs the deterministic depth-first pre-order expansion of the SQL relationship closure
    /// (Amendment 2: SQL owns sibling collation via <c>SiblingOrder</c>; C# owns the traversal).
    /// When a part is visited, its children are all closure rows for that part — re-listed under
    /// every parent occurrence that reaches it (repeated components, diamonds, and shared physical
    /// relationships are all preserved) — recursed beneath each child in <c>SiblingOrder</c>
    /// rank. <see cref="BomOccurrence.Level"/> is the 1-based DFS depth (actual structural level,
    /// preserved through hidden intermediates, never renumbered).
    /// <see cref="BomOccurrence.OccurrenceKey"/> is generated here as the deterministic path of
    /// relationship OIDs from the root (e.g. <c>"oidA/oidB/oidC"</c>): it identifies the expanded
    /// occurrence, so the same physical relationship reached through different structural paths
    /// yields different keys, and it is never used to determine ordering. A child part that is
    /// already on the current ancestor path is a cycle and fails with a descriptive
    /// <see cref="InvalidOperationException"/> (defensive: the SQL <c>MAXRECURSION</c> ceiling
    /// fails cyclical BOMs first — a diamond, where a part sits under two different parents, is
    /// not a cycle and is fully preserved). Pure and testable; no SQL concepts.
    /// </summary>
    public static IReadOnlyList<BomOccurrence> TraverseDepthFirst(
        string rootParent,
        IReadOnlyList<QadBomStructuralRawRow> rows)
    {
        if (rootParent is null)
            throw new ArgumentNullException(nameof(rootParent));
        if (rows is null)
            throw new ArgumentNullException(nameof(rows));

        var root = rootParent.Trim();
        if (root.Length == 0)
            throw new ArgumentException("Root parent part must not be blank.", nameof(rootParent));

        var childrenByParent = new Dictionary<string, List<QadBomStructuralRawRow>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (!childrenByParent.TryGetValue(row.ParentPart, out var list))
            {
                list = [];
                childrenByParent.Add(row.ParentPart, list);
            }

            list.Add(row);
        }

        // Sibling order is the SQL-assigned SiblingOrder rank (database collation: Component →
        // Reference → OID). C# consumes it as-is and never re-derives a string comparison.
        foreach (var list in childrenByParent.Values)
        {
            list.Sort(static (a, b) => a.SiblingOrder.CompareTo(b.SiblingOrder));
        }

        var result = new List<BomOccurrence>(rows.Count);
        var ancestorParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { root };

        Walk(root, 1, string.Empty);
        return result;

        void Walk(string parentPart, int level, string parentKey)
        {
            if (!childrenByParent.TryGetValue(parentPart, out var children) || children.Count == 0)
                return;

            foreach (var child in children)
            {
                if (!ancestorParts.Add(child.ComponentPart))
                {
                    throw new InvalidOperationException(
                        $"BOM cycle detected: part '{child.ComponentPart}' is already in its own ancestor path. " +
                        "The BOM was not returned rather than a silently incomplete one.");
                }

                var key = parentKey.Length == 0
                    ? FormatOid(child.OidPsMstr)
                    : $"{parentKey}/{FormatOid(child.OidPsMstr)}";

                try
                {
                    result.Add(Normalize(child, level, key));
                    Walk(child.ComponentPart, level + 1, key);
                }
                finally
                {
                    ancestorParts.Remove(child.ComponentPart);
                }
            }
        }
    }

    /// <summary>
    /// Resolves the effective P/M classification for a component (P/M classification only — this
    /// is the accepted exception to "no general pt_mstr fallback for other planning fields"):
    /// the selected-site <c>ptp_det.ptp_pm_code</c> wins when it is available; a NULL or
    /// whitespace-only site value is unavailable (live QAD data stores unset codes as empty
    /// strings, so blank handling is load-bearing); otherwise the part-master
    /// <c>pt_mstr.pt_pm_code</c> is used. Any code (P, M, or known non-P/M codes) passes through
    /// unclassified — selecting scheduler-visible P/M rows is Application-owned. Pure and testable.
    /// </summary>
    public static string? ResolveEffectivePmCode(string? sitePmCode, string? globalPmCode)
    {
        var site = sitePmCode?.Trim();
        if (!string.IsNullOrEmpty(site))
            return site;

        var global = globalPmCode?.Trim();
        return string.IsNullOrEmpty(global) ? null : global;
    }

    /// <summary>
    /// Combines the part-master description segments null-safely: each segment is trimmed,
    /// NULL/whitespace-only segments are dropped, and the remaining segments are joined with a
    /// single space. One NULL segment never erases the other; none remaining is <c>null</c>
    /// (missing description is null, matching the Stage 6 PartDetail description convention).
    /// Pure and testable.
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

    /// <summary>
    /// Maps one closure row to the structural occurrence at the given traversal position.
    /// <paramref name="occurrenceKey"/> is the DFS-generated expanded-occurrence identity (see
    /// <see cref="TraverseDepthFirst"/>); all other facts are per-row normalization — P/M
    /// fallback, null-safe description, phantom passthrough, and verbatim relationship-level
    /// Qty Per / Scrap (never multiplied or re-derived).
    /// </summary>
    public static BomOccurrence Normalize(QadBomStructuralRawRow raw, int level, string occurrenceKey) => new(
        OccurrenceKey: occurrenceKey,
        Level: level,
        ComponentPart: raw.ComponentPart,
        PmCode: ResolveEffectivePmCode(raw.SitePmCode, raw.GlobalPmCode),
        IsPhantom: raw.Phantom ?? false,
        Description: CombineDescription(raw.Description1, raw.Description2),
        QuantityPer: raw.QuantityPer,
        ScrapPercentage: raw.ScrapPercentage);

    /// <summary>
    /// Serializes a relationship OID (live-confirmed <c>decimal(28,10)</c>, fractional OIDs such
    /// as <c>201306300024529805.0009000000</c>) as the invariant-culture exact string used in
    /// path-based occurrence keys. Invariant culture and exact decimal formatting keep keys
    /// stable/deterministic; "/" cannot appear in a decimal string, so joined paths are
    /// unambiguous. Internal to this integration layer — the key is opaque past it.
    /// </summary>
    private static string FormatOid(decimal oid) => oid.ToString(CultureInfo.InvariantCulture);
}

/// <summary>
/// QAD-shaped raw Dapper result row: one unique physical <c>ps_mstr</c> relationship in the
/// effective closure of the requested parent, enriched with part-master / selected-site facts and
/// the SQL-assigned sibling rank. Constructor parameter order mirrors the <see cref="BuildQuery"/>
/// SELECT column order exactly — Dapper materializes this record positionally (a live-confirmed
/// 2026-07 driver behavior; see also the Stage 7 positional-deserialization lesson).
/// Types reflect the live-confirmed QADPRO2 schema (2026-07): <c>oid_ps_mstr</c> decimal(28,10);
/// <c>ps_qty_per</c>/<c>ps_scrp_pct</c> decimal(28,10) nullable; <c>ps_par</c>/<c>ps_comp</c>/<c>ps_ref</c>
/// nvarchar(60); <c>pt_desc1</c>/<c>pt_desc2</c> nvarchar(160); <c>pt_phantom</c> bit nullable;
/// <c>pt_pm_code</c>/<c>ptp_pm_code</c> nvarchar(60). <c>SiblingOrder</c> is <see langword="long"/> because
/// the driver reports <c>ROW_NUMBER()</c> as Int64 (live-confirmed). Does not travel past this
/// integration boundary.
/// </summary>
public sealed record QadBomStructuralRawRow(
    decimal OidPsMstr,
    string ParentPart,
    string ComponentPart,
    string? Reference,
    decimal? QuantityPer,
    decimal? ScrapPercentage,
    string? Description1,
    string? Description2,
    bool? Phantom,
    string? SitePmCode,
    string? GlobalPmCode,
    long SiblingOrder);
