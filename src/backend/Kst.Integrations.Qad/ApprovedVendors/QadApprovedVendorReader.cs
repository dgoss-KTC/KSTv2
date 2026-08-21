using System.Diagnostics;
using Dapper;
using Microsoft.Extensions.Logging;
using Kst.Domain.ApprovedVendors;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.ApprovedVendors;

/// <summary>
/// Direct, parameterized QAD adapter for Stage 8D.7 Approved Vendors: the accepted
/// <c>pt_mstr</c> INNER JOIN <c>vp_mstr</c> INNER JOIN <c>ad_mstr</c> relationship query, filtered
/// by Domain + Part and ordered by Supplier (<c>vp_vend</c>). Domain is derived from
/// <paramref name="site"/> via <see cref="QadSiteDomainMap"/> at this integration boundary — AVL
/// itself is not Site-specific. No DISTINCT, no dedup: source row multiplicity and duplicate
/// supplier/vendor relationships are preserved as-is. A nonexistent <c>pt_mstr</c> row for the
/// part/domain naturally yields zero rows through the INNER JOIN chain — this reader performs no
/// separate existence check.
/// </summary>
public sealed class QadApprovedVendorReader
{
    private readonly QadConnectionOptions _options;
    private readonly ILogger<QadApprovedVendorReader> _logger;

    public QadApprovedVendorReader(QadConnectionOptions options, ILogger<QadApprovedVendorReader> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Reads Approved Vendors for one component part. Domain is resolved from
    /// <paramref name="site"/>. Returns zero-to-many rows, preserving source order (Supplier
    /// ascending) and multiplicity.
    /// </summary>
    public async Task<IReadOnlyList<ApprovedVendor>> ReadAsync(
        string site,
        string componentPart,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("QAD connection is not configured.");

        var domain = QadSiteDomainMap.Resolve(site);
        var stopwatch = Stopwatch.StartNew();

        await using var connection = await QadConnectionFactory.OpenAsync(_options, cancellationToken);

        var (sql, parameters) = BuildQuery(domain, componentPart);
        var command = new CommandDefinition(
            sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        var rawRows = (await connection.QueryAsync<QadApprovedVendorRawRow>(command)).ToList();

        stopwatch.Stop();
        _logger.LogInformation(
            "Approved Vendors read for part {ComponentPart} in domain {Domain} returned {RowCount} row(s) in {ElapsedMs}ms.",
            componentPart, domain, rawRows.Count, stopwatch.ElapsedMilliseconds);

        return rawRows.Select(Normalize).ToList();
    }

    /// <summary>
    /// Builds the accepted Domain + Part AVL relationship query. Public and pure so SQL/parameter
    /// shape is independently testable. Joins <c>vp_mstr</c> on domain + part and <c>ad_mstr</c>
    /// on domain + vendor/address; orders by Supplier (<c>vp_vend</c>) ascending; no DISTINCT.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildQuery(string domain, string componentPart)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Part", componentPart);

        const string sql = """
            SELECT
                vp.vp_vend       AS Supplier,
                ad.ad_name       AS VendorName,
                vp.vp_vend_part  AS SupplierItem,
                vp.vp_mfgr_part  AS ManufacturerPart
            FROM qadpro2.dbo.pt_mstr AS pt
            INNER JOIN qadpro2.dbo.vp_mstr AS vp
                ON pt.pt_domain = vp.vp_domain
               AND pt.pt_part = vp.vp_part
            INNER JOIN qadpro2.dbo.ad_mstr AS ad
                ON vp.vp_domain = ad.ad_domain
               AND vp.vp_vend = ad.ad_addr
            WHERE pt.pt_domain = @Domain
              AND pt.pt_part = @Part
            ORDER BY vp.vp_vend;
            """;

        return (sql, parameters);
    }

    public static ApprovedVendor Normalize(QadApprovedVendorRawRow row) => new(
        Supplier: row.Supplier.Trim(),
        VendorName: NormalizeOptional(row.VendorName),
        SupplierItem: NormalizeOptional(row.SupplierItem),
        ManufacturerPart: NormalizeOptional(row.ManufacturerPart));

    /// <summary>
    /// Trims an optional string field, mapping blank/whitespace-only values to null. Never drops
    /// the row for a null/blank Supplier Item or MFG Part — those fields may legitimately be
    /// absent.
    /// </summary>
    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}

/// <summary>QAD-shaped raw AVL relationship Dapper result row. Does not travel past this integration boundary.</summary>
public sealed record QadApprovedVendorRawRow(
    string Supplier,
    string? VendorName,
    string? SupplierItem,
    string? ManufacturerPart);
