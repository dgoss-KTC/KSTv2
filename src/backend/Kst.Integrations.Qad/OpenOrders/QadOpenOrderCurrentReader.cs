using Dapper;
using Kst.Domain.OpenOrders;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.OpenOrders;

public sealed class QadOpenOrderCurrentReader(QadConnectionOptions options)
{
    public static (string Sql, DynamicParameters Parameters) BuildQuery(string domain, string site, IReadOnlyList<OpenOrderLineKey> keys)
    {
        if (keys.Count == 0) throw new ArgumentException("Changed identities required", nameof(keys));
        var parameters = new DynamicParameters();
        parameters.Add("Domain", domain);
        parameters.Add("Site", site);
        var identities = new List<string>(keys.Count);
        for (var i = 0; i < keys.Count; i++)
        {
            parameters.Add($"Order{i}", keys[i].SalesOrder);
            parameters.Add($"Line{i}", keys[i].Line);
            identities.Add($"(@Order{i}, @Line{i})");
        }
        var sql = $"""
            WITH Requested (SalesOrder, Line) AS
            (SELECT SalesOrder, Line FROM (VALUES {string.Join(", ", identities)}) AS Keys (SalesOrder, Line))
            SELECT sod.sod_domain AS Domain, sod.sod_nbr AS SalesOrder, sod.sod_line AS Line,
                   sod.sod_site AS Site, sod.sod_part AS ItemNumber,
                   sod.sod_qty_ord AS OrderQty, sod.sod_qty_ship AS ShippedQty,
                   sod.sod_price AS Price, sod.sod_due_date AS DueDate,
                   sod.sod_per_date AS PerformDate, sod.sod_req_date AS RequiredDate,
                   sod.sod__dte01 AS DockDate
            FROM qadpro2.dbo.sod_det AS sod
            INNER JOIN Requested AS requested ON requested.SalesOrder = sod.sod_nbr AND requested.Line = sod.sod_line
            WHERE sod.sod_domain = @Domain AND sod.sod_site = @Site
              AND sod.sod_qty_ord - sod.sod_qty_ship > 0
            """;
        return (sql, parameters);
    }

    public async Task<IReadOnlyList<OpenOrderCurrentLine>> ReadAsync(string site, IReadOnlyList<OpenOrderLineKey> keys, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (keys.Count == 0) return [];
        if (!options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        var domain = QadSiteDomainMap.Resolve(site);
        if (keys.Any(k => k.Domain != domain)) throw new InvalidOperationException("Source domain does not match workspace site.");
        await using var connection = await QadConnectionFactory.OpenAsync(options, ct);
        var rows = new List<OpenOrderCurrentLine>();
        // Two parameters per identity plus scope; remain below SQL Server's 2100 parameter limit.
        foreach (var batch in keys.Chunk(Math.Min(options.MaxPartBatchSize, 500)))
        {
            var (sql, parameters) = BuildQuery(domain, site, batch);
            var result = await connection.QueryAsync<CurrentRow>(new CommandDefinition(sql, parameters,
                commandTimeout: options.CommandTimeoutSeconds, cancellationToken: ct));
            rows.AddRange(result.Select(r => new OpenOrderCurrentLine(new(r.Domain, r.SalesOrder, r.Line), r.Site,
                r.ItemNumber, r.ShippedQty, new(Date(r.DueDate), Date(r.PerformDate), Date(r.RequiredDate),
                    Date(r.DockDate), r.OrderQty, r.Price))));
        }
        return rows;
    }

    private static DateOnly? Date(DateTime? date) => date.HasValue ? DateOnly.FromDateTime(date.Value) : null;

    public sealed class CurrentRow
    {
        public string Domain { get; set; } = "";
        public string SalesOrder { get; set; } = "";
        public int Line { get; set; }
        public string Site { get; set; } = "";
        public string ItemNumber { get; set; } = "";
        public decimal OrderQty { get; set; }
        public decimal ShippedQty { get; set; }
        public decimal Price { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? PerformDate { get; set; }
        public DateTime? RequiredDate { get; set; }
        public DateTime? DockDate { get; set; }
    }
}
