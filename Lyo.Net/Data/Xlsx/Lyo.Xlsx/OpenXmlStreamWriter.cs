using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Lyo.Xlsx;

/// <summary>Kind of value carried by an <see cref="XlsxCell" />, which decides its XLSX cell type and style.</summary>
internal enum XlsxCellKind
{
    Text,
    Number,
    Boolean,
    Date
}

/// <summary>
/// A single worksheet cell captured in a form that can be both written to the OpenXML stream and measured for approximate column width without
/// re-inspecting the source value.
/// </summary>
internal readonly struct XlsxCell
{
    private XlsxCell(XlsxCellKind kind, string displayText, double numeric, int colSpan = 1, int rowSpan = 1)
    {
        Kind = kind;
        DisplayText = displayText;
        Numeric = numeric;
        ColSpan = colSpan < 1 ? 1 : colSpan;
        RowSpan = rowSpan < 1 ? 1 : rowSpan;
    }

    public XlsxCellKind Kind { get; }

    /// <summary>Text used for inline-string cells and for approximate width measurement (also the display form of numbers/dates/booleans).</summary>
    public string DisplayText { get; }

    /// <summary>Serialized numeric payload: the number itself, the OADate serial for dates, or 1/0 for booleans.</summary>
    public double Numeric { get; }

    /// <summary>Number of columns this cell spans (1 = no spanning). The cell is the top-left anchor of the merged range.</summary>
    public int ColSpan { get; }

    /// <summary>Number of rows this cell spans (1 = no spanning). The cell is the top-left anchor of the merged range.</summary>
    public int RowSpan { get; }

    public static XlsxCell Text(string? value) => new(XlsxCellKind.Text, value ?? string.Empty, 0d);

    public static XlsxCell Number(double value) => new(XlsxCellKind.Number, value.ToString(CultureInfo.InvariantCulture), value);

    public static XlsxCell Boolean(bool value) => new(XlsxCellKind.Boolean, value ? "TRUE" : "FALSE", value ? 1d : 0d);

    public static XlsxCell Date(DateTime value) => new(XlsxCellKind.Date, value.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture), value.ToOADate());

    /// <summary>Returns a copy of this cell marked as the anchor of a merged range spanning the given columns and rows.</summary>
    public XlsxCell WithSpan(int colSpan, int rowSpan) => new(Kind, DisplayText, Numeric, colSpan, rowSpan);
}

/// <summary>
/// Streams one or more worksheets straight into an XLSX package via <see cref="OpenXmlWriter" />, keeping memory bounded regardless of row count.
/// Column widths are approximated from a bounded sample of the leading rows instead of ClosedXML's graphics-engine auto-fit.
/// </summary>
internal sealed class OpenXmlStreamWriter : IDisposable
{
    private const uint StyleDefault = 0;
    private const uint StyleBoldHeader = 1;
    private const uint StyleDate = 2;
    private const uint DateNumberFormatId = 164;

    /// <summary>Number of leading rows (plus the header) sampled to size columns before streaming begins.</summary>
    private const int WidthSampleRows = 200;

    /// <summary>Upper bound on an approximated column width, in character units.</summary>
    private const double MaxColumnWidth = 80d;

    private readonly SpreadsheetDocument _document;
    private readonly Sheets _sheets;
    private readonly WorkbookPart _workbookPart;
    private uint _nextSheetId = 1;

    public OpenXmlStreamWriter(Stream stream)
    {
        _document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
        _workbookPart = _document.AddWorkbookPart();
        _workbookPart.Workbook = new Workbook();
        var stylesPart = _workbookPart.AddNewPart<WorkbookStylesPart>();
        stylesPart.Stylesheet = BuildStylesheet();
        stylesPart.Stylesheet.Save();
        _sheets = _workbookPart.Workbook.AppendChild(new Sheets());
    }

    public void Dispose()
    {
        _workbookPart.Workbook.Save();
        _document.Dispose();
    }

