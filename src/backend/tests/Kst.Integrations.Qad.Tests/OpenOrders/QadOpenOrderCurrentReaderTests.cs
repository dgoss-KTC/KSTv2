using Dapper;
using Kst.Domain.OpenOrders;
using Kst.Integrations.Qad.OpenOrders;

namespace Kst.Integrations.Qad.Tests.OpenOrders;

public sealed class QadOpenOrderCurrentReaderTests
{
    [Fact]
    public void QueryTargetsOnlyPassedIdentitiesAndUsesReadOnlyParameterizedScope()
    {
        var keys = new[] { new OpenOrderLineKey("TEST", "SO-1", 3), new OpenOrderLineKey("TEST", "SO-2", 5) };
        var (sql, parameters) = QadOpenOrderCurrentReader.BuildQuery("TEST", "SW", keys);
        Assert.Contains("(@Order0, @Line0), (@Order1, @Line1)", sql);
        Assert.Contains("sod.sod_domain = @Domain AND sod.sod_site = @Site", sql);
        Assert.Contains("sod.sod_qty_ord - sod.sod_qty_ship > 0", sql);
        Assert.Contains("sod.sod__dte01 AS DockDate", sql);
        Assert.DoesNotContain("SO-1", sql);
        Assert.DoesNotContain("TOP", sql, StringComparison.OrdinalIgnoreCase);
        foreach (var verb in new[] { "INSERT ", "UPDATE ", "DELETE ", "MERGE ", "EXEC " })
            Assert.DoesNotContain(verb, sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("TEST", parameters.Get<string>("Domain"));
        Assert.Equal("SW", parameters.Get<string>("Site"));
        Assert.Equal("SO-1", parameters.Get<string>("Order0"));
        Assert.Equal(5, parameters.Get<int>("Line1"));
    }
}
