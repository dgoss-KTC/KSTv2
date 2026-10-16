using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.PartInfo;

namespace Kst.Integrations.Qad.Tests.PartInfo;

public sealed class QadPartInfoContextTests
{
    [Theory]
    [InlineData("SW", "KTC")]
    [InlineData("KV", "KTV")]
    public void QueryIsBoundedAndRetainsAcceptedSelection(string site, string domain)
    {
        Assert.Equal(domain, QadSiteDomainMap.Resolve(site));
        const string part = "EXACT' OR 1=1--";
        var (sql, p) = QadPartInfoContextReader.BuildQuery(domain, site, part);
        Assert.Equal(part, p.Get<string>("Part")); Assert.Equal(site, p.Get<string>("Site")); Assert.Equal(domain, p.Get<string>("Domain"));
        Assert.DoesNotContain(part, sql);
        Assert.Contains("pm.pt_domain = @Domain AND pm.pt_part = @Part", sql);
        Assert.Contains("pod.pod_domain = @Domain AND pod.pod_site = @Site AND pod.pod_part = @Part", sql);
        Assert.Contains("pod.pod_qty_ord - pod.pod_qty_rcvd > 0 AND pod.pod__log01 = 1", sql);
        Assert.Contains("ORDER BY pod.pod_due_date, pod.pod_nbr, pod.pod_line", sql);
        Assert.Contains("ptp.ptp_mfg_lead AS ManufacturingLeadWorkingDays", sql);
        Assert.DoesNotContain("mrp_det", sql); Assert.DoesNotContain("ld_det", sql);
    }
}
