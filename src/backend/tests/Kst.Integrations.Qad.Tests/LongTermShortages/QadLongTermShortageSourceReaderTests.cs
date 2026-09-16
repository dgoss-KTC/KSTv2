using Kst.Integrations.Qad.LongTermShortages;

namespace Kst.Integrations.Qad.Tests.LongTermShortages;

public sealed class QadLongTermShortageSourceReaderTests
{
    [Fact]
    public void BuildBatchQuery_IsParameterizedAndContainsApprovedProjectionFactShapes()
    {
        var (sql, parameters) = QadLongTermShortageSourceReader.BuildBatchQuery("KTC", "SW", ["C1", "C2"], new DateOnly(2026, 9, 15), new DateOnly(2027, 3, 7));

        Assert.Equal("C1", parameters.Get<string>("Part0"));
        Assert.DoesNotContain("C1", sql);
        Assert.Contains("UPPER(ld.ld_status) NOT IN ('MRB', 'INSPECT', 'NCMINSP')", sql);
        Assert.Contains("UPPER(ld.ld_lot) NOT LIKE 'RMA%'", sql);
        Assert.DoesNotContain("ld.ld_qty_oh > 0", sql);
        Assert.DoesNotContain("in_mstr", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lad.lad_nbr AS Woid", sql);
        Assert.Contains("fa.Woid = wod.wod_lot", sql);
        Assert.Contains("fa.OperationNumber = wod.wod_op", sql);
        Assert.Contains("wo.wo_status IN ('A', 'F', 'R', 'E', 'P')", sql);
        Assert.Contains("mrp.mrp_dataset = 'fcs_sum'", sql);
        Assert.Contains("mrp.mrp_qty", sql);
        Assert.DoesNotContain("mrp.fcs_sum", sql);
        Assert.Contains("pod.pod__log01", sql);
        Assert.Contains("ISNULL(po.po_sched, 0) = 0", sql);
        Assert.Contains("icc.icc_iss_days", sql);
        Assert.Contains("pm.pt_um AS UnitOfMeasure", sql);
        Assert.DoesNotContain("po_stat", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wod_qty_pick", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wod_qty_all", sql, StringComparison.OrdinalIgnoreCase);
    }
}
