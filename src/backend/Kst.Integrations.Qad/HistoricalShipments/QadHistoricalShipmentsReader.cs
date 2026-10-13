using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using Dapper;
using Kst.Domain.HistoricalShipments;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.HistoricalShipments;

public sealed class QadHistoricalShipmentsReader(QadConnectionOptions options)
{
    public async IAsyncEnumerable<ShipmentRow> ReadAsync(HistoricalShipmentsFilter filter, [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var row in ReadQueryAsync(BuildQuery(filter), ct)) yield return row;
    }
    public async IAsyncEnumerable<ShipmentRow> ReadWorkspaceAsync(WorkspaceShipmentsFilter filter, [EnumeratorCancellation] CancellationToken ct)
    {
        if (filter.Validate().ParentParts.Count == 0) yield break;
        await foreach (var row in ReadQueryAsync(BuildWorkspaceQuery(filter), ct)) yield return row;
    }
    private async IAsyncEnumerable<ShipmentRow> ReadQueryAsync((string Sql, DynamicParameters Parameters) query, [EnumeratorCancellation] CancellationToken ct)
    {
        if (!options.IsConfigured) throw new InvalidOperationException("QAD is not configured.");
        var (sql, parameters) = query;
        await using var connection = await QadConnectionFactory.OpenAsync(options, ct);
        DbDataReader reader;
        try { reader = (DbDataReader)await connection.ExecuteReaderAsync(new CommandDefinition(sql, parameters, commandTimeout: options.CommandTimeoutSeconds, cancellationToken: ct)); }
        catch (Microsoft.Data.SqlClient.SqlException e) when (e.Number == -2)
        { throw new ShipmentSourceException("The source did not answer within the time limit. Narrow the filters (for example add Item Number or Product Line) and try again."); }
        using var _ = reader;
        await foreach (var row in ParseAsync(reader, ct)) yield return row;
    }
    public static async IAsyncEnumerable<ShipmentRow> ParseAsync(DbDataReader reader, [EnumeratorCancellation] CancellationToken ct)
    {
        var count = HistoricalShipmentsRules.Columns.Count;
        if (reader.FieldCount != count + 3 || reader.GetName(0) != "PhysicalDetailId" || reader.GetName(1) != "HeaderCount" ||
            reader.GetName(2) != "UnavailableEnrichment" || !Enumerable.Range(0, count).All(i => reader.GetName(i + 3) == HistoricalShipmentsRules.Columns[i].Id))
            throw new ShipmentSourceException("Unsupported Historical Shipments source schema. No complete report was acquired.");
        string? previous = null;
        while (await reader.ReadAsync(ct))
        {
            ct.ThrowIfCancellationRequested();
            if (reader.GetValue(0) is not string id || id.Length == 0 || id == previous)
                throw new ShipmentSourceException("Invalid or repeated physical invoice detail identity. No complete report was acquired.");
            previous = id;
            var headers = Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture);
            if (headers != 1) throw new ShipmentSourceException(headers == 0
                ? "An invoice detail has no matching header in its domain/invoice. No complete report was acquired. Review source header association."
                : "An invoice detail has multiple matching headers in its domain/invoice. No complete report was acquired. Review source header association.");
            var cells = new string?[count];
            for (var i = 0; i < count; i++)
            {
                var value = reader.GetValue(i + 3); var column = HistoricalShipmentsRules.Columns[i];
                cells[i] = value is DBNull ? null : column.Kind switch
                {
                    "date" when value is DateTime date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    "decimal" when value is decimal number => number.ToString("0.##########", CultureInfo.InvariantCulture),
                    "decimal" when column.Id is "extendedPrice" or "retroExtendedPrice" && value is string number => number,
                    "decimal" when column.Id == "onTime" && value is int flag && flag is 0 or 1 => flag.ToString(CultureInfo.InvariantCulture),
                    "text" when value is string text => text,
                    _ => throw new ShipmentSourceException("Unsupported Historical Shipments source value. No complete report was acquired.")
                };
            }
            var quantity = cells[6]; var price = cells[19];
            cells[20] = quantity is null || price is null ? null : Multiply(quantity, price);
            var retroPrice = cells[37];
            cells[38] = quantity is null || retroPrice is null ? null : Multiply(quantity, retroPrice);
            yield return new(id, Array.AsReadOnly(cells), Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture) != 0);
        }
        if (await reader.NextResultAsync(ct)) throw new ShipmentSourceException("Unexpected Historical Shipments result set.");
    }
    public static string Multiply(string left, string right)
    {
        static (BigInteger Value, int Scale) Parse(string text)
        {
            var dot = text.IndexOf('.'); return (BigInteger.Parse(text.Replace(".", ""), CultureInfo.InvariantCulture), dot < 0 ? 0 : text.Length - dot - 1);
        }
        var a = Parse(left); var b = Parse(right); var value = a.Value * b.Value; var scale = a.Scale + b.Scale;
        var text = BigInteger.Abs(value).ToString(CultureInfo.InvariantCulture).PadLeft(scale + 1, '0');
        if (scale > 0) text = text.Insert(text.Length - scale, ".").TrimEnd('0').TrimEnd('.');
        return value.Sign < 0 ? "-" + text : text;
    }
    public static (string Sql, DynamicParameters Parameters) BuildQuery(HistoricalShipmentsFilter filter)
        => BuildQueryCore(filter, null);
    public static (string Sql, DynamicParameters Parameters) BuildWorkspaceQuery(WorkspaceShipmentsFilter filter)
    {
        var validated = filter.Validate();
        if (validated.ParentParts.Count == 0) throw new ArgumentException("Workspace parent scope must not be empty when building a QAD query.");
        return BuildQueryCore(validated.Projection(), validated);
    }
    public static string BuildWorkspaceParentXml(IReadOnlyList<string> parentParts)
    {
        ArgumentNullException.ThrowIfNull(parentParts);
        var output = new StringBuilder();
        using (var writer = XmlWriter.Create(output, new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Document }))
        {
            writer.WriteStartElement("parts");
            foreach (var part in parentParts)
            {
                if (string.IsNullOrWhiteSpace(part)) throw new ArgumentException("Workspace parent scope contains a blank part.", nameof(parentParts));
                writer.WriteElementString("part", part);
            }
            writer.WriteEndElement();
        }
        return output.ToString();
    }
    private static (string Sql, DynamicParameters Parameters) BuildQueryCore(HistoricalShipmentsFilter filter, WorkspaceShipmentsFilter? workspace)
    {
        var f = HistoricalShipmentsRules.Validate(filter); var p = new DynamicParameters();
        p.Add("Site", f.Site); p.Add("Domain", QadSiteDomainMap.Resolve(f.Site));
        var detail = new List<string> { "d.idh_site = @Site", "d.idh_domain = @Domain" };
        var header = new List<string>();
        var parentScope = "";
        var fromClause = "";
        if (workspace is not null)
        {
            p.Add("WorkspaceParentParts", BuildWorkspaceParentXml(workspace.ParentParts), DbType.Xml);
            parentScope = """
                ;WITH WorkspaceParentParts AS
                (
                    SELECT DISTINCT
                        partNode.value('.', 'nvarchar(4000)') AS ParentPart,
                        partNode.value('string-length(.)', 'int') AS ParentLength
                    FROM @WorkspaceParentParts.nodes('/parts/part') AS scope(partNode)
                )
                """;
            fromClause = "FROM dbo.idh_hist d INNER JOIN WorkspaceParentParts scope ON scope.ParentLength = LEN(d.idh_part) AND scope.ParentPart = d.idh_part";
        }
        if (workspace?.PartNumber is not null)
        { detail.Add("d.idh_part = @PartNumber"); p.Add("PartNumber", workspace.PartNumber); }
        if (workspace?.ShipDateFrom is not null)
        { header.Add("hf.ih_ship_date >= @ShipDateFrom"); p.Add("ShipDateFrom", workspace.ShipDateFrom.Value.ToDateTime(TimeOnly.MinValue), DbType.DateTime2); }
        if (workspace?.ShipDateTo is not null && workspace.ShipDateTo != DateOnly.MaxValue)
        { header.Add("hf.ih_ship_date < @ShipDateEndExclusive"); p.Add("ShipDateEndExclusive", workspace.ShipDateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DbType.DateTime2); }
        else if (workspace?.ShipDateTo is not null) header.Add("hf.ih_ship_date IS NOT NULL");
        void Range(List<string> predicates, string field, string name, string? from, string? to)
        {
            if (from is not null) { predicates.Add($"{field} >= @{name}From"); p.Add(name + "From", from); }
            if (to is not null) { predicates.Add($"{field} <= @{name}To"); p.Add(name + "To", to); }
        }
        Range(detail, "d.idh_part", "Item", f.ItemFrom, f.ItemTo);
        Range(detail, "d.idh_prodline", "ProductLine", f.ProductLineFrom, f.ProductLineTo);
        Range(header, "hf.ih_cust", "Customer", f.CustomerFrom, f.CustomerTo);
        Range(header, "hf.[ih_slspsn##1]", "Salesperson", f.SalespersonFrom, f.SalespersonTo);
        if (f.InvoiceDateFrom is not null)
        { header.Add("hf.ih_inv_date >= @InvoiceDateFrom"); p.Add("InvoiceDateFrom", f.InvoiceDateFrom.Value.ToDateTime(TimeOnly.MinValue), DbType.DateTime2); }
        if (f.InvoiceDateTo is not null && f.InvoiceDateTo != DateOnly.MaxValue)
        { header.Add("hf.ih_inv_date < @InvoiceDateEndExclusive"); p.Add("InvoiceDateEndExclusive", f.InvoiceDateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DbType.DateTime2); }
        else if (f.InvoiceDateTo is not null) header.Add("hf.ih_inv_date IS NOT NULL");
        if (f.CorpGroup is not null)
        {
            p.Add("CorpGroup", f.CorpGroup);
            header.Add("""
                EXISTS (SELECT 1 FROM dbo.ad_mstr a
                  INNER JOIN dbo.businessrelation br ON br.businessrelationcode = a.ad_bus_relation
                  INNER JOIN dbo.corporategroup cg ON cg.corporategroup_id = br.corporategroup_id
                  WHERE a.ad_addr = hf.ih_cust AND a.ad_domain = @Domain AND cg.corporategroupcode = @CorpGroup
                    AND (br.businessrelationisdomrestr = 0 OR (br.businessrelationisdomrestr = 1 AND EXISTS
                      (SELECT 1 FROM dbo.domains dm WHERE dm.domain_id = br.domain_id AND dm.domaincode = @Domain))))
                """);
        }
        // Header filters drive from ih_hist (indexed by domain/customer/invoice date) so they do not scan the site's whole detail history.
        // A matching invoice that has multiple headers still fails acquisition; a detail with no header cannot satisfy a header filter.
        var headerDriven = header.Count > 0; if (headerDriven) detail.Add("hf.ih_domain = @Domain");
        if (headerDriven) detail.AddRange(header);
        if (headerDriven)
            fromClause = workspace is null
                ? "FROM dbo.ih_hist hf INNER JOIN dbo.idh_hist d ON d.idh_domain = hf.ih_domain AND d.idh_inv_nbr = hf.ih_inv_nbr"
                : "FROM dbo.ih_hist hf INNER JOIN dbo.idh_hist d ON d.idh_domain = hf.ih_domain AND d.idh_inv_nbr = hf.ih_inv_nbr INNER JOIN WorkspaceParentParts scope ON scope.ParentLength = LEN(d.idh_part) AND scope.ParentPart = d.idh_part";
        else if (workspace is null) fromClause = "FROM dbo.idh_hist d";
        // Aggregate applies return exactly one record. MIN is exposed only with COUNT=1, never a tie-breaker.
        return ($"""
            {parentScope}
            SELECT d.prrowid AS PhysicalDetailId, hc.HeaderCount,
              CASE WHEN cm.N <> 1 OR ship.N <> 1 OR part.N <> 1 THEN 1 ELSE 0 END AS UnavailableEnrichment,
              d.idh_site AS site, h.ih_cust AS customerNumber, CASE WHEN cm.N = 1 THEN cm.Name END AS customerName,
              h.ih_inv_nbr AS invoiceNumber, h.ih_inv_date AS invoiceDate, d.idh_part AS itemNumber,
              d.idh_qty_inv AS invoicedQuantity, h.ih_nbr AS salesOrder, CONVERT(nvarchar(20), d.idh_line) AS salesOrderLine,
              h.ih_po AS customerPo, h.ih_ship AS shipTo, CASE WHEN ship.N = 1 THEN ship.Name END AS shipToName,
              d.idh_custpart AS customerItem, CASE WHEN part.N = 1 THEN part.Desc1 END AS description1,
              CASE WHEN part.N = 1 THEN part.Desc2 END AS description2, h.ih_ship_date AS shipDate,
              h.ih_ord_date AS orderDate, d.idh_due_date AS dueDate, d.idh_per_date AS originalCommit,
              d.idh_price AS unitPrice, CAST(NULL AS nvarchar(100)) AS extendedPrice, h.ih_curr AS currency,
              h.ih_ex_rate AS exchangeRate, d.idh_type AS lineType, h.ih_shipvia AS shipVia, h.ih_bol AS billOfLading,
              h.ih__qad03 AS boxes, h.ih__dec01 AS pallets, h.[ih_slspsn##1] AS salesperson,
              h.ih__chr01 AS shippingAccount, h.ih__chr05 AS ncAccount, d.idh__chr09 AS release,
              h.ih_rmks AS remarks, d.idh_prodline AS productLine, h.ih_channel AS channel, d.idh_um AS recordedUom,
              d.idh_req_date AS requiredDate, d.idh_retro_price AS retroUnitPrice, CAST(NULL AS nvarchar(100)) AS retroExtendedPrice,
              CASE WHEN h.ih_channel = 'Standard' AND d.idh_due_date <= d.idh_per_date THEN 1
                   WHEN h.ih_channel = 'Standard' AND d.idh_due_date > d.idh_per_date THEN 0 ELSE NULL END AS onTime,
              d.idh_nbr AS detailSalesOrder
            {fromClause}
            OUTER APPLY (SELECT COUNT_BIG(*) AS HeaderCount FROM dbo.ih_hist x
              WHERE x.ih_domain = d.idh_domain AND x.ih_inv_nbr = d.idh_inv_nbr) hc
            LEFT JOIN dbo.ih_hist h ON hc.HeaderCount = 1 AND h.ih_domain = d.idh_domain AND h.ih_inv_nbr = d.idh_inv_nbr
            OUTER APPLY (SELECT COUNT_BIG(*) N, MIN(c.cm_sort) Name FROM dbo.cm_mstr c
              WHERE c.cm_domain = d.idh_domain AND c.cm_addr = h.ih_cust) cm
            OUTER APPLY (SELECT COUNT_BIG(*) N, MIN(a.ad_sort) Name FROM dbo.ad_mstr a
              WHERE a.ad_domain = d.idh_domain AND a.ad_addr = h.ih_ship) ship
            OUTER APPLY (SELECT COUNT_BIG(*) N, MIN(t.pt_desc1) Desc1, MIN(t.pt_desc2) Desc2 FROM dbo.pt_mstr t
              WHERE t.pt_domain = d.idh_domain AND t.pt_part = d.idh_part) part
            WHERE {string.Join(" AND ", detail)}
            ORDER BY {(workspace is null ? "d.prrowid" : "h.ih_ship_date DESC, d.prrowid")}
            """, p);
    }
}