    /// <summary>Writes a worksheet: a bold header row followed by <paramref name="rows" />, with column widths approximated from the leading rows.</summary>
    public void WriteSheet(string sheetName, IReadOnlyList<string> headers, IEnumerable<XlsxCell[]> rows, CancellationToken ct = default)
    {
        var worksheetPart = _workbookPart.AddNewPart<WorksheetPart>();
        var relationshipId = _workbookPart.GetIdOfPart(worksheetPart);
        var columnCount = headers.Count;
        var columnLetters = BuildColumnLetters(columnCount);

        var maxChars = new int[columnCount];
        for (var c = 0; c < columnCount; c++)
            maxChars[c] = headers[c]?.Length ?? 0;

        // Sample the leading rows to size columns; <cols> must be emitted before <sheetData>, so widths come from this bounded buffer.
        var sample = new List<XlsxCell[]>(Math.Min(WidthSampleRows, 1024));
        using var enumerator = rows.GetEnumerator();
        while (sample.Count < WidthSampleRows && enumerator.MoveNext()) {
            ct.ThrowIfCancellationRequested();
            var row = enumerator.Current;
            UpdateWidths(maxChars, row);
            sample.Add(row);
        }

        // pendingRows[col] > 0 means the column is covered by a rowspan from a previous row; mergeRefs collects A1-style merge ranges.
        var pendingRows = new int[columnCount];
        var mergeRefs = new List<string>();
        using (var writer = OpenXmlWriter.Create(worksheetPart)) {
            writer.WriteStartElement(new Worksheet());
            WriteColumns(writer, maxChars);
            writer.WriteStartElement(new SheetData());

            uint rowIndex = 1;
            WriteHeaderRow(writer, headers, columnLetters, rowIndex);
            rowIndex++;

            foreach (var row in sample) {
                WriteDataRow(writer, row, columnLetters, rowIndex, pendingRows, mergeRefs);
                rowIndex++;
            }

            while (enumerator.MoveNext()) {
                ct.ThrowIfCancellationRequested();
                WriteDataRow(writer, enumerator.Current, columnLetters, rowIndex, pendingRows, mergeRefs);
                rowIndex++;
            }

            writer.WriteEndElement(); // SheetData

            // mergeCells is valid after sheetData in the worksheet schema, so streaming is preserved.
            if (mergeRefs.Count > 0) {
                writer.WriteStartElement(new MergeCells { Count = (uint)mergeRefs.Count });
                foreach (var mergeRef in mergeRefs)
                    writer.WriteElement(new MergeCell { Reference = mergeRef });

                writer.WriteEndElement(); // MergeCells
            }

            writer.WriteEndElement(); // Worksheet
        }

        _sheets.Append(new Sheet { Name = sheetName, SheetId = _nextSheetId++, Id = relationshipId });
    }

    private static void UpdateWidths(int[] maxChars, XlsxCell[] row)
    {
        var count = Math.Min(maxChars.Length, row.Length);
        for (var c = 0; c < count; c++) {
            var length = row[c].DisplayText.Length;
            if (length > maxChars[c])
                maxChars[c] = length;
        }
    }

    private static void WriteColumns(OpenXmlWriter writer, int[] maxChars)
    {
        if (maxChars.Length == 0)
            return;

        writer.WriteStartElement(new Columns());
        for (var c = 0; c < maxChars.Length; c++) {
            var width = Math.Min(maxChars[c] + 2d, MaxColumnWidth);
            if (width < 1d)
                width = 1d;

            var column = new Column {
                Min = (uint)(c + 1),
                Max = (uint)(c + 1),
                Width = width,
                CustomWidth = true
            };

            writer.WriteElement(column);
        }

        writer.WriteEndElement(); // Columns
    }

    private static void WriteHeaderRow(OpenXmlWriter writer, IReadOnlyList<string> headers, string[] columnLetters, uint rowIndex)
    {
        writer.WriteStartElement(new Row { RowIndex = rowIndex });
        var rowRef = rowIndex.ToString(CultureInfo.InvariantCulture);
        for (var c = 0; c < headers.Count; c++)
            WriteInlineStringCell(writer, columnLetters[c] + rowRef, headers[c] ?? string.Empty, StyleBoldHeader);

        writer.WriteEndElement(); // Row
    }

