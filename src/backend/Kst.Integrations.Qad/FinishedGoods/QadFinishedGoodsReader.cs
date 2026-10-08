using Dapper;
using Kst.Domain.FinishedGoods;
using Kst.Domain.Mps;
using Kst.Integrations.Qad.Inventory;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;
using Microsoft.Extensions.Logging;

namespace Kst.Integrations.Qad.FinishedGoods;

/// <summary>Positive physical detail plus separate complete MAX-price envelopes; never multiplies inventory by price records.</summary>
public sealed class QadFinishedGoodsReader(QadConnectionOptions options, ILogger<QadFinishedGoodsReader> logger)
{
    public async Task<IReadOnlyList<FinishedGoodsSourceRow>> ReadAsync(string site, IReadOnlyList<string> parts, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var keys = QadPartInventoryReader.NormalizePartNumbers(parts);
        if (keys.Count == 0) return [];
        if (!options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        var domain = QadSiteDomainMap.Resolve(site);
        await using var connection = await QadConnectionFactory.OpenAsync(options, ct);
        var result = new List<FinishedGoodsSourceRow>();
        foreach (var batch in MpsPartBatcher.Batch(keys, options.MaxPartBatchSize))
        {
            var (sql, parameters) = BuildBatchQuery(domain, site, batch);
            using var acquisition = await connection.QueryMultipleAsync(new CommandDefinition(sql, parameters,
                commandTimeout: options.CommandTimeoutSeconds, cancellationToken: ct));
            var inventory = (await acquisition.ReadAsync<QadFinishedGoodsRawRow>()).ToArray();
            var prices = (await acquisition.ReadAsync<QadFinishedGoodsPriceRow>()).ToArray();
            ct.ThrowIfCancellationRequested();
            result.AddRange(Normalize(domain, site, batch, inventory, prices));
        }
        logger.LogInformation("Finished Goods acquisition completed for {PartCount} parts with {RowCount} inventory rows.", keys.Count, result.Count);
        return result;
    }

    public static (string Sql, DynamicParameters Parameters) BuildBatchQuery(string domain, string site, IReadOnlyList<string> parts)
    {
        if (parts.Count == 0) throw new ArgumentException("Part scope is required.", nameof(parts));
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain); parameters.Add("Site", site);
        var values = new List<string>();
        for (var i = 0; i < parts.Count; i++) { parameters.Add($"Part{i}", parts[i]); values.Add($"(@Part{i})"); }
        var scope = $"SELECT PartNumber FROM (VALUES {string.Join(", ", values)}) AS Parts(PartNumber)";
        var sql = $"""
            WITH ScopeParts(PartNumber) AS ({scope})
            SELECT ld.ld_domain AS Domain, ld.ld_site AS Site, ld.ld_part AS Product,
                   ld.ld_loc AS Location, ld.ld_lot AS Lot, ld.ld_ref AS Reference,
                   CONVERT(varchar(100), ld.ld_qty_oh) AS OnHand
            FROM qadpro2.dbo.ld_det AS ld
            WHERE ld.ld_domain = @Domain AND ld.ld_site = @Site
              AND ld.ld_part IN (SELECT PartNumber FROM ScopeParts) AND ld.ld_qty_oh > 0;

            WITH ScopeParts(PartNumber) AS ({scope}), Prices AS
            (
                SELECT inp_part, inp_domain, COUNT_BIG(*) AS RecordCount,
                       COUNT_BIG(inp_custprice) AS ValueCount, MAX(inp_custprice) AS MaximumPrice
                FROM Analysis.dbo.in_price
                WHERE inp_domain = @Domain AND inp_part IN (SELECT PartNumber FROM ScopeParts)
                GROUP BY inp_part, inp_domain
            )
            SELECT scope.PartNumber AS Product, COALESCE(price.RecordCount, 0) AS RecordCount,
                   COALESCE(price.ValueCount, 0) AS ValueCount,
                   CONVERT(varchar(100), price.MaximumPrice) AS MaximumPrice
            FROM ScopeParts AS scope
            LEFT JOIN Prices AS price ON price.inp_part = scope.PartNumber AND price.inp_domain = @Domain;
            """;
        return (sql, parameters);
    }

    public static IReadOnlyList<FinishedGoodsSourceRow> Normalize(string domain, string site, IReadOnlyList<string> parts,
        IReadOnlyList<QadFinishedGoodsRawRow> inventory, IReadOnlyList<QadFinishedGoodsPriceRow> prices)
    {
        var scope = new HashSet<string>(parts, StringComparer.OrdinalIgnoreCase);
        var selected = new Dictionary<string, (decimal Price, bool Missing)>(StringComparer.OrdinalIgnoreCase);
        foreach (var price in prices)
        {
            if (price.Product is null || !scope.Contains(price.Product) || selected.ContainsKey(price.Product) ||
                price.RecordCount is null or < 0 || price.ValueCount is null or < 0 || price.ValueCount > price.RecordCount)
                throw FinishedGoodsFailure.Create(FinishedGoodsFailureReason.PriceEnvelopeInvalid);
            if (price.RecordCount == 0)
            {
                if (price.ValueCount != 0 || price.MaximumPrice is not null) throw FinishedGoodsFailure.Create(FinishedGoodsFailureReason.MissingPriceEnvelopeInvalid);
                selected.Add(price.Product, (0m, true));
            }
            else
            {
                if (price.ValueCount == 0 || price.MaximumPrice is null) throw FinishedGoodsFailure.Create(FinishedGoodsFailureReason.PriceAllNull);
                selected.Add(price.Product, (FinishedGoodsDecimal.ParseExact(price.MaximumPrice), false));
            }
        }
        if (scope.Any(p => !selected.ContainsKey(p))) throw FinishedGoodsFailure.Create(FinishedGoodsFailureReason.PriceScopeIncomplete);
        var result = new List<FinishedGoodsSourceRow>(inventory.Count);
        foreach (var row in inventory)
        {
            if (!string.Equals(row.Domain, domain, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(row.Site, site, StringComparison.OrdinalIgnoreCase) ||
                row.Product is null || !scope.Contains(row.Product) || row.OnHand is null)
                throw FinishedGoodsFailure.Create(FinishedGoodsFailureReason.InventoryScopeInvalid);
            var quantity = FinishedGoodsDecimal.ParseExact(row.OnHand);
            if (quantity <= 0) throw FinishedGoodsFailure.Create(FinishedGoodsFailureReason.InventoryQuantityInvalid);
            var price = selected[row.Product];
            result.Add(new(domain, site, row.Product, row.Location, row.Lot, row.Reference, quantity, price.Price, price.Missing));
        }
        return result;
    }
}

public sealed record QadFinishedGoodsRawRow(string? Domain, string? Site, string? Product, string? Location, string? Lot, string? Reference, string? OnHand);
public sealed record QadFinishedGoodsPriceRow(string? Product, long? RecordCount, long? ValueCount, string? MaximumPrice);
