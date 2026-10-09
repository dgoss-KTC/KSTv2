using Kst.Integrations.Qad.ComponentMrp;

namespace Kst.Integrations.Qad.Tests.ComponentMrp;

public class ComponentMrpMappingTests
{
    private static QadComponentMrpReader.RawRow Row(string id = "physical-A") => new()
    {
        ComponentPart = "ABC-123", Description = " First ", Description2 = " second ", UnitOfMeasure = "KG",
        OpeningQoh = -2.125m, BuyerPlannerCode = "site-only", SitePlanningPresent = true, SafetyStock = 0,
        MrpType = "SUPPLY", Quantity = .125m, DueDate = new DateTime(2026, 9, 24, 15, 0, 0),
        SourceRowId = id, SourceNumber = "000123", SourceLine = "01", SourceLine2 = "002", IsPoReceipt = true, PoConfirmed = false,
        Ios = "IOS", TimeFence = 4, MinimumOrder = .25m
    };

    [Fact]
    public void Independently_maps_signed_inventory_exact_decimals_identity_and_confirmation()
    {
        var acquired = DateTimeOffset.Parse("2026-09-24T12:00:00Z");
        var source = QadComponentMrpReader.Map([Row()], .00152m, acquired);
        Assert.Equal("First second", source.Input.Description);
        Assert.Equal(-2.125m, source.Input.OpeningQoh);
        Assert.Equal("site-only", source.Input.BuyerPlannerCode);
        Assert.Null(source.Input.Planner);
        Assert.Empty(source.Input.DemandParentParts);
        var fact = Assert.Single(source.Input.Evidence);
        Assert.Equal(.125m, fact.Quantity);
        Assert.Equal(new DateOnly(2026, 9, 24), fact.DueDate);
        Assert.Equal("physical-A", fact.SourceRowId);
        Assert.Equal("000123", fact.SourceNumber);
        Assert.Equal("01", fact.SourceLine); Assert.Equal("002", fact.SourceLine2);
        Assert.True(fact.IsPoReceipt); Assert.False(fact.PoConfirmed);
        Assert.Equal(.00152m, source.Metadata.Qctc); Assert.Equal(acquired, source.AcquiredAtUtc);
    }

    [Fact]
    public void Physical_identity_is_not_business_field_deduplication()
    {
        Assert.Equal(2, QadComponentMrpReader.Map([Row(), Row(), Row("physical-B")], null, DateTimeOffset.UtcNow).Input.Evidence.Count);
        var conflict = Row(); conflict.Quantity = 4;
        Assert.Throws<InvalidOperationException>(() => QadComponentMrpReader.Map([Row(), conflict], null, DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => QadComponentMrpReader.Map([Row("")], null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Empty_mrp_missing_planning_and_zero_cost_are_distinct()
    {
        var row = new QadComponentMrpReader.RawRow { ComponentPart = "M", OpeningQoh = 5, SitePlanningPresent = false };
        var source = QadComponentMrpReader.Map([row], 0, DateTimeOffset.UtcNow);
        Assert.Empty(source.Input.Evidence); Assert.Null(source.Input.SafetyStock);
        Assert.False(source.Input.SitePlanningPresent); Assert.Equal(0m, source.Metadata.Qctc);
        Assert.Null(QadComponentMrpReader.Map([row], null, DateTimeOffset.UtcNow).Metadata.Qctc);
    }

    [Fact]
    public void Missing_quantity_confirmation_or_scope_is_an_error()
    {
        var row = Row(); row.Quantity = null;
        Assert.Throws<InvalidOperationException>(() => QadComponentMrpReader.Map([row], null, DateTimeOffset.UtcNow));
        row = Row(); row.PoConfirmed = null;
        Assert.Throws<InvalidOperationException>(() => QadComponentMrpReader.Map([row], null, DateTimeOffset.UtcNow));
        row = Row(); row.ComponentPart = "different";
        Assert.Throws<InvalidOperationException>(() => QadComponentMrpReader.Map([Row(), row], null, DateTimeOffset.UtcNow));
        row = Row(); row.SafetyStock = 999;
        Assert.Throws<InvalidOperationException>(() => QadComponentMrpReader.Map([Row(), row], null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Focused_query_is_parameterized_and_excludes_broad_presentation()
    {
        var (sql, parameters) = QadComponentMrpReader.BuildQuery("KTC", "SW", "x' OR 1=1--", new(2027, 1, 1));
        Assert.DoesNotContain("x' OR", sql); Assert.Equal("x' OR 1=1--", parameters.Get<string>("Part0"));
        Assert.Contains("INNER JOIN qadpro2.dbo.pt_mstr", sql);
        Assert.Contains("ptp.ptp_buyer AS BuyerPlannerCode", sql);
        Assert.DoesNotContain("code_mstr", sql); Assert.DoesNotContain("po_mstr", sql);
        Assert.DoesNotContain("sct_det", sql); Assert.DoesNotContain("Kss", sql);
        Assert.Contains("mrp.prrowid", sql); Assert.Contains("mrp.mrp_rel_date < @HorizonEnd", sql);
        Assert.DoesNotContain("ld_qty_oh >", sql);
    }
}
