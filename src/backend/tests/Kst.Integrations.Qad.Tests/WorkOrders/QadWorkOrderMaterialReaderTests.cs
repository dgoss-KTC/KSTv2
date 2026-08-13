using Kst.Integrations.Qad.WorkOrders;

namespace Kst.Integrations.Qad.Tests.WorkOrders;

public sealed class QadWorkOrderMaterialReaderTests
{
    [Fact]
    public void BuildQuery_Parameterizes_Domain_Site_And_Woid()
    {
        var (sql, parameters) = QadWorkOrderMaterialReader.BuildQuery("KTC", "SW", "1001");

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("SW", parameters.Get<string>("Site"));
        Assert.Equal("1001", parameters.Get<string>("Woid"));
        Assert.Contains("@Domain", sql);
        Assert.Contains("@Site", sql);
        Assert.Contains("@Woid", sql);
    }

    [Fact]
    public void BuildQuery_Joins_Wo_Mstr_To_Wod_Det_By_Domain_And_Lot()
    {
        var (sql, _) = QadWorkOrderMaterialReader.BuildQuery("KTC", "SW", "1001");

        Assert.Contains("wod.wod_domain = wo.wo_domain", sql);
        Assert.Contains("wod.wod_lot = wo.wo_lot", sql);
    }

    [Fact]
    public void BuildQuery_LeftJoins_PtMstr_For_Description_And_Pm_Code()
    {
        var (sql, _) = QadWorkOrderMaterialReader.BuildQuery("KTC", "SW", "1001");

        Assert.Contains("LEFT JOIN qadpro2.dbo.pt_mstr", sql);
        Assert.Contains("pt.pt_domain = wod.wod_domain", sql);
        Assert.Contains("pt.pt_part = wod.wod_part", sql);
        Assert.Contains("pt.pt_desc1      AS ComponentDescription", sql);
        Assert.Contains("pt.pt_pm_code    AS ComponentPmCode", sql);
    }

    [Fact]
    public void BuildQuery_Excludes_Zero_Required_Lines()
    {
        var (sql, _) = QadWorkOrderMaterialReader.BuildQuery("KTC", "SW", "1001");

        Assert.Contains("wod.wod_qty_req <> 0", sql);
    }

    [Fact]
    public void BuildQuery_Does_Not_Deduplicate_Component_Rows()
    {
        var (sql, _) = QadWorkOrderMaterialReader.BuildQuery("KTC", "SW", "1001");

        Assert.DoesNotContain("DISTINCT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GROUP BY", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildQuery_Never_Concatenates_Raw_Woid_Value_Into_Sql_Text()
    {
        var maliciousWoid = "1'; DROP TABLE wod_det; --";
        var (sql, parameters) = QadWorkOrderMaterialReader.BuildQuery("KTC", "SW", maliciousWoid);

        Assert.DoesNotContain(maliciousWoid, sql);
        Assert.Equal(maliciousWoid, parameters.Get<string>("Woid"));
    }

    private static QadWorkOrderMaterialRawRow Raw(
        string componentPart = "COMP1",
        string? componentDescription = "Widget",
        decimal required = 10m,
        decimal issued = 10m,
        string? pmCode = null) => new(
        ComponentPart: componentPart,
        ComponentDescription: componentDescription,
        RequiredQuantity: required,
        IssuedQuantity: issued,
        ComponentPmCode: pmCode);

    [Fact]
    public void Normalize_Maps_Raw_Row_Into_Typed_WorkOrderMaterialLine()
    {
        var normalized = QadWorkOrderMaterialReader.Normalize(Raw(required: 10m, issued: 6m));

        Assert.Equal("COMP1", normalized.ComponentPart);
        Assert.Equal("Widget", normalized.ComponentDescription);
        Assert.Equal(10m, normalized.RequiredQuantity);
        Assert.Equal(6m, normalized.IssuedQuantity);
        Assert.Equal(-4m, normalized.VarianceQuantity);
    }

    [Theory]
    [InlineData("M", true)]
    [InlineData("m", true)]
    [InlineData("P", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void Normalize_Maps_PmCode_M_To_IsManufactured(string? pmCode, bool expected)
    {
        Assert.Equal(expected, QadWorkOrderMaterialReader.Normalize(Raw(pmCode: pmCode)).IsManufactured);
    }

    [Fact]
    public void Normalize_Allows_Null_Description()
    {
        Assert.Null(QadWorkOrderMaterialReader.Normalize(Raw(componentDescription: null)).ComponentDescription);
    }
}
