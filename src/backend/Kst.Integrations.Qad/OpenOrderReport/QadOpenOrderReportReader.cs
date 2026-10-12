using System.Data;
using System.Data.Common;
using Dapper;
using Kst.Domain.OpenOrderReport;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.OpenOrderReport;

/// <summary>Independent Python SELECT, never workspace parent/open-line scope.</summary>
public sealed class QadOpenOrderReportReader(QadConnectionOptions options)
{
    public async Task<IReadOnlyList<OpenOrderReportRow>> ReadAsync(OpenOrderReportFilter filter, CancellationToken ct)
    {
        if (!options.IsConfigured) throw new InvalidOperationException("QAD is not configured.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(100));
        var token = deadline.Token;
        var (sql, parameters) = BuildQuery(filter);
        await using var connection = await QadConnectionFactory.OpenAsync(options, token);
        using var reader = await connection.ExecuteReaderAsync(new CommandDefinition(sql, parameters,
            commandTimeout: options.CommandTimeoutSeconds, cancellationToken: token));
        return await ParseAsync((DbDataReader)reader, token);
    }

    public static async Task<IReadOnlyList<OpenOrderReportRow>> ParseAsync(DbDataReader reader, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (reader.FieldCount != 34 || !Enumerable.Range(0, 34).Select(reader.GetName).SequenceEqual(OpenOrderReportRules.Headers))
            throw new InvalidOperationException("Unsupported Open Order Report schema.");
        for (var i = 0; i < 34; i++)
        {
            var type = reader.GetFieldType(i);
            var valid = OpenOrderReportRules.IsDate(i) ? type == typeof(DateTime)
                : OpenOrderReportRules.IsNumber(i) ? type == typeof(decimal) || type == typeof(int) || type == typeof(short) || type == typeof(long) || type == typeof(byte)
                : i == 17 ? type == typeof(bool) : type == typeof(string);
            if (!valid) throw Schema();
        }
        var rows = new List<OpenOrderReportRow>();
        long bytes = 0;
        while (await reader.ReadAsync(ct))
        {
            if (rows.Count >= OpenOrderReportRules.MaxRows) throw new InvalidOperationException("Open Order Report exceeds report capacity.");
            var cells = new OpenOrderReportCell[34];
            for (var i = 0; i < cells.Length; i++)
            {
                var value = reader.GetValue(i);
                cells[i] = value is DBNull ? new() : OpenOrderReportRules.IsDate(i)
                    ? value is DateTime date ? new(Date: date) : throw Schema()
                    : OpenOrderReportRules.IsNumber(i) ? value switch
                    {
                        decimal n => new(Number: n), int n => new(Number: n), short n => new(Number: n),
                        long n => new(Number: n), byte n => new(Number: n), _ => throw Schema()
                    }
                    : i == 17 ? value is bool flag ? new(Boolean: flag) : throw Schema()
                    : value is string text && text.Length <= OpenOrderReportRules.MaxText ? new(Text: text) : throw Schema();
                bytes += cells[i].Text is null ? 16 : 16 + (long)cells[i].Text!.Length * 2;
            }
            if (bytes > OpenOrderReportRules.MaxSourceBytes) throw new InvalidOperationException("Open Order Report exceeds report capacity.");
            rows.Add(new(Array.AsReadOnly(cells)));
        }
        if (await reader.NextResultAsync(ct)) throw Schema();
        return rows.AsReadOnly();
    }

    private static InvalidOperationException Schema() => new("Unsupported Open Order Report schema/value or cell capacity.");

    public static (string Sql, DynamicParameters Parameters) BuildQuery(OpenOrderReportFilter filter)
    {
        var p = new DynamicParameters();
        var sites = filter.Site is null ? OpenOrderReportRules.Sites : new[] { filter.Site };
        var pairs = new List<string>();
        for (var i = 0; i < sites.Count; i++)
        {
            p.Add($"Site{i}", sites[i]); p.Add($"Domain{i}", QadSiteDomainMap.Resolve(sites[i]));
            pairs.Add($"(A.sod_site = @Site{i} AND A.sod_domain = @Domain{i})");
        }
        var conditions = new List<string> { "(" + string.Join(" OR ", pairs) + ")" };
        void Add(string? value, string predicate, string parameter)
        {
            if (value is null) return;
            conditions.Add(predicate); p.Add(parameter, value);
        }
        Add(filter.CustomerName is null ? null : "%" + filter.CustomerName + "%", "Cust.ad_name LIKE @CustomerName", "CustomerName");
        Add(filter.CustomerNumber, "B.so_cust = @CustomerNumber", "CustomerNumber");
        Add(filter.SalespersonId, "A.sod_slspsn##1 = @SalespersonId", "SalespersonId");
        if (filter.ProductLineFrom is not null)
        {
            conditions.Add("A.sod_prodline BETWEEN @ProductLineFrom AND @ProductLineTo");
            p.Add("ProductLineFrom", filter.ProductLineFrom); p.Add("ProductLineTo", filter.ProductLineTo);
        }
        Add(filter.Ios, "PT.pt_warr_cd = @Ios", "Ios");
        if (filter.DueDateFrom is not null)
        {
            conditions.Add("A.sod_due_date BETWEEN @DueDateFrom AND @DueDateTo");
            p.Add("DueDateFrom", filter.DueDateFrom.Value.ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            p.Add("DueDateTo", filter.DueDateTo!.Value.ToDateTime(TimeOnly.MinValue), DbType.DateTime);
        }
        // Legacy aliases, expressions, correlated enrichment, multiplicity and ordering preserved.
        return ($"""
            SELECT
                A.sod_due_date AS [Due], A.sod_nbr AS [Order], B.so_po AS [PO], A.sod_line AS [Ln],
                A.sod_part AS [Item Number], A.sod_site AS [Site],
                (A.sod_qty_ord - A.sod_qty_ship) AS [Open], B.so_stat AS [Stat],
                (A.sod_price * (A.sod_qty_ord - A.sod_qty_ship)) AS [Ext Price],
                A.sod_qty_all AS [Allocated], B.so_cust AS [Customer], Cust.ad_name AS [Customer Name],
                A.sod_custpart AS [Customer Part], A.sod__dte01 AS [Dock Date], PT.pt_warr_cd AS [IOS],
                CASE WHEN A.sod_cmtindx <> 0 THEN
                    STUFF((SELECT ';' + ISNULL(cmt_cmmt##1, '') + ISNULL(cmt_cmmt##2, '') +
                        ISNULL(cmt_cmmt##3, '') + ISNULL(cmt_cmmt##4, '') + ISNULL(cmt_cmmt##5, '') +
                        ISNULL(cmt_cmmt##6, '') + ISNULL(cmt_cmmt##7, '') + ISNULL(cmt_cmmt##8, '') +
                        ISNULL(cmt_cmmt##9, '') + ISNULL(cmt_cmmt##10, '') + ISNULL(cmt_cmmt##11, '') +
                        ISNULL(cmt_cmmt##12, '') + ISNULL(cmt_cmmt##13, '') + ISNULL(cmt_cmmt##14, '') +
                        ISNULL(cmt_cmmt##15, '')
                        FROM QADPRO2.dbo.cmt_det
                        WHERE cmt_domain = A.sod_domain AND cmt_indx = A.sod_cmtindx
                        FOR XML PATH('')), 1, 1, '') ELSE '' END AS [Line Comments],
                A.sod_hold_stat AS [Line Hold], B.so_partial AS [Partials], A.sod_per_date AS [Perform Date],
                A.sod_qty_pick AS [Picked], PT.pt_buyer AS [Plnr], PT.pt_status AS [Prod Stat],
                A.sod_prodline AS [Product Line], A.sod__chr06 AS [QA Hold], B.so_rmks AS [Remarks],
                A.sod_req_date AS [Required Date], A.sod__chr05 AS [Revision], B.so__chr01 AS [Ship Acct],
                Ship.ad_sort AS [Ship To], B.so_shipvia AS [Ship Via],
                (SELECT SUM(LD.ld_qty_oh) FROM QADPRO2.dbo.ld_det LD
                    WHERE LD.ld_part = A.sod_part AND LD.ld_domain = A.sod_domain AND LD.ld_site = A.sod_site) AS [Site QOH],
                B.so_hold_stat AS [SO Hold Status], B.so_type AS [SO Type],
                CASE WHEN A.sod_consignment = 'TRUE' THEN 0 ELSE A.sod_price END AS [Unit Price]
            FROM QADPRO2.dbo.sod_det A
            INNER JOIN QADPRO2.dbo.so_mstr B ON A.sod_nbr = B.so_nbr AND A.sod_domain = B.so_domain
            LEFT JOIN QADPRO2.dbo.ad_mstr Cust ON Cust.ad_domain = A.sod_domain AND Cust.ad_addr = B.so_cust
            LEFT JOIN QADPRO2.dbo.ad_mstr Ship ON Ship.ad_domain = A.sod_domain AND Ship.ad_addr = B.so_ship
            LEFT JOIN QADPRO2.dbo.pt_mstr PT ON PT.pt_part = A.sod_part AND PT.pt_domain = A.sod_domain
            WHERE {string.Join("\nAND ", conditions)}
            ORDER BY Cust.ad_name, A.sod_part, A.sod_due_date
            """, p);
    }
}
