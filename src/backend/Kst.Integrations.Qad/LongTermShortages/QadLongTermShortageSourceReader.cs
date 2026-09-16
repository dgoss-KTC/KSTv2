using System.Data;
using Dapper;
using Kst.Domain.LongTermShortages;
using Kst.Domain.Mps;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;
using Microsoft.Extensions.Logging;

namespace Kst.Integrations.Qad.LongTermShortages;

/// <summary>Read-only Stage 11-A batch source adapter. Its opening-QOH rule intentionally differs from Stage 9.</summary>
public sealed class QadLongTermShortageSourceReader(QadConnectionOptions options, ILogger<QadLongTermShortageSourceReader> logger)
{
    public async Task<IReadOnlyList<LongTermShortageInput>> ReadAsync(
        string site,
        IReadOnlyDictionary<string, IReadOnlyList<string>> componentParents,
        DateOnly refreshDate,
        DateOnly horizonEnd,
        IReadOnlySet<string> workspaceParents,
        CancellationToken cancellationToken = default)
    {
        if (!options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        if (componentParents.Count == 0) return [];

        var rows = new List<RawRow>();
        await using var connection = await QadConnectionFactory.OpenAsync(options, cancellationToken);
        foreach (var batch in MpsPartBatcher.Batch(componentParents.Keys.ToList(), options.MaxPartBatchSize))
        {
            var (sql, parameters) = BuildBatchQuery(QadSiteDomainMap.Resolve(site), site, batch, refreshDate, horizonEnd);
            rows.AddRange(await connection.QueryAsync<RawRow>(new CommandDefinition(
                sql, parameters, commandTimeout: options.CommandTimeoutSeconds, cancellationToken: cancellationToken)));
        }

        logger.LogInformation("Stage 11-A source read for site {Site} returned {Rows} source facts.", site, rows.Count);
        return rows.GroupBy(row => row.ComponentPart, StringComparer.OrdinalIgnoreCase)
            .Select(group => ToInput(group, componentParents, workspaceParents))
            .ToList();
    }

    public static (string Sql, DynamicParameters Parameters) BuildBatchQuery(
        string domain, string site, IReadOnlyList<string> parts, DateOnly refreshDate, DateOnly horizonEnd)
    {
        ArgumentNullException.ThrowIfNull(parts);
        if (parts.Count == 0) throw new ArgumentException("At least one component part is required.", nameof(parts));

        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        parameters.Add("WeekStart", MpsBusinessCalendar.GetBusinessWeekStart(refreshDate).ToDateTime(TimeOnly.MinValue), DbType.Date);
        parameters.Add("HorizonEnd", horizonEnd.ToDateTime(TimeOnly.MinValue), DbType.Date);
        var values = parts.Select((part, index) => { parameters.Add($"Part{index}", part); return $"(@Part{index})"; });

        var sql = $"""
            WITH ScopeParts (PartNumber) AS
            (
                SELECT PartNumber FROM (VALUES {string.Join(", ", values)}) AS Parts (PartNumber)
            ),
            Qoh AS
            (
                SELECT ld.ld_part AS ComponentPart, COALESCE(SUM(ld.ld_qty_oh), 0) AS OpeningQoh
                FROM qadpro2.dbo.ld_det AS ld
                WHERE ld.ld_domain = @Domain AND ld.ld_site = @Site
                  AND ld.ld_part IN (SELECT PartNumber FROM ScopeParts)
                  AND UPPER(ld.ld_status) NOT IN ('MRB', 'INSPECT', 'NCMINSP')
                  AND UPPER(ld.ld_lot) NOT LIKE 'RMA%'
                  AND UPPER(ld.ld_lot) NOT LIKE 'RA%'
                GROUP BY ld.ld_part
            ),
            KssComponents AS
            (
                SELECT DISTINCT pod.pod_part AS ComponentPart
                FROM qadpro2.dbo.pod_det AS pod
                INNER JOIN qadpro2.dbo.po_mstr AS po ON po.po_domain = pod.pod_domain AND po.po_nbr = pod.pod_nbr
                WHERE pod.pod_domain = @Domain AND pod.pod_site = @Site
                  AND pod.pod_part IN (SELECT PartNumber FROM ScopeParts)
                  AND (pod.pod_end_eff##1 IS NULL OR pod.pod_end_eff##1 >= @WeekStart)
                  AND po.po_sched = 1 AND (po.po_eff_to IS NULL OR po.po_eff_to >= @WeekStart)
            ),
            FirmAllocation AS
            (
                SELECT lad.lad_nbr AS Woid, lad.lad_part AS ComponentPart, lad.lad_line AS OperationNumber,
                       SUM(lad.lad_qty_all) AS Quantity
                FROM qadpro2.dbo.lad_det AS lad
                INNER JOIN qadpro2.dbo.ld_det AS ld ON ld.ld_domain = lad.lad_domain AND ld.ld_site = lad.lad_site AND ld.ld_part = lad.lad_part AND ld.ld_loc = lad.lad_loc AND ld.ld_lot = lad.lad_lot
                INNER JOIN qadpro2.dbo.loc_mstr AS loc ON loc.loc_domain = ld.ld_domain AND loc.loc_site = ld.ld_site AND loc.loc_loc = ld.ld_loc
                INNER JOIN qadpro2.dbo.is_mstr AS inv ON inv.is_domain = loc.loc_domain AND inv.is_status = loc.loc_status
                WHERE lad.lad_domain = @Domain AND lad.lad_site = @Site AND lad.lad_dataset = 'wod_det' AND lad.lad_qty_all > 0
                  AND UPPER(inv.is_status) = 'STOCK' AND inv.is_nettable = 1
                  AND UPPER(ld.ld_lot) NOT LIKE 'RA%'
                  AND (ld.ld_expire IS NULL OR ld.ld_expire > DATEADD(day, (SELECT TOP (1) icc.icc_iss_days FROM qadpro2.dbo.icc_ctrl AS icc WHERE icc.icc_domain = @Domain AND icc.icc_site = @Site), @WeekStart))
                GROUP BY lad.lad_nbr, lad.lad_part, lad.lad_line
            ),
            Facts AS
            (
                SELECT wod.wod_part AS ComponentPart, 'W' AS FactType, wo.wo_lot AS Woid, wod.wod_op AS OperationNumber,
                       wo.wo_part AS ParentPart, wo.wo_due_date AS DueDate,
                       CASE WHEN wod.wod_qty_req - wod.wod_qty_iss - ISNULL(fa.Quantity, 0) > 0 THEN wod.wod_qty_req - wod.wod_qty_iss - ISNULL(fa.Quantity, 0) ELSE 0 END AS Quantity,
                       CAST(NULL AS nvarchar(80)) AS PoNumber, CAST(NULL AS int) AS PoLine, CAST(NULL AS bit) AS Confirmed, CAST(NULL AS nvarchar(80)) AS ManufacturerItem, CAST(NULL AS bit) AS IsScheduled
                FROM qadpro2.dbo.wo_mstr AS wo
                INNER JOIN qadpro2.dbo.wod_det AS wod ON wod.wod_domain = wo.wo_domain AND wod.wod_lot = wo.wo_lot
                LEFT JOIN FirmAllocation AS fa ON fa.Woid = wod.wod_lot AND fa.ComponentPart = wod.wod_part AND fa.OperationNumber = wod.wod_op
                WHERE wo.wo_domain = @Domain AND wo.wo_site = @Site AND wod.wod_part IN (SELECT PartNumber FROM ScopeParts)
                  AND wo.wo_status IN ('A', 'F', 'R', 'E', 'P') AND ISNULL(wo.wo_bom_code, '') <> 'RMABOM'
                  AND wo.wo_due_date < @HorizonEnd
                UNION ALL
                SELECT mrp.mrp_part, 'F', CAST(NULL AS nvarchar(80)), NULL, NULL, mrp.mrp_due_date, mrp.mrp_qty, NULL, NULL, NULL, NULL, NULL
                FROM qadpro2.dbo.mrp_det AS mrp
                WHERE mrp.mrp_domain = @Domain AND mrp.mrp_site = @Site AND mrp.mrp_part IN (SELECT PartNumber FROM ScopeParts)
                  AND mrp.mrp_dataset = 'fcs_sum' AND mrp.mrp_due_date >= @WeekStart AND mrp.mrp_due_date < @HorizonEnd
                UNION ALL
                SELECT pod.pod_part, 'P', NULL, NULL, NULL, pod.pod_due_date, pod.pod_qty_ord - pod.pod_qty_rcvd, pod.pod_nbr, pod.pod_line, pod.pod__log01, pod.pod_vpart, CAST(0 AS bit)
                FROM qadpro2.dbo.pod_det AS pod
                INNER JOIN qadpro2.dbo.po_mstr AS po ON po.po_domain = pod.pod_domain AND po.po_nbr = pod.pod_nbr
                WHERE pod.pod_domain = @Domain AND pod.pod_site = @Site AND pod.pod_part IN (SELECT PartNumber FROM ScopeParts)
                  AND LOWER(ISNULL(pod.pod_status, '')) NOT IN ('c', 'x') AND pod.pod_qty_ord - pod.pod_qty_rcvd > 0
                  AND ISNULL(po.po_sched, 0) = 0 AND (pod.pod_due_date IS NULL OR pod.pod_due_date < @HorizonEnd)
            )
            SELECT s.PartNumber AS ComponentPart, pm.pt_um AS UnitOfMeasure, pm.pt_status AS QadStatus, pm.pt_desc1 AS Description,
                   ISNULL(ptp.ptp_pur_lead, pm.pt_pur_lead) AS LeadTimeDays, cm.code_user1 AS Planner,
                   CASE WHEN LTRIM(RTRIM(ISNULL(ptp.ptp_buyer, ''))) <> '' THEN ptp.ptp_buyer ELSE pm.pt_buyer END AS BuyerPlannerCode,
                   CAST(CASE WHEN ptp.ptp_part IS NOT NULL AND ptp.ptp_sfty_stk IS NULL THEN 1 ELSE 0 END AS bit) AS SiteSafetyMissing,
                   CASE WHEN ptp.ptp_part IS NULL THEN pm.pt_sfty_stk ELSE ptp.ptp_sfty_stk END AS SafetyStock, ISNULL(q.OpeningQoh, 0) AS OpeningQoh,
                   CAST(CASE WHEN kc.ComponentPart IS NULL THEN 0 ELSE 1 END AS bit) AS IsKss,
                   f.FactType, f.Woid, f.OperationNumber, f.ParentPart, f.DueDate, f.Quantity, f.PoNumber, f.PoLine, f.Confirmed, f.ManufacturerItem, f.IsScheduled
            FROM ScopeParts AS s
            LEFT JOIN qadpro2.dbo.pt_mstr AS pm ON pm.pt_domain = @Domain AND pm.pt_part = s.PartNumber
            LEFT JOIN qadpro2.dbo.ptp_det AS ptp ON ptp.ptp_domain = @Domain AND ptp.ptp_site = @Site AND ptp.ptp_part = s.PartNumber
            LEFT JOIN qadpro2.dbo.code_mstr AS cm ON cm.code_domain = @Domain AND cm.code_fldname = CASE WHEN LTRIM(RTRIM(ISNULL(ptp.ptp_buyer, ''))) <> '' THEN 'ptp_buyer' ELSE 'pt_buyer' END AND cm.code_value = CASE WHEN LTRIM(RTRIM(ISNULL(ptp.ptp_buyer, ''))) <> '' THEN ptp.ptp_buyer ELSE pm.pt_buyer END
            LEFT JOIN Qoh AS q ON q.ComponentPart = s.PartNumber
            LEFT JOIN KssComponents AS kc ON kc.ComponentPart = s.PartNumber
            LEFT JOIN Facts AS f ON f.ComponentPart = s.PartNumber;
            """;
        return (sql, parameters);
    }

    private static LongTermShortageInput ToInput(IGrouping<string, RawRow> rows, IReadOnlyDictionary<string, IReadOnlyList<string>> parents, IReadOnlySet<string> workspaceParents)
    {
        var first = rows.First();
        var demands = new List<LongTermDemandEvent>();
        var purchaseOrders = new List<LongTermPurchaseOrder>();
        var otherParents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (row.FactType is "W" or "F" && row.DueDate is not null && row.Quantity is not null)
            {
                demands.Add(new LongTermDemandEvent(DateOnly.FromDateTime(row.DueDate.Value), row.Quantity.Value, row.FactType == "F"));
                if (row.FactType == "W" && !string.IsNullOrWhiteSpace(row.ParentPart) && !workspaceParents.Contains(row.ParentPart)) otherParents.Add(row.ParentPart);
            }
            if (row.FactType == "P" && !string.IsNullOrEmpty(row.PoNumber)) purchaseOrders.Add(new LongTermPurchaseOrder(row.PoNumber, row.PoLine ?? 0, row.DueDate is null ? null : DateOnly.FromDateTime(row.DueDate.Value), row.Quantity ?? 0, row.Confirmed, row.ManufacturerItem, row.IsScheduled));
        }
        return new LongTermShortageInput(first.ComponentPart, first.UnitOfMeasure, first.QadStatus, first.Description, first.IsKss, first.LeadTimeDays, first.Planner, first.BuyerPlannerCode, first.OpeningQoh,
            first.SiteSafetyMissing ? SafetyStockState.SelectedSiteValueMissing : SafetyStockState.Resolved, first.SafetyStock,
            parents.TryGetValue(first.ComponentPart, out var demandParents) ? demandParents : [], otherParents.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList(), demands, purchaseOrders);
    }

    private sealed class RawRow
    {
        public string ComponentPart { get; set; } = string.Empty;
        public string? UnitOfMeasure { get; set; }
        public string? QadStatus { get; set; }
        public string? Description { get; set; }
        public int? LeadTimeDays { get; set; }
        public string? Planner { get; set; }
        public string? BuyerPlannerCode { get; set; }
        public bool SiteSafetyMissing { get; set; }
        public decimal? SafetyStock { get; set; }
        public decimal OpeningQoh { get; set; }
        public bool IsKss { get; set; }
        public string? FactType { get; set; }
        public string? Woid { get; set; }
        public string? OperationNumber { get; set; }
        public string? ParentPart { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal? Quantity { get; set; }
        public string? PoNumber { get; set; }
        public int? PoLine { get; set; }
        public bool? Confirmed { get; set; }
        public string? ManufacturerItem { get; set; }
        public bool IsScheduled { get; set; }
    }
}
