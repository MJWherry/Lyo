using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Lyo.DataTable.Models;

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
/// A single worksheet cell captured in a form that can be both written to the OpenXML stream and measured for approximate column width without re-inspecting the source
/// value.
/// </summary>
internal readonly struct XlsxCell
{
    private XlsxCell(XlsxCellKind kind, string displayText, double numeric, int colSpan = 1, int rowSpan = 1, DataTableCellFormat? format = null)
    {
        Kind = kind;
        DisplayText = displayText;
        Numeric = numeric;
        ColSpan = colSpan < 1 ? 1 : colSpan;
        RowSpan = rowSpan < 1 ? 1 : rowSpan;
        Format = format;
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

    /// <summary>Optional per-cell format from a DataTable sparse map.</summary>
    public DataTableCellFormat? Format { get; }

    public static XlsxCell Text(string? value) => new(XlsxCellKind.Text, value ?? string.Empty, 0d);

    public static XlsxCell Number(double value) => new(XlsxCellKind.Number, value.ToString(CultureInfo.InvariantCulture), value);

    public static XlsxCell Boolean(bool value) => new(XlsxCellKind.Boolean, value ? "TRUE" : "FALSE", value ? 1d : 0d);

    public static XlsxCell Date(DateTime value) => new(XlsxCellKind.Date, value.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture), value.ToOADate());

    /// <summary>Returns a copy of this cell marked as the anchor of a merged range spanning the given columns and rows.</summary>
    public XlsxCell WithSpan(int colSpan, int rowSpan) => new(Kind, DisplayText, Numeric, colSpan, rowSpan, Format);

    /// <summary>Returns a copy with the given format attached.</summary>
    public XlsxCell WithFormat(DataTableCellFormat? format) => new(Kind, DisplayText, Numeric, ColSpan, RowSpan, format);
}

/// <summary>
/// Streams one or more worksheets straight into an XLSX package via <see cref="OpenXmlWriter" />, keeping memory bounded regardless of row count. Column widths are
/// approximated from a bounded sample of the leading rows instead of ClosedXML's graphics-engine auto-fit. When cells carry <see cref="DataTableCellFormat" />, a style cache maps
/// unique formats to stylesheet xf indices (capped to avoid pathological stylesheets).
/// </summary>
internal sealed class OpenXmlStreamWriter : IDisposable
{
    private const uint StyleDefault = 0;
    private const uint StyleBoldHeader = 1;
    private const uint StyleDate = 2;
    private const uint DateNumberFormatId = 164;
    private const int MaxCustomStyles = 512;

    /// <summary>Number of leading rows (plus the header) sampled to size columns before streaming begins.</summary>
    private const int WidthSampleRows = 200;

    /// <summary>Upper bound on an approximated column width, in character units.</summary>
    private const double MaxColumnWidth = 80d;

    private readonly SpreadsheetDocument _document;
    private readonly Dictionary<DataTableCellFormat, uint> _formatStyles = new();
    private readonly Sheets _sheets;
    private readonly WorkbookStylesPart _stylesPart;
    private readonly WorkbookPart _workbookPart;
    private uint _nextCustomStyleIndex = 3;
    private uint _nextNumFmtId = 165;
    private uint _nextSheetId = 1;

    public OpenXmlStreamWriter(Stream stream)
    {
        _document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
        _workbookPart = _document.AddWorkbookPart();
        _workbookPart.Workbook = new();
        _stylesPart = _workbookPart.AddNewPart<WorkbookStylesPart>();
        _stylesPart.Stylesheet = BuildBaseStylesheet();
        _stylesPart.Stylesheet.Save();
        _sheets = _workbookPart.Workbook.AppendChild(new Sheets());
    }

    public void Dispose()
    {
        _workbookPart.Workbook.Save();
        _document.Dispose();
    }

    /// <summary>Writes a worksheet: a bold header row followed by <paramref name="rows" />, with column widths approximated from the leading rows.</summary>
    public void WriteSheet(string sheetName, IReadOnlyList<string> headers, IEnumerable<XlsxCell[]> rows, CancellationToken ct = default)
        => WriteSheet(sheetName, headers, rows, null, null, null, ct);

