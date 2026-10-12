using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace Deskair.Tests.Fakes;

/// <summary>Builds a minimal one-sheet .xlsx file in memory, so tests need no binary fixture.</summary>
public static class XlsxBuilder
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    /// <summary>Each row is a list of cells. A string is written as text; a double as a number.</summary>
    public static MemoryStream Build(params object[][] rows)
    {
        var stream = new MemoryStream();

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(archive, "[Content_Types].xml",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>""");
            Write(archive, "_rels/.rels",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            Write(archive, "xl/workbook.xml",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Sheet1" sheetId="1" r:id="rId1"/></sheets></workbook>""");
            Write(archive, "xl/_rels/workbook.xml.rels",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>""");
            Write(archive, "xl/worksheets/sheet1.xml", SheetXml(rows));
        }

        stream.Position = 0;

        return stream;
    }

    private static string SheetXml(object[][] rows)
    {
        var data = new XElement(XName.Get("sheetData", Main));

        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            var row = new XElement(XName.Get("row", Main), new XAttribute("r", rowIndex + 1));

            for (var column = 0; column < rows[rowIndex].Length; column++)
            {
                var reference = $"{(char)('A' + column)}{rowIndex + 1}";

                row.Add(rows[rowIndex][column] switch
                {
                    string text => new XElement(XName.Get("c", Main), new XAttribute("r", reference), new XAttribute("t", "inlineStr"),
                        new XElement(XName.Get("is", Main), new XElement(XName.Get("t", Main), text))),
                    double number => new XElement(XName.Get("c", Main), new XAttribute("r", reference),
                        new XElement(XName.Get("v", Main), number.ToString("R", System.Globalization.CultureInfo.InvariantCulture))),
                    _ => throw new ArgumentException("Only string and double cells are supported.")
                });
            }

            data.Add(row);
        }

        var worksheet = new XElement(XName.Get("worksheet", Main), data);

        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), worksheet).ToString(SaveOptions.DisableFormatting);
    }

    private static void Write(ZipArchive archive, string path, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(path).Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}
