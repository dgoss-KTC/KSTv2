using System.Data;
using Dapper;
using Kst.Domain.LongTermShortages;
using Kst.Domain.Mps;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;
using Microsoft.Extensions.Logging;

namespace Kst.Integrations.Qad.LongTermShortages;

/// <summary>Read-only raw QAD MRP and direct ld_det opening-QOH adapter.</summary>
public sealed class QadLongTermShortageSourceReader(QadConnectionOptions options, ILogger<QadLongTermShortageSourceReader> logger)
{
    // Stage 11-local measured ceiling; do not change the shared MPS/Stage 9/10 batch policy.
    public const int MaxSourceBatchSize = 250;

    public async Task<LongTermShortageAcquisition> ReadAsync(string site, IReadOnlyDictionary<string, IReadOnlyList<string>> componentParents, DateOnly refreshDate, DateOnly horizonEnd, CancellationToken cancellationToken = default)
    {
        if (!options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        if (componentParents.Count == 0) return new(DateTimeOffset.UtcNow, []);
        var rows = new List<RawRow>();
        await using var connection = await QadConnectionFactory.OpenAsync(options, cancellationToken);
        // QadConnectionFactory sets READ UNCOMMITTED on this connection. Pro2 reports
        // must not hold a transaction across batches or block the reporting replica.
        foreach (var batch in MpsPartBatcher.Batch(componentParents.Keys.ToList(), Math.Min(options.MaxPartBatchSize, MaxSourceBatchSize)))
        {
            var (sql, parameters) = BuildBatchQuery(QadSiteDomainMap.Resolve(site), site, batch, horizonEnd);
            rows.AddRange(await connection.QueryAsync<RawRow>(new CommandDefinition(sql, parameters, commandTimeout: options.CommandTimeoutSeconds, cancellationToken: cancellationToken)));
        }
        var presentation = new Dictionary<string, LongTermShortagePresentationContext>(StringComparer.OrdinalIgnoreCase);
        foreach (var batch in MpsPartBatcher.Batch(componentParents.Keys.ToList(), options.MaxPartBatchSize))
        {
            var (sql, parameters) = BuildPresentationQuery(QadSiteDomainMap.Resolve(site), site, batch, refreshDate);
            foreach (var row in await connection.QueryAsync<PresentationRow>(new CommandDefinition(sql, parameters, commandTimeout: options.CommandTimeoutSeconds, cancellationToken: cancellationToken)))
                presentation[row.ComponentPart] = new LongTermShortagePresentationContext(row.ManufacturerItem, row.PoNumber, row.PoLine,
                    ToDateOnly(row.PoDueDate), row.PoOpenQuantity, row.PoConfirmed, row.IsKss);
        }
        var acquiredAtUtc = DateTimeOffset.UtcNow;
        if (rows.GroupBy(row => row.ComponentPart, StringComparer.OrdinalIgnoreCase).Count() != componentParents.Count)
            throw new InvalidOperationException("The QAD component batch was incomplete.");
        if (presentation.Count != componentParents.Count || componentParents.Keys.Any(part => !presentation.ContainsKey(part)))
            throw new InvalidOperationException("The QAD presentation batch was incomplete.");
        logger.LogInformation("Stage 11 Component MRP source read for site {Site} returned {Rows} raw facts.", site, rows.Count);
        return new(acquiredAtUtc, rows.GroupBy(row => row.ComponentPart, StringComparer.OrdinalIgnoreCase).Select(group => ToInput(group, componentParents, presentation)).ToList());
    }

    public static (string Sql, DynamicParameters Parameters) BuildBatchQuery(string domain, string site, IReadOnlyList<string> parts, DateOnly horizonEnd)
    {
        ArgumentNullException.ThrowIfNull(parts);
        if (parts.Count == 0) throw new ArgumentException("At least one component part is required.", nameof(parts));
        var parameters = new DynamicParameters(); parameters.Add("Domain", domain); parameters.Add("Site", site); parameters.Add("HorizonEnd", horizonEnd.ToDateTime(TimeOnly.MinValue), DbType.Date);
        var values = parts.Select((part, index) => { parameters.Add($"Part{index}", part); return $"(@Part{index})"; });
        var sql = $"""
            WITH ScopeParts (PartNumber) AS (SELECT PartNumber FROM (VALUES {string.Join(", ", values)}) AS Parts (PartNumber)),
            Qoh AS (
                SELECT ld.ld_part AS ComponentPart, COALESCE(SUM(ld.ld_qty_oh), 0) AS OpeningQoh
                FROM qadpro2.dbo.ld_det AS ld
                LEFT JOIN qadpro2.dbo.loc_mstr AS loc ON loc.loc_domain = ld.ld_domain AND loc.loc_site = ld.ld_site AND loc.loc_loc = ld.ld_loc
                WHERE ld.ld_domain = @Domain AND ld.ld_site = @Site AND ld.ld_part IN (SELECT PartNumber FROM ScopeParts)
                  AND UPPER(ld.ld_lot) NOT LIKE 'RMA%' AND UPPER(ld.ld_lot) NOT LIKE 'RA%'
                  AND UPPER(COALESCE(NULLIF(LTRIM(RTRIM(ld.ld_status)), ''), loc.loc_status)) IN ('INSPECT', 'NCMINSP', 'RIP', 'STOCK', 'TRAN', 'VMI')
                GROUP BY ld.ld_part)
            SELECT s.PartNumber AS ComponentPart, pm.pt_um AS UnitOfMeasure, pm.pt_status AS QadStatus, pm.pt_desc1 AS Description,
                   cm.code_user1 AS Planner, CASE WHEN LTRIM(RTRIM(ISNULL(ptp.ptp_buyer, ''))) <> '' THEN ptp.ptp_buyer ELSE pm.pt_buyer END AS BuyerPlannerCode,
                   CAST(CASE WHEN ptp.ptp_part IS NOT NULL AND ptp.ptp_sfty_stk IS NULL THEN 1 ELSE 0 END AS bit) AS SiteSafetyMissing,
                   CAST(CASE WHEN ptp.ptp_part IS NOT NULL THEN 1 ELSE 0 END AS bit) AS SitePlanningPresent,
                   ptp.ptp_sfty_stk AS SafetyStock, ISNULL(q.OpeningQoh, 0) AS OpeningQoh,
                   COALESCE(NULLIF(LTRIM(RTRIM(ptp.ptp_pm_code)), ''), pm.pt_pm_code) AS EffectivePmCode,
                   ptp.ptp_ord_per AS OrderPeriodDays, ptp.ptp_sfty_tme AS SafetyTimeWorkingDays,
                   ptp.ptp_mfg_lead AS ManufacturingLeadWorkingDays, ptp.ptp_pur_lead AS PurchasingLeadCalendarDays,
                   ptp.ptp_cum_lead AS CumulativeLeadCalendarDays,
                   mrp.mrp_type AS MrpType, mrp.mrp_due_date AS DueDate, mrp.mrp_rel_date AS ReleaseDate, mrp.mrp_qty AS Quantity,
                    mrp.mrp_nbr AS SourceNumber, CONVERT(varchar(50), mrp.mrp_line) AS SourceLine, CONVERT(varchar(50), mrp.mrp_line2) AS SourceLine2,
                    mrp.prrowid AS SourceRowId,
                   CAST(CASE WHEN mrp.mrp_dataset = 'pod_det' THEN 1 ELSE 0 END AS bit) AS IsPoReceipt,
                   CASE WHEN mrp.mrp_dataset = 'pod_det' AND pod.MatchCount = 1 THEN pod.PoConfirmed END AS PoConfirmed
            FROM ScopeParts AS s
            LEFT JOIN qadpro2.dbo.pt_mstr AS pm ON pm.pt_domain = @Domain AND pm.pt_part = s.PartNumber
            LEFT JOIN qadpro2.dbo.ptp_det AS ptp ON ptp.ptp_domain = @Domain AND ptp.ptp_site = @Site AND ptp.ptp_part = s.PartNumber
            LEFT JOIN qadpro2.dbo.code_mstr AS cm ON cm.code_domain = @Domain AND cm.code_fldname = CASE WHEN LTRIM(RTRIM(ISNULL(ptp.ptp_buyer, ''))) <> '' THEN 'ptp_buyer' ELSE 'pt_buyer' END AND cm.code_value = CASE WHEN LTRIM(RTRIM(ISNULL(ptp.ptp_buyer, ''))) <> '' THEN ptp.ptp_buyer ELSE pm.pt_buyer END
            LEFT JOIN Qoh AS q ON q.ComponentPart = s.PartNumber
            LEFT JOIN qadpro2.dbo.mrp_det AS mrp ON mrp.mrp_domain = @Domain AND mrp.mrp_site = @Site AND mrp.mrp_part = s.PartNumber
                AND (mrp.mrp_due_date < @HorizonEnd OR mrp.mrp_rel_date < @HorizonEnd)
            OUTER APPLY (SELECT COUNT(*) AS MatchCount, MAX(CAST(pod.pod__log01 AS int)) AS PoConfirmed
                FROM qadpro2.dbo.pod_det AS pod WHERE mrp.mrp_dataset = 'pod_det' AND pod.pod_domain = mrp.mrp_domain
                AND pod.pod_site = mrp.mrp_site AND pod.pod_part = mrp.mrp_part AND pod.pod_nbr = mrp.mrp_nbr
                AND CONVERT(varchar(50), pod.pod_line) = CONVERT(varchar(50), mrp.mrp_line)) AS pod
            ORDER BY s.PartNumber, mrp.mrp_type, mrp.mrp_due_date, mrp.mrp_rel_date, mrp.mrp_qty, mrp.mrp_nbr, mrp.mrp_line, mrp.mrp_line2;
            """;
        return (sql, parameters);
    }

    /// <summary>
    /// Reads one bounded presentation record per component in a separate command. It deliberately
    /// does not join MRP facts, so PO/KSS context cannot multiply or change schedule inputs.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildPresentationQuery(string domain, string site, IReadOnlyList<string> parts, DateOnly refreshDate)
    {
        ArgumentNullException.ThrowIfNull(parts);
        if (parts.Count == 0) throw new ArgumentException("At least one component part is required.", nameof(parts));
        var parameters = new DynamicParameters(); parameters.Add("Domain", domain); parameters.Add("Site", site); parameters.Add("RefreshDate", refreshDate.ToDateTime(TimeOnly.MinValue), DbType.Date);
        var values = parts.Select((part, index) => { parameters.Add($"Part{index}", part); return $"(@Part{index})"; });
        var sql = $"""
            WITH ScopeParts (PartNumber) AS (SELECT PartNumber FROM (VALUES {string.Join(", ", values)}) AS Parts (PartNumber)),
            KssComponents AS (
                SELECT DISTINCT pod.pod_part AS ComponentPart
                FROM qadpro2.dbo.pod_det AS pod
                INNER JOIN qadpro2.dbo.po_mstr AS po ON po.po_domain = pod.pod_domain AND po.po_nbr = pod.pod_nbr
                WHERE pod.pod_domain = @Domain AND pod.pod_site = @Site AND pod.pod_part IN (SELECT PartNumber FROM ScopeParts)
                  AND (pod.pod_end_eff##1 IS NULL OR pod.pod_end_eff##1 >= @RefreshDate)
                  AND po.po_sched = 1 AND (po.po_eff_to IS NULL OR po.po_eff_to >= @RefreshDate)
            )
            SELECT s.PartNumber AS ComponentPart, context.PoNumber, context.PoLine, context.PoDueDate, context.PoOpenQuantity,
                   context.PoConfirmed, context.ManufacturerItem, CAST(CASE WHEN k.ComponentPart IS NOT NULL THEN 1 ELSE 0 END AS bit) AS IsKss
            FROM ScopeParts AS s
            OUTER APPLY (
                SELECT TOP (1) pod.pod_nbr AS PoNumber, pod.pod_line AS PoLine, pod.pod_due_date AS PoDueDate,
                       pod.pod_qty_ord - pod.pod_qty_rcvd AS PoOpenQuantity, pod.pod__log01 AS PoConfirmed, pod.pod_vpart AS ManufacturerItem
                FROM qadpro2.dbo.pod_det AS pod
                WHERE pod.pod_domain = @Domain AND pod.pod_site = @Site AND pod.pod_part = s.PartNumber
                  AND LOWER(ISNULL(pod.pod_status, '')) NOT IN ('c', 'x')
                  AND pod.pod_qty_ord - pod.pod_qty_rcvd > 0 AND pod.pod__log01 = 1
                ORDER BY pod.pod_due_date, pod.pod_nbr, pod.pod_line
            ) AS context
            LEFT JOIN KssComponents AS k ON k.ComponentPart = s.PartNumber
            ORDER BY s.PartNumber;
            """;
        return (sql, parameters);
    }

    private static LongTermShortageInput ToInput(IGrouping<string, RawRow> rows, IReadOnlyDictionary<string, IReadOnlyList<string>> parents, IReadOnlyDictionary<string, LongTermShortagePresentationContext> presentation)
    {
        var first = rows.First();
        var evidence = SelectEvidence(rows.Where(row => row.MrpType is not null || row.DueDate is not null || row.ReleaseDate is not null || row.Quantity is not null)
            .Select(row => new LongTermMrpFact(0, row.MrpType, ToDateOnly(row.DueDate), ToDateOnly(row.ReleaseDate), row.Quantity ?? 0m, MrpScheduleCategory.Unclassified)
            { SourceNumber = row.SourceNumber, SourceLine = row.SourceLine, SourceLine2 = row.SourceLine2, SourceRowId = row.SourceRowId, IsPoReceipt = row.IsPoReceipt, PoConfirmed = row.PoConfirmed })).ToList();
        if (rows.Any(row => row.MrpType is not null && row.Quantity is null) || evidence.Any(f => f.IsPoReceipt == true && f.PoConfirmed is null && string.Equals(f.Type, "SUPPLY", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("MRP quantity or PO confirmation is unavailable for a source event.");
        return new LongTermShortageInput(first.ComponentPart, first.UnitOfMeasure, first.QadStatus, first.Description, first.Planner, first.BuyerPlannerCode, first.OpeningQoh,
            first.SiteSafetyMissing ? SafetyStockState.SelectedSiteValueMissing : SafetyStockState.Resolved, first.SafetyStock,
            parents.TryGetValue(first.ComponentPart, out var demandParents) ? demandParents : [], evidence,
            presentation.TryGetValue(first.ComponentPart, out var context) ? context : null)
        {
            EffectivePmCode = first.EffectivePmCode, OrderPeriodDays = first.OrderPeriodDays,
            SafetyTimeWorkingDays = first.SafetyTimeWorkingDays, ManufacturingLeadWorkingDays = first.ManufacturingLeadWorkingDays,
            PurchasingLeadCalendarDays = first.PurchasingLeadCalendarDays, CumulativeLeadCalendarDays = first.CumulativeLeadCalendarDays,
            SitePlanningPresent = first.SitePlanningPresent
        };
    }
    public static IReadOnlyList<LongTermMrpFact> SelectEvidence(IEnumerable<LongTermMrpFact> facts)
    {
        var seen = new Dictionary<string, LongTermMrpFact>(StringComparer.OrdinalIgnoreCase);
        var distinct = new List<LongTermMrpFact>();
        foreach (var fact in facts)
        {
            if (string.IsNullOrWhiteSpace(fact.SourceRowId))
                throw new InvalidOperationException("An MRP source row has no exact prrowid.");
            if (seen.TryGetValue(fact.SourceRowId, out var prior))
            {
                // A dirty scan can observe the same physical row before and after an update.
                // Different values for one ID are not a safe basis for a projected balance.
                if ((prior with { EvidenceOrdinal = 0 }) != (fact with { EvidenceOrdinal = 0 }))
                    throw new InvalidOperationException("An MRP source row has conflicting values for one prrowid.");
                continue;
            }
            seen.Add(fact.SourceRowId, fact);
            distinct.Add(fact);
        }
        return distinct.OrderBy(f => f.Type, StringComparer.Ordinal).ThenBy(f => f.DueDate).ThenBy(f => f.ReleaseDate)
            .ThenBy(f => f.SourceRowId, StringComparer.OrdinalIgnoreCase)
            .Select((fact, index) => fact with { EvidenceOrdinal = index + 1 }).ToList();
    }
    private static DateOnly? ToDateOnly(DateTime? value) => value is null ? null : DateOnly.FromDateTime(value.Value);
    private sealed class RawRow { public string ComponentPart { get; set; } = string.Empty; public string? UnitOfMeasure { get; set; } public string? QadStatus { get; set; } public string? Description { get; set; } public string? Planner { get; set; } public string? BuyerPlannerCode { get; set; } public bool SiteSafetyMissing { get; set; } public bool SitePlanningPresent { get; set; } public decimal? SafetyStock { get; set; } public decimal OpeningQoh { get; set; } public string? EffectivePmCode { get; set; } public int? OrderPeriodDays { get; set; } public decimal? SafetyTimeWorkingDays { get; set; } public int? ManufacturingLeadWorkingDays { get; set; } public int? PurchasingLeadCalendarDays { get; set; } public int? CumulativeLeadCalendarDays { get; set; } public string? MrpType { get; set; } public DateTime? DueDate { get; set; } public DateTime? ReleaseDate { get; set; } public decimal? Quantity { get; set; } public string? SourceNumber { get; set; } public string? SourceLine { get; set; } public string? SourceLine2 { get; set; } public string? SourceRowId { get; set; } public bool IsPoReceipt { get; set; } public bool? PoConfirmed { get; set; } }
    private sealed class PresentationRow { public string ComponentPart { get; set; } = string.Empty; public string? ManufacturerItem { get; set; } public string? PoNumber { get; set; } public int? PoLine { get; set; } public DateTime? PoDueDate { get; set; } public decimal? PoOpenQuantity { get; set; } public bool? PoConfirmed { get; set; } public bool IsKss { get; set; } }
}