    private static void WriteDataRow(OpenXmlWriter writer, XlsxCell[] row, string[] columnLetters, uint rowIndex, int[] pendingRows, List<string> mergeRefs)
    {
        writer.WriteStartElement(new Row { RowIndex = rowIndex });
        var rowRef = rowIndex.ToString(CultureInfo.InvariantCulture);
        var c = 0;
        while (c < columnLetters.Length) {
            if (pendingRows[c] > 0) {
                // Covered by a rowspan from a previous row: emit nothing for this coordinate.
                pendingRows[c]--;
                c++;
                continue;
            }

            if (c >= row.Length) {
                c++;
                continue;
            }

            var cell = row[c];
            var colSpan = Math.Min(cell.ColSpan, columnLetters.Length - c);
            var rowSpan = cell.RowSpan;
            WriteDataCell(writer, columnLetters[c] + rowRef, cell);
            if (colSpan > 1 || rowSpan > 1) {
                var endRow = rowIndex + (uint)(rowSpan - 1);
                mergeRefs.Add($"{columnLetters[c]}{rowRef}:{columnLetters[c + colSpan - 1]}{endRow.ToString(CultureInfo.InvariantCulture)}");
                if (rowSpan > 1) {
                    for (var k = c; k < c + colSpan; k++)
                        pendingRows[k] += rowSpan - 1;
                }
            }

            c += colSpan; // covered columns of the merged range in this row are skipped
        }

        writer.WriteEndElement(); // Row
    }

    private static void WriteDataCell(OpenXmlWriter writer, string cellReference, XlsxCell cell)
    {
        switch (cell.Kind) {
            case XlsxCellKind.Number:
                writer.WriteStartElement(new Cell { CellReference = cellReference });
                writer.WriteElement(new CellValue(cell.Numeric.ToString(CultureInfo.InvariantCulture)));
                writer.WriteEndElement();
                break;
            case XlsxCellKind.Boolean:
                writer.WriteStartElement(new Cell { CellReference = cellReference, DataType = CellValues.Boolean });
                writer.WriteElement(new CellValue(cell.Numeric != 0d ? "1" : "0"));
                writer.WriteEndElement();
                break;
            case XlsxCellKind.Date:
                writer.WriteStartElement(new Cell { CellReference = cellReference, StyleIndex = StyleDate });
                writer.WriteElement(new CellValue(cell.Numeric.ToString(CultureInfo.InvariantCulture)));
                writer.WriteEndElement();
                break;
            default:
                WriteInlineStringCell(writer, cellReference, cell.DisplayText, StyleDefault);
                break;
        }
    }

    private static void WriteInlineStringCell(OpenXmlWriter writer, string cellReference, string text, uint styleIndex)
    {
        var cell = new Cell { CellReference = cellReference, DataType = CellValues.InlineString };
        if (styleIndex != StyleDefault)
            cell.StyleIndex = styleIndex;

        writer.WriteStartElement(cell);
        writer.WriteStartElement(new InlineString());
        writer.WriteElement(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        writer.WriteEndElement(); // InlineString
        writer.WriteEndElement(); // Cell
    }

    private static string[] BuildColumnLetters(int columnCount)
    {
        var letters = new string[columnCount];
        for (var c = 0; c < columnCount; c++)
            letters[c] = ColumnLetter(c);

        return letters;
    }

    private static string ColumnLetter(int zeroBasedIndex)
    {
        var index = zeroBasedIndex;
        var buffer = new char[8];
        var position = buffer.Length;
        do {
            var remainder = index % 26;
            buffer[--position] = (char)('A' + remainder);
            index = index / 26 - 1;
        } while (index >= 0);

        return new string(buffer, position, buffer.Length - position);
    }

    private static Stylesheet BuildStylesheet()
        => new(
            new NumberingFormats(
                new NumberingFormat { NumberFormatId = DateNumberFormatId, FormatCode = "mm/dd/yyyy" }
            ) { Count = 1 },
            new Fonts(
                new Font(new FontSize { Val = 11d }, new Color { Theme = 1U }, new FontName { Val = "Calibri" }),
                new Font(new Bold(), new FontSize { Val = 11d }, new Color { Theme = 1U }, new FontName { Val = "Calibri" })
            ) { Count = 2 },
            new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }),
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 })
            ) { Count = 2 },
            new Borders(
                new Border(new LeftBorder(), new RightBorder(), new TopBorder(), new BottomBorder(), new DiagonalBorder())
            ) { Count = 1 },
            new CellStyleFormats(
                new CellFormat { NumberFormatId = 0, FontId = 0, FillId = 0, BorderId = 0 }
            ) { Count = 1 },
            new CellFormats(
                new CellFormat { NumberFormatId = 0, FontId = 0, FillId = 0, BorderId = 0 },
                new CellFormat { NumberFormatId = 0, FontId = 1, FillId = 0, BorderId = 0, ApplyFont = true },
                new CellFormat { NumberFormatId = DateNumberFormatId, FontId = 0, FillId = 0, BorderId = 0, ApplyNumberFormat = true }
            ) { Count = 3 }
        );
}
