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
    public async Task<IReadOnlyList<LongTermShortageInput>> ReadAsync(string site, IReadOnlyDictionary<string, IReadOnlyList<string>> componentParents, DateOnly horizonEnd, CancellationToken cancellationToken = default)
    {
        if (!options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        if (componentParents.Count == 0) return [];
        var rows = new List<RawRow>();
        await using var connection = await QadConnectionFactory.OpenAsync(options, cancellationToken);
        foreach (var batch in MpsPartBatcher.Batch(componentParents.Keys.ToList(), options.MaxPartBatchSize))
        {
            var (sql, parameters) = BuildBatchQuery(QadSiteDomainMap.Resolve(site), site, batch, horizonEnd);
            rows.AddRange(await connection.QueryAsync<RawRow>(new CommandDefinition(sql, parameters, commandTimeout: options.CommandTimeoutSeconds, cancellationToken: cancellationToken)));
        }
        var presentation = new Dictionary<string, LongTermShortagePresentationContext>(StringComparer.OrdinalIgnoreCase);
        foreach (var batch in MpsPartBatcher.Batch(componentParents.Keys.ToList(), options.MaxPartBatchSize))
        {
            var (sql, parameters) = BuildPresentationQuery(QadSiteDomainMap.Resolve(site), site, batch, horizonEnd.AddDays(-LongTermShortagesBuilder.WeekCount * 7));
            foreach (var row in await connection.QueryAsync<PresentationRow>(new CommandDefinition(sql, parameters, commandTimeout: options.CommandTimeoutSeconds, cancellationToken: cancellationToken)))
                presentation[row.ComponentPart] = new LongTermShortagePresentationContext(row.ManufacturerItem, row.PoNumber, row.PoLine,
                    ToDateOnly(row.PoDueDate), row.PoOpenQuantity, row.PoConfirmed, row.IsKss);
        }
        logger.LogInformation("Stage 11-A shared MRP source read for site {Site} returned {Rows} raw facts.", site, rows.Count);
        return rows.GroupBy(row => row.ComponentPart, StringComparer.OrdinalIgnoreCase).Select(group => ToInput(group, componentParents, presentation)).ToList();
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
                WHERE ld.ld_domain = @Domain AND ld.ld_site = @Site AND ld.ld_part IN (SELECT PartNumber FROM ScopeParts)
                  AND UPPER(ld.ld_status) <> 'MRB' AND UPPER(ld.ld_lot) NOT LIKE 'RMA%' AND UPPER(ld.ld_lot) NOT LIKE 'RA%'
                GROUP BY ld.ld_part)
            SELECT s.PartNumber AS ComponentPart, pm.pt_um AS UnitOfMeasure, pm.pt_status AS QadStatus, pm.pt_desc1 AS Description,
                   cm.code_user1 AS Planner, CASE WHEN LTRIM(RTRIM(ISNULL(ptp.ptp_buyer, ''))) <> '' THEN ptp.ptp_buyer ELSE pm.pt_buyer END AS BuyerPlannerCode,
                   CAST(CASE WHEN ptp.ptp_part IS NOT NULL AND ptp.ptp_sfty_stk IS NULL THEN 1 ELSE 0 END AS bit) AS SiteSafetyMissing,
                   CASE WHEN ptp.ptp_part IS NULL THEN pm.pt_sfty_stk ELSE ptp.ptp_sfty_stk END AS SafetyStock, ISNULL(q.OpeningQoh, 0) AS OpeningQoh,
                   mrp.mrp_type AS MrpType, mrp.mrp_due_date AS DueDate, mrp.mrp_rel_date AS ReleaseDate, mrp.mrp_qty AS Quantity
            FROM ScopeParts AS s
            LEFT JOIN qadpro2.dbo.pt_mstr AS pm ON pm.pt_domain = @Domain AND pm.pt_part = s.PartNumber
            LEFT JOIN qadpro2.dbo.ptp_det AS ptp ON ptp.ptp_domain = @Domain AND ptp.ptp_site = @Site AND ptp.ptp_part = s.PartNumber
            LEFT JOIN qadpro2.dbo.code_mstr AS cm ON cm.code_domain = @Domain AND cm.code_fldname = CASE WHEN LTRIM(RTRIM(ISNULL(ptp.ptp_buyer, ''))) <> '' THEN 'ptp_buyer' ELSE 'pt_buyer' END AND cm.code_value = CASE WHEN LTRIM(RTRIM(ISNULL(ptp.ptp_buyer, ''))) <> '' THEN ptp.ptp_buyer ELSE pm.pt_buyer END
            LEFT JOIN Qoh AS q ON q.ComponentPart = s.PartNumber
            LEFT JOIN qadpro2.dbo.mrp_det AS mrp ON mrp.mrp_domain = @Domain AND mrp.mrp_site = @Site AND mrp.mrp_part = s.PartNumber
                AND (mrp.mrp_due_date < @HorizonEnd OR mrp.mrp_rel_date < @HorizonEnd)
            ORDER BY s.PartNumber, mrp.mrp_type, mrp.mrp_due_date, mrp.mrp_rel_date, mrp.mrp_qty;
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
        var first = rows.First(); var ordinal = 0;
        var evidence = rows.Where(row => row.MrpType is not null || row.DueDate is not null || row.ReleaseDate is not null || row.Quantity is not null)
            .OrderBy(row => row.MrpType, StringComparer.Ordinal).ThenBy(row => row.DueDate).ThenBy(row => row.ReleaseDate).ThenBy(row => row.Quantity)
            .Select(row => new LongTermMrpFact(++ordinal, row.MrpType, ToDateOnly(row.DueDate), ToDateOnly(row.ReleaseDate), row.Quantity ?? 0m, MrpScheduleCategory.Unclassified)).ToList();
        return new(first.ComponentPart, first.UnitOfMeasure, first.QadStatus, first.Description, first.Planner, first.BuyerPlannerCode, first.OpeningQoh,
            first.SiteSafetyMissing ? SafetyStockState.SelectedSiteValueMissing : SafetyStockState.Resolved, first.SafetyStock,
            parents.TryGetValue(first.ComponentPart, out var demandParents) ? demandParents : [], evidence,
            presentation.TryGetValue(first.ComponentPart, out var context) ? context : null);
    }
    private static DateOnly? ToDateOnly(DateTime? value) => value is null ? null : DateOnly.FromDateTime(value.Value);
    private sealed class RawRow { public string ComponentPart { get; set; } = string.Empty; public string? UnitOfMeasure { get; set; } public string? QadStatus { get; set; } public string? Description { get; set; } public string? Planner { get; set; } public string? BuyerPlannerCode { get; set; } public bool SiteSafetyMissing { get; set; } public decimal? SafetyStock { get; set; } public decimal OpeningQoh { get; set; } public string? MrpType { get; set; } public DateTime? DueDate { get; set; } public DateTime? ReleaseDate { get; set; } public decimal? Quantity { get; set; } }
    private sealed class PresentationRow { public string ComponentPart { get; set; } = string.Empty; public string? ManufacturerItem { get; set; } public string? PoNumber { get; set; } public int? PoLine { get; set; } public DateTime? PoDueDate { get; set; } public decimal? PoOpenQuantity { get; set; } public bool? PoConfirmed { get; set; } public bool IsKss { get; set; } }
}