    /// <summary>
    /// Writes a worksheet. When <paramref name="headerFormats" /> or cell <see cref="XlsxCell.Format" /> values are present, those styles are written into the stylesheet. Header
    /// map formats win over the default bold-header style; missing header format keeps bold header.
    /// </summary>
    public void WriteSheet(
        string sheetName,
        IReadOnlyList<string> headers,
        IEnumerable<XlsxCell[]> rows,
        IReadOnlyList<DataTableCellFormat?>? headerFormats,
        CancellationToken ct = default)
        => WriteSheet(sheetName, headers, rows, headerFormats, null, null, ct);

    /// <summary>
    /// Writes a worksheet with optional header/footer format maps and an optional trailing <paramref name="footer" /> row (bold by default, like the header). Per-cell and
    /// <paramref name="footerFormats" /> win over the bold default.
    /// </summary>
    public void WriteSheet(
        string sheetName,
        IReadOnlyList<string> headers,
        IEnumerable<XlsxCell[]> rows,
        IReadOnlyList<DataTableCellFormat?>? headerFormats,
        XlsxCell[]? footer,
        IReadOnlyList<DataTableCellFormat?>? footerFormats,
        CancellationToken ct = default)
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

        if (footer != null)
            UpdateWidths(maxChars, footer);

        RegisterFormatsFromSheet(headerFormats, footerFormats, footer, sample, enumerator);

