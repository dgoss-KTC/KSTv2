using System.Diagnostics;
using Kst.Integrations.Shortages.ComponentOrders;
using Xunit.Abstractions;

namespace Kst.Integrations.Qad.Tests.Shortages;

public sealed class ShortageCommentParserTests(ITestOutputHelper output)
{
    private const string Example = "1/31/2022 1:30 PM jdoe  Resolved with 7500 pc buy from TI Online Store. PO 1974131.    12/8/2021 11:10 AM psmith      12/8/2021 11:10 AM psmith  updated notes......blah  blah    12/8/2021 11:09 AM psmith  short part mc test";

    [Fact]
    public void SuppliedExampleUsesFirstBlockOnlyWithUnspecifiedWallClock()
    {
        var result = ShortageCommentParser.Parse(Example, "fallback");
        Assert.Equal("Resolved with 7500 pc buy from TI Online Store. PO 1974131.", result.Text);
        Assert.Equal(new DateTime(2022, 1, 31, 13, 30, 0), result.RecordedAt);
        Assert.Equal(DateTimeKind.Unspecified, result.RecordedAt!.Value.Kind);
        Assert.Equal("jdoe", result.Author);
    }

    [Theory]
    [InlineData("1/31/2022 1:30 PM jdoe  only entry", "only entry")]
    [InlineData("1/31/2022 1:30 PM jdoe  first  2/1/2023 2:45 PM newer  not selected", "first")]
    [InlineData("1/31/2022 1:30 PM jdoe", "")]
    [InlineData("1/31/2022 1:30 PM jdoe   1/31/2022 1:30 PM jdoe older", "")]
    [InlineData("1/31/2022 1:30 PM jdoe\r\n\r\n12/8/2021 11:10 AM psmith older", "")]
    [InlineData("  1/31/2022\t1:30\tpm\tjdoe  line one\r\n  indented line\r\n\r\nlast line  \r\n12/8/2021 11:10 AM psmith old", "line one\r\n  indented line\r\n\r\nlast line")]
    [InlineData("1/31/2022 1:30 PM jdoe  due 2/1/2022 and 12/8/2021 11:10; ask buyer", "due 2/1/2022 and 12/8/2021 11:10; ask buyer")]
    [InlineData("1/31/2022 1:30 PM jdoe  invalid 2/30/2022 1:30 PM x remains prose", "invalid 2/30/2022 1:30 PM x remains prose")]
    public void KeepsFirstBodyIncludingEmptyAndMeaningfulInteriorFormatting(string history, string expected)
    {
        var result = ShortageCommentParser.Parse(history, "must not resurrect");
        Assert.Equal(expected, result.Text);
        Assert.Equal("jdoe", result.Author);
        Assert.NotNull(result.RecordedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \r\n ")]
    [InlineData("unrecognized 1/31/2022 1:30 PM jdoe content")]
    [InlineData("1/31/2022 1:30 PM")]
    [InlineData("2/30/2022 1:30 PM jdoe invalid")]
    [InlineData("1/31/2022 13:30 PM jdoe invalid")]
    [InlineData("1/31/2022 1:99 PM jdoe invalid")]
    [InlineData("1/31/0000 1:30 PM jdoe invalid")]
    public void UnrecognizedLeadingHistoryKeepsUndatedFallbackExactly(string? history)
    {
        var result = ShortageCommentParser.Parse(history, "  fallback\r\nsecond  ");
        Assert.Equal("  fallback\r\nsecond  ", result.Text);
        Assert.Null(result.RecordedAt);
        Assert.Null(result.Author);
        Assert.Null(ShortageCommentParser.Parse(history, null).Text);
    }

    [Theory]
    [InlineData("12:00 AM", 0)]
    [InlineData("12:00 PM", 12)]
    public void HandlesTwelveHourClock(string time, int hour)
    {
        Assert.Equal(hour, ShortageCommentParser.Parse($"1/1/2022 {time} user text", null).RecordedAt!.Value.Hour);
    }

    [Fact]
    public void LongSyntheticHistoryTimingSeparatesEarlyBoundaryFromLongBodyAndMalformedScan()
    {
        var older = string.Concat(Enumerable.Repeat("\r\n12/8/2021 11:10 AM psmith older synthetic block", 100_000));
        var longHistory = "1/31/2022 1:30 PM jdoe newest" + older;
        var longBody = new string('x', 1_000_000) + " ordinary date 12/8/2021 in prose";
        var cases = new[]
        {
            (Name: "early-boundary", History: longHistory, Expected: "newest", Repeats: 1000),
            (Name: "long-newest-body", History: "1/31/2022 1:30 PM jdoe " + longBody + older, Expected: longBody, Repeats: 10),
            (Name: "malformed-leading", History: new string(' ', 1_000_000) + "not a header", Expected: "fallback", Repeats: 10)
        };
        foreach (var item in cases)
        {
            _ = ShortageCommentParser.Parse(item.History, "fallback"); // warm runtime regex engine
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            for (var i = 0; i < item.Repeats; i++)
            {
                var result = ShortageCommentParser.Parse(item.History, "fallback");
                Assert.Equal(item.Expected, result.Text);
            }
            timer.Stop();
            output.WriteLine("{0}: chars={1}, repeats={2}, elapsedMs={3:F3}, allocatedBytes={4}",
                item.Name, item.History.Length, item.Repeats, timer.Elapsed.TotalMilliseconds,
                GC.GetAllocatedBytesForCurrentThread() - allocated);
        }
        // These are parser-only synthetic costs. No database transfer time or production payload is measured.
    }
}
