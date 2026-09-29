using Kst.Integrations.Qad.LongTermShortages;
using Kst.Domain.LongTermShortages;
namespace Kst.Integrations.Qad.Tests.LongTermShortages;
public sealed class QadLongTermShortageSourceReaderTests
{
    [Fact]
    public void SourceBatchCeiling_PartitionsEveryComponentExactlyOnce()
    {
        var parts = Enumerable.Range(0, 600).Select(i => $"COMP-{i}").ToArray();
        var batches = Kst.Domain.Mps.MpsPartBatcher.Batch(parts, QadLongTermShortageSourceReader.MaxSourceBatchSize);
        Assert.Equal([250, 250, 100], batches.Select(b => b.Count));
        Assert.Equal(parts, batches.SelectMany(b => b));
        foreach (var batch in batches)
        {
            var (_, parameters) = QadLongTermShortageSourceReader.BuildBatchQuery("KTC", "SW", batch, new(2027, 3, 1));
            Assert.Equal(batch, Enumerable.Range(0, batch.Count).Select(i => parameters.Get<string>($"Part{i}")));
        }
    }

    [Fact]
    public void BuildBatchQuery_IsParameterizedAndReadsRawMrpAndDirectQoh()
    {
        var (sql, parameters) = QadLongTermShortageSourceReader.BuildBatchQuery("KTC", "SW", ["C1", "C2"], new DateOnly(2027, 3, 1));
        Assert.Equal("C1", parameters.Get<string>("Part0"));
        Assert.DoesNotContain("C1", sql);
        Assert.Contains("COALESCE(NULLIF(LTRIM(RTRIM(ld.ld_status)), ''), loc.loc_status)", sql);
        Assert.Contains("'INSPECT', 'NCMINSP', 'RIP', 'STOCK', 'TRAN', 'VMI'", sql);
        Assert.Contains("UPPER(ld.ld_lot) NOT LIKE 'RMA%'", sql);
        Assert.Contains("UPPER(ld.ld_lot) NOT LIKE 'RA%'", sql);
        Assert.Contains("mrp.mrp_line2", sql);
        Assert.Contains("mrp.prrowid AS SourceRowId", sql);
        Assert.Contains("pod.pod__log01", sql);
        Assert.Contains("ptp.ptp_ord_per AS OrderPeriodDays", sql);
        Assert.Contains("ptp.ptp_mfg_lead AS ManufacturingLeadWorkingDays", sql);
        Assert.Contains("ptp.ptp_pur_lead AS PurchasingLeadCalendarDays", sql);
        Assert.Contains("ptp.ptp_sfty_tme AS SafetyTimeWorkingDays", sql);
        Assert.Contains("pod.MatchCount = 1", sql);
        Assert.Contains("mrp.mrp_nbr", sql);
        Assert.Contains("CONVERT(varchar(50), pod.pod_line) = CONVERT(varchar(50), mrp.mrp_line)", sql);
        Assert.DoesNotContain("DISTINCT mrp", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("in_mstr", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lad_det", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mrp.mrp_due_date < @HorizonEnd OR mrp.mrp_rel_date < @HorizonEnd", sql);
        Assert.Contains("mrp.mrp_type", sql);
        Assert.DoesNotContain("mrp.mrp_dataset = 'pod_det' AND mrp.mrp_due_date", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GROUP BY mrp", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ROUND", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Acquisition_UsesConnectionReadUncommittedWithoutTransactionOrSnapshotGate()
    {
        var integrationRoot = Path.GetFullPath("../../../../../Kst.Integrations.Qad", AppContext.BaseDirectory);
        var connectionSource = System.IO.File.ReadAllText(Path.Combine(integrationRoot, "QadConnectionFactory.cs"));
        var readerSource = System.IO.File.ReadAllText(Path.Combine(integrationRoot, "LongTermShortages", "QadLongTermShortageSourceReader.cs"));
        Assert.Contains("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED", connectionSource);
        Assert.Contains("QadConnectionFactory.OpenAsync", readerSource);
        Assert.DoesNotContain("BeginTransaction", readerSource);
        Assert.DoesNotContain("snapshot_isolation_state_desc", readerSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sys.databases", readerSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SelectEvidence_CollapsesOnlySameExactRowIdentity()
    {
        var date = new DateOnly(2026, 9, 15);
        var first = new LongTermMrpFact(0, "DEMAND", date, null, 4m, MrpScheduleCategory.Unclassified)
        { SourceRowId = "00000000000000000000000000000001", SourceNumber = "WO", SourceLine = "1", SourceLine2 = "10" };
        var second = first with { SourceRowId = "00000000000000000000000000000002", SourceLine2 = "20" };
        var matching = first with { SourceRowId = "00000000000000000000000000000003" };
        var facts = QadLongTermShortageSourceReader.SelectEvidence([first, first, second, matching]);
        Assert.Equal(3, facts.Count);
        Assert.Equal(12m, facts.Sum(f => f.Quantity));
        Assert.Contains(facts, f => f.SourceLine2 == "10" && f.SourceRowId == first.SourceRowId);
        Assert.Contains(facts, f => f.SourceLine2 == "20" && f.SourceRowId == second.SourceRowId);
        Assert.Equal(2, facts.Count(f => f.SourceLine2 == "10"));
        Assert.Equal([1, 2, 3], facts.Select(f => f.EvidenceOrdinal));
    }

    [Fact]
    public void SelectEvidence_RejectsMissingOrConflictingRowIdentity()
    {
        var fact = new LongTermMrpFact(0, "DEMAND", new DateOnly(2026, 9, 15), null, 1m, MrpScheduleCategory.Unclassified) { SourceRowId = "01" };
        Assert.Throws<InvalidOperationException>(() => QadLongTermShortageSourceReader.SelectEvidence([fact with { SourceRowId = null }]));
        Assert.Throws<InvalidOperationException>(() => QadLongTermShortageSourceReader.SelectEvidence([fact, fact with { Quantity = 2m }]));
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