        // pendingRows[col] > 0 means the column is covered by a rowspan from a previous row; mergeRefs collects A1-style merge ranges.
        var pendingRows = new int[columnCount];
        var mergeRefs = new List<string>();
        using (var writer = OpenXmlWriter.Create(worksheetPart)) {
            writer.WriteStartElement(new Worksheet());
            WriteColumns(writer, maxChars);
            writer.WriteStartElement(new SheetData());
            uint rowIndex = 1;
            WriteHeaderRow(writer, headers, columnLetters, rowIndex, headerFormats);
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

            if (footer != null)
                WriteFooterRow(writer, footer, columnLetters, rowIndex, footerFormats, pendingRows, mergeRefs);

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

    private void RegisterFormatsFromSheet(
        IReadOnlyList<DataTableCellFormat?>? headerFormats,
        IReadOnlyList<DataTableCellFormat?>? footerFormats,
        XlsxCell[]? footer,
        List<XlsxCell[]> sample,
        IEnumerator<XlsxCell[]> enumerator)
    {
        var hasHeaderFormats = headerFormats != null && headerFormats.Any(static f => f != null);
        var hasFooterFormats = footerFormats != null && footerFormats.Any(static f => f != null);
        var footerCellHasFormats = false;
        if (footer != null) {
            foreach (var cell in footer) {
                if (cell.Format == null)
                    continue;

                footerCellHasFormats = true;
                break;
            }
        }

        var sampleHasFormats = false;
        if (!hasHeaderFormats && !hasFooterFormats && !footerCellHasFormats) {
            foreach (var row in sample) {
                foreach (var cell in row) {
                    if (cell.Format == null)
                        continue;

                    sampleHasFormats = true;
                    break;
                }

                if (sampleHasFormats)
                    break;
            }

            if (!sampleHasFormats)
                return;
        }

        var dirty = false;
        var needsFullScan = hasHeaderFormats || hasFooterFormats || footerCellHasFormats || sampleHasFormats;
        if (headerFormats != null) {
            foreach (var format in headerFormats) {
                if (format != null && EnsureFormatStyle(format))
                    dirty = true;
            }
        }

        if (footerFormats != null) {
            foreach (var format in footerFormats) {
                if (format != null && EnsureFormatStyle(format))
                    dirty = true;
            }
        }

        if (footer != null) {
            foreach (var cell in footer) {
                if (cell.Format != null && EnsureFormatStyle(cell.Format))
                    dirty = true;
            }
        }

        foreach (var row in sample) {
            foreach (var cell in row) {
                if (cell.Format == null)
                    continue;

                needsFullScan = true;
                if (EnsureFormatStyle(cell.Format))
                    dirty = true;
            }
        }

        // When any format is present, drain the rest of the sequence so styles exist before sheetData is written.
        // Typed/streaming exports without formats keep the enumerator for bounded-memory writing.
        if (needsFullScan) {
            while (enumerator.MoveNext()) {
                var row = enumerator.Current;
                sample.Add(row);
                foreach (var cell in row) {
                    if (cell.Format != null && EnsureFormatStyle(cell.Format))
                        dirty = true;
                }
            }
        }

        if (dirty)
            _stylesPart.Stylesheet.Save();
    }

    private bool EnsureFormatStyle(DataTableCellFormat format)
    {
        if (_formatStyles.ContainsKey(format))
            return false;

        if (_formatStyles.Count >= MaxCustomStyles)
            return false;

        var stylesheet = _stylesPart.Stylesheet;
        var fonts = stylesheet.Fonts!;
        var fills = stylesheet.Fills!;
        var borders = stylesheet.Borders!;
        var cellFormats = stylesheet.CellFormats!;
        var numberingFormats = stylesheet.NumberingFormats ?? stylesheet.AppendChild(new NumberingFormats { Count = 0 });
        var fontId = (uint)fonts.ChildElements.Count;
        fonts.Append(BuildFont(format));
        fonts.Count = (uint)fonts.ChildElements.Count;
        uint fillId = 0;
        if (!string.IsNullOrEmpty(format.BackgroundColor)) {
            fillId = (uint)fills.ChildElements.Count;
            fills.Append(
                new Fill(
                    new PatternFill(new ForegroundColor { Rgb = HexToRgb(format.BackgroundColor!) }, new BackgroundColor { Rgb = HexToRgb(format.BackgroundColor!) }) {
                        PatternType = PatternValues.Solid
                    }));

            fills.Count = (uint)fills.ChildElements.Count;
        }

        uint borderId = 0;
        if (!string.IsNullOrEmpty(format.BorderTop) || !string.IsNullOrEmpty(format.BorderBottom) || !string.IsNullOrEmpty(format.BorderLeft) ||
            !string.IsNullOrEmpty(format.BorderRight)) {
            borderId = (uint)borders.ChildElements.Count;
            borders.Append(BuildBorder(format));
            borders.Count = (uint)borders.ChildElements.Count;
        }

        uint numberFormatId = 0;
        if (!string.IsNullOrEmpty(format.NumberFormat)) {
            numberFormatId = _nextNumFmtId++;
            numberingFormats.Append(new NumberingFormat { NumberFormatId = numberFormatId, FormatCode = format.NumberFormat });
            numberingFormats.Count = (uint)numberingFormats.ChildElements.Count;
        }

        var cellFormat = new CellFormat {
            NumberFormatId = numberFormatId,
            FontId = fontId,
            FillId = fillId,
            BorderId = borderId,
            ApplyFont = true
        };

        if (fillId != 0)
            cellFormat.ApplyFill = true;

        if (borderId != 0)
            cellFormat.ApplyBorder = true;

        if (numberFormatId != 0)
            cellFormat.ApplyNumberFormat = true;

        if (!string.IsNullOrEmpty(format.HorizontalAlignment) || !string.IsNullOrEmpty(format.VerticalAlignment) || format.WrapText == true || format.TextRotation.HasValue) {
            cellFormat.Alignment = new();
            if (!string.IsNullOrEmpty(format.HorizontalAlignment) && Enum.TryParse<HorizontalAlignmentValues>(format.HorizontalAlignment, true, out var h))
                cellFormat.Alignment.Horizontal = h;

            if (!string.IsNullOrEmpty(format.VerticalAlignment) && Enum.TryParse<VerticalAlignmentValues>(format.VerticalAlignment, true, out var v))
                cellFormat.Alignment.Vertical = v;

            if (format.WrapText == true)
                cellFormat.Alignment.WrapText = true;

            if (format.TextRotation.HasValue)
                cellFormat.Alignment.TextRotation = (uint)format.TextRotation.Value;

            cellFormat.ApplyAlignment = true;
        }

        var styleIndex = _nextCustomStyleIndex++;
        cellFormats.Append(cellFormat);
        cellFormats.Count = (uint)cellFormats.ChildElements.Count;
        _formatStyles[format] = styleIndex;
        return true;
    }

    private static Font BuildFont(DataTableCellFormat format)
    {
        var font = new Font();
        if (format.FontBold == true)
            font.Append(new Bold());

        if (format.FontItalic == true)
            font.Append(new Italic());

        if (format.FontUnderline == true)
            font.Append(new Underline());

        if (format.FontStrikethrough == true)
            font.Append(new Strike());

        font.Append(new FontSize { Val = format.FontSize ?? 11d });
        if (!string.IsNullOrEmpty(format.FontColor))
            font.Append(new Color { Rgb = HexToRgb(format.FontColor!) });
        else
            font.Append(new Color { Theme = 1U });

        font.Append(new FontName { Val = string.IsNullOrEmpty(format.FontName) ? "Calibri" : format.FontName });
        return font;
    }

    private static Border BuildBorder(DataTableCellFormat format)
    {
        var rgb = string.IsNullOrEmpty(format.BorderColor) ? null : HexToRgb(format.BorderColor!);
        return new(
            MakeLeftBorder(format.BorderLeft, rgb), MakeRightBorder(format.BorderRight, rgb), MakeTopBorder(format.BorderTop, rgb), MakeBottomBorder(format.BorderBottom, rgb),
            new DiagonalBorder());
    }

    private static LeftBorder MakeLeftBorder(string? style, string? rgb)
    {
        var edge = new LeftBorder();
        ApplyBorderStyle(edge, style, rgb);
        return edge;
    }

    private static RightBorder MakeRightBorder(string? style, string? rgb)
    {
        var edge = new RightBorder();
        ApplyBorderStyle(edge, style, rgb);
        return edge;
    }

    private static TopBorder MakeTopBorder(string? style, string? rgb)
    {
        var edge = new TopBorder();
        ApplyBorderStyle(edge, style, rgb);
        return edge;
    }

    private static BottomBorder MakeBottomBorder(string? style, string? rgb)
    {
        var edge = new BottomBorder();
        ApplyBorderStyle(edge, style, rgb);
        return edge;
    }

    private static void ApplyBorderStyle(BorderPropertiesType edge, string? style, string? rgb)
    {
        if (string.IsNullOrEmpty(style) || !Enum.TryParse<BorderStyleValues>(style, true, out var borderStyle))
            return;

        edge.Style = borderStyle;
        if (rgb != null)
            edge.Color = new() { Rgb = rgb };
    }

    private static string HexToRgb(string hex)
    {
        var h = hex.TrimStart('#');
        if (h.Length == 6)
            return "FF" + h.ToUpperInvariant();

        if (h.Length == 8)
            return h.ToUpperInvariant();

        return "FF000000";
    }

    private uint ResolveStyle(DataTableCellFormat? format, uint fallback)
    {
        if (format == null)
            return fallback;

        return _formatStyles.TryGetValue(format, out var index) ? index : fallback;
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

    private void WriteHeaderRow(OpenXmlWriter writer, IReadOnlyList<string> headers, string[] columnLetters, uint rowIndex, IReadOnlyList<DataTableCellFormat?>? headerFormats)
    {
        writer.WriteStartElement(new Row { RowIndex = rowIndex });
        var rowRef = rowIndex.ToString(CultureInfo.InvariantCulture);
        for (var c = 0; c < headers.Count; c++) {
            var format = headerFormats != null && c < headerFormats.Count ? headerFormats[c] : null;
            var style = ResolveStyle(format, StyleBoldHeader);
            WriteInlineStringCell(writer, columnLetters[c] + rowRef, headers[c] ?? string.Empty, style);
        }

        writer.WriteEndElement(); // Row
    }

    private void WriteDataRow(OpenXmlWriter writer, XlsxCell[] row, string[] columnLetters, uint rowIndex, int[] pendingRows, List<string> mergeRefs)
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

    /// <summary>
    /// Writes a trailing footer row. Defaults to bold-header style like <see cref="WriteHeaderRow" />; per-cell or <paramref name="footerFormats" /> win. Preserves merges like
    /// data rows.
    /// </summary>
    private void WriteFooterRow(
        OpenXmlWriter writer,
        XlsxCell[] footer,
        string[] columnLetters,
        uint rowIndex,
        IReadOnlyList<DataTableCellFormat?>? footerFormats,
        int[] pendingRows,
        List<string> mergeRefs)
    {
        writer.WriteStartElement(new Row { RowIndex = rowIndex });
        var rowRef = rowIndex.ToString(CultureInfo.InvariantCulture);
        var c = 0;
        while (c < columnLetters.Length) {
            if (pendingRows[c] > 0) {
                pendingRows[c]--;
                c++;
                continue;
            }

            if (c >= footer.Length) {
                c++;
                continue;
            }

            var cell = footer[c];
            var format = cell.Format ?? (footerFormats != null && c < footerFormats.Count ? footerFormats[c] : null);
            var style = ResolveStyle(format, StyleBoldHeader);
            var colSpan = Math.Min(cell.ColSpan, columnLetters.Length - c);
            var rowSpan = cell.RowSpan;
            WriteInlineStringCell(writer, columnLetters[c] + rowRef, cell.DisplayText, style);
            if (colSpan > 1 || rowSpan > 1) {
                var endRow = rowIndex + (uint)(rowSpan - 1);
                mergeRefs.Add($"{columnLetters[c]}{rowRef}:{columnLetters[c + colSpan - 1]}{endRow.ToString(CultureInfo.InvariantCulture)}");
                if (rowSpan > 1) {
                    for (var k = c; k < c + colSpan; k++)
                        pendingRows[k] += rowSpan - 1;
                }
            }

            c += colSpan;
        }

        writer.WriteEndElement(); // Row
    }

    private void WriteDataCell(OpenXmlWriter writer, string cellReference, XlsxCell cell)
    {
        var customStyle = cell.Format != null ? ResolveStyle(cell.Format, StyleDefault) : (uint?)null;
        switch (cell.Kind) {
            case XlsxCellKind.Number: {
                var openXmlCell = new Cell { CellReference = cellReference };
                if (customStyle is > 0)
                    openXmlCell.StyleIndex = customStyle;

                writer.WriteStartElement(openXmlCell);
                writer.WriteElement(new CellValue(cell.Numeric.ToString(CultureInfo.InvariantCulture)));
                writer.WriteEndElement();
                break;
            }
            case XlsxCellKind.Boolean: {
                var openXmlCell = new Cell { CellReference = cellReference, DataType = CellValues.Boolean };
                if (customStyle is > 0)
                    openXmlCell.StyleIndex = customStyle;

                writer.WriteStartElement(openXmlCell);
                writer.WriteElement(new CellValue(cell.Numeric != 0d ? "1" : "0"));
                writer.WriteEndElement();
                break;
            }
            case XlsxCellKind.Date: {
                var style = customStyle ?? StyleDate;
                writer.WriteStartElement(new Cell { CellReference = cellReference, StyleIndex = style });
                writer.WriteElement(new CellValue(cell.Numeric.ToString(CultureInfo.InvariantCulture)));
                writer.WriteEndElement();
                break;
            }
            default:
                WriteInlineStringCell(writer, cellReference, cell.DisplayText, customStyle ?? StyleDefault);
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

        return new(buffer, position, buffer.Length - position);
    }

    private static Stylesheet BuildBaseStylesheet()
        => new(
            new NumberingFormats(new NumberingFormat { NumberFormatId = DateNumberFormatId, FormatCode = "mm/dd/yyyy" }) { Count = 1 },
            new Fonts(
                new Font(new FontSize { Val = 11d }, new Color { Theme = 1U }, new FontName { Val = "Calibri" }),
                new Font(new Bold(), new FontSize { Val = 11d }, new Color { Theme = 1U }, new FontName { Val = "Calibri" })) { Count = 2 },
            new Fills(new Fill(new PatternFill { PatternType = PatternValues.None }), new Fill(new PatternFill { PatternType = PatternValues.Gray125 })) { Count = 2 },
            new Borders(new Border(new LeftBorder(), new RightBorder(), new TopBorder(), new BottomBorder(), new DiagonalBorder())) { Count = 1 },
            new CellStyleFormats(
                new CellFormat {
                    NumberFormatId = 0,
                    FontId = 0,
                    FillId = 0,
                    BorderId = 0
                }) { Count = 1 }, new CellFormats(
                new CellFormat {
                    NumberFormatId = 0,
                    FontId = 0,
                    FillId = 0,
                    BorderId = 0
                }, new CellFormat {
                    NumberFormatId = 0,
                    FontId = 1,
                    FillId = 0,
                    BorderId = 0,
                    ApplyFont = true
                }, new CellFormat {
                    NumberFormatId = DateNumberFormatId,
                    FontId = 0,
                    FillId = 0,
                    BorderId = 0,
                    ApplyNumberFormat = true
                }) { Count = 3 });
}