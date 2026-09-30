using Dapper;

namespace Kst.Integrations.Qad.OpenOrders;

/// <summary>
/// Stage 13.1 SQL contract only: constructs a parameterized, read-only, full-field source read.
/// No runtime reader, database connection, DTO or endpoint is introduced at this checkpoint.
/// </summary>
public static class QadOpenOrderQueryContract
{
    public static (string Sql, DynamicParameters Parameters) BuildBatchQuery(
        string domain, string site, IReadOnlyList<string> parentParts)
    {
        ArgumentNullException.ThrowIfNull(parentParts);
        if (parentParts.Count == 0)
            throw new ArgumentException("At least one resolved parent is required.", nameof(parentParts));

        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        var values = new string[parentParts.Count];
        for (var i = 0; i < parentParts.Count; i++)
        {
            parameters.Add($"Part{i}", parentParts[i]);
            values[i] = $"(@Part{i})";
        }

        var sql = $"""
            WITH ScopeParts (ParentPart) AS
            (
                SELECT ParentPart FROM (VALUES {string.Join(", ", values)}) AS Parts (ParentPart)
            )
            SELECT
                sod.sod_domain AS Domain,
                sod.sod_due_date AS DueDate,
                sod.sod_nbr AS SalesOrder,
                so.so_po AS PurchaseOrder,
                sod.sod_line AS Line,
                sod.sod_part AS ItemNumber,
                sod.sod_site AS Site,
                sod.sod_qty_ord AS OrderQty,
                sod.sod_qty_ship AS ShippedQty,
                sod.sod_qty_ord - sod.sod_qty_ship AS [Open],
                so.so_stat AS Stat,
                sod.sod_price AS Price,
                sod.sod_price * (sod.sod_qty_ord - sod.sod_qty_ship) AS ExtPrice,
                sod.sod_qty_all AS Allocated,
                so.so_cust AS Customer,
                cust.ad_name AS CustomerName,
                sod.sod_slspsn##1 AS Salesperson,
                sod.sod_custpart AS CustomerPart,
                sod.sod__dte01 AS DockDate,
                pt.pt_warr_cd AS Ios,
                CASE WHEN sod.sod_cmtindx <> 0 THEN
                    STUFF((
                        SELECT ';' + ISNULL(cmt.cmt_cmmt##1, '') + ISNULL(cmt.cmt_cmmt##2, '')
                            + ISNULL(cmt.cmt_cmmt##3, '') + ISNULL(cmt.cmt_cmmt##4, '')
                            + ISNULL(cmt.cmt_cmmt##5, '') + ISNULL(cmt.cmt_cmmt##6, '')
                            + ISNULL(cmt.cmt_cmmt##7, '') + ISNULL(cmt.cmt_cmmt##8, '')
                            + ISNULL(cmt.cmt_cmmt##9, '') + ISNULL(cmt.cmt_cmmt##10, '')
                            + ISNULL(cmt.cmt_cmmt##11, '') + ISNULL(cmt.cmt_cmmt##12, '')
                            + ISNULL(cmt.cmt_cmmt##13, '') + ISNULL(cmt.cmt_cmmt##14, '')
                            + ISNULL(cmt.cmt_cmmt##15, '')
                        FROM qadpro2.dbo.cmt_det AS cmt
                        WHERE cmt.cmt_domain = sod.sod_domain
                          AND cmt.cmt_indx = sod.sod_cmtindx
                        FOR XML PATH('')
                    ), 1, 1, '')
                ELSE '' END AS LineComments,
                sod.sod_hold_stat AS LineHold,
                so.so_partial AS Partials,
                sod.sod_per_date AS PerformDate,
                sod.sod_qty_pick AS Picked,
                pt.pt_buyer AS Plnr,
                pt.pt_status AS ProdStat,
                sod.sod_prodline AS ProductLine,
                sod.sod__chr06 AS QaHold,
                so.so_rmks AS Remarks,
                sod.sod_req_date AS RequiredDate,
                sod.sod__chr05 AS Revision,
                so.so__chr01 AS ShipAcct,
                ship.ad_sort AS ShipTo,
                so.so_shipvia AS ShipVia,
                (SELECT SUM(ld.ld_qty_oh)
                 FROM qadpro2.dbo.ld_det AS ld
                 WHERE ld.ld_part = sod.sod_part
                   AND ld.ld_domain = sod.sod_domain
                   AND ld.ld_site = sod.sod_site) AS SiteQoh,
                so.so_hold_stat AS SoHoldStatus,
                so.so_type AS SoType,
                CASE WHEN sod.sod_consignment = 'TRUE' THEN 0
                     ELSE sod.sod_price END AS UnitPrice
            FROM qadpro2.dbo.sod_det AS sod
            INNER JOIN ScopeParts AS scope ON scope.ParentPart = sod.sod_part
            INNER JOIN qadpro2.dbo.so_mstr AS so
                ON so.so_nbr = sod.sod_nbr AND so.so_domain = sod.sod_domain
            LEFT JOIN qadpro2.dbo.ad_mstr AS cust
                ON cust.ad_domain = sod.sod_domain AND cust.ad_addr = so.so_cust
            LEFT JOIN qadpro2.dbo.ad_mstr AS ship
                ON ship.ad_domain = sod.sod_domain AND ship.ad_addr = so.so_ship
            LEFT JOIN qadpro2.dbo.pt_mstr AS pt
                ON pt.pt_domain = sod.sod_domain AND pt.pt_part = sod.sod_part
            WHERE sod.sod_domain = @Domain
              AND sod.sod_site = @Site
              AND sod.sod_qty_ord - sod.sod_qty_ship > 0
            ORDER BY cust.ad_name, sod.sod_part, sod.sod_due_date;
            """;

        return (sql, parameters);
    }
}
