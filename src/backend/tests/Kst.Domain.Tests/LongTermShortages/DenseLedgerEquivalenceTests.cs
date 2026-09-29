using System.Text.Json;
using Kst.Domain.LongTermShortages;

namespace Kst.Domain.Tests.LongTermShortages;

public sealed class DenseLedgerEquivalenceTests
{
    [Theory]
    [InlineData(13)]
    [InlineData(26)]
    [InlineData(52)]
    [InlineData(72)]
    public void BothModes_PreserveIndexedStateTransitionsAndDecimalOperationOrder(int weeks)
    {
        var date = new DateOnly(2026, 9, 24);
        var random = new Random(110929);
        var inputs = Enumerable.Range(0, 100).Select(sample =>
        {
            var facts = Enumerable.Range(0, 70).Select(id =>
            {
                var type = new[] { "DEMAND", "SUPPLY", "SUPPLYP", "SUPPLYF" }[random.Next(4)];
                return new LongTermMrpFact(id, type, date.AddDays(random.Next(-20, 520)), date.AddDays(random.Next(-20, 520)),
                    random.Next(-100, 1000) / 1000m, MrpScheduleCategory.Unclassified)
                { SourceRowId = $"{sample}-{id}", SourceLine2 = id.ToString(), IsPoReceipt = type == "SUPPLY" && random.Next(2) == 0,
                  PoConfirmed = type == "SUPPLY" ? random.Next(2) == 0 : null };
            }).ToArray();
            return new LongTermShortageInput($"COMP-{sample}", sample % 2 == 0 ? "EA" : "KG", null, null, null, null,
                random.Next(-100, 100) / 100m, SafetyStockState.Resolved, sample % 3 == 0 ? null : 1.234m, [], facts);
        }).ToList();
        // Magnitude/scale mixtures expose regrouping that normal quantities can hide. Expected
        // behavior comes exclusively from the preserved indexed implementation, not invented values.
        inputs.Add(new("DECIMAL-ORDER", "KG", null, null, null, null, 100000000000000000000m,
            SafetyStockState.Resolved, 0m, [], [
                new(1, "DEMAND", date, null, 0.1234567890123456789m, MrpScheduleCategory.Unclassified),
                new(2, "SUPPLY", date, null, 1.234567890123456789m, MrpScheduleCategory.Unclassified),
                new(3, "SUPPLY", date, null, -1.234567890123456788m, MrpScheduleCategory.Unclassified) { IsPoReceipt = true, PoConfirmed = true },
                new(4, "DEMAND", date.AddDays(1), null, 99999999999999999999m, MrpScheduleCategory.Unclassified)]));
        var pair = LongTermShortagesBuilder.BuildBoth(date, inputs, weeks);
        Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(IndexedProjectionReference.Build(date, inputs, weeks)), JsonSerializer.SerializeToUtf8Bytes(pair.Confirmed));
        Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(IndexedProjectionReference.Build(date, inputs, weeks, true)), JsonSerializer.SerializeToUtf8Bytes(pair.All));
        Assert.Throws<NotSupportedException>(() => ((IList<LongTermShortageRow>)pair.Confirmed).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<LongTermMrpFact>)pair.Confirmed[0].Evidence).Clear());
    }
}
