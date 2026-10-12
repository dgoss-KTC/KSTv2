using ClosedXML.Excel;
using Kst.Domain.OpenOrderReport;

namespace Kst.Exports;

public static class OpenOrderReportWorkbook
{
    public static byte[] Create(OpenOrderReportDocument report)
    {
        if (report.Rows.Count == 0) throw new ArgumentException("No report rows.");
        var rows = OpenOrderReportRules.Freeze(report.Rows);
        using var book = new XLWorkbook();
        var sheet = book.AddWorksheet("OOR");
        for (var col = 0; col < report.Columns.Count; col++)
        {
            var index = OpenOrderReportRules.Headers.ToList().IndexOf(report.Columns[col]);
            if (index < 0) throw new ArgumentException("Unsupported report column.");
            sheet.Cell(1, col + 1).Value = report.Columns[col];
            for (var r = 0; r < rows.Count; r++)
            {
                var fact = rows[r].Cells[index];
                var cell = sheet.Cell(r + 2, col + 1);
                if (fact.Text is not null) cell.Value = fact.Text;
                else if (fact.Number is not null) cell.Value = (double)fact.Number.Value;
                else if (fact.Date is not null) cell.Value = fact.Date.Value;
                else if (fact.Boolean is not null) cell.Value = fact.Boolean.Value;
                if (OpenOrderReportRules.IsDate(index)) cell.Style.DateFormat.Format = "m/d/yyyy";
            }
        }
        var table = sheet.Range(1, 1, rows.Count + 1, report.Columns.Count).CreateTable("OpenOrders");
        table.Theme = XLTableTheme.TableStyleLight9;
        table.ShowAutoFilter = true;
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        book.SaveAs(stream);
        return stream.ToArray();
    }
}
