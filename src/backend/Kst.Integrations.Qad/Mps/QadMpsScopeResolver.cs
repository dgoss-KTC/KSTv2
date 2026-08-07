using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Kst.Domain.Mps;
using Kst.Domain.Workspaces;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.Mps;

/// <summary>
/// Resolves a workspace's parent-level MPS part scope: product-line-derived discovery (when a
/// product-line range is configured) unioned with the workspace's explicitly configured parent
/// parts. Explicit parts are never dropped for lacking planning activity or item-master metadata;
/// only product-line discovery applies the accepted pm_code/status filters, because that path is a
/// discovery query rather than a scheduler's explicit declaration.
/// </summary>
public sealed class QadMpsScopeResolver
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadMpsScopeResolver> _logger;

    public QadMpsScopeResolver(QadConnectionOptions options, ILogger<QadMpsScopeResolver> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MpsResolvedPart>> ResolveAsync(
        WorkspaceAssignment workspace,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("QAD connection is not configured.");

        var domain = QadSiteDomainMap.Resolve(workspace.Site);
        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);

        var resolved = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(workspace.ProductLineFrom))
        {
            var discovered = await DiscoverProductLinePartsAsync(
                connection, domain, workspace.Site, workspace.ProductLineFrom,
                workspace.ProductLineTo ?? workspace.ProductLineFrom, cancellationToken);

            foreach (var part in discovered.OrderBy(p => p.ParentPart, StringComparer.OrdinalIgnoreCase))
                resolved[part.ParentPart] = part.Description;
        }

        if (workspace.ParentParts.Count > 0)
        {
            var descriptions = await LookupPartDescriptionsAsync(connection, domain, workspace.ParentParts, cancellationToken);

            foreach (var part in workspace.ParentParts)
            {
                var description = descriptions.TryGetValue(part, out var found) ? found : null;
                if (!resolved.TryGetValue(part, out var existing) || existing is null)
                    resolved[part] = description;
            }
        }

        return resolved.Select(kv => new MpsResolvedPart(kv.Key, kv.Value)).ToList();
    }

    private async Task<IReadOnlyList<MpsResolvedPart>> DiscoverProductLinePartsAsync(
        SqlConnection connection, string domain, string site, string productLineFrom, string productLineTo,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT DISTINCT
                pt.pt_part  AS ParentPart,
                pt.pt_desc1 AS Description
            FROM qadpro2.dbo.pt_mstr AS pt
            INNER JOIN qadpro2.dbo.mrp_det AS mrp
                ON mrp.mrp_part = pt.pt_part
                AND mrp.mrp_domain = pt.pt_domain
            WHERE
                pt.pt_domain = @Domain
                AND mrp.mrp_site = @Site
                AND pt.pt_prod_line BETWEEN @ProductLineFrom AND @ProductLineTo
                AND LOWER(pt.pt_pm_code) NOT IN ('p', 'f')
                AND pt.pt_status NOT IN ('E', 'O')
                AND mrp.mrp_dataset <> 'pod_det'
                AND LOWER(mrp.mrp_type) IN ('supply', 'supplyf', 'supplyp')
            """;

        var stopwatch = Stopwatch.StartNew();
        var command = new CommandDefinition(
            sql,
            new { Domain = domain, Site = site, ProductLineFrom = productLineFrom, ProductLineTo = productLineTo },
            commandTimeout: _options.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        var rows = (await connection.QueryAsync<MpsResolvedPart>(command)).ToList();
        stopwatch.Stop();

        _logger.LogInformation(
            "MPS product-line scope discovery returned {PartCount} parts in {ElapsedMs}ms.",
            rows.Count, stopwatch.ElapsedMilliseconds);

        return rows;
    }

    private async Task<IReadOnlyDictionary<string, string?>> LookupPartDescriptionsAsync(
        SqlConnection connection, string domain, IReadOnlyList<string> parts, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var batch in MpsPartBatcher.Batch(parts, _options.MaxPartBatchSize))
        {
            var (sql, parameters) = BuildDescriptionLookupQuery(domain, batch);
            var command = new CommandDefinition(
                sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken);

            var rows = await connection.QueryAsync<MpsResolvedPart>(command);
            foreach (var row in rows)
                result[row.ParentPart] = row.Description;
        }

        return result;
    }

    public static (string Sql, DynamicParameters Parameters) BuildDescriptionLookupQuery(
        string domain, IReadOnlyList<string> parts)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);

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
                pt.pt_desc1      AS Description
            FROM ScopeParts AS scope
            LEFT JOIN qadpro2.dbo.pt_mstr AS pt
                ON pt.pt_part = scope.ParentPart
                AND pt.pt_domain = @Domain;
            """;

        return (sql, parameters);
    }
}
