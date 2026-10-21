using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using ClosedXML.Excel;
using Kst.Api.Dtos;
using Kst.Application.ComponentMrp;
using Kst.Domain.Common;
using Kst.Domain.ComponentMrp;
using Kst.Domain.LongTermShortages;
using Kst.Exports;
using Microsoft.Extensions.DependencyInjection;

namespace Kst.Api.IntegrationTests;

public class ComponentMrpScreenTests
{
    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.Parse("2026-09-29T12:00Z");
        public DateTimeOffset LocalNow => UtcNow;
    }
    private static LongTermMrpFact Fact(string type, string due, decimal qty, bool? confirmed = null, string? release = null) =>
        new(1, type, DateOnly.Parse(due), release is null ? null : DateOnly.Parse(release), qty, MrpScheduleCategory.Unclassified)
        { IsPoReceipt = confirmed is not null, PoConfirmed = confirmed };
    private static ComponentMrpSource Source(string part = "EXACT", string uom = "KG") => new(new(part, uom, "A", "Synthetic", null, "buyer", 1.005m,
        SafetyStockState.Resolved, 0, [], [Fact("DEMAND", "2026-09-28", 2), Fact("SUPPLY", "2026-09-28", 50, false),
            Fact("DEMAND", "2026-09-29", .004m), Fact("DEMAND", "2026-09-30", .004m), Fact("SUPPLY", "2026-09-30", 3, true),
            Fact("SUPPLY", "2026-10-01", 5, false), Fact("SUPPLY", "2026-10-02", -5, false),
            Fact("SUPPLYP", "2026-10-03", 7, release: "2026-09-30"), Fact("DEMAND", "2026-10-04", 4)]),
        new(null, null, null, null, null, .00152m), DateTimeOffset.Parse("2026-09-29T12:00Z"));
    private const string Root = "/api/v1/reports/component-mrp";
    private static HttpClient Client(KstApiFactory factory, string? owner = null)
    {
        var client = factory.CreateClient(); client.DefaultRequestHeaders.Add("X-KST-Report-Session", owner ?? Guid.NewGuid().ToString()); return client;
    }
    private static ComponentMrpRequestDto Request(string? id = null) => new("SW", "EXACT", 13, true, "Week", id, id is null ? null : new(2026, 9, 29));
    private static decimal[] Values(ComponentMrpPeriodDto p) => [decimal.Parse(p.Demand, CultureInfo.InvariantCulture), decimal.Parse(p.Receipts, CultureInfo.InvariantCulture),
        decimal.Parse(p.ProjectedQoh, CultureInfo.InvariantCulture), decimal.Parse(p.PlannedDue, CultureInfo.InvariantCulture), decimal.Parse(p.PlannedRelease, CultureInfo.InvariantCulture)];

    [Theory]
    [InlineData("Day", false)]
    [InlineData("Day", true)]
    [InlineData("Week", false)]
    [InlineData("Week", true)]
    [InlineData("Month", false)]
    [InlineData("Month", true)]
    public async Task Screen_and_actual_workbook_match_independent_expected_values_without_prepare_reads(string period, bool include)
    {
        var reads = 0; var clock = new Clock();
        await using var factory = new KstApiFactory
        {
            Clock = clock,
            ComponentMrpReader = new DelegateComponentMrpReader((_, _, _, _) =>
            { reads++; return Task.FromResult<ComponentMrpSource?>(Source()); })
        };
        using var client = Client(factory);
        var first = await client.PostAsJsonAsync(Root + "/projection", Request());
        first.EnsureSuccessStatusCode(); var held = (await first.Content.ReadFromJsonAsync<ComponentMrpScreenDto>())!;
        var selection = Request(held.AcquisitionId) with { Period = period, IncludeUnconfirmed = include };
        var response = await client.PostAsJsonAsync(Root + "/projection", selection); response.EnsureSuccessStatusCode();
        var screen = (await response.Content.ReadFromJsonAsync<ComponentMrpScreenDto>())!;
        Assert.Equal(held.AcquisitionId, screen.AcquisitionId); Assert.Equal(held.AcquiredAtUtc, screen.AcquiredAtUtc);
        Assert.Equal("-0.995", screen.AdjustedOpeningQoh); Assert.Equal(new DateOnly(2026, 12, 27), screen.ForecastEndExclusive);
        var expected = new Dictionary<string, decimal[]>
        {
            ["Past Due"] = [2, include ? 50 : 0, -.995m, 0, 0]
        };
        if (period == "Day")
        {
            expected["2026-09-29"] = [.004m, 0, -.999m, 0, 0];
            expected["2026-09-30"] = [.004m, 3, 1.997m, 0, 7];
            expected["2026-10-01"] = [0, include ? 5 : 0, include ? 6.997m : 1.997m, 0, 0];
            expected["2026-10-02"] = [0, include ? -5 : 0, 1.997m, 0, 0];
            expected["2026-10-03"] = [0, 0, 1.997m, 7, 0];
            expected["2026-10-04"] = [4, 0, -2.003m, 0, 0];
        }
        else if (period == "Week")
        {
            expected["2026-09-28"] = [.008m, 3, 1.997m, 7, 7];
            expected["2026-10-05"] = [4, 0, -2.003m, 0, 0];
            Assert.Equal(new DateOnly(2026, 9, 29), screen.Periods[1].Start);
            Assert.Equal(new DateOnly(2026, 10, 4), screen.Periods[1].EndExclusive);
        }
        else
        {
            expected["2026-09 *"] = [.008m, 3, 1.997m, 0, 7];
            expected["2026-10"] = [4, 0, -2.003m, 7, 0];
            Assert.Equal("2026-12 *", screen.Periods[^1].Label);
        }
        var preparedResponse = await client.PostAsJsonAsync(Root + "/screen-preparations", selection); preparedResponse.EnsureSuccessStatusCode();
        var prepared = (await preparedResponse.Content.ReadFromJsonAsync<ComponentMrpPreparedDto>())!;
        Assert.Equal("EXACT_MRP_SW_2026-09-29_01.xlsx", prepared.FileName);
        var bytes = await client.GetByteArrayAsync($"{Root}/preparations/{prepared.PreparationId}/bytes");
        var evidenceRoot = Environment.GetEnvironmentVariable("KST_MRP_SCREEN_EVIDENCE_ROOT");
        if (evidenceRoot is not null) await File.WriteAllBytesAsync(Path.Combine(evidenceRoot, $"screen-{period}-{include}.xlsx"), bytes);
        using var book = new XLWorkbook(new MemoryStream(bytes)); var sheet = book.Worksheet("Component MRP"); Assert.Single(book.Worksheets);
        for (var i = 0; i < screen.Periods.Count; i++)
        {
            var p = screen.Periods[i]; var values = expected.GetValueOrDefault(p.Label, [0, 0, -2.003m, 0, 0]);
            Assert.Equal(values, Values(p)); Assert.Equal(p.Label, sheet.Cell(ComponentMrpWorkbook.HeadingRow, i + 2).GetString());
            for (var j = 0; j < 5; j++) Assert.Equal((double)Math.Round(values[j], 2, MidpointRounding.AwayFromZero), sheet.Cell(ComponentMrpWorkbook.FirstMetricRow + j, i + 2).GetDouble());
        }
        Assert.Equal(include, screen.Periods[0].HasUnconfirmed);
        if (period == "Month") Assert.Equal(include, screen.Periods[2].HasUnconfirmed); // +5/-5 still classified
        Assert.Equal(1, reads);
    }

    [Fact]
    public async Task Unknown_foreign_mismatched_and_expired_tokens_fail_closed_but_prepared_bytes_survive()
    {
        var reads = 0; var clock = new Clock();
        await using var factory = new KstApiFactory
        {
            Clock = clock,
            ComponentMrpReader = new DelegateComponentMrpReader((_, _, _, _) =>
            { reads++; return Task.FromResult<ComponentMrpSource?>(Source()); })
        };
        using var client = Client(factory); using var foreign = Client(factory);
        var response = await client.PostAsJsonAsync(Root + "/projection", Request());
        var data = (await response.Content.ReadFromJsonAsync<ComponentMrpScreenDto>())!; var request = Request(data.AcquisitionId);
        foreach (var invalid in new[] { request with { Site = "KV" }, request with { Part = "OTHER" }, request with { HorizonWeeks = 26 },
            request with { AsOfDate = new(2026, 9, 28) }, request with { AcquisitionId = Guid.NewGuid().ToString() }, request with { AcquisitionId = "bad" }, request with { AcquisitionId = null } })
            Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync(Root + "/screen-preparations", invalid)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await foreign.PostAsJsonAsync(Root + "/screen-preparations", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await foreign.PostAsJsonAsync(Root + "/projection", request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Root, request)).StatusCode);
        var preparedResponse = await client.PostAsJsonAsync(Root + "/screen-preparations", request);
        var prepared = (await preparedResponse.Content.ReadFromJsonAsync<ComponentMrpPreparedDto>())!;
        var bytes = await client.GetByteArrayAsync($"{Root}/preparations/{prepared.PreparationId}/bytes");
        clock.UtcNow += ComponentMrpAcquisitions.Lifetime;
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync(Root + "/screen-preparations", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync(Root + "/projection", request)).StatusCode);
        Assert.Equal(bytes, await client.GetByteArrayAsync($"{Root}/preparations/{prepared.PreparationId}/bytes")); Assert.Equal(1, reads);
        var refreshed = await client.PostAsJsonAsync(Root + "/projection", Request()); refreshed.EnsureSuccessStatusCode(); Assert.Equal(2, reads);
    }

    [Fact]
    public async Task Store_bounds_replacement_expiry_business_date_and_evidence_capacity()
    {
        var clock = new Clock(); var store = new ComponentMrpAcquisitions(clock);
        var report = (await new ComponentMrpService(new DelegateComponentMrpReader((_, _, _, _) => Task.FromResult<ComponentMrpSource?>(Source())))
            .GenerateAsync("SW", "EXACT", 13, true, new(2026, 9, 29), default))!;
        var first = store.Add("owner", report); var second = store.Add("owner", report);
        Assert.Null(store.Get(first.Id, "owner", "SW", "EXACT", 13, report.AsOfDate));
        Assert.NotNull(store.Get(second.Id, "owner", "SW", "EXACT", 13, report.AsOfDate));
        for (var i = 1; i < ComponentMrpAcquisitions.MaximumOwners; i++) store.Add("other" + i, report);
        Assert.Throws<InvalidOperationException>(() => store.Add("ninth", report));
        var oversized = report with { Source = report.Source with { Input = report.Source.Input with { Evidence = Enumerable.Repeat(report.Source.Input.Evidence[0], 100_001).ToArray() } } };
        Assert.Throws<InvalidOperationException>(() => store.Add("owner", oversized));
        var hugeText = report with { Source = report.Source with { Input = report.Source.Input with { Description = new string('x', ComponentMrpAcquisitions.MaximumSourceBytes) } } };
        Assert.Throws<InvalidOperationException>(() => store.Add("owner", hugeText));
        var byteStore = new ComponentMrpAcquisitions(clock);
        var largeText = report with { Source = report.Source with { Input = report.Source.Input with { Description = new string('x', 6 * 1024 * 1024) } } };
        byteStore.Add("one", largeText); byteStore.Add("two", largeText);
        Assert.Throws<InvalidOperationException>(() => byteStore.Add("three", largeText));
        clock.UtcNow = clock.UtcNow.AddDays(1); Assert.Null(store.Get(second.Id, "owner", "SW", "EXACT", 13, report.AsOfDate));
        store.Add("ninth", report);
        clock.UtcNow = DateTimeOffset.Parse("2026-09-29T23:59Z"); var rollover = store.Add("rollover", report);
        clock.UtcNow = clock.UtcNow.AddMinutes(2);
        Assert.True(rollover.Entry.Expires > clock.UtcNow);
        Assert.Null(store.Get(rollover.Id, "rollover", "SW", "EXACT", 13, report.AsOfDate));
    }

    [Theory]
    [InlineData(13, "Day", 90)]
    [InlineData(26, "Week", 27)]
    [InlineData(52, "Month", 14)]
    [InlineData(72, "Day", 503)]
    public async Task Supported_horizons_keep_server_bounds_and_bounded_widest_screen(int weeks, string period, int count)
    {
        var calls = 0;
        await using var factory = new KstApiFactory
        {
            Clock = new Clock(),
            ComponentMrpReader = new DelegateComponentMrpReader((_, _, end, _) =>
            { calls++; Assert.Equal(new DateOnly(2026, 9, 27).AddDays(weeks * 7), end); return Task.FromResult<ComponentMrpSource?>(Source()); })
        };
        using var client = Client(factory);
        var response = await client.PostAsJsonAsync(Root + "/projection", Request() with { HorizonWeeks = weeks, Period = period }); response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsByteArrayAsync(); Assert.True(json.Length < 512 * 1024);
        var data = (await response.Content.ReadFromJsonAsync<ComponentMrpScreenDto>())!;
        Assert.Equal(count, data.Periods.Count); Assert.Equal(data.ForecastEndExclusive, data.Periods[^1].EndExclusive); Assert.Equal(1, calls);
        var limiter = factory.Services.GetRequiredService<ComponentMrpPreparations>().Generations;
        await limiter.WaitAsync(); await limiter.WaitAsync();
        try { Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync(Root + "/projection", Request())).StatusCode); Assert.Equal(1, calls); }
        finally { limiter.Release(2); }
    }

    [Fact]
    public async Task Zero_flows_are_a_real_projection_missing_master_and_failure_remain_distinct()
    {
        await using var factory = new KstApiFactory
        {
            Clock = new Clock(),
            ComponentMrpReader = new DelegateComponentMrpReader((_, part, _, _) =>
            part == "missing" ? Task.FromResult<ComponentMrpSource?>(null) : part == "error" ? throw new Exception("secret")
            : Task.FromResult<ComponentMrpSource?>(Source() with { Input = Source().Input with { OpeningQoh = 1.5m, UnitOfMeasure = "EA", Evidence = [] } }))
        };
        using var client = Client(factory);
        var response = await client.PostAsJsonAsync(Root + "/projection", Request()); response.EnsureSuccessStatusCode();
        var data = (await response.Content.ReadFromJsonAsync<ComponentMrpScreenDto>())!; Assert.Equal(14, data.Periods.Count); Assert.Equal("1.5", data.Periods[1].ProjectedQoh);
        var preparedResponse = await client.PostAsJsonAsync(Root + "/screen-preparations", Request(data.AcquisitionId));
        var prepared = (await preparedResponse.Content.ReadFromJsonAsync<ComponentMrpPreparedDto>())!;
        using var book = new XLWorkbook(new MemoryStream(await client.GetByteArrayAsync($"{Root}/preparations/{prepared.PreparationId}/bytes")));
        Assert.Equal(2, book.Worksheet(1).Cell(ComponentMrpWorkbook.FirstMetricRow + 2, 3).GetDouble());
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Root + "/projection", Request() with { Part = "missing" })).StatusCode);
        var failed = await client.PostAsJsonAsync(Root + "/projection", Request() with { Part = "error" }); Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.DoesNotContain("secret", await failed.Content.ReadAsStringAsync());
    }
}
