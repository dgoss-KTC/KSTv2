using Kst.Domain.HistoricalShipments;

namespace Kst.Domain.Tests.HistoricalShipments;

public class ShipmentViewRulesTests
{
    private static bool Matches(string? value, ShipmentViewFilter filter) =>
        ShipmentViewRules.Predicate([filter.Column], ShipmentViewRules.Validate([filter]))(new("physical", new[] { value }));

    [Theory]
    [InlineData("Ab%_C", "%_", true)]
    [InlineData("AbZZC", "%_", false)]
    [InlineData("Abc", "BC", true)]
    [InlineData(null, "BC", false)]
    [InlineData("", "BC", false)]
    public void Contains_is_case_insensitive_literal_and_never_SQL_pattern(string? value, string text, bool expected) =>
        Assert.Equal(expected, Matches(value, new("itemNumber", "contains", Text: text)));

    [Fact]
    public void Values_are_ordinal_and_blanks_are_not_captions_or_whitespace()
    {
        var include = new ShipmentViewFilter("itemNumber", "values", "include", ["A", "(Blanks)"]);
        Assert.True(Matches("A", include)); Assert.False(Matches("a", include));
        Assert.True(Matches("(Blanks)", include)); Assert.False(Matches(null, include));
        Assert.False(Matches("", include)); Assert.False(Matches(" ", include));
        Assert.True(Matches(null, include with { IncludeBlanks = true }));
        Assert.True(Matches("", include with { IncludeBlanks = true }));
        Assert.True(Matches(" ", include with { Mode = "exclude" }));
        Assert.False(Matches("A", include with { Mode = "exclude" }));
        Assert.True(Matches("unlisted", include with { Mode = "exclude" }));
        Assert.True(Matches("anything", include with { Mode = "exclude", Values = [] }));
    }

    [Theory]
    [InlineData("-0.00000000000000000001", "lt", "0", null, true)]
    [InlineData("0", "gt", "0", null, false)]
    [InlineData("-0.00", "eq", "+.0", null, true)]
    [InlineData("123456789012345678901234567890.123456789", "eq", "123456789012345678901234567890.123456789", null, true)]
    [InlineData("123456789012345678901234567890.123456788", "eq", "123456789012345678901234567890.123456789", null, false)]
    [InlineData("-2.5", "between", "-2.50", "0", true)]
    [InlineData("0.00", "between", "-2.5", "0", true)]
    [InlineData("0.00000000000000000001", "between", "-2.5", "0", false)]
    public void Decimal_comparisons_preserve_sign_zero_and_exact_boundaries(string value, string op, string lower, string? upper, bool expected) =>
        Assert.Equal(expected, Matches(value, new("retroExtendedPrice", "compare", Operator: op, Value: lower, Upper: upper)));

    [Fact]
    public void Dates_are_inclusive_independent_and_blanks_are_explicit()
    {
        var filter = new ShipmentViewFilter("originalCommit", "dateRange", From: new(2026, 10, 1), To: new(2026, 10, 19));
        Assert.True(Matches("2026-10-01", filter)); Assert.True(Matches("2026-10-19", filter));
        Assert.False(Matches("2026-09-30", filter)); Assert.False(Matches("2026-10-20", filter));
        Assert.False(Matches(null, filter)); Assert.True(Matches(null, filter with { IncludeBlanks = true }));
        Assert.True(Matches("2027-01-01", filter with { To = null }));
    }

    [Fact]
    public void Invalid_columns_operators_duplicate_fields_lengths_and_precision_are_rejected()
    {
        var valid = new ShipmentViewFilter("itemNumber", "values", "include", ["A"]);
        foreach (var invalid in new[] {
            valid with { Column = "shipDate" }, valid with { Column = "unknown" }, valid with { Column = "retroUnitPrice" },
            valid with { Mode = "all" }, valid with { Values = ["A", "A"] }, valid with { Values = [""] },
            valid with { Values = ["control\n"] }, valid with { Values = [new string('x', 257)] },
            valid with { Values = Enumerable.Range(0, 1001).Select(i => i.ToString()).ToArray() },
            valid with { Text = "extra" }, valid with { Column = "onTime", Values = ["2"] },
            new("itemNumber", "contains", Text: ""), new("itemNumber", "dateRange"),
            new("invoicedQuantity", "compare", Operator: "eq", Value: "1e-3"),
            new("invoicedQuantity", "compare", Operator: "eq", Value: "1,000"),
            new("invoicedQuantity", "compare", Operator: "eq", Value: new string('1', 77)),
            new("invoicedQuantity", "compare", Operator: "eq", Value: "0." + new string('0', 76)),
            new("invoicedQuantity", "compare", Operator: "between", Value: "1", Upper: "0"),
            new("originalCommit", "dateRange", From: new(2026, 10, 20), To: new(2026, 10, 19)) })
            Assert.Throws<ArgumentException>(() => ShipmentViewRules.Validate([invalid]));
        Assert.Throws<ArgumentException>(() => ShipmentViewRules.Validate([valid, valid]));
        Assert.Throws<ArgumentException>(() => ShipmentViewRules.Validate(null));
    }

    [Fact]
    public void Columns_combine_with_AND_and_facet_predicate_excludes_only_its_own_column()
    {
        var filters = ShipmentViewRules.Validate([new("itemNumber", "values", "include", ["A"]), new("onTime", "values", "include", ["0"])]);
        var row = new ShipmentRow("id", ["B", "0"]);
        Assert.False(ShipmentViewRules.Predicate(["itemNumber", "onTime"], filters)(row));
        Assert.True(ShipmentViewRules.Predicate(["itemNumber", "onTime"], filters, "itemNumber")(row));
    }
}
