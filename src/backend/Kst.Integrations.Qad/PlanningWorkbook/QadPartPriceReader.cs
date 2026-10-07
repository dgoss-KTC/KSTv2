using System.Diagnostics;
using Dapper;
using Microsoft.Extensions.Logging;
using Kst.Domain.Mps;
using Kst.Domain.PartDetail;
using Kst.Integrations.Qad.Inventory;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.PlanningWorkbook;

/// <summary>
/// Batched, parameterized, read-only QAD price-tier adapter for the Stage 14 Planning Workbook. It
/// reproduces the accepted Stage 6 Part Info single-part rule
/// (<see cref="Kst.Integrations.Qad.PartDetail.QadPartDetailReader.BuildPriceQuery"/>) for many
/// parts: domain + part scope, the latest <c>pi_mstr</c> list with <c>pi_start &lt;= </c> the
/// supplied effective date, only that list's <c>pid_det</c> tiers, tiers ordered by MOQ ascending.
/// No currency/UoM filter or conversion is applied and no tie-breaker is added for equal
/// <c>pi_start</c> or equal MOQ: like the accepted rule, such ties are not deterministic in SQL
/// (unverified evidence E2 / question Q-N4). Every distinct requested part receives exactly one
/// result; an empty tier list means "no current price list or no tiers" (a successful absence: Planning uses a $0 price, owner-approved amendment 2026-10-07), never a
/// failure. A zero <c>pid_amt</c> is a valid price. Query failures, missing parts and malformed rows throw (never become $0).
/// </summary>
public sealed class QadPartPriceReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadPartPriceReader> _logger;

    public QadPartPriceReader(QadConnectionOptions options, ILogger<QadPartPriceReader> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<QadPartPriceResult>> ReadAsync(
        string site,
        IReadOnlyList<string> partNumbers,
        DateOnly effectiveDate,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("QAD connection is not configured.");

        var domain = QadSiteDomainMap.Resolve(site);
        var lookupKeys = QadPartInventoryReader.NormalizePartNumbers(partNumbers);
        if (lookupKeys.Count == 0)
            return [];

        var batches = MpsPartBatcher.Batch(lookupKeys, _options.MaxPartBatchSize);
        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);

        var results = new List<QadPartPriceResult>(lookupKeys.Count);
        for (var i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            var stopwatch = Stopwatch.StartNew();
            var (sql, parameters) = BuildBatchQuery(domain, batch, effectiveDate);
            var command = new CommandDefinition(
                sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken);

            var rawRows = (await connection.QueryAsync<QadPartPriceBatchRawRow>(command)).ToList();
            var normalized = Normalize(batch, rawRows);
            stopwatch.Stop();

            _logger.LogInformation(
                "Planning price batch {BatchIndex}/{BatchCount} ({PartCount} parts) returned {RowCount} rows in {ElapsedMs}ms.",
                i + 1, batches.Count, batch.Count, rawRows.Count, stopwatch.ElapsedMilliseconds);

            results.AddRange(normalized);
        }

        return results;
    }

    /// <summary>
    /// Builds the parameterized batch price query. Per requested part, <c>ROW_NUMBER() OVER
    /// (PARTITION BY pi_part_code ORDER BY pi_start DESC)</c> selects the same "latest eligible list"
    /// as the accepted single-part <c>TOP (1) … ORDER BY pi_start DESC</c> (with the same
    /// non-deterministic behavior for equal <c>pi_start</c>). The outer query keeps every requested
    /// part (LEFT JOINs) so a part without a current list or tiers is returned explicitly. Public and
    /// pure so SQL/parameter shape is independently testable.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildBatchQuery(
        string domain,
        IReadOnlyList<string> partNumbers,
        DateOnly effectiveDate)
    {
        ArgumentNullException.ThrowIfNull(partNumbers);
        if (partNumbers.Count == 0)
            throw new ArgumentException("At least one part number is required.", nameof(partNumbers));

        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Today", effectiveDate.ToDateTime(TimeOnly.MinValue));

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
            RankedPriceLists AS
            (
                SELECT
                    scope.PartNumber AS PartNumber,
                    pi.pi_list_id AS ListId,
                    ROW_NUMBER() OVER (PARTITION BY scope.PartNumber ORDER BY pi.pi_start DESC) AS ListRank
                FROM ScopeParts AS scope
                INNER JOIN qadpro2.dbo.pi_mstr AS pi
                    ON pi.pi_part_code = scope.PartNumber
                WHERE pi.pi_domain = @Domain
                  AND pi.pi_start <= @Today
            )
            SELECT
                scope.PartNumber AS PartNumber,
                CASE WHEN pid.pid_list_id IS NULL THEN 0 ELSE 1 END AS HasTier,
                pid.pid_qty AS MinimumOrderQuantity,
                pid.pid_amt AS UnitPrice
            FROM ScopeParts AS scope
            LEFT JOIN RankedPriceLists AS cpl
                ON cpl.PartNumber = scope.PartNumber
                AND cpl.ListRank = 1
            LEFT JOIN qadpro2.dbo.pid_det AS pid
                ON pid.pid_domain = @Domain
                AND pid.pid_list_id = cpl.ListId
            ORDER BY scope.PartNumber, pid.pid_qty ASC;
            """;

        return (sql, parameters);
    }

    /// <summary>
    /// Maps raw rows to exactly one result per requested part, in request order. The query returns at
    /// least one row per requested part (an explicit <c>HasTier = 0</c> row means unpriced), so a
    /// requested part with no row at all is an incomplete read and throws rather than being reported
    /// as unpriced. Tiers are ordered by MOQ ascending (stable, as in the accepted Part Info mapping)
    /// and are never rounded. A tier row with a missing MOQ or price, or a row for a part that was not
    /// requested, is malformed source data and throws rather than becoming zero or disappearing.
    /// </summary>
    public static IReadOnlyList<QadPartPriceResult> Normalize(
        IReadOnlyList<string> requestedParts,
        IReadOnlyList<QadPartPriceBatchRawRow> rows)
    {
        var tiers = requestedParts.ToDictionary(
            p => p, _ => new List<PartPriceBreak>(), StringComparer.OrdinalIgnoreCase);
        var returned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (row.PartNumber is null || !tiers.TryGetValue(row.PartNumber, out var list))
                throw new InvalidDataException("Price source returned a row for a part that was not requested.");
            returned.Add(row.PartNumber);
            if (row.HasTier == 0)
                continue;
            if (row.MinimumOrderQuantity is not { } moq || row.UnitPrice is not { } price)
                throw new InvalidDataException("Price source returned a tier without a minimum order quantity or price.");
            list.Add(new PartPriceBreak(moq, price));
        }

        if (requestedParts.Any(p => !returned.Contains(p)))
            throw new InvalidDataException("Price source returned no row for a requested part.");


        return requestedParts
            .Select(p => new QadPartPriceResult(p, tiers[p].OrderBy(t => t.MinimumOrderQuantity).ToList()))
            .ToList();
    }
}

/// <summary>Normalized price result for one requested part; an empty list means unpriced.</summary>
public sealed record QadPartPriceResult(string PartNumber, IReadOnlyList<PartPriceBreak> Tiers);

/// <summary>QAD-shaped raw Dapper row. Does not travel past this integration boundary.</summary>
public sealed record QadPartPriceBatchRawRow(
    string? PartNumber,
    int HasTier,
    decimal? MinimumOrderQuantity,
    decimal? UnitPrice);
