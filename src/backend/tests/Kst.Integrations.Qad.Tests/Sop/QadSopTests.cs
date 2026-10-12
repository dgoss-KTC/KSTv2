using System.Data;
using Kst.Domain.Sop;
using Kst.Integrations.Qad.Sop;

namespace Kst.Integrations.Qad.Tests.Sop;

public class QadSopTests
{
    private static DataTable Table(params object[][] rows)
    {
        var table = new DataTable();
        foreach (var name in new[] { "Customer", "Part", "St", "Planr", "Prod", "type", "Time Std", " Falldown", "2026-10-04", "2026-10-11", "2026-10-18" }) table.Columns.Add(name, typeof(object));
        foreach (var row in rows) table.Rows.Add(row);
        return table;
    }
    [Fact]
    public async Task Fixed_output_cells_delete_raw_I_and_preserve_null_zero_text_numbers()
    {
        using var t = Table(["=source", "0007", "C", DBNull.Value, 2140, "SUPPLY", 1.25m, 99m, "discarded uninterpreted", 0m, ""]);
        using var reader = t.CreateDataReader();
        var source = await QadSopReader.ParseAsync(reader, default);
        Assert.Equal(new[] { new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 19) }, source.WeekLabels);
        var r = Assert.Single(source.Rows);
        Assert.Equal("=source", r.Fixed[0].Text); Assert.Equal("0007", r.Fixed[1].Text);
        Assert.Equal(SopCell.Blank, r.Fixed[3]); Assert.Equal(2140, r.Fixed[4].Number);
        Assert.Equal(1.25, r.Fixed[6].Number); Assert.Equal(99, r.Fixed[7].Number);
        Assert.Equal(0, r.Weeks[0].Number); Assert.Equal("", r.Weeks[1].Text);
    }
    [Theory]
    [InlineData("2026-10-11", true)]
    [InlineData("10/11/2026", true)]
    [InlineData("11/10/2026", false)]
    [InlineData("2026-10-12", false)]
    [InlineData("10/11/26", false)]
    [InlineData("October 11 2026", false)]
    public void Deterministic_date_headers(string header, bool valid)
    {
        if (valid) Assert.Equal(new DateOnly(2026, 10, 11), QadSopReader.ParseWeekHeader(header));
        else Assert.Throws<InvalidOperationException>(() => QadSopReader.ParseWeekHeader(header));
    }
    [Fact]
    public async Task Malformed_schema_values_extra_results_and_cancel_fail()
    {
        using var t = Table(); t.Columns[0].ColumnName = "Other";
        using var bad = t.CreateDataReader(); await Assert.ThrowsAsync<InvalidOperationException>(() => QadSopReader.ParseAsync(bad, default));
        using var v = Table(["A", "P", "C", "B", 1, "SUPPLY", 0, 0, 0, double.NaN, 0]);
        using var values = v.CreateDataReader(); await Assert.ThrowsAsync<InvalidOperationException>(() => QadSopReader.ParseAsync(values, default));
        using var second = new DataTableReader([Table(), Table()]); await Assert.ThrowsAsync<InvalidOperationException>(() => QadSopReader.ParseAsync(second, default));
        using var valid = Table(); using var canceled = valid.CreateDataReader();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => QadSopReader.ParseAsync(canceled, new CancellationToken(true)));
    }
    [Fact]
    public async Task Volume_limit_rejects_instead_of_truncating()
    {
        using var table = Table();
        for (var i = 0; i <= SopRules.MaxRows; i++) table.Rows.Add("A", "P", "C", "B", 1, "SUPPLY", 0, 0, 0, 1, 2);
        using var reader = table.CreateDataReader();
        await Assert.ThrowsAsync<InvalidOperationException>(() => QadSopReader.ParseAsync(reader, default));
    }
    [Fact]
    public async Task Ordered_overlap_empty_failure_and_cancellation_never_partial_success()
    {
        var source = new SopSource(["Customer", "Part", "St", "Planr", "Prod", "type", "Time Std", " Falldown"], [new(2026, 10, 12)], [new(Enumerable.Repeat(new SopCell(Text: "P"), 8).ToArray(), [new(Number: 7)])]);
        SopRange[] pairs = [new("2100", "2102"), new("2102", "2104"), new("2100", "2102")];
        var seen = new List<SopRange>();
        var result = await QadSopReader.ReadPairsAsync(pairs, (r, _) => { seen.Add(r); return Task.FromResult(source); }, default);
        Assert.Equal(pairs, seen); Assert.Equal(3, result.Rows.Count); Assert.Equal(21, result.Rows.Sum(r => r.Weeks[0].Number));
        var calls = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => QadSopReader.ReadPairsAsync(pairs, (_, _) => ++calls == 2 ? throw new InvalidOperationException("failure") : Task.FromResult(source), default));
        Assert.Equal(2, calls);
        var empty = await QadSopReader.ReadPairsAsync(pairs, (_, _) => Task.FromResult(source with { Rows = [] }), default); Assert.Empty(empty.Rows);
        calls = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => QadSopReader.ReadPairsAsync(pairs, (_, _) => { calls++; return Task.FromResult(source); }, new CancellationToken(true))); Assert.Equal(0, calls);
        calls = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => QadSopReader.ReadPairsAsync(pairs, (_, _) => Task.FromResult(++calls == 2 ? source with { WeekLabels = [new(2026, 10, 19)] } : source), default));
    }
}
