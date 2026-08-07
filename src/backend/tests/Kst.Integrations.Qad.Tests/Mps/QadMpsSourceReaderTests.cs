using Kst.Domain.Mps;
using Kst.Integrations.Qad.Mps;

namespace Kst.Integrations.Qad.Tests.Mps;

public sealed class QadMpsSourceReaderTests
{
    [Fact]
    public void BuildBatchQuery_Adds_One_Parameter_Per_Part_Plus_Domain_And_Site()
    {
        var (sql, parameters) = QadMpsSourceReader.BuildBatchQuery("KTC", "SW", ["ABC100", "ABC200"]);

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("SW", parameters.Get<string>("Site"));
        Assert.Equal("ABC100", parameters.Get<string>("Part0"));
        Assert.Equal("ABC200", parameters.Get<string>("Part1"));
        Assert.Contains("@Part0", sql);
        Assert.Contains("@Part1", sql);
    }

    [Fact]
    public void BuildBatchQuery_Never_Concatenates_Raw_Part_Values_Into_Sql_Text()
    {
        var maliciousPart = "ABC'; DROP TABLE wo_mstr; --";
        var (sql, parameters) = QadMpsSourceReader.BuildBatchQuery("KTC", "SW", [maliciousPart]);

        Assert.DoesNotContain(maliciousPart, sql);
        Assert.Equal(maliciousPart, parameters.Get<string>("Part0"));
    }

    [Fact]
    public void BuildBatchQuery_Filters_Wo_Status_Closed_And_Rmabom_NullSafe()
    {
        var (sql, _) = QadMpsSourceReader.BuildBatchQuery("KTC", "SW", ["ABC100"]);

        Assert.Contains("wo.wo_status <> 'C'", sql);
        Assert.Contains("ISNULL(wo.wo_bom_code, '') <> 'RMABOM'", sql);
        Assert.Contains("mrp.mrp_dataset = 'wo_mstr'", sql);
        Assert.DoesNotContain("DISTINCT", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildBatchQuery_Safely_Associates_Mrp_To_WorkOrder_By_Domain_Site_Part_Nbr_Lot()
    {
        var (sql, _) = QadMpsSourceReader.BuildBatchQuery("KTC", "SW", ["ABC100"]);

        Assert.Contains("wo.wo_nbr = mrp.mrp_nbr", sql);
        Assert.Contains("wo.wo_lot = mrp.mrp_line", sql);
        Assert.Contains("wo.wo_domain = mrp.mrp_domain", sql);
        Assert.Contains("wo.wo_site = mrp.mrp_site", sql);
        Assert.Contains("wo.wo_part = mrp.mrp_part", sql);
    }

    [Theory]
    [InlineData("supply", MpsSupplyType.Supply)]
    [InlineData("SUPPLY", MpsSupplyType.Supply)]
    [InlineData("supplyf", MpsSupplyType.SupplyF)]
    [InlineData("SUPPLYF", MpsSupplyType.SupplyF)]
    [InlineData("supplyp", MpsSupplyType.SupplyP)]
    public void NormalizeSupplyType_Maps_Known_Values_Case_Insensitively(string raw, MpsSupplyType expected)
    {
        Assert.Equal(expected, QadMpsSourceReader.NormalizeSupplyType(raw));
    }

    [Theory]
    [InlineData("A", MpsWorkOrderState.Allocating)]
    [InlineData("F", MpsWorkOrderState.Frozen)]
    [InlineData("R", MpsWorkOrderState.Released)]
    [InlineData("P", MpsWorkOrderState.Planned)]
    [InlineData("e", MpsWorkOrderState.ExplicitlyScheduled)]
    [InlineData("Z", MpsWorkOrderState.Unknown)]
    [InlineData("c", MpsWorkOrderState.Unknown)]
    public void NormalizeWorkOrderState_Maps_Known_Values_And_Defaults_To_Unknown(string raw, MpsWorkOrderState expected)
    {
        Assert.Equal(expected, QadMpsSourceReader.NormalizeWorkOrderState(raw));
    }

    [Fact]
    public void Normalize_Maps_Raw_Row_Into_Typed_MpsSourceRow()
    {
        var raw = new QadMpsRawRow(
            Domain: "KTC",
            Site: "SW",
            ParentPart: "ABC100",
            Description: "Widget",
            DueDate: new DateTime(2026, 8, 10),
            ReleaseDate: new DateTime(2026, 8, 3),
            Quantity: 42.5m,
            MrpType: "supplyf",
            WorkOrderId: "12345",
            WorkOrderStatus: "R");

        var normalized = QadMpsSourceReader.Normalize(raw);

        Assert.Equal("KTC", normalized.Domain);
        Assert.Equal("SW", normalized.Site);
        Assert.Equal("ABC100", normalized.ParentPart);
        Assert.Equal("Widget", normalized.Description);
        Assert.Equal(new DateOnly(2026, 8, 10), normalized.DueDate);
        Assert.Equal(new DateOnly(2026, 8, 3), normalized.ReleaseDate);
        Assert.Equal(42.5m, normalized.Quantity);
        Assert.Equal(MpsSupplyType.SupplyF, normalized.SupplyType);
        Assert.Equal("12345", normalized.WorkOrderId);
        Assert.Equal(MpsWorkOrderState.Released, normalized.WorkOrderState);
    }

    [Fact]
    public void Normalize_Maps_Null_ReleaseDate_To_Null()
    {
        var raw = new QadMpsRawRow(
            Domain: "KTC", Site: "SW", ParentPart: "ABC100", Description: null,
            DueDate: new DateTime(2026, 8, 10), ReleaseDate: null, Quantity: 1m,
            MrpType: "supply", WorkOrderId: "1", WorkOrderStatus: "P");

        var normalized = QadMpsSourceReader.Normalize(raw);

        Assert.Null(normalized.ReleaseDate);
        Assert.Null(normalized.Description);
    }
}
