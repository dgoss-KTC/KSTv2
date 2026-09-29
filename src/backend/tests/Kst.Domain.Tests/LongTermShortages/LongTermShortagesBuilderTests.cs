using Kst.Domain.LongTermShortages;
namespace Kst.Domain.Tests.LongTermShortages;
public sealed class LongTermShortagesBuilderTests
{
    [Fact]
    public void Build_UsesSundayWeeks_PastDemand_AndPlanningOnlyReceipts()
    {
        var row = Assert.Single(LongTermShortagesBuilder.Build(new DateOnly(2026, 9, 15), [Input(200m, 0m,
            new(1, "DEMAND", new(2026, 9, 10), null, 98.2456140337m, MrpScheduleCategory.Unclassified),
            new(2, "SUPPLYP", new(2026, 9, 22), new(2026, 9, 18), 49m, MrpScheduleCategory.Unclassified),
            new(3, "SUPPLYP", null, new(2026, 9, 18), 11m, MrpScheduleCategory.Unclassified))]));

        Assert.Equal(new DateOnly(2026, 9, 13), row.Weeks[0].WeekStart);
        Assert.Equal(26, row.Weeks.Count);
        Assert.Equal(new DateOnly(2027, 3, 7), row.Weeks[^1].WeekStart);
        Assert.Equal(101.7543859663m, row.Past.ProjectedQoh);
        Assert.Equal(49m, row.Weeks[1].PlannedOrdersDue);
        Assert.Equal(101.7543859663m, row.Weeks[1].ConfirmedEnding);
        Assert.Equal(150.7543859663m, row.Weeks[1].PlanningEnding);
        Assert.Equal(60m, row.Weeks[0].PlannedOrdersRelease);
        Assert.Contains(row.Evidence, fact => fact.IsPlannedOrderReleaseEvidence && fact.Quantity == 11m);
    }

    [Fact]
    public void Build_PreservesMatchingDemandRowsAndRecognizesOpeningShortage()
    {
        var row = Assert.Single(LongTermShortagesBuilder.Build(new DateOnly(2026, 9, 15), [Input(10m, 5m,
            new(1, "DEMAND", new(2026, 9, 1), null, 20m, MrpScheduleCategory.Unclassified),
            new(2, "SUPPLY", new(2026, 9, 15), null, 20m, MrpScheduleCategory.Unclassified),
            new(3, "DEMAND", new(2026, 9, 16), null, 2m, MrpScheduleCategory.Unclassified),
            new(4, "DEMAND", new(2026, 9, 16), null, 2m, MrpScheduleCategory.Unclassified))]));

        Assert.Equal(-10m, row.Past.ProjectedQoh);
        Assert.Equal(6m, row.Weeks[0].ProjectedQoh);
        Assert.Equal(LongTermShortageSeverity.CriticalShort, row.Severity);
        Assert.Equal(4m, row.Weeks[0].GrossRequirements);
        Assert.Equal(4, row.Evidence.Count);
    }

    [Fact]
    public void Build_SelectedSiteNullSafetyDoesNotFallback()
    {
        var row = Assert.Single(LongTermShortagesBuilder.Build(new DateOnly(2026, 9, 15), [new LongTermShortageInput("COMP", null, null, null, null, null, 0m, SafetyStockState.SelectedSiteValueMissing, null, [], [])]));
        Assert.Equal(LongTermShortageSeverity.SafetyStockUnavailable, row.Severity);
    }

    [Fact]
    public void Build_PresentationContextDoesNotAlterRawMrpBalance()
    {
        var facts = new[] { new LongTermMrpFact(1, "DEMAND", new(2026, 9, 15), null, 4m, MrpScheduleCategory.Unclassified) };
        var raw = Assert.Single(LongTermShortagesBuilder.Build(new DateOnly(2026, 9, 15), [Input(10m, 0m, facts)]));
        var contextual = Assert.Single(LongTermShortagesBuilder.Build(new DateOnly(2026, 9, 15), [Input(10m, 0m, facts) with
        {
            Presentation = new LongTermShortagePresentationContext("MFG-1", "PO-1", 2, new DateOnly(2026, 9, 16), 999m, true, true)
        }]));

        Assert.Equal(raw.Past, contextual.Past);
        Assert.Equal(raw.Weeks, contextual.Weeks);
        Assert.Equal(raw.Evidence, contextual.Evidence);
        Assert.Equal(6m, contextual.Weeks[0].ProjectedQoh);
        Assert.True(contextual.Presentation!.IsKss);
    }

