using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Kst.Domain.Inventory;
using Kst.Domain.Mps;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.Inventory;

/// <summary>
/// Direct, parameterized QAD adapter for the shared Site + Part inventory summary — the accepted
/// Stage 6 classification (positive-only, RMA <c>RA%</c>-lot precedence, nettable/non-nettable split)
/// extracted from <see cref="Kst.Integrations.Qad.PartDetail.QadPartDetailReader"/> so Stage 6 and
/// future Stage 8 consumers share one authoritative implementation. Owns SQL text/parameters,
/// lookup-key normalization/deduplication, part-list batching, the QAD-shaped raw result, and
/// raw-to-normalized mapping. Does not know about PartDetail, BOM, caching, or workspace state.
/// </summary>
public sealed class QadPartInventoryReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadPartInventoryReader> _logger;

    public QadPartInventoryReader(QadConnectionOptions options, ILogger<QadPartInventoryReader> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Reads inventory summaries for the requested parts at one site. Domain is derived from
    /// <paramref name="site"/> via <see cref="QadSiteDomainMap"/> here, at the QAD integration
    /// boundary, so callers never need QAD-specific domain knowledge. Requested part numbers are
    /// normalized and deduplicated in C# (<see cref="NormalizePartNumbers"/>) before SQL batching, so
    /// a repeated requested part produces exactly one summary. Every distinct requested part receives
    /// exactly one summary — with authoritative zeroes when it has no qualifying inventory rows — so
    /// callers never infer zero from a missing result row. Empty input returns an empty result
    /// without opening a connection. A QAD/query failure propagates as an exception; it is never
    /// converted to zeroes.
    /// </summary>
    public async Task<IReadOnlyList<PartInventorySummary>> ReadSummariesAsync(
        string site,
        IReadOnlyList<string> partNumbers,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("QAD connection is not configured.");

        var domain = QadSiteDomainMap.Resolve(site);
        var lookupKeys = NormalizePartNumbers(partNumbers);
        if (lookupKeys.Count == 0)
            return [];

        var batches = MpsPartBatcher.Batch(lookupKeys, _options.MaxPartBatchSize);
        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);

        var results = new List<PartInventorySummary>(lookupKeys.Count);
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

            var rawRows = await connection.QueryAsync<QadPartInventoryRawRow>(command);
            var normalized = rawRows.Select(Normalize).ToList();
            stopwatch.Stop();

            _logger.LogInformation(
                "Part inventory batch {BatchIndex}/{BatchCount} ({PartCount} parts) returned {RowCount} rows in {ElapsedMs}ms.",
                i + 1, batches.Count, batch.Count, normalized.Count, stopwatch.ElapsedMilliseconds);

            results.AddRange(normalized);
        }

        return results;
    }

    /// <summary>
    /// Normalizes and deduplicates requested part-number lookup keys in C# before SQL batching:
    /// trims surrounding whitespace and collapses case-insensitive duplicates (QAD part comparisons
    /// are case-insensitive; same convention as Stage 6 PartDetail scope matching), keeping the first
    /// occurrence. Blank keys are rejected — silently dropping a key would let a caller infer zero
    /// from a missing row. Pure and testable; no SQL concepts.
    /// </summary>
    public static IReadOnlyList<string> NormalizePartNumbers(IReadOnlyList<string> partNumbers)
    {
        var keys = new List<string>(partNumbers.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var partNumber in partNumbers)
        {
            if (partNumber is null)
                throw new ArgumentNullException(nameof(partNumbers), "Part number entries must not be null.");

            var key = partNumber.Trim();
            if (key.Length == 0)
                throw new ArgumentException("Part number entries must not be blank.", nameof(partNumbers));

            if (seen.Add(key))
                keys.Add(key);
        }

        return keys;
    }

    /// <summary>
    /// Builds the parameterized batch inventory aggregation query for a bounded set of parts. The
    /// inner aggregate CTE is the accepted Stage 6 inventory query body verbatim (positive-only rows,
    /// <c>ld_det</c> INNER JOIN <c>loc_mstr</c> on domain + site + location, INNER JOIN <c>is_mstr</c>
    /// on domain + location status, RMA <c>RA%</c>-lot classification in the SELECT CASE expressions
    /// with precedence over net/non-net), grouped per part. The outer SELECT joins the requested
    /// scope back to the aggregates so every requested part yields exactly one row, with
    /// <c>ISNULL(..., 0)</c> giving authoritative zeroes for parts with no qualifying rows. Public
    /// and pure (no connection) so SQL/parameter shape is independently testable.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildBatchQuery(
        string domain,
        string site,
        IReadOnlyList<string> partNumbers)
    {
        if (partNumbers is null)
            throw new ArgumentNullException(nameof(partNumbers));
        if (partNumbers.Count == 0)
            throw new ArgumentException("At least one part number is required.", nameof(partNumbers));

        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);

        var valueRows = new List<string>(partNumbers.Count);
        for (var i = 0; i < partNumbers.Count; i++)
        {
            var paramName = $"Part{i}";
            parameters.Add(paramName, partNumbers[i]);
            valueRows.Add($"(@{paramName})");
        }

        var sql = $"""
            WITH ScopeParts (PartNumber) AS
            (
                SELECT PartNumber FROM (VALUES {string.Join(", ", valueRows)}) AS Parts (PartNumber)
            ),
            InventoryAggregates AS
            (
                SELECT
                    ld.ld_part AS PartNumber,
                    SUM(CASE WHEN ld.ld_lot NOT LIKE 'RA%' AND ism.is_nettable = 1 THEN ld.ld_qty_oh ELSE 0 END) AS NetQuantityOnHand,
                    SUM(CASE WHEN ld.ld_lot NOT LIKE 'RA%' AND ism.is_nettable = 0 THEN ld.ld_qty_oh ELSE 0 END) AS NonNetQuantityOnHand,
                    SUM(CASE WHEN ld.ld_lot LIKE 'RA%' THEN ld.ld_qty_oh ELSE 0 END) AS RmaQuantityOnHand
                FROM qadpro2.dbo.ld_det AS ld
                INNER JOIN qadpro2.dbo.loc_mstr AS loc
                    ON loc.loc_domain = ld.ld_domain
                    AND loc.loc_site = ld.ld_site
                    AND loc.loc_loc = ld.ld_loc
                INNER JOIN qadpro2.dbo.is_mstr AS ism
                    ON ism.is_domain = loc.loc_domain
                    AND ism.is_status = loc.loc_status
                WHERE ld.ld_domain = @Domain
                  AND ld.ld_site = @Site
                  AND ld.ld_part IN (SELECT PartNumber FROM ScopeParts)
                  AND ld.ld_qty_oh > 0
                GROUP BY ld.ld_part
            )
            SELECT
                @Site AS Site,
                scope.PartNumber AS PartNumber,
                ISNULL(inv.NetQuantityOnHand, 0) AS NetQuantityOnHand,
                ISNULL(inv.NonNetQuantityOnHand, 0) AS NonNetQuantityOnHand,
                ISNULL(inv.RmaQuantityOnHand, 0) AS RmaQuantityOnHand
            FROM ScopeParts AS scope
            LEFT JOIN InventoryAggregates AS inv
                ON inv.PartNumber = scope.PartNumber;
            """;

        return (sql, parameters);
    }

    /// <summary>Maps the QAD-shaped raw row to the shared Site + Part summary. Passthrough by design: all classification happened in SQL.</summary>
    public static PartInventorySummary Normalize(QadPartInventoryRawRow raw) => new(
        Site: raw.Site,
        PartNumber: raw.PartNumber,
        NetQuantityOnHand: raw.NetQuantityOnHand,
        NonNetQuantityOnHand: raw.NonNetQuantityOnHand,
        RmaQuantityOnHand: raw.RmaQuantityOnHand);
}

/// <summary>QAD-shaped raw Dapper result row: one inventory summary per requested part. Does not travel past this integration boundary.</summary>
public sealed record QadPartInventoryRawRow(
    string Site,
    string PartNumber,
    decimal NetQuantityOnHand,
    decimal NonNetQuantityOnHand,
    decimal RmaQuantityOnHand);
