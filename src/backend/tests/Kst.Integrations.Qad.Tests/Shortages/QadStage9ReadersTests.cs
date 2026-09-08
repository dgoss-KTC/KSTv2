using Kst.Domain.Mps;
using Kst.Domain.Shortages;
using Kst.Integrations.Qad.Shortages;

namespace Kst.Integrations.Qad.Tests.Shortages;

public sealed class QadStage9ReadersTests
{
    [Fact]
    public void CommittedPopulation_Is_SiteWide_WindowBounded_And_Limited_To_R_And_A()
    {
        var (sql, parameters) = QadCommittedWorkOrderPopulationReader.BuildQuery("KTC", "SW", MpsDateBasis.ReleaseDate, new(2026, 8, 30), new(2026, 9, 27));

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("SW", parameters.Get<string>("Site"));
        Assert.Equal("releaseDate", parameters.Get<string>("DateBasis"));
        Assert.Contains("UPPER(wo.wo_status) IN ('R', 'A')", sql);
        Assert.Contains("wo.wo_due_date < @WeekStart", sql);
        Assert.Contains("wo.wo_rel_date >= @WeekStart AND wo.wo_rel_date < @WindowEnd", sql);
        Assert.DoesNotContain("wo.wo_part =", sql);
        Assert.Contains("ISNULL(wo.wo_bom_code, '') <> 'RMABOM'", sql);
    }

    [Fact]
    public void HardAllocation_Uses_Lad_Identity_Without_A_Wod_Existence_Predicate_And_Has_No_Window_Predicate()
    {
        var (sql, parameters) = QadHardAllocationReader.BuildQuery("KTC", "SW", new(2026, 9, 3), 7);

        Assert.Equal(new DateTime(2026, 9, 10), parameters.Get<DateTime>("ExpirationCutoff"));
        Assert.Contains("lad.lad_dataset = 'wod_det'", sql);
        Assert.Contains("lad.lad_nbr     AS Woid", sql);
        Assert.Contains("lad.lad_line    AS OperationNumber", sql);
        Assert.Contains("lad.lad_part    AS ComponentPart", sql);
        Assert.Contains("lad.lad_qty_all > 0", sql);
        Assert.DoesNotContain("@WeekStart", sql);
        Assert.DoesNotContain("@WindowEnd", sql);
        Assert.DoesNotContain("wod_qty_all", sql);
        Assert.DoesNotContain("qadpro2.dbo.wod_det", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EXISTS", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UPPER(ism.is_status) = 'STOCK'", sql);
        Assert.Contains("ism.is_nettable = 1", sql);
        Assert.Contains("ld.ld_qty_oh > 0", sql);
        Assert.Contains("ld.ld_lot NOT LIKE 'RA%'", sql);
        Assert.Contains("ld.ld_expire IS NULL OR ld.ld_expire > @ExpirationCutoff", sql);
    }

    [Fact]
    public void HardAllocation_Normalizes_The_Allocation_Detail_Without_Altering_Quantity()
    {
        var result = QadHardAllocationReader.Normalize(new("WO1", "10", "C1", "LOC", "LOT", 6.93m));

        Assert.Equal("WO1", result.Woid);
        Assert.Equal("10", result.OperationNumber);
        Assert.Equal("C1", result.ComponentPart);
        Assert.Equal("LOC", result.Location);
        Assert.Equal("LOT", result.Lot);
        Assert.Equal(6.93m, result.AllocatedQuantity);
    }

