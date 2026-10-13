using System.Globalization;
using System.IO.Compression;
using System.Xml;
using Kst.Domain.HistoricalShipments;

namespace Kst.Exports;

/// <summary>Forward-only XLSX: no shared string table, worksheet DOM or history-wide buffer.</summary>
public static class HistoricalShipmentsWorkbook
{
    public static async Task WriteAsync(IAsyncEnumerable<ShipmentRow> rows, IReadOnlyList<string> columns, Stream destination, CancellationToken ct)
    {
        using var zip = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);
        WritePart(zip, "[Content_Types].xml", """
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>
            """);
        WritePart(zip, "_rels/.rels", """
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
            """);
        WritePart(zip, "xl/workbook.xml", """
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Historical Shipments" sheetId="1" r:id="rId1"/></sheets></workbook>
            """);
        WritePart(zip, "xl/_rels/workbook.xml.rels", """
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>
            """);
        using var output = zip.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Fastest).Open();
        using var xml = XmlWriter.Create(output, new XmlWriterSettings { Encoding = new System.Text.UTF8Encoding(false), CloseOutput = false });
        const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        xml.WriteStartElement("worksheet", ns); xml.WriteStartElement("sheetData", ns);
        var specs = columns.Select(id => HistoricalShipmentsRules.Columns.Single(c => c.Id == id)).ToArray();
        void Row(IReadOnlyList<string?> values, bool header)
        {
            xml.WriteStartElement("row", ns);
            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (value is null) { xml.WriteStartElement("c", ns); xml.WriteEndElement(); continue; }
                if (value.Length > 32767) throw new ShipmentWorkbookCapacityException("XLSX cell exceeds 32,767 characters. No workbook was prepared; content was not truncated.");
                // Excel numbers are limited to 15 significant digits. Preserve larger exact values as literal text.
                var digits = value.TrimStart('-', '+').Replace(".", "").TrimStart('0').Length;
                var number = !header && specs[i].Kind == "decimal" && digits <= 15 &&
                    decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _);
                xml.WriteStartElement("c", ns); xml.WriteAttributeString("t", number ? "n" : "inlineStr");
                if (number) xml.WriteElementString("v", ns, value);
                else
                {
                    xml.WriteStartElement("is", ns); xml.WriteStartElement("t", ns);
                    xml.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
                    xml.WriteString(value); xml.WriteEndElement(); xml.WriteEndElement();
                }
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
        }
        Row(specs.Select(c => (string?)c.Label).ToArray(), true);
        long count = 0;
        await foreach (var row in rows.WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            if (++count > HistoricalShipmentsRules.MaximumXlsxDataRows)
                throw new ShipmentWorkbookCapacityException("XLSX exceeds 1,048,575 detail rows plus its header. No workbook was prepared; rows were not truncated.");
            if (row.Cells.Count != columns.Count) throw new InvalidOperationException("Invalid frozen projection.");
            Row(row.Cells, false);
        }
        xml.WriteEndElement(); xml.WriteEndElement(); xml.Flush();
        ct.ThrowIfCancellationRequested();
    }
    private static void WritePart(ZipArchive zip, string path, string text)
    {
        using var writer = new StreamWriter(zip.CreateEntry(path).Open(), new System.Text.UTF8Encoding(false));
        writer.Write(text);
    }
}
