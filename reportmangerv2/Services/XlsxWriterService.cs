using System;

namespace reportmangerv2.Services;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Oracle.ManagedDataAccess.Client;



public interface IXlsxWriterService
{
    /// <summary>Write a plain SQL OracleDataReader to a single (multi-sheet-if-overflow) XLSX file.</summary>
    Task<(string filePath, long rowCount)> WriteQueryAsync(
        OracleDataReader reader, string outputDir, string baseFileName, CancellationToken ct);

    /// <summary>Write multiple OracleDataReaders (one per RefCursor) as separate sheets in one XLSX file.</summary>
    Task<(string filePath, long totalRows, int cursorCount)> WriteMultiCursorAsync(
        IEnumerable<(OracleDataReader reader, string sheetName)> cursors,
        string outputDir, string baseFileName, CancellationToken ct);
}

public class XlsxWriterService : IXlsxWriterService
{
    private const long MaxRowsPerSheet = 1_048_000;

    public async Task<(string filePath, long rowCount)> WriteQueryAsync(
        OracleDataReader reader, string outputDir, string baseFileName, CancellationToken ct)
    {
        Directory.CreateDirectory(outputDir);
        var filePath = Path.Combine(outputDir, $"{baseFileName}.xlsx");

        using var doc = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
        var wbPart = doc.AddWorkbookPart();
        wbPart.Workbook = new Workbook();
        var sheets = wbPart.Workbook.AppendChild(new Sheets());
        
    
    

        uint sheetId = 1;
        long totalRows = 0;
        int subSheet = 1;

        var (wsPart, writer) = CreateSheet(wbPart, sheets, "Sheet1", sheetId++);
        long rowsInSheet = 0;
        WriteHeaderRow(writer, reader);

        while (await reader.ReadAsync(ct))
        {
            if (rowsInSheet >= MaxRowsPerSheet)
            {
                CloseSheet(writer);
                subSheet++;
                (wsPart, writer) = CreateSheet(wbPart, sheets, $"Sheet{subSheet}", sheetId++);
                WriteHeaderRow(writer, reader);
                rowsInSheet = 0;
            }
            WriteDataRow(writer, reader);
            rowsInSheet++;
            totalRows++;
        }

        CloseSheet(writer);
        wbPart.Workbook.Save();
        return (filePath, totalRows);
    }

    public async Task<(string filePath, long totalRows, int cursorCount)> WriteMultiCursorAsync(
        IEnumerable<(OracleDataReader reader, string sheetName)> cursors,
        string outputDir, string baseFileName, CancellationToken ct)
    {
        Directory.CreateDirectory(outputDir);
        var filePath = Path.Combine(outputDir, $"{baseFileName}.xlsx");

        using var doc = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
        var wbPart = doc.AddWorkbookPart();
        wbPart.Workbook = new Workbook();
        var sheets = wbPart.Workbook.AppendChild(new Sheets());

        uint sheetId = 1;
        long totalRows = 0;
        int cursorCount = 0;

        foreach (var (reader, sheetBaseName) in cursors)
        {
            cursorCount++;
            var safeName = TruncateSheetName(sheetBaseName);
            int subSheet = 1;

            var (_, writer) = CreateSheet(wbPart, sheets, safeName, sheetId++);
            long rowsInSheet = 0;
            WriteHeaderRow(writer, reader);

            while (await reader.ReadAsync(ct))
            {
                if (rowsInSheet >= MaxRowsPerSheet)
                {
                    CloseSheet(writer);
                    subSheet++;
                    var continueName = TruncateSheetName($"{safeName} ({subSheet})");
                    (_, writer) = CreateSheet(wbPart, sheets, continueName, sheetId++);
                    WriteHeaderRow(writer, reader);
                    rowsInSheet = 0;
                }
                WriteDataRow(writer, reader);
                rowsInSheet++;
                totalRows++;
            }
            CloseSheet(writer);
        }

        wbPart.Workbook.Save();
        return (filePath, totalRows, cursorCount);
    }

    // ── Helpers ────────────────────────────────────────────────

    private static (WorksheetPart, OpenXmlWriter) CreateSheet(
        WorkbookPart wbPart, Sheets sheets, string name, uint sheetId)
    {
        var wsPart = wbPart.AddNewPart<WorksheetPart>();
        var writer = OpenXmlWriter.Create(wsPart);
        writer.WriteStartElement(new Worksheet());
        writer.WriteStartElement(new SheetData());

        var sheet = new Sheet
        {
            Id   = wbPart.GetIdOfPart(wsPart),
            SheetId = sheetId,
            Name = name
        };
        sheets.Append(sheet);
        return (wsPart, writer);
    }

    private static void CloseSheet(OpenXmlWriter writer)
    {
        writer.WriteEndElement(); // SheetData
        writer.WriteEndElement(); // Worksheet
        writer.Close();
    }

    private static void WriteHeaderRow(OpenXmlWriter writer, OracleDataReader reader)
    {
        writer.WriteStartElement(new Row());
        for (int i = 0; i < reader.FieldCount; i++)
        {
            writer.WriteStartElement(new Cell { DataType = CellValues.InlineString });
            writer.WriteElement(new InlineString(new Text(reader.GetName(i))));
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    private static void WriteDataRow(OpenXmlWriter writer, OracleDataReader reader)
    {
        writer.WriteStartElement(new Row());
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var val = reader.IsDBNull(i) ? string.Empty : Convert.ToString(reader.GetValue(i)) ?? string.Empty;
            writer.WriteStartElement(new Cell { DataType = CellValues.InlineString });
            writer.WriteElement(new InlineString(new Text(val)));
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    private static string TruncateSheetName(string name)
    {
        // Excel sheet names: max 31 chars, no special chars
        var clean = string.Concat(name.Where(c => !"\\/*?:[]\n\r".Contains(c)));
        return clean.Length > 31 ? clean[..31] : clean;
    }
}
