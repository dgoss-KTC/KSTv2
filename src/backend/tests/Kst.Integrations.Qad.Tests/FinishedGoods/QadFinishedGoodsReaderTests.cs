using Kst.Integrations.Qad.FinishedGoods;
using Kst.Integrations.Qad.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Integrations.Qad.Tests.FinishedGoods;

public sealed class QadFinishedGoodsReaderTests
{
    [Fact]
    public void QueryIsParameterizedPositiveDetailAndDomainPartMaxOnly()
    {
        const string part = "P'; DROP TABLE ld_det;--";
        var (sql, parameters) = QadFinishedGoodsReader.BuildBatchQuery("KTV", "KV", [part]);
        Assert.DoesNotContain(part, sql);
        Assert.Equal(part, parameters.Get<string>("Part0"));
        Assert.Equal("KTV", parameters.Get<string>("Domain"));
        Assert.Equal("KV", parameters.Get<string>("Site"));
        Assert.Contains("ld.ld_qty_oh > 0", sql);
        Assert.Contains("ld.ld_ref AS Reference", sql);
        Assert.Contains("MAX(inp_custprice)", sql);
        Assert.Contains("COUNT_BIG(*)", sql);
        Assert.Contains("COUNT_BIG(inp_custprice)", sql);
        Assert.Contains("GROUP BY inp_part, inp_domain", sql);
        foreach (var excluded in new[] { "DISTINCT", "pt_mstr", "vw_fin", "loc_mstr", "is_mstr", "inp_site", "inp_start_date", "inp_source", "pid_det", "sod_det", "RA%" })
            Assert.DoesNotContain(excluded, sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("JOIN Prices", sql[..sql.IndexOf("WITH ScopeParts", 10, StringComparison.Ordinal)]);
    }

    [Fact]
    public void MixedNullMaxAndDuplicateLookingReferencesComposeWithoutFanout()
    {
        var rows = QadFinishedGoodsReader.Normalize("KTC", "SW", ["P"],
            [Raw("R1"), Raw("R2"), Raw("R2")], [new("P", 4, 3, "2.75")]);
        Assert.Equal(3, rows.Count);
        Assert.Equal(["R1", "R2", "R2"], rows.Select(r => r.Reference));
        Assert.All(rows, r => { Assert.Equal(2.75m, r.UnitPrice); Assert.False(r.IsMissingPrice); });
    }

    [Theory]
    [InlineData(0, 0, null, true, "0")]
    [InlineData(1, 1, "0", false, "0")]
    [InlineData(3, 2, "-1", false, "-1")]
    public void MissingZeroAndNegativeMaxStayDistinct(long records, long values, string? maximum, bool missing, string expected)
    {
        var row = Assert.Single(QadFinishedGoodsReader.Normalize("KTC", "SW", ["P"], [Raw()], [new("P", records, values, maximum)]));
        Assert.Equal(missing, row.IsMissingPrice);
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), row.UnitPrice);
    }

    [Fact]
    public void AllNullIncompleteDuplicateAndMalformedPricesFail()
    {
        foreach (var prices in new IReadOnlyList<QadFinishedGoodsPriceRow>[] {
            [], [new("P", 1, 0, null)], [new("P", 1, 1, null)], [new("P", null, null, null)],
            [new("OTHER", 0, 0, null)], [new("P", 0, 0, null), new("P", 0, 0, null)] })
            Assert.Throws<InvalidDataException>(() => QadFinishedGoodsReader.Normalize("KTC", "SW", ["P"], [], prices));
        Assert.Throws<FormatException>(() => QadFinishedGoodsReader.Normalize("KTC", "SW", ["P"], [], [new("P", 1, 1, "bad")]));
    }

    [Fact]
    public void ScopeAndPositiveQuantityAreValidatedAgainAtBoundary()
    {
        foreach (var raw in new[] { Raw() with { Domain = "KTV" }, Raw() with { Site = "KV" }, Raw() with { Product = "OTHER" },
            Raw() with { OnHand = "0" }, Raw() with { OnHand = "-3" }, Raw() with { OnHand = null } })
            Assert.Throws<InvalidDataException>(() => QadFinishedGoodsReader.Normalize("KTC", "SW", ["P"], [raw], [new("P", 0, 0, null)]));
    }

    [Fact]
    public async Task EmptyScopeNeverOpensConnectionEvenWhenUnconfigured()
    {
        var reader = new QadFinishedGoodsReader(new QadConnectionOptions(), NullLogger<QadFinishedGoodsReader>.Instance);
        Assert.Empty(await reader.ReadAsync("SW", [], default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadAsync("SW", ["P"], default));
    }

    [Fact]
    public void CaseVariantsOfScopedSiteAndDomainAreAcceptedWithoutCollapsingRows()
    {
        var rows = QadFinishedGoodsReader.Normalize("KTC", "SW", ["P"],
            [Raw("R1") with { Site = "sw" }, Raw("R2") with { Domain = "ktc", Site = "Sw" }],
            [new("P", 1, 1, "1.23456")]);
        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => { Assert.Equal("SW", row.Site); Assert.Equal("KTC", row.Domain); Assert.Equal("ship", row.Location); });
        Assert.Equal(["R1", "R2"], rows.Select(row => row.Reference));
    }

    [Fact]
    public void GuardDiagnosticsUseOnlyStaticCodesAndPreserveAllNullError()
    {
        var error = Assert.Throws<InvalidDataException>(() => QadFinishedGoodsReader.Normalize("KTC", "SW", ["SECRET-PART"], [],
            [new("SECRET-PART", 1, 0, null)]));
        Assert.Equal(Kst.Domain.FinishedGoods.FinishedGoodsFailureReason.PriceAllNull,
            Kst.Domain.FinishedGoods.FinishedGoodsFailure.Reason(error));
        Assert.DoesNotContain("SECRET-PART", error.ToString());
        Assert.Equal(Kst.Domain.FinishedGoods.FinishedGoodsFailureReason.Unknown,
            Kst.Domain.FinishedGoods.FinishedGoodsFailure.Reason(new Exception("private SQL")));
    }

    private static QadFinishedGoodsRawRow Raw(string? reference = null) => new("KTC", "SW", "P", "ship", "ra1", reference, "2");
}