    [Fact]
    public void Build_DistinctDemand_DemandFirst_AndIndependentPoModes()
    {
        var row = Assert.Single(LongTermShortagesBuilder.Build(new(2026, 9, 15), [Input(5m, 0m,
            new(1, "DEMAND", new(2026, 9, 15), null, 7m, MrpScheduleCategory.Unclassified) { SourceLine2 = "1" },
            new(2, "DEMAND", new(2026, 9, 15), null, 7m, MrpScheduleCategory.Unclassified) { SourceLine2 = "2" },
            new(3, "SUPPLY", new(2026, 9, 15), null, 8m, MrpScheduleCategory.Unclassified) { IsPoReceipt = true, PoConfirmed = true },
            new(4, "SUPPLY", new(2026, 9, 15), null, 3m, MrpScheduleCategory.Unclassified) { IsPoReceipt = true, PoConfirmed = false },
            new(5, "SUPPLY", new(2026, 9, 15), null, 1m, MrpScheduleCategory.Unclassified))]));
        Assert.Equal(14m, row.Weeks[0].GrossRequirements);
        Assert.Equal(-9m, row.Weeks[0].LowestProjectedBalance);
        Assert.Equal(0m, row.Weeks[0].ConfirmedEnding);
        Assert.Equal(3m, row.Weeks[0].AllReceiptsEnding);
        Assert.Equal(3m, row.Weeks[0].UnconfirmedReceipts);
        Assert.Equal(5, row.Evidence.Count);
        var withUnconfirmed = Assert.Single(LongTermShortagesBuilder.Build(new(2026, 9, 15), [Input(5m, 0m,
            new(1, "DEMAND", new(2026, 9, 15), null, 14m, MrpScheduleCategory.Unclassified),
            new(2, "SUPPLY", new(2026, 9, 15), null, 9m, MrpScheduleCategory.Unclassified),
            new(3, "SUPPLY", new(2026, 9, 15), null, 3m, MrpScheduleCategory.Unclassified) { IsPoReceipt = true, PoConfirmed = false })], includeUnconfirmed: true));
        Assert.Equal(3m, withUnconfirmed.Weeks[0].ProjectedQoh);
        Assert.Equal(0m, withUnconfirmed.Weeks[0].ConfirmedEnding);
    }

    [Fact]
    public void Build_PastReceiptsAndSupplyFDoNotCoverDemand_KssDoesNotChangeLedger()
    {
        var row = Assert.Single(LongTermShortagesBuilder.Build(new(2026, 9, 15), [Input(0m, 0m,
            new(1, "DEMAND", new(2026, 9, 12), null, 2m, MrpScheduleCategory.Unclassified),
            new(2, "SUPPLY", new(2026, 9, 12), null, 100m, MrpScheduleCategory.Unclassified),
            new(3, "SUPPLYF", new(2026, 9, 15), null, 200m, MrpScheduleCategory.Unclassified)) with
            { Presentation = new(null, null, null, null, null, null, true) }]));
        Assert.Equal(-2m, row.Past.ProjectedQoh);
        Assert.Equal(100m, row.Past.OverdueReceipts);
        Assert.Equal(100m, row.Past.ScheduledReceipts);
        Assert.Equal(-2m, row.Weeks[0].ProjectedQoh);
        Assert.True(row.Presentation!.IsKss);
    }

    [Fact]
    public void Build_SafetyTransition_Episodes_AndUomEvaluation()
    {
        var row = Assert.Single(LongTermShortagesBuilder.Build(new(2026, 9, 15), [Input(5.4m, 5m,
            new(1, "DEMAND", new(2026, 9, 14), null, 0.6m, MrpScheduleCategory.Unclassified),
            new(2, "DEMAND", new(2026, 9, 15), null, 6m, MrpScheduleCategory.Unclassified),
            new(3, "SUPPLY", new(2026, 9, 16), null, 10m, MrpScheduleCategory.Unclassified),
            new(4, "DEMAND", new(2026, 9, 17), null, 10m, MrpScheduleCategory.Unclassified),
            new(5, "SUPPLY", new(2026, 9, 18), null, 10m, MrpScheduleCategory.Unclassified))]));
        Assert.Equal(2, row.Episodes.Count);
        Assert.Equal(1.2m, row.Episodes[0].MaximumShortage);
        Assert.Equal(new DateOnly(2026, 9, 16), row.Episodes[0].FirstRecoveryDate);
        Assert.Null(row.Episodes[0].StableClearDate);
        Assert.Equal(LongTermShortageSeverity.CriticalShort, row.Weeks[0].Severity);
        Assert.Equal("EA", row.UnitOfMeasure);
    }

