using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Kst.Integrations.Qad.Tests.OpenOrders;

/// <summary>
/// Offline checks on the Stage 13.1 source contract fixture. These do not exercise a production
/// reader or prove QAD schema; that reader must be verified after the approved metadata probe.
/// </summary>
public sealed class Stage13SourceContractFixtureTests
{
    [Fact]
    public void ReportFixture_PreservesScopedOpenPopulationAndLegacyDecimalCalculations()
    {
        using var fixture = Load();
        var root = fixture.RootElement;
        var scope = root.GetProperty("scope");
        var site = scope.GetProperty("site").GetString();
        var parents = scope.GetProperty("resolvedParents").EnumerateArray()
            .Select(x => x.GetString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rows = root.GetProperty("report").GetProperty("sourceLines").EnumerateArray().ToArray();

        foreach (var row in rows)
        {
            var open = Decimal(row, "orderQty") - Decimal(row, "shippedQty");
            var inScope = row.GetProperty("site").GetString() == site
                          && parents.Contains(row.GetProperty("part").GetString());
            Assert.Equal(inScope && open > 0, row.GetProperty("included").GetBoolean());
            if (!inScope || open <= 0) continue;

            Assert.Equal(open, Decimal(row, "expectedOpen"));
            Assert.Equal(Decimal(row, "price") * open, Decimal(row, "expectedExtPrice"));
            Assert.Equal(row.GetProperty("consignment").GetBoolean() ? 0 : Decimal(row, "price"),
                Decimal(row, "expectedUnitPrice"));
        }

        var included = rows.Where(x => x.GetProperty("included").GetBoolean()).ToArray();
        var defaultOrder = included.OrderBy(x => x.GetProperty("customerName").GetString(), StringComparer.Ordinal)
            .ThenBy(x => x.GetProperty("part").GetString(), StringComparer.Ordinal)
            .ThenBy(x => x.GetProperty("due").GetString(), StringComparer.Ordinal)
            .Select(Key);
        Assert.Equal(root.GetProperty("report").GetProperty("defaultSortOrder").EnumerateArray()
            .Select(x => x.GetString()), defaultOrder);

        var filter = root.GetProperty("report").GetProperty("andFilter");
        var filtered = included.Where(x =>
            x.GetProperty("customerName").GetString()!.Contains(filter.GetProperty("customerNameContains").GetString()!, StringComparison.OrdinalIgnoreCase)
            && x.GetProperty("customer").GetString() == filter.GetProperty("customer").GetString()
            && x.GetProperty("salesperson").GetString() == filter.GetProperty("salesperson").GetString()
            && string.CompareOrdinal(x.GetProperty("productLine").GetString(), filter.GetProperty("productLineFrom").GetString()) >= 0
            && string.CompareOrdinal(x.GetProperty("productLine").GetString(), filter.GetProperty("productLineTo").GetString()) <= 0
            && x.GetProperty("ios").GetString() == filter.GetProperty("ios").GetString()
            && string.CompareOrdinal(x.GetProperty("due").GetString(), filter.GetProperty("dueFrom").GetString()) >= 0
            && string.CompareOrdinal(x.GetProperty("due").GetString(), filter.GetProperty("dueTo").GetString()) <= 0)
            .Select(Key);
        Assert.Equal(filter.GetProperty("expectedOrdersAndLines").EnumerateArray().Select(x => x.GetString()), filtered);
    }

    [Fact]
    public void QxtendFixture_GroupsEachFamilyIndependentlyAndRepeatsChangedRowReason()
    {
        using var fixture = Load();
        var qxtend = fixture.RootElement.GetProperty("qxtend");
        var changes = qxtend.GetProperty("changedRows").EnumerateArray().ToArray();
        var allowedReasons = new HashSet<string>(["Cust/PM", "Buyers", "Planning", "Factory", "C&R", "Quality", "Engineer"], StringComparer.Ordinal);
        var dateCells = qxtend.GetProperty("expectedLogicalRows").GetProperty("date").EnumerateArray()
            .Select(row => row.EnumerateArray().Select(cell => cell.GetString()).ToArray()).ToArray();
        Assert.Contains(dateCells, cells => cells[9] == "");
        Assert.Contains(dateCells, cells => cells[9] == "1/6/2027");

        foreach (var kind in new[] { "quantity", "price", "date" })
        {
            var selected = changes.Where(row => row.GetProperty("families").EnumerateArray()
                    .Any(f => f.GetString() == kind))
                .OrderBy(row => row.GetProperty("order").GetString(), StringComparer.Ordinal)
                .ThenBy(row => row.GetProperty("line").GetInt32()).ToArray();
            var expected = qxtend.GetProperty("expectedLogicalRows").GetProperty(kind).EnumerateArray().ToArray();
            Assert.Equal(selected.Length, expected.Length);
            string? previousOrder = null;
            for (var i = 0; i < selected.Length; i++)
            {
                var row = selected[i];
                var cells = expected[i].EnumerateArray().Select(c => c.GetString()).ToArray();
                var order = row.GetProperty("order").GetString();
                Assert.Equal(order == previousOrder ? "" : "M", cells[0]);
                Assert.Equal(order == previousOrder ? "" : order, cells[1]);
                Assert.Equal("M", cells[2]);
                Assert.Equal(order, cells[3]);
                Assert.Equal(row.GetProperty("line").GetInt32().ToString(CultureInfo.InvariantCulture), cells[4]);
                var reason = row.GetProperty("reason").GetString()!;
                Assert.Contains(reason, allowedReasons);
                Assert.Equal(reason, cells[kind == "quantity" ? 6 : kind == "price" ? 6 : 5]);
                if (kind == "quantity") Assert.Equal(row.GetProperty("proposedOrderQty").GetString(), cells[5]);
                if (kind == "price")
                {
                    Assert.Equal("TRUE", cells[5]);
                    Assert.Equal(row.GetProperty("proposedPrice").GetString(), cells[7]);
                    Assert.Equal(cells[7], cells[8]);
                }
                if (kind == "date")
                {
                    var dates = row.GetProperty("proposedDates");
                    foreach (var (field, index) in new[] { ("required", 6), ("due", 7), ("perform", 8), ("dock", 9) })
                    {
                        var source = dates.GetProperty(field);
                        var formatted = source.ValueKind == JsonValueKind.Null ? "" :
                            DateOnly.ParseExact(source.GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                                .ToString("M/d/yyyy", CultureInfo.InvariantCulture);
                        Assert.Equal(formatted, cells[index]);
                    }
                }
                previousOrder = order;
            }
        }

        var cases = qxtend.GetProperty("additionalCases").EnumerateArray()
            .ToDictionary(row => row.GetProperty("case").GetString()!, StringComparer.Ordinal);
        var equalsShipped = cases["quantity-equals-shipped"];
        Assert.Equal(Decimal(equalsShipped, "proposedOrderQty") - Decimal(equalsShipped, "shippedQty"),
            Decimal(equalsShipped, "expectedProposedOpen"));
        Assert.True(equalsShipped.GetProperty("expectedValid").GetBoolean());
        var belowShipped = cases["quantity-below-shipped"];
        Assert.True(Decimal(belowShipped, "proposedOrderQty") < Decimal(belowShipped, "shippedQty"));
        Assert.False(belowShipped.GetProperty("expectedValid").GetBoolean());
        Assert.Equal(Decimal(cases["no-op"], "originalPrice"), Decimal(cases["no-op"], "proposedPrice"));
        Assert.False(cases["no-op"].GetProperty("expectedExport").GetBoolean());
        var escaping = cases["csv-escaping"];
        var escapedOrder = "\"" + escaping.GetProperty("order").GetString()!.Replace("\"", "\"\"") + "\"";
        Assert.Equal(escaping.GetProperty("expectedSerializedOrderCell").GetString(), escapedOrder);
    }

    [Theory]
    [InlineData("quantity", "UpdateQuantities.expected.csv")]
    [InlineData("price", "UpdatePrices.expected.csv")]
    [InlineData("date", "DateChange.expected.csv")]
    public void QxtendGolden_IsNoBomCrLfAndMatchesRecordedHeaderAndLogicalRows(
        string kind, string goldenName)
    {
        using var fixture = Load();
        var root = RepositoryRoot();
        var expected = fixture.RootElement.GetProperty("qxtend");
        var header = expected.GetProperty("headers").GetProperty(kind).GetString();

        var golden = File.ReadAllBytes(Path.Combine(root, "docs", "reference", "Stage13Fixtures", goldenName));
        Assert.False(golden.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
        var text = new UTF8Encoding(false, true).GetString(golden);
        Assert.EndsWith("\r\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", text.Replace("\r\n", ""), StringComparison.Ordinal);
        Assert.DoesNotContain("\r", text.Replace("\r\n", ""), StringComparison.Ordinal);
        var lines = text.Split("\r\n", StringSplitOptions.None);
        Assert.Equal(header, lines[0]);
        var rows = expected.GetProperty("expectedLogicalRows").GetProperty(kind).EnumerateArray()
            .Select(row => string.Join(",", row.EnumerateArray().Select(cell => cell.GetString()))).ToArray();
        Assert.Equal(rows, lines[1..^1]);
    }

    private static string Key(JsonElement row) => $"{row.GetProperty("order").GetString()}/{row.GetProperty("line").GetInt32()}";

    private static decimal Decimal(JsonElement row, string property) =>
        decimal.Parse(row.GetProperty(property).GetString()!, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

    private static JsonDocument Load()
    {
        var path = Path.Combine(RepositoryRoot(), "docs", "reference", "STAGE_13_OPEN_ORDERS_DETERMINISTIC_FIXTURES.json");
        return JsonDocument.Parse(File.ReadAllBytes(path));
    }

    private static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "docs", "reference", "STAGE_13_OPEN_ORDERS_DETERMINISTIC_FIXTURES.json")))
                return dir.FullName;
        throw new DirectoryNotFoundException("Stage 13.1 reference fixture not found.");
    }
}
