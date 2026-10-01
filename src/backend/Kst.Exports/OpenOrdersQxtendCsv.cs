using System.Globalization;
using System.Text;
using Kst.Domain.OpenOrders;

namespace Kst.Exports;

public enum OpenOrdersQxtendKind { Quantity, Price, Date }

public static class OpenOrdersQxtendCsv
{
    // Exact owner-template headers, captured in the accepted Stage 13 deterministic fixture.
    public const string QuantityHeader = "Operation (salesOrder.operation)  String,Sales Order (salesOrder.soNbr)  String,Operation (salesOrderDetail.operation)  String,Sales Order (salesOrderDetail.soNbr)  String,Line (salesOrderDetail.line)  Integer,Quantity Ordered (salesOrderDetail.sodQtyOrd)  Decimal,reasonCode (salesOrderDetail.reasonCode)  String";
    public const string PriceHeader = "Operation (salesOrder.operation)  String,Sales Order (salesOrder.soNbr)  String,Operation (salesOrderDetail.operation)  String,Sales Order (salesOrderDetail.soNbr)  String,Line (salesOrderDetail.line)  Integer,Reprice/Edit (salesOrderDetail.repriceDtl)  Logical,reasonCode (salesOrderDetail.reasonCode)  String,List Price (salesOrderDetail.sodListPr)  Decimal,Price (salesOrderDetail.sodPrice)  Decimal";
    public const string DateHeader = "Operation (salesOrder.operation)  String,Sales Order (salesOrder.soNbr)  String,Operation (salesOrderDetail.operation)  String,Sales Order (salesOrderDetail.soNbr)  String,Line (salesOrderDetail.line)  Integer,reasonCode (salesOrderDetail.reasonCode)  String,Required Date (salesOrderDetail.sodReqDate)  Date,Due Date (salesOrderDetail.sodDueDate)  Date,Performance Date (salesOrderDetail.sodPerDate)  Date,Dock Date (salesOrderDetail.sodDte01)  Date";

    public static IReadOnlyDictionary<OpenOrdersQxtendKind, byte[]> Create(IReadOnlyList<OpenOrderProposal> proposals)
    {
        var result = new Dictionary<OpenOrdersQxtendKind, byte[]>();
        foreach (var kind in Enum.GetValues<OpenOrdersQxtendKind>())
        {
            var rows = proposals.Where(p => kind switch
            {
                OpenOrdersQxtendKind.Quantity => p.Proposed.OrderQty != p.Original.OrderQty,
                OpenOrdersQxtendKind.Price => p.Proposed.Price != p.Original.Price,
                _ => OpenOrderPlanning.ChangesDates(p)
            }).OrderBy(p => p.Key.SalesOrder, StringComparer.Ordinal).ThenBy(p => p.Key.Line).ToArray();
            if (rows.Length == 0) continue;
            var text = new StringBuilder(kind switch
            {
                OpenOrdersQxtendKind.Quantity => QuantityHeader,
                OpenOrdersQxtendKind.Price => PriceHeader,
                _ => DateHeader
            }).Append("\r\n");
            string? previous = null;
            foreach (var p in rows)
            {
                var first = previous != p.Key.SalesOrder;
                previous = p.Key.SalesOrder;
                var cells = new List<string?> { first ? "M" : "", first ? p.Key.SalesOrder : "", "M", p.Key.SalesOrder,
                    p.Key.Line.ToString(CultureInfo.InvariantCulture) };
                switch (kind)
                {
                    case OpenOrdersQxtendKind.Quantity:
                        cells.Add(Number(p.Proposed.OrderQty)); cells.Add(p.ReasonCode); break;
                    case OpenOrdersQxtendKind.Price:
                        cells.Add("TRUE"); cells.Add(p.ReasonCode);
                        cells.Add(Number(p.Proposed.Price)); cells.Add(Number(p.Proposed.Price)); break;
                    case OpenOrdersQxtendKind.Date:
                        cells.Add(p.ReasonCode);
                        cells.Add(Date(p.Proposed.RequiredDate)); cells.Add(Date(p.Proposed.DueDate));
                        cells.Add(Date(p.Proposed.PerformDate)); cells.Add(Date(p.Proposed.DockDate)); break;
                }
                text.AppendJoin(',', cells.Select(Escape)).Append("\r\n");
            }
            result.Add(kind, new UTF8Encoding(false).GetBytes(text.ToString()));
        }
        return result;
    }

    private static string Number(decimal value) => value.ToString("0.############################", CultureInfo.InvariantCulture);
    private static string Date(DateOnly? value) => value?.ToString("M/d/yyyy", CultureInfo.InvariantCulture) ?? "";
    private static string Escape(string? value)
    {
        value ??= "";
        return value.IndexOfAny([',', '"', '\r', '\n']) < 0 ? value : $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