    [Fact]
    public void Build_SundayBucketsAndUnknownSafetyDoNotBecomeHealthy()
    {
        var row = Assert.Single(LongTermShortagesBuilder.Build(new(2026, 9, 13), [new LongTermShortageInput("MFG", "unknown", null, null, null, null, 0m, SafetyStockState.SelectedSiteValueMissing, null, [],
            [new(1, "DEMAND", new(2026, 9, 19), null, 0.004m, MrpScheduleCategory.Unclassified)])], 13));
        Assert.Equal(13, row.Weeks.Count);
        Assert.Equal(new DateOnly(2026, 9, 13), row.Weeks[0].WeekStart);
        Assert.Equal(-0.004m, row.Weeks[0].ProjectedQoh);
        Assert.Equal(LongTermShortageSeverity.SafetyStockUnavailable, row.Severity);
        Assert.Null(row.DataQualityWarning);
    }

    [Fact]
    public void Build_AtRiskWhenRoundedBalanceFallsBelowSafety_AndNoSafetyIsUnknown()
    {
        var input = Input(5.49m, 6m);
        var row = Assert.Single(LongTermShortagesBuilder.Build(new(2026, 9, 15), [input]));
        Assert.Equal(LongTermShortageSeverity.SafetyStockShort, row.Weeks[0].Severity);
        Assert.Equal(5.49m, row.Weeks[0].ProjectedQoh);
        Assert.Equal(new DateOnly(2026, 9, 15), row.FirstAtRiskDate);
        var unknown = Assert.Single(LongTermShortagesBuilder.Build(new(2026, 9, 15), [input with { SafetyStock = null }]));
        Assert.Equal(LongTermShortageSeverity.SafetyStockUnavailable, unknown.Severity);
    }

    [Fact]
    public void Build_PastCarryShortThatRecoversIsStillAnEpisode()
    {
        var row = Assert.Single(LongTermShortagesBuilder.Build(new(2026, 9, 15), [Input(1m, 0m,
            new(1, "DEMAND", new(2026, 9, 12), null, 3m, MrpScheduleCategory.Unclassified),
            new(2, "SUPPLY", new(2026, 9, 13), null, 4m, MrpScheduleCategory.Unclassified))]));
        var episode = Assert.Single(row.Episodes);
        Assert.Equal(new DateOnly(2026, 9, 15), episode.StartDate);
        Assert.Null(episode.FirstRecoveryDate);
        Assert.Equal(0m, row.Weeks[0].ScheduledReceipts);
        Assert.Equal(2m, episode.MaximumShortage);
    }

    [Fact]
    public void Build_UnknownPoReceiptConfirmationFailsClosed()
    {
        var input = Input(0m, 0m, new LongTermMrpFact(1, "SUPPLY", new(2026, 9, 15), null, 2m, MrpScheduleCategory.Unclassified)
        { IsPoReceipt = true, PoConfirmed = null });
        Assert.Throws<InvalidOperationException>(() => LongTermShortagesBuilder.Build(new(2026, 9, 15), [input]));
    }

    [Fact]
    public void Build_UnknownDemandDateFailsClosed()
    {
        var input = Input(10m, 0m, new LongTermMrpFact(1, "DEMAND", null, null, 4m, MrpScheduleCategory.Unclassified));
        Assert.Throws<InvalidOperationException>(() => LongTermShortagesBuilder.Build(new(2026, 9, 15), [input]));
    }

    [Fact]
    public void Build_MapsStage11StatusDescriptionWithoutChangingBalance()
    {
        var input = Input(5m, 0m) with { QadStatus = "C", EffectivePmCode = "M", ManufacturingLeadWorkingDays = 12 };
        var row = Assert.Single(LongTermShortagesBuilder.Build(new(2026, 9, 15), [input]));
        Assert.Equal("CURRENTLY IN PRODUCTION", row.PartStatusDescription);
        Assert.Equal("M", row.EffectivePmCode);
        Assert.Equal(12, row.ManufacturingLeadWorkingDays);
        Assert.Equal(5m, row.Weeks[0].ProjectedQoh);
    }

    [Fact]
    public void Build_MissingSitePlanningWarnsWithoutMasterFallback()
    {
        var row = Assert.Single(LongTermShortagesBuilder.Build(new(2026, 9, 15), [Input(5m, 0m) with
        { SitePlanningPresent = false, SafetyStock = null }]));
        Assert.Contains("Selected-site planning data missing", row.DataQualityWarning);
        Assert.Equal(LongTermShortageSeverity.SafetyStockUnavailable, row.Severity);
    }

