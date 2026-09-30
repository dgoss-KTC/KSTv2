using ClosedXML.Excel;
using Kst.Domain.OpenOrders;

namespace Kst.Exports;

public static class OpenOrdersWorkbook
{
    public static byte[] Create(IReadOnlyList<OpenOrderLine> rows, IReadOnlyList<string> columns,
        DateTimeOffset acquiredAtUtc, bool isStale)
    {
        using var book = new XLWorkbook();
        var sheet = book.Worksheets.Add("Open Orders");
        for (var col = 0; col < columns.Count; col++)
        {
            var definition = OpenOrdersReportColumns.All[columns[col]];
            sheet.Cell(1, col + 1).Value = definition.Label;
            for (var index = 0; index < rows.Count; index++)
            {
                var cell = sheet.Cell(index + 2, col + 1);
                var value = definition.Value(rows[index]);
                cell.Value = XLCellValue.FromObject(value);
                if (value is DateOnly) cell.Style.DateFormat.Format = "m/d/yyyy";
            }
        }
        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
        var metadata = book.Worksheets.Add("Report Metadata");
        metadata.Cell(1, 1).Value = "Acquired At UTC";
        metadata.Cell(1, 2).Value = acquiredAtUtc.ToUniversalTime().ToString("O");
        metadata.Cell(2, 1).Value = "Status";
        metadata.Cell(2, 2).Value = isStale ? "STALE — not valid for operational/QXtend validation" : "Report only — not operational/QXtend validation";
        metadata.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        book.SaveAs(stream);
        return stream.ToArray();
    }
}
