using Kst.Integrations.Qad.OpenOrders;
using Kst.Integrations.Qad.Mps;
using Kst.Domain.Mps;

namespace Kst.Integrations.Qad.Tests.OpenOrders;

public sealed class QadOpenOrdersReaderTests
{
    [Fact]
    public void NormalizationRetainsSourceFieldsAndNullableInventory()
    {
        var line = QadOpenOrdersReader.Normalize(new OpenOrderRawRow
        {
            Domain = "TEST", Site = "S1", SalesOrder = "SO-1", Line = 2,
            ItemNumber = "P-1", OrderQty = 5m, ShippedQty = 2m, Price = 0.0125m,
            Salesperson = "SP", SiteQoh = null, Consignment = true,
            DockDate = new DateTime(2027, 1, 6), LineComments = "first;second"
        });
        Assert.Equal("TEST", line.Key.Domain);
        Assert.Equal("SO-1", line.Key.SalesOrder);
        Assert.Equal(2, line.Key.Line);
        Assert.Equal("SP", line.Salesperson);
        Assert.Equal(0.0125m, line.SourceValues.Price);
        Assert.Equal(3m, line.Open);
        Assert.Equal(0.0375m, line.ExtPrice);
        Assert.Equal(0m, line.UnitPrice);
        Assert.Null(line.SiteQoh);
        Assert.Equal(new DateOnly(2027, 1, 6), line.SourceValues.DockDate);
        Assert.Equal("first;second", line.LineComments);
    }

    [Fact]
    public void UsesEstablishedBatchSizeAndFullParameterizedReadOnlyQuery()
    {
        var batches = MpsPartBatcher.Batch(Enumerable.Range(0, 1001).Select(x => $"P{x}").ToArray());
        Assert.Equal([500, 500, 1], batches.Select(x => x.Count));
        foreach (var batch in batches)
        {
            var (sql, parameters) = QadOpenOrderQueryContract.BuildBatchQuery(QadSiteDomainMap.Resolve("SW"), "SW", batch);
            Assert.Equal(batch.Count + 2, parameters.ParameterNames.Count());
            Assert.Contains("sod.sod_consignment AS Consignment", sql);
            Assert.Contains("sod.sod_qty_ord - sod.sod_qty_ship > 0", sql);
            Assert.Contains("cmt.cmt_domain = sod.sod_domain", sql);
            Assert.Contains("ld.ld_site = sod.sod_site", sql);
            Assert.DoesNotContain("TOP (", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DISTINCT", sql, StringComparison.OrdinalIgnoreCase);
            foreach (var verb in new[] { "INSERT ", "UPDATE ", "DELETE ", "MERGE ", "EXEC " })
                Assert.DoesNotContain(verb, sql, StringComparison.OrdinalIgnoreCase);
        }
    }
}
