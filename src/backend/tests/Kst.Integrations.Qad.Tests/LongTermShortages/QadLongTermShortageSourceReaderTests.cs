using Kst.Integrations.Qad.LongTermShortages;
namespace Kst.Integrations.Qad.Tests.LongTermShortages;
public sealed class QadLongTermShortageSourceReaderTests
{
    [Fact]
    public void BuildBatchQuery_IsParameterizedAndReadsRawMrpAndDirectQoh()
    {
        var (sql, parameters) = QadLongTermShortageSourceReader.BuildBatchQuery("KTC", "SW", ["C1", "C2"], new DateOnly(2027, 3, 1));
        Assert.Equal("C1", parameters.Get<string>("Part0"));
        Assert.DoesNotContain("C1", sql);
        Assert.Contains("UPPER(ld.ld_status) <> 'MRB'", sql);
        Assert.Contains("UPPER(ld.ld_lot) NOT LIKE 'RMA%'", sql);
        Assert.Contains("UPPER(ld.ld_lot) NOT LIKE 'RA%'", sql);
        Assert.DoesNotContain("INSPECT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("in_mstr", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lad_det", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mrp.mrp_due_date < @HorizonEnd OR mrp.mrp_rel_date < @HorizonEnd", sql);
        Assert.Contains("mrp.mrp_type", sql);
        Assert.DoesNotContain("mrp_dataset", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GROUP BY mrp", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ROUND", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildPresentationQuery_IsParameterizedAndCannotJoinOrAlterRawMrpRows()
    {
        var (sql, parameters) = QadLongTermShortageSourceReader.BuildPresentationQuery("KTC", "SW", ["C1"], new DateOnly(2026, 9, 15));
        Assert.Equal("C1", parameters.Get<string>("Part0"));
        Assert.DoesNotContain("C1", sql);
        Assert.Contains("@RefreshDate", sql);
        Assert.Contains("pod.pod_vpart", sql);
        Assert.Contains("KssComponents", sql);
        Assert.DoesNotContain("mrp_det", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ld_det", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE", sql, StringComparison.OrdinalIgnoreCase);
    }
}
