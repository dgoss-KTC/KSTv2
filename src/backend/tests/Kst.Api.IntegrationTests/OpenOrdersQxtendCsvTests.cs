using Kst.Domain.OpenOrders;
using Kst.Exports;

namespace Kst.Api.IntegrationTests;

public sealed class OpenOrdersQxtendCsvTests
{
    private static OpenOrderProposal Proposal(string order, int line, OpenOrderEditableValues original,
        OpenOrderEditableValues proposed, string reason) => new(new("TEST", order, line), "S1", "P-1", original, proposed, reason);

    [Fact]
    public void SyntheticFixtureMatchesEachGoldenByteExactly()
    {
        var original10a = new OpenOrderEditableValues(new(2026, 10, 1), new(2026, 9, 28), new(2026, 9, 27), new(2026, 9, 29), 5m, 2.50m);
        var original10b = new OpenOrderEditableValues(new(2026, 10, 2), new(2026, 9, 29), new(2026, 9, 28), new(2026, 9, 30), 10m, 1.25m);
        var original20 = new OpenOrderEditableValues(new(2026, 10, 3), new(2026, 10, 1), new(2026, 9, 30), null, 3m, 0.0100m);
        var files = OpenOrdersQxtendCsv.Create([
            Proposal("SO-20", 3, original20, original20 with { OrderQty = 4m, Price = 0.0125m,
                DueDate = new(2026, 10, 4), DockDate = new(2027, 1, 6) }, "Quality"),
            Proposal("SO-10", 2, original10b, original10b with { OrderQty = 12m }, "Cust/PM"),
            Proposal("SO-10", 1, original10a, original10a with { OrderQty = 6m, Price = 3.125m, DockDate = null }, "Buyers")
        ]);
        Assert.Equal(3, files.Count);
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../../docs/reference/Stage13Fixtures"));
        foreach (var (kind, name) in new[] { (OpenOrdersQxtendKind.Quantity, "UpdateQuantities.expected.csv"),
                     (OpenOrdersQxtendKind.Price, "UpdatePrices.expected.csv"),
                     (OpenOrdersQxtendKind.Date, "DateChange.expected.csv") })
            Assert.Equal(File.ReadAllBytes(Path.Combine(root, name)), files[kind]);
    }

    [Fact]
    public void EscapesOrderAndDoesNotProduceFileForUnchangedFamily()
    {
        var original = new OpenOrderEditableValues(null, null, null, null, 1m, 0.0100m);
        var files = OpenOrdersQxtendCsv.Create([Proposal("SO,\"X\"", 2, original,
            original with { Price = 0.0000000000000000000000000001m }, "Cust/PM")]);
        Assert.Single(files);
        Assert.False(files.ContainsKey(OpenOrdersQxtendKind.Quantity));
        Assert.False(files.ContainsKey(OpenOrdersQxtendKind.Date));
        var bytes = files[OpenOrdersQxtendKind.Price];
        Assert.False(bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
        var text = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.Contains("M,\"SO,\"\"X\"\"\",M,\"SO,\"\"X\"\"\",2,TRUE,Cust/PM,0.0000000000000000000000000001,0.0000000000000000000000000001\r\n", text);
        Assert.DoesNotContain("\n", text.Replace("\r\n", ""));
    }
}
