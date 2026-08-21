using Kst.Integrations.Qad.ApprovedVendors;

namespace Kst.Integrations.Qad.Tests.ApprovedVendors;

public sealed class QadApprovedVendorReaderTests
{
    [Fact]
    public void BuildQuery_Parameterizes_Domain_And_Part()
    {
        var (sql, parameters) = QadApprovedVendorReader.BuildQuery("KTC", "ABC100");

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("ABC100", parameters.Get<string>("Part"));
        Assert.Contains("@Domain", sql);
        Assert.Contains("@Part", sql);
        Assert.Contains("pt.pt_domain = @Domain", sql);
        Assert.Contains("pt.pt_part = @Part", sql);
    }

    [Fact]
    public void BuildQuery_Joins_VpMstr_On_Domain_And_Part()
    {
        var (sql, _) = QadApprovedVendorReader.BuildQuery("KTC", "ABC100");

        Assert.Contains("INNER JOIN qadpro2.dbo.vp_mstr AS vp", sql);
        Assert.Contains("pt.pt_domain = vp.vp_domain", sql);
        Assert.Contains("pt.pt_part = vp.vp_part", sql);
    }

    [Fact]
    public void BuildQuery_Joins_AdMstr_On_Domain_And_Vendor_Address()
    {
        var (sql, _) = QadApprovedVendorReader.BuildQuery("KTC", "ABC100");

        Assert.Contains("INNER JOIN qadpro2.dbo.ad_mstr AS ad", sql);
        Assert.Contains("vp.vp_domain = ad.ad_domain", sql);
        Assert.Contains("vp.vp_vend = ad.ad_addr", sql);
    }

    [Fact]
    public void BuildQuery_Orders_By_Supplier_Ascending()
    {
        var (sql, _) = QadApprovedVendorReader.BuildQuery("KTC", "ABC100");

        Assert.Contains("ORDER BY vp.vp_vend", sql);
    }

    [Fact]
    public void BuildQuery_Does_Not_Use_Distinct()
    {
        var (sql, _) = QadApprovedVendorReader.BuildQuery("KTC", "ABC100");

        Assert.DoesNotContain("DISTINCT", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildQuery_Never_Concatenates_Raw_Part_Value_Into_Sql_Text()
    {
        var maliciousPart = "ABC'; DROP TABLE vp_mstr; --";
        var (sql, parameters) = QadApprovedVendorReader.BuildQuery("KTC", maliciousPart);

        Assert.DoesNotContain(maliciousPart, sql);
        Assert.Equal(maliciousPart, parameters.Get<string>("Part"));
    }

    [Fact]
    public void BuildQuery_Does_Not_Filter_By_Site()
    {
        // AVL grain is Domain + Part; the accepted query is not Site-specific.
        var (sql, _) = QadApprovedVendorReader.BuildQuery("KTC", "ABC100");

        Assert.DoesNotContain("@Site", sql);
    }

    // ---------- Normalize / mapping ----------

    [Fact]
    public void Normalize_Maps_Single_Row_Preserving_All_Fields()
    {
        var row = new QadApprovedVendorRawRow("V001", "Acme Supply", "SUP-1", "MFG-1");

        var vendor = InvokeNormalize(row);

        Assert.Equal("V001", vendor.Supplier);
        Assert.Equal("Acme Supply", vendor.VendorName);
        Assert.Equal("SUP-1", vendor.SupplierItem);
        Assert.Equal("MFG-1", vendor.ManufacturerPart);
    }

    [Fact]
    public void Normalize_Maps_Null_SupplierItem_To_Null()
    {
        var row = new QadApprovedVendorRawRow("V001", "Acme Supply", null, "MFG-1");

        var vendor = InvokeNormalize(row);

        Assert.Null(vendor.SupplierItem);
    }

    [Fact]
    public void Normalize_Maps_Null_ManufacturerPart_To_Null()
    {
        var row = new QadApprovedVendorRawRow("V001", "Acme Supply", "SUP-1", null);

        var vendor = InvokeNormalize(row);

        Assert.Null(vendor.ManufacturerPart);
    }

    [Fact]
    public void Normalize_Maps_Blank_SupplierItem_To_Null()
    {
        var row = new QadApprovedVendorRawRow("V001", "Acme Supply", "   ", "MFG-1");

        var vendor = InvokeNormalize(row);

        Assert.Null(vendor.SupplierItem);
    }

    [Fact]
    public void Normalize_Trims_Supplier_And_VendorName()
    {
        var row = new QadApprovedVendorRawRow("  V001  ", "  Acme Supply  ", "SUP-1", "MFG-1");

        var vendor = InvokeNormalize(row);

        Assert.Equal("V001", vendor.Supplier);
        Assert.Equal("Acme Supply", vendor.VendorName);
    }

    private static Kst.Domain.ApprovedVendors.ApprovedVendor InvokeNormalize(QadApprovedVendorRawRow row) =>
        QadApprovedVendorReader.Normalize(row);
}
