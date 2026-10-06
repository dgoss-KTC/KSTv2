using System.Diagnostics;
using Dapper;
using Microsoft.Extensions.Logging;
using Kst.Domain.Mps;
using Kst.Domain.PlanningWorkbook;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.PlanningWorkbook;

/// <summary>
/// Batched, parameterized, read-only QAD adapter for display-only Planning Workbook forecast facts:
/// <c>mrp_det</c> rows with <c>mrp_detail = 'FORECAST'</c> and a nonzero quantity, scoped by the
/// workspace domain, site and resolved parent parts (accepted owner decision 2; legacy source
/// <c>master_plan.py</c>). Each row keeps its own <c>mrp_due_date</c> and signed source quantity;
/// Order Period / Safety Time are not re-applied and no <c>mrp_dataset</c>/<c>mrp_type</c> filter is
/// added (their live values are unverified evidence E1). The Domain applies the absolute-value
/// display transform. A row with a missing due date or quantity is malformed and throws rather
/// than being dropped or zeroed; successful absence of rows returns an empty list.
/// </summary>
public sealed class QadForecastReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadForecastReader> _logger;

    public QadForecastReader(QadConnectionOptions options, ILogger<QadForecastReader> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PlanningForecastRow>> ReadAsync(
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

        var results = new List<PlanningForecastRow>();
        for (var i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            var stopwatch = Stopwatch.StartNew();
            var (sql, parameters) = BuildBatchQuery(domain, site, batch);
            var command = new CommandDefinition(
                sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken);

            var rawRows = await connection.QueryAsync<QadForecastRawRow>(command);
            var normalized = rawRows.Select(Normalize).ToList();
            stopwatch.Stop();

            _logger.LogInformation(
                "Planning forecast batch {BatchIndex}/{BatchCount} ({PartCount} parts) returned {RowCount} rows in {ElapsedMs}ms.",
                i + 1, batches.Count, batch.Count, normalized.Count, stopwatch.ElapsedMilliseconds);

            results.AddRange(normalized);
        }

        return results;
    }

    /// <summary>
    /// Builds the parameterized forecast query for one batch of parent parts. The quantity predicate
    /// keeps NULL quantities so malformed rows reach <see cref="Normalize"/> and fail loudly instead
    /// of silently disappearing. Public and pure so SQL/parameter shape is independently testable.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildBatchQuery(
        string domain,
        string site,
        IReadOnlyList<string> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        if (parts.Count == 0)
            throw new ArgumentException("At least one part number is required.", nameof(parts));

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
                scope.ParentPart AS ParentPart,
                mrp.mrp_due_date AS DueDate,
                mrp.mrp_qty      AS Quantity
            FROM ScopeParts AS scope
            INNER JOIN qadpro2.dbo.mrp_det AS mrp
                ON mrp.mrp_part = scope.ParentPart
                AND mrp.mrp_domain = @Domain
                AND mrp.mrp_site = @Site
            WHERE
                mrp.mrp_detail = 'FORECAST'
                AND (mrp.mrp_qty IS NULL OR mrp.mrp_qty <> 0)
            ORDER BY
                scope.ParentPart,
                mrp.mrp_due_date;
            """;

        return (sql, parameters);
    }

    public static PlanningForecastRow Normalize(QadForecastRawRow raw)
    {
        if (raw.ParentPart is null || raw.DueDate is not { } dueDate || raw.Quantity is not { } quantity)
            throw new InvalidDataException("Forecast source returned a row without a part, due date or quantity.");
        return new PlanningForecastRow(raw.ParentPart, DateOnly.FromDateTime(dueDate), quantity);
    }
}

/// <summary>QAD-shaped raw Dapper row. Does not travel past this integration boundary.</summary>
public sealed record QadForecastRawRow(string? ParentPart, DateTime? DueDate, decimal? Quantity);