    [Theory]
    [InlineData(" EA ", "EA", "1", null)]
    [InlineData("pk", "PK", "1", null)]
    [InlineData("bx", "BX", "1", null)]
    [InlineData("unlisted", "UNLISTED", "1.25", null)]
    [InlineData(" ", "", "1.25", null)]
    [InlineData(null, null, "1.25", null)]
    public void Build_UomFallbackRoundsAtEvaluationOnly(string? uom, string? normalized, string expectedEnding, string? warning)
    {
        var row = Assert.Single(LongTermShortagesBuilder.Build(new(2026, 9, 24), [Input(1.245m, 0m) with { UnitOfMeasure = uom }]));
        Assert.Equal(normalized, row.UnitOfMeasure);
        Assert.Equal(1.245m, row.Weeks[0].ProjectedQoh);
        Assert.Equal(decimal.Parse(expectedEnding, System.Globalization.CultureInfo.InvariantCulture), LongTermQuantityPresentation.Round(row.Weeks[0].ProjectedQoh, row.UnitOfMeasure));
        Assert.Equal(warning, row.DataQualityWarning);
    }

    [Fact]
    public void Build_74320_27_SeparatesSeptember24FromSundayAndExcludesOverdueReceipt()
    {
        var asOf = new DateOnly(2026, 9, 24);
        var row = Assert.Single(LongTermShortagesBuilder.Build(asOf, [Input(75_000m, 0m,
            new(1, "DEMAND", new(2026, 9, 15), null, 66_931m, MrpScheduleCategory.Unclassified),
            new(2, "DEMAND", new(2026, 9, 22), null, 71_215m, MrpScheduleCategory.Unclassified),
            new(3, "DEMAND", asOf, null, 1_252m, MrpScheduleCategory.Unclassified),
            new(4, "SUPPLY", new(2026, 9, 23), null, 270_000m, MrpScheduleCategory.Unclassified) { IsPoReceipt = true, PoConfirmed = true },
            new(5, "DEMAND", new(2026, 10, 2), null, 59_325m, MrpScheduleCategory.Unclassified),
            new(6, "SUPPLY", new(2026, 10, 2), null, 270_000m, MrpScheduleCategory.Unclassified) { IsPoReceipt = true, PoConfirmed = true }) with { ComponentPart = "74320-27" }]));

        Assert.Equal(new DateOnly(2026, 9, 20), row.Weeks[0].WeekStart);
        Assert.Equal(138_146m, row.Past.GrossRequirements);
        Assert.Equal(270_000m, row.Past.ScheduledReceipts);
        Assert.Equal(-63_146m, row.Past.ProjectedQoh);
        Assert.Equal(1_252m, row.Weeks[0].GrossRequirements);
        Assert.Equal(0m, row.Weeks[0].ScheduledReceipts);
        Assert.Equal(-64_398m, row.Weeks[0].ProjectedQoh);
        Assert.Equal(146_277m, row.Weeks[1].ProjectedQoh);
        Assert.Equal(LongTermShortageSeverity.CriticalShort, row.Severity);
        var episode = Assert.Single(row.Episodes);
        Assert.Equal(asOf, episode.StartDate);
        Assert.Equal(new DateOnly(2026, 10, 2), episode.DeepestDate);
        Assert.Equal(123_723m, episode.MaximumShortage);
        Assert.Equal(new DateOnly(2026, 10, 2), episode.FirstRecoveryDate);
    }

    [Fact]
    public void Build_355203_Pur_RecoversSaturdayButRemainsCritical()
    {
        var asOf = new DateOnly(2026, 9, 24);
        var row = Assert.Single(LongTermShortagesBuilder.Build(asOf, [Input(214m, 0m,
            new(1, "DEMAND", new(2026, 9, 22), null, 1_420m, MrpScheduleCategory.Unclassified),
            new(2, "SUPPLY", new(2026, 9, 26), null, 8_000m, MrpScheduleCategory.Unclassified) { IsPoReceipt = true, PoConfirmed = true }) with { ComponentPart = "355203-PUR" }]));

        Assert.Equal(-1_206m, row.Past.ProjectedQoh);
        Assert.Equal(8_000m, row.Weeks[0].ScheduledReceipts);
        Assert.Equal(6_794m, row.Weeks[0].ProjectedQoh);
        Assert.Equal(LongTermShortageSeverity.CriticalShort, row.Severity);
        var episode = Assert.Single(row.Episodes);
        Assert.Equal(asOf, episode.StartDate);
        Assert.Equal(new DateOnly(2026, 9, 26), episode.FirstRecoveryDate);
        Assert.Equal(new DateOnly(2026, 9, 26), episode.StableClearDate);
        Assert.True(row.HasShortage);
    }