    [Fact]
    public void IssuePolicy_Uses_Independent_SiteFirst_MasterFallback_Then_TrueDefault()
    {
        var (sql, _) = QadIssuePolicyReader.BuildQuery("KTC", "SW", "C1");

        Assert.Contains("COALESCE(ptp.ptp_iss_pol, pt.pt_iss_pol, 1)", sql);
        Assert.Contains("FROM (VALUES (@Part)) AS scope (PartNumber)", sql);
        Assert.Contains("ptp.ptp_domain = @Domain", sql);
        Assert.Contains("ptp.ptp_part = scope.PartNumber", sql);
        Assert.Contains("ptp.ptp_site = @Site", sql);
        Assert.Contains("pt.pt_domain = @Domain", sql);
        Assert.Contains("pt.pt_part = scope.PartNumber", sql);
        Assert.DoesNotContain("ptp.ptp_domain = pt.", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ptp.ptp_part = pt.", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IssueDays_Is_Scoped_To_Domain_And_Site()
    {
        var (sql, _) = QadIssueDaysReader.BuildQuery("KTC", "SW");

        Assert.Contains("qadpro2.dbo.icc_ctrl", sql);
        Assert.Contains("icc.icc_iss_days AS IssueDays", sql);
        Assert.Contains("icc.icc_domain = @Domain", sql);
        Assert.Contains("icc.icc_site = @Site", sql);
    }

    [Fact]
    public void InventoryPosition_Uses_Accepted_Usable_And_Activity_Predicates()
    {
        var (sql, parameters) = QadInventoryPositionReader.BuildBatchQuery("KTC", "SW", ["C1"], new(2026, 9, 3), 7);

        Assert.Equal("C1", parameters.Get<string>("Part0"));
        Assert.Contains("UPPER(ism.is_status) = 'STOCK' AND ism.is_nettable = 1", sql);
        Assert.Contains("ld.ld_expire IS NULL OR ld.ld_expire > @ExpirationCutoff", sql);
        Assert.Contains("UPPER(ism.is_status) = 'TRAN'", sql);
        Assert.Contains("UPPER(ism.is_status) = 'INSPECT'", sql);
        Assert.Contains("ism.is_nettable = 0", sql);
        Assert.Contains("UPPER(ism.is_status) = 'MRB'", sql);
        Assert.Contains("UPPER(ism.is_status) = 'NCMINSP'", sql);
        Assert.Contains("ld.ld_expire <= @ExpirationCutoff", sql);
    }

    [Fact]
    public void InventoryPosition_Normalization_Keeps_All_Activity_Buckets_Separate_From_Usable()
    {
        var position = QadInventoryPositionReader.Normalize(new("C1", 10m, 1m, 2m, 3m, 4m, 5m, 6m)).Position;

        Assert.Equal(10m, position.UsableQuantity);
        Assert.Equal(1m, position.ActivityQuantities[InventoryActivity.Transit]);
        Assert.Equal(6m, position.ActivityQuantities[InventoryActivity.ExpiredExpiring]);
    }

    [Fact]
    public void NextPurchaseOrder_Is_Earliest_Open_Positive_Line_And_Does_Not_Require_Confirmation()
    {
        var (sql, _) = QadNextPurchaseOrderReader.BuildQuery("KTC", "SW", "C1");

        Assert.Contains("TOP (1)", sql);
        Assert.Contains("pod.pod_domain = @Domain", sql);
        Assert.Contains("pod.pod_site = @Site", sql);
        Assert.Contains("pod.pod_qty_ord - pod.pod_qty_rcvd AS OpenQuantity", sql);
        Assert.Contains("LOWER(ISNULL(pod.pod_status, '')) NOT IN ('c', 'x')", sql);
        Assert.Contains("pod.pod_qty_ord - pod.pod_qty_rcvd > 0", sql);
        Assert.Contains("ORDER BY pod.pod_due_date, po.po_nbr, pod.pod_line", sql);
        Assert.Contains("po.po_confirm AS IsConfirmed", sql);
        Assert.Contains("pod.pod__chr06 AS TrackingInfo", sql);
        Assert.Contains("po.po_sched = 1 OR pod.pod_sched = 1", sql);
        Assert.DoesNotContain("po.po_confirm = 1", sql);
    }

    [Fact]
    public void NextPurchaseOrder_Normalizes_Informational_Fields()
    {
        var result = QadNextPurchaseOrderReader.Normalize(new("PO1", new(2026, 9, 10), 12m, true, "  TRACK  ", true, "  O "));

        Assert.Equal(new DateOnly(2026, 9, 10), result.DueDate);
        Assert.Equal("TRACK", result.TrackingInfo);
        Assert.True(result.IsKss);
        Assert.Equal("O", result.PoState);
    }

    [Fact]
    public void KssSchedule_Uses_Effective_Domain_Site_Part_Relationship_Without_Conventional_Po_Qualification()
    {
        var (sql, parameters) = QadKssScheduleReader.BuildQuery("KTC", "SW", "C1", new(2026, 9, 7));

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("SW", parameters.Get<string>("Site"));
        Assert.Equal("C1", parameters.Get<string>("Part"));
        Assert.Equal(new DateTime(2026, 9, 7), parameters.Get<DateTime>("Today"));
        Assert.Contains("pod.pod_domain = @Domain", sql);
        Assert.Contains("pod.pod_site = @Site", sql);
        Assert.Contains("pod.pod_part = @Part", sql);
        Assert.Contains("pod.pod_end_eff##1 IS NULL OR pod.pod_end_eff##1 >= @Today", sql);
        Assert.Contains("po.po_sched = 1", sql);
        Assert.Contains("po.po_eff_to IS NULL OR po.po_eff_to >= @Today", sql);
        Assert.DoesNotContain("pod.pod_qty_ord - pod.pod_qty_rcvd > 0", sql);
        Assert.DoesNotContain("pod.pod_status", sql);
    }
}
