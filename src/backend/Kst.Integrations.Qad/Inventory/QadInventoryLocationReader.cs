using System.Globalization;
using Dapper;
using Kst.Domain.Inventory;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.Inventory;

public sealed class QadInventoryLocationReader(QadConnectionOptions options)
{
    public async Task<IReadOnlyList<InventoryLocationRow>> ReadAsync(string site, string part, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        var domain = QadSiteDomainMap.Resolve(site);
        var (sql, parameters) = BuildQuery(domain, site, part);
        await using var connection = await QadConnectionFactory.OpenAsync(options, ct);
        var rows = await connection.QueryAsync<QadInventoryLocationRawRow>(new CommandDefinition(sql, parameters,
            commandTimeout: options.CommandTimeoutSeconds, cancellationToken: ct));
        ct.ThrowIfCancellationRequested();
        return Normalize(domain, site, part, rows.ToArray());
    }

    public static (string Sql, DynamicParameters Parameters) BuildQuery(string domain, string site, string part)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("Part", part);
        return ("""
            SELECT ld.ld_domain AS Domain, ld.ld_site AS Site, ld.ld_part AS Part,
                   ld.ld_loc AS Location, ld.ld_lot AS LotSerial, ld.ld_status AS Status,
                   CONVERT(varchar(100), ld.ld_qty_oh) AS QuantityOnHand
            FROM QADPRO2.dbo.ld_det AS ld
            WHERE ld.ld_domain = @Domain AND ld.ld_site = @Site AND ld.ld_part = @Part;
            """, parameters);
    }

    public static IReadOnlyList<InventoryLocationRow> Normalize(string domain, string site, string part, IReadOnlyList<QadInventoryLocationRawRow> rows)
    {
        var result = new List<InventoryLocationRow>(rows.Count);
        foreach (var row in rows)
        {
            if (!string.Equals(row.Domain, domain, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(row.Site, site, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(row.Part, part, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Inventory location scope mismatch.");
            // Keep SQL's decimal text directly: no double/decimal conversion, rounding, or null coalescing.
            if (row.QuantityOnHand is not null && !System.Text.RegularExpressions.Regex.IsMatch(row.QuantityOnHand, @"\A[+-]?[0-9]+(?:\.[0-9]+)?\z"))
                throw new InvalidOperationException("Invalid inventory quantity representation.");
            result.Add(new(result.Count.ToString(CultureInfo.InvariantCulture), row.Location, row.LotSerial, row.Status, row.QuantityOnHand));
        }
        return result;
    }
}

public sealed record QadInventoryLocationRawRow(string? Domain, string? Site, string? Part,
    string? Location, string? LotSerial, string? Status, string? QuantityOnHand);
