using Dapper;
using Kst.Domain.ComponentDetail;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.PartInfo;

/// <summary>Bounded informational subset of accepted Stage 11 presentation/planning mappings. No MRP acquisition.</summary>
public sealed class QadPartInfoContextReader(QadConnectionOptions options)
{
    public async Task<PartInfoContext> ReadAsync(string site, string part, CancellationToken ct)
    {
        if (!options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        var (sql, parameters) = BuildQuery(QadSiteDomainMap.Resolve(site), site, part);
        await using var connection = await QadConnectionFactory.OpenAsync(options, ct);
        var row = await connection.QuerySingleAsync<ContextRow>(new CommandDefinition(sql, parameters,
            commandTimeout: options.CommandTimeoutSeconds, cancellationToken: ct));
        return new(row.ManufacturerItem, row.EffectivePmCode, row.ManufacturingLeadWorkingDays);
    }

    public static (string Sql, DynamicParameters Parameters) BuildQuery(string domain, string site, string part)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain); parameters.Add("Site", site); parameters.Add("Part", part);
        const string sql = """
            SELECT context.ManufacturerItem,
                COALESCE(NULLIF(LTRIM(RTRIM(ptp.ptp_pm_code)), ''), pm.pt_pm_code) AS EffectivePmCode,
                ptp.ptp_mfg_lead AS ManufacturingLeadWorkingDays
            FROM qadpro2.dbo.pt_mstr AS pm
            LEFT JOIN qadpro2.dbo.ptp_det AS ptp ON ptp.ptp_domain = pm.pt_domain
                AND ptp.ptp_part = pm.pt_part AND ptp.ptp_site = @Site
            OUTER APPLY (
                SELECT TOP (1) pod.pod_vpart AS ManufacturerItem
                FROM qadpro2.dbo.pod_det AS pod
                WHERE pod.pod_domain = @Domain AND pod.pod_site = @Site AND pod.pod_part = @Part
                    AND LOWER(ISNULL(pod.pod_status, '')) NOT IN ('c', 'x')
                    AND pod.pod_qty_ord - pod.pod_qty_rcvd > 0 AND pod.pod__log01 = 1
                ORDER BY pod.pod_due_date, pod.pod_nbr, pod.pod_line
            ) AS context
            WHERE pm.pt_domain = @Domain AND pm.pt_part = @Part;
            """;
        return (sql, parameters);
    }

    // Property binding follows the accepted wide-reader pattern and tolerates source integer coercion.
    private sealed class ContextRow
    {
        public string? ManufacturerItem { get; set; }
        public string? EffectivePmCode { get; set; }
        public int? ManufacturingLeadWorkingDays { get; set; }
    }
}