    [Fact]
    public void Build_FutureShort_AtRisk_AndHealthyHaveDistinctSummarySeverity()
    {
        var asOf = new DateOnly(2026, 9, 24);
        var rows = LongTermShortagesBuilder.Build(asOf, [
            Input(10m, 0m, new LongTermMrpFact(1, "DEMAND", new(2026, 9, 25), null, 11m, MrpScheduleCategory.Unclassified)) with { ComponentPart = "FUTURE" },
            Input(10m, 8m, new LongTermMrpFact(2, "DEMAND", new(2026, 9, 25), null, 3m, MrpScheduleCategory.Unclassified)) with { ComponentPart = "AT-RISK" },
            Input(10m, 8m) with { ComponentPart = "HEALTHY" }
        ]).ToDictionary(row => row.ComponentPart);

        Assert.Equal(LongTermShortageSeverity.FutureShort, rows["FUTURE"].Severity);
        Assert.Equal(new DateOnly(2026, 9, 25), rows["FUTURE"].FirstShortDate);
        Assert.True(rows["FUTURE"].HasShortage);
        Assert.Equal(LongTermShortageSeverity.SafetyStockShort, rows["AT-RISK"].Severity);
        Assert.Null(rows["AT-RISK"].FirstShortDate);
        Assert.Empty(rows["AT-RISK"].Episodes);
        Assert.Equal(LongTermShortageSeverity.Healthy, rows["HEALTHY"].Severity);
        Assert.False(rows["HEALTHY"].HasShortage);
    }

    [Fact]
    public void Build_OpeningAndDemandSameDayAreCriticalEvenWhenReceiptClearsThem()
    {
        var asOf = new DateOnly(2026, 9, 24);
        var row = Assert.Single(LongTermShortagesBuilder.Build(asOf, [Input(5m, 0m,
            new(1, "DEMAND", asOf, null, 8m, MrpScheduleCategory.Unclassified),
            new(2, "SUPPLY", asOf, null, 10m, MrpScheduleCategory.Unclassified))]));
        Assert.Equal(LongTermShortageSeverity.CriticalShort, row.Severity);
        Assert.Equal(asOf, row.FirstShortDate);
        Assert.Equal(7m, row.Weeks[0].ProjectedQoh);
        Assert.Equal(-3m, row.Weeks[0].LowestProjectedBalance);
        Assert.Equal(asOf, Assert.Single(row.Episodes).StartDate);
    }

    [Fact]
    public void Build_WeekBoundaryAndHorizonRemainCalendarWeeksFromMidweekReportDate()
    {
        var asOf = new DateOnly(2026, 9, 24);
        var row = Assert.Single(LongTermShortagesBuilder.Build(asOf, [Input(10m, 0m,
            new(1, "DEMAND", new(2026, 9, 20), null, 1m, MrpScheduleCategory.Unclassified),
            new(2, "DEMAND", new(2026, 9, 23), null, 2m, MrpScheduleCategory.Unclassified),
            new(3, "DEMAND", new(2026, 9, 24), null, 3m, MrpScheduleCategory.Unclassified),
            new(4, "DEMAND", new(2026, 9, 26), null, 4m, MrpScheduleCategory.Unclassified),
            new(5, "SUPPLY", new(2026, 9, 27), null, 9m, MrpScheduleCategory.Unclassified))], 13));
        Assert.Equal(13, row.Weeks.Count);
        Assert.Equal(new DateOnly(2026, 9, 20), row.Weeks[0].WeekStart);
        Assert.Equal(new DateOnly(2026, 9, 26), row.Weeks[0].WeekStart!.Value.AddDays(6));
        Assert.Equal(new DateOnly(2026, 9, 27), row.Weeks[1].WeekStart);
        Assert.Equal(new DateOnly(2026, 12, 13), row.Weeks[^1].WeekStart);
        Assert.Equal(3m, row.Past.GrossRequirements);
        Assert.Equal(7m, row.Weeks[0].GrossRequirements);
        Assert.Equal(0m, row.Weeks[0].ScheduledReceipts);
        Assert.Equal(9m, row.Weeks[1].ScheduledReceipts);
    }

    private static LongTermShortageInput Input(decimal qoh, decimal safety, params LongTermMrpFact[] facts) => new("COMP", "EA", null, null, null, null, qoh, SafetyStockState.Resolved, safety, ["PARENT"], facts);
}
