using Dapper;
using Kst.Domain.ShipmentsToGo;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;
using Microsoft.Extensions.Logging;

namespace Kst.Integrations.Qad.ShipmentsToGo;

public sealed class QadShipmentsToGoReader(QadConnectionOptions options, ILogger<QadShipmentsToGoReader> logger)
{
    public async Task<IReadOnlyList<ShipmentToGoLine>> ReadOrdersAsync(IReadOnlyList<string> sites, bool includeNextWeek, CancellationToken ct)
    {
        if (!options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        var (sql, parameters) = BuildOrdersQuery(sites, includeNextWeek);
        await using var connection = await QadConnectionFactory.OpenAsync(options, ct);
        var rows = await connection.QueryAsync<RawRow>(new CommandDefinition(sql, parameters,
            commandTimeout: options.CommandTimeoutSeconds, cancellationToken: ct));
        return rows.Select(Map).ToArray();
    }

    public Task<IReadOnlyDictionary<string, double>> ReadShippedTodayAsync(IReadOnlyList<string> sites, CancellationToken ct) =>
        AccumulateShippedAsync(sites, async () =>
        {
            if (!options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
            var (sql, parameters) = BuildShippedQuery(sites);
            await using var connection = await QadConnectionFactory.OpenAsync(options, ct);
            return await connection.QueryAsync<ShippedRow>(new CommandDefinition(sql, parameters,
                commandTimeout: options.CommandTimeoutSeconds, cancellationToken: ct));
        }, () => logger.LogWarning("Could not fetch tr_hist totals."), ct);

    /// <summary>Initialize zero; retain each assigned total if later conversion/iteration fails, as Python does.</summary>
    public static async Task<IReadOnlyDictionary<string, double>> AccumulateShippedAsync(IReadOnlyList<string> sites,
        Func<Task<IEnumerable<ShippedRow>>> acquire, Action warn, CancellationToken ct)
    {
        var totals = sites.ToDictionary(s => s, _ => 0d, StringComparer.Ordinal);
        try
        {
            foreach (var row in await acquire())
                totals[row.Site.Trim().ToUpperInvariant()] = row.Total is null
                    ? throw new InvalidOperationException("Unavailable shipped total.") : (double)row.Total.Value;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { warn(); }
        return totals;
    }

    public static ShipmentToGoLine Map(RawRow r) => new(r.DueDate, r.Order, r.PurchaseOrder, r.Channel,
        r.Line, r.Ios, r.ItemNumber, r.ProductLine, r.Site, r.OrderSite, (double)r.Open,
        Number(r.Allocated), Number(r.Picked), (double)r.Qoh, r.ShipToName, Number(r.ExtendedPrice), r.Crc, Number(r.Risk), r.Comments);
    private static double? Number(decimal? value) => value is null ? null : (double)value.Value;

    private static (string Scope, DynamicParameters Parameters) Scope(IReadOnlyList<string> sites)
    {
        if (!ShipmentsToGoRules.ValidSites(sites)) throw new ArgumentException("Select supported sites.");
        var parameters = new DynamicParameters();
        var clauses = new List<string>();
        for (var i = 0; i < sites.Count; i++)
        {
            parameters.Add($"Site{i}", sites[i]); parameters.Add($"Domain{i}", QadSiteDomainMap.Resolve(sites[i]));
            clauses.Add($"(sod.sod_site = @Site{i} AND sod.sod_domain = @Domain{i})");
        }
        return (string.Join(" OR ", clauses), parameters);
    }

    public static (string Sql, DynamicParameters Parameters) BuildShippedQuery(IReadOnlyList<string> sites, DateTime? fixtureServerNow = null)
    {
        var (_, parameters) = Scope(sites);
        var selected = string.Join(", ", Enumerable.Range(0, sites.Count).Select(i => $"@Site{i}"));
        var clock = fixtureServerNow is null ? "GETDATE()" : "@FixtureServerNow";
        if (fixtureServerNow is not null) parameters.Add("FixtureServerNow", fixtureServerNow.Value);
        return ($"""
            SELECT tr_site AS Site, SUM(tr_price * (tr_qty_chg * -1)) AS Total
            FROM QADPRO2.dbo.tr_hist shp
            WHERE shp.tr_site IN ({selected}) AND shp.tr_date >= CAST({clock} AS DATE)
              AND shp.tr_type = 'ISS-SO' AND shp.tr_date < DATEADD(DAY, 1, CAST({clock} AS DATE))
            GROUP BY tr_site;
            """, parameters);
    }

    public static (string Sql, DynamicParameters Parameters) BuildOrdersQuery(IReadOnlyList<string> sites, bool includeNextWeek, DateTime? fixtureServerNow = null)
    {
        var (scope, parameters) = Scope(sites);
        parameters.Add("ExtraDays", includeNextWeek ? 7 : 0);
        var clock = fixtureServerNow is null ? "GETDATE()" : "@FixtureServerNow";
        if (fixtureServerNow is not null) parameters.Add("FixtureServerNow", fixtureServerNow.Value);
        // Python SQL, aliases changed only for typed mapping; ambiguous joins/ties deliberately retained.
        return ($"""
            SELECT sod.sod_due_date AS DueDate, sod.sod_nbr AS [Order], so.so_po AS PurchaseOrder,
              so.so_channel AS Channel, sod.sod_line AS Line, pt.pt_warr_cd AS Ios,
              sod.sod_part AS ItemNumber, sod.sod_prodline AS ProductLine, sod.sod_site AS Site,
              so.so_site AS OrderSite, (sod.sod_qty_ord - sod.sod_qty_ship) AS [Open],
              sod.sod_qty_all AS Allocated, sod.sod_qty_pick AS Picked,
              COALESCE(SUM(im.in_qty_oh), 0) AS Qoh, ad1.ad_name AS ShipToName,
              (sod.sod_price * (sod.sod_qty_ord - sod.sod_qty_ship)) AS ExtendedPrice,
              COALESCE(mrp_pegging.linked_SP, ad2.ad_sort) AS Crc,
              CASE WHEN COALESCE(SUM(im.in_qty_oh), 0) = 0
                THEN -(sod.sod_price * (sod.sod_qty_ord - sod.sod_qty_ship)) ELSE 0 END AS Risk,
              C.Comments
            FROM QADPRO2.dbo.sod_det sod
            LEFT JOIN QADPRO2.dbo.so_mstr so ON sod.sod_nbr = so.so_nbr AND sod.sod_domain = so.so_domain
            LEFT JOIN (
              SELECT mrpp.mrpp_pegnbr, mrpp.mrpp_pegline, mrpp.mrpp_domain, mrpp.mrpp_site,
                linked_ad.ad_sort AS linked_SP,
                ROW_NUMBER() OVER (PARTITION BY mrpp.mrpp_pegnbr, mrpp.mrpp_pegline, mrpp.mrpp_domain, mrpp.mrpp_site
                  ORDER BY mrpp.mrpp_pegqty DESC) AS RN
              FROM Analysis.dbo.mrpp_det mrpp
              INNER JOIN QADPRO2.dbo.sod_det linked_sod ON linked_sod.sod_nbr = mrpp.mrpp_nbr
                AND linked_sod.sod_line = mrpp.mrpp_line AND linked_sod.sod_domain = mrpp.mrpp_domain
                AND linked_sod.sod_site = mrpp.mrpp_site
              INNER JOIN QADPRO2.dbo.ad_mstr linked_ad ON linked_sod.sod_slspsn##1 = linked_ad.ad_addr
                AND mrpp.mrpp_domain = linked_ad.ad_domain
              WHERE '' NOT IN (mrpp.mrpp_nbr, mrpp.mrpp_line, mrpp.mrpp_pegnbr, mrpp.mrpp_pegline)
            ) mrp_pegging ON mrp_pegging.mrpp_pegnbr = so.so_nbr AND mrp_pegging.mrpp_pegline = sod.sod_line
              AND mrp_pegging.RN = 1
            LEFT JOIN QADPRO2.dbo.ad_mstr ad1 ON so.so_ship = ad1.ad_addr AND so.so_domain = ad1.ad_domain
            LEFT JOIN QADPRO2.dbo.ad_mstr ad2 ON sod.sod_slspsn##1 = ad2.ad_addr AND so.so_domain = ad2.ad_domain
            LEFT JOIN QADPRO2.dbo.pt_mstr pt ON sod.sod_part = pt.pt_part AND sod.sod_domain = pt.pt_domain
            LEFT JOIN QADPRO2.dbo.in_mstr im ON sod.sod_part = im.in_part AND sod.sod_domain = im.in_domain
              AND sod.sod_site = im.in_site
            CROSS APPLY (SELECT AllTxt = CONCAT(
              CASE WHEN LOWER(LTRIM(RTRIM(so.so__chr03))) = 'yes' THEN 'CS Hold; ' ELSE '' END,
              CASE WHEN LOWER(LTRIM(RTRIM(sod.sod__chr03))) = 'yes' THEN 'Ln Hold; ' ELSE '' END,
              CASE WHEN sod.sod_site = 'CH' THEN CONCAT('KS: ', LTRIM(RTRIM(so.so__chr04)), '; ') ELSE '' END,
              CASE WHEN sod.sod_site = 'VT' THEN CONCAT('KV: ', LTRIM(RTRIM(so.so__chr04)), '; ') ELSE '' END,
              CASE WHEN LEN(LTRIM(RTRIM(sod.sod__chr06))) = 6 AND UPPER(LEFT(LTRIM(RTRIM(sod.sod__chr06)), 1)) IN ('A', 'B', 'S') THEN 'QA Hold; ' ELSE '' END,
              CASE pt.pt_status WHEN 'N' THEN 'NPI; ' WHEN 'P' THEN 'Proto; ' ELSE '' END,
              CASE WHEN NULLIF(LTRIM(RTRIM(sod.sod_type)), '') IS NOT NULL THEN 'Memo; ' ELSE '' END,
              CASE WHEN UPPER(LTRIM(RTRIM(so.so_stat))) = 'HD' THEN 'CredHold; ' ELSE '' END,
              CASE WHEN UPPER(LTRIM(RTRIM(so.so_channel))) = 'ADJ' THEN 'ADJ; ' ELSE '' END)) X
            CROSS APPLY (SELECT Comments = CASE WHEN LEN(X.AllTxt) > 0 THEN LEFT(X.AllTxt, LEN(X.AllTxt) - 1) ELSE '' END) C
            WHERE CAST(sod.sod_due_date AS DATE) <= DATEADD(DAY,
              CASE DATENAME(WEEKDAY, {clock}) WHEN 'Sunday' THEN 6 WHEN 'Monday' THEN 5 WHEN 'Tuesday' THEN 4
                WHEN 'Wednesday' THEN 3 WHEN 'Thursday' THEN 2 WHEN 'Friday' THEN 1 WHEN 'Saturday' THEN 7 END + @ExtraDays,
              CAST({clock} AS DATE)) AND (sod.sod_qty_ord - sod.sod_qty_ship) > 0 AND ({scope})
            GROUP BY sod.sod_due_date, sod.sod_nbr, so.so_po, so.so_channel, sod.sod_line, pt.pt_status,
              pt.pt_warr_cd, sod.sod_part, sod.sod_prodline, so.so_site, so.so_partial, sod.sod_site,
              sod.sod_qty_ord, sod.sod_qty_ship, sod.sod_qty_all, sod.sod_qty_pick, ad1.ad_name,
              sod.sod_price, sod.sod_slspsn##1, COALESCE(mrp_pegging.linked_SP, ad2.ad_sort), C.Comments
            ORDER BY LTRIM(RTRIM(COALESCE(mrp_pegging.linked_SP, ad2.ad_sort))) COLLATE SQL_Latin1_General_CP1_CI_AS;
            """, parameters);
    }

    public sealed class ShippedRow { public string Site { get; set; } = ""; public decimal? Total { get; set; } }
    public sealed class RawRow
    {
        public DateTime DueDate { get; set; }
        public string Order { get; set; } = "";
        public string? PurchaseOrder { get; set; }
        public string? Channel { get; set; }
        public int Line { get; set; }
        public string? Ios { get; set; }
        public string ItemNumber { get; set; } = "";
        public string? ProductLine { get; set; }
        public string Site { get; set; } = "";
        public string? OrderSite { get; set; }
        public decimal Open { get; set; }
        public decimal? Allocated { get; set; }
        public decimal? Picked { get; set; }
        public decimal Qoh { get; set; }
        public string? ShipToName { get; set; }
        public decimal? ExtendedPrice { get; set; }
        public string? Crc { get; set; }
        public decimal? Risk { get; set; }
        public string? Comments { get; set; }
    }
}
