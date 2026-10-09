using Dapper;
using Kst.Domain.ComponentMrp;
using Kst.Domain.LongTermShortages;
using Kst.Integrations.Qad.ComponentDetail;
using Kst.Integrations.Qad.LongTermShortages;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.ComponentMrp;

/// <summary>Independent direct-part acquisition; no workspace, BOM or purchasing presentation reads.</summary>
public sealed class QadComponentMrpReader(QadConnectionOptions options)
{
    public async Task<ComponentMrpSource?> ReadAsync(string site, string part, DateOnly horizonEnd, CancellationToken ct)
    {
        var domain = QadSiteDomainMap.Resolve(site);
        if (!options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        await using var connection = await QadConnectionFactory.OpenAsync(options, ct);
        var (sql, parameters) = BuildQuery(domain, site, part, horizonEnd);
        var rows = (await connection.QueryAsync<RawRow>(new CommandDefinition(sql, parameters,
            commandTimeout: options.CommandTimeoutSeconds, cancellationToken: ct))).ToList();
        // Every required section is part of this completed command; no exception becomes an empty section.
        if (rows.Count == 0) return null;
        if (rows.Any(row => !string.Equals(row.ComponentPart, part, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("The acquired component does not match the requested scope.");
        var (costSql, costParameters) = QadComponentSourceReader.BuildQctcQuery(domain, site, part);
        var qctc = await connection.QuerySingleOrDefaultAsync<QctcRow>(new CommandDefinition(costSql, costParameters,
            commandTimeout: options.CommandTimeoutSeconds, cancellationToken: ct));
        return Map(rows, qctc?.Qctc, DateTimeOffset.UtcNow);
    }

    public static (string Sql, DynamicParameters Parameters) BuildQuery(string domain, string site, string part, DateOnly horizonEnd)
    {
        // Compose the accepted raw schedule SQL only, not the broad Stage 11 reader or its presentation query.
        var (sql, parameters) = QadLongTermShortageSourceReader.BuildBatchQuery(domain, site, [part], horizonEnd);
        sql = sql.Replace("pm.pt_desc1 AS Description,", "pm.pt_desc1 AS Description, pm.pt_desc2 AS Description2, pm.pt_warr_cd AS Ios, ptp.ptp_timefnce AS TimeFence, ptp.ptp_ins_lead AS InspectionLeadTime, ptp.ptp_ord_min AS MinimumOrder, ptp.ptp_ord_mult AS OrderMultiple,")
            .Replace("cm.code_user1 AS Planner, CASE WHEN LTRIM(RTRIM(ISNULL(ptp.ptp_buyer, ''))) <> '' THEN ptp.ptp_buyer ELSE pm.pt_buyer END AS BuyerPlannerCode,", "ptp.ptp_buyer AS BuyerPlannerCode,")
            .Replace("LEFT JOIN qadpro2.dbo.pt_mstr AS pm", "INNER JOIN qadpro2.dbo.pt_mstr AS pm");
        var start = sql.IndexOf("LEFT JOIN qadpro2.dbo.code_mstr AS cm", StringComparison.Ordinal);
        var end = sql.IndexOf("LEFT JOIN Qoh AS q", start, StringComparison.Ordinal);
        if (start < 0 || end < start) throw new InvalidOperationException("Locked source SQL shape changed; review the focused adapter.");
        sql = sql.Remove(start, end - start);
        return (sql, parameters);
    }

    public static ComponentMrpSource Map(IReadOnlyList<RawRow> rows, decimal? qctc, DateTimeOffset acquired)
    {
        if (rows.Count == 0) throw new InvalidOperationException("No master row supplied.");
        var first = rows[0];
        if (string.IsNullOrWhiteSpace(first.ComponentPart) || rows.Any(r => !string.Equals(r.ComponentPart, first.ComponentPart, StringComparison.Ordinal)))
            throw new InvalidOperationException("Component scope is incomplete or conflicting.");
        if (rows.Any(row => !SameMetadata(first, row)))
            throw new InvalidOperationException("Component metadata changed or is ambiguous within the acquisition.");
        var facts = new List<LongTermMrpFact>();
        foreach (var row in rows)
        {
            if (row.MrpType is null && row.DueDate is null && row.ReleaseDate is null && row.Quantity is null && row.SourceRowId is null) continue;
            if (row.Quantity is null) throw new InvalidOperationException("MRP quantity is unavailable.");
            facts.Add(new(0, row.MrpType, Date(row.DueDate), Date(row.ReleaseDate), row.Quantity.Value, MrpScheduleCategory.Unclassified)
            {
                SourceNumber = row.SourceNumber, SourceLine = row.SourceLine, SourceLine2 = row.SourceLine2,
                SourceRowId = row.SourceRowId, IsPoReceipt = row.IsPoReceipt, PoConfirmed = row.PoConfirmed
            });
        }
        var evidence = QadLongTermShortageSourceReader.SelectEvidence(facts);
        if (evidence.Any(f => string.Equals(f.Type?.Trim(), "SUPPLY", StringComparison.OrdinalIgnoreCase) && f.IsPoReceipt == true && f.PoConfirmed is null))
            throw new InvalidOperationException("PO receipt confirmation is unavailable.");
        var input = new LongTermShortageInput(first.ComponentPart, first.UnitOfMeasure, first.QadStatus,
            QadComponentSourceReader.CombineDescription(first.Description, first.Description2), null, first.BuyerPlannerCode,
            first.OpeningQoh, first.SiteSafetyMissing ? SafetyStockState.SelectedSiteValueMissing : SafetyStockState.Resolved,
            first.SafetyStock, [], evidence)
        {
            SitePlanningPresent = first.SitePlanningPresent, EffectivePmCode = first.EffectivePmCode,
            OrderPeriodDays = first.OrderPeriodDays, SafetyTimeWorkingDays = first.SafetyTimeWorkingDays,
            ManufacturingLeadWorkingDays = first.ManufacturingLeadWorkingDays,
            PurchasingLeadCalendarDays = first.PurchasingLeadCalendarDays, CumulativeLeadCalendarDays = first.CumulativeLeadCalendarDays
        };
        return new(input, new(first.Ios, first.TimeFence, first.InspectionLeadTime, first.MinimumOrder, first.OrderMultiple, qctc), acquired);
    }

    private static DateOnly? Date(DateTime? value) => value is null ? null : DateOnly.FromDateTime(value.Value);
    private static bool SameMetadata(RawRow a, RawRow b) =>
        (a.UnitOfMeasure, a.QadStatus, a.Description, a.Description2, a.BuyerPlannerCode, a.Ios, a.TimeFence,
            a.InspectionLeadTime, a.MinimumOrder, a.OrderMultiple, a.SiteSafetyMissing, a.SitePlanningPresent,
            a.SafetyStock, a.OpeningQoh, a.EffectivePmCode, a.OrderPeriodDays, a.SafetyTimeWorkingDays,
            a.ManufacturingLeadWorkingDays, a.PurchasingLeadCalendarDays, a.CumulativeLeadCalendarDays)
        == (b.UnitOfMeasure, b.QadStatus, b.Description, b.Description2, b.BuyerPlannerCode, b.Ios, b.TimeFence,
            b.InspectionLeadTime, b.MinimumOrder, b.OrderMultiple, b.SiteSafetyMissing, b.SitePlanningPresent,
            b.SafetyStock, b.OpeningQoh, b.EffectivePmCode, b.OrderPeriodDays, b.SafetyTimeWorkingDays,
            b.ManufacturingLeadWorkingDays, b.PurchasingLeadCalendarDays, b.CumulativeLeadCalendarDays);
    private sealed class QctcRow { public decimal? Qctc { get; set; } }
    public sealed class RawRow
    {
        public string ComponentPart { get; set; } = "";
        public string? UnitOfMeasure { get; set; }
        public string? QadStatus { get; set; }
        public string? Description { get; set; }
        public string? Description2 { get; set; }
        public string? BuyerPlannerCode { get; set; }
        public string? Ios { get; set; }
        public int? TimeFence { get; set; }
        public int? InspectionLeadTime { get; set; }
        public decimal? MinimumOrder { get; set; }
        public decimal? OrderMultiple { get; set; }
        public bool SiteSafetyMissing { get; set; }
        public bool SitePlanningPresent { get; set; }
        public decimal? SafetyStock { get; set; }
        public decimal OpeningQoh { get; set; }
        public string? EffectivePmCode { get; set; }
        public int? OrderPeriodDays { get; set; }
        public decimal? SafetyTimeWorkingDays { get; set; }
        public int? ManufacturingLeadWorkingDays { get; set; }
        public int? PurchasingLeadCalendarDays { get; set; }
        public int? CumulativeLeadCalendarDays { get; set; }
        public string? MrpType { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public decimal? Quantity { get; set; }
        public string? SourceNumber { get; set; }
        public string? SourceLine { get; set; }
        public string? SourceLine2 { get; set; }
        public string? SourceRowId { get; set; }
        public bool IsPoReceipt { get; set; }
        public bool? PoConfirmed { get; set; }
    }
}
