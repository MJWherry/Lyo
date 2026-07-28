using System.Data;
using System.Runtime.InteropServices;
using ClosedXML.Excel;
using ClosedXML.Graphics;
using ExcelDataReader;
using Lyo.Exceptions;
using Lyo.Result;
using Lyo.Xlsx.Models;
using Microsoft.Extensions.Logging;

namespace Lyo.Xlsx;

internal sealed class XlsxReader : IXlsxReader
{
    private readonly Func<ExcelDataTableConfiguration> _getConfig;
    private readonly ILogger _logger;

    private ExcelDataTableConfiguration Config => _getConfig();

    static XlsxReader()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            LoadOptions.DefaultGraphicEngine = new DefaultGraphicEngine("DejaVu Sans");
    }

    internal XlsxReader(Func<ExcelDataTableConfiguration> getConfig, ILogger logger)
    {
        _getConfig = getConfig;
        _logger = logger;
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDictionary(System.String)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        _logger.LogDebug("Parsing {ParsingXlsxPath} as dictionary", xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDictionary(inputStream);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDictionary(System.IO.Stream)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Parsing xlsx stream as dictionary");
        using var reader = ExcelReaderFactory.CreateReader(xlsxStream);
        var dataSet = reader.AsDataSet(new() { ConfigureDataTable = _ => Config });
        if (dataSet.Tables.Count == 0)
            return new Dictionary<int, IReadOnlyDictionary<int, string>>();

        var table = dataSet.Tables[0];
        return ConvertDataTableToDictionary(table);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTable(System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        _logger.LogDebug("Parsing {ParsingXlsxPath} as DataTable", xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDataTable(inputStream, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTable(System.IO.Stream,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Parsing xlsx stream as DataTable");
        try {
            using var workbook = new XLWorkbook(xlsxStream);
            return ConvertWorkbookToDataTable(workbook, useHeaderRow ?? Config.UseHeaderRow);
        }
        catch (Exception ex) {
            return Result<DataTable.Models.DataTable>.Failure(ex);
        }
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTable(System.Byte[],System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDataTable(ms, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDictionary(System.Byte[])' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDictionary(ms);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ListSheetNames(System.String)' />
    public IReadOnlyList<string> ListSheetNames(string xlsxFilePath)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ListSheetNames(inputStream);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ListSheetNames(System.IO.Stream)' />
    public IReadOnlyList<string> ListSheetNames(Stream xlsxStream)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Listing xlsx sheet names");
        using var reader = ExcelReaderFactory.CreateReader(xlsxStream);
        var names = new List<string>();
        do
            names.Add(reader.Name);
        while (reader.NextResult());

        return names;
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ListSheetNames(System.Byte[])' />
    public IReadOnlyList<string> ListSheetNames(byte[] xlsxBytes)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ListSheetNames(ms);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDictionary(System.String,System.String)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath, string sheetName)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDictionary(inputStream, sheetName);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDictionary(System.String,System.Int32)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath, int sheetIndex)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDictionary(inputStream, sheetIndex);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDictionary(System.IO.Stream,System.String)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream, string sheetName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sheetName);
        return ParseXlsxStreamAsDictionaryCore(
            xlsxStream, dataSet => dataSet.Tables.Contains(sheetName) ? dataSet.Tables[sheetName] : null, $"Worksheet '{sheetName}' was not found in the workbook.");
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDictionary(System.IO.Stream,System.Int32)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream, int sheetIndex)
    {
        ArgumentHelpers.ThrowIfNegative(sheetIndex);
        return ParseXlsxStreamAsDictionaryCore(
            xlsxStream, dataSet => sheetIndex < dataSet.Tables.Count ? dataSet.Tables[sheetIndex] : null, $"Worksheet index {sheetIndex} is out of range.");
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDictionary(System.Byte[],System.String)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes, string sheetName)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDictionary(ms, sheetName);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDictionary(System.Byte[],System.Int32)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes, int sheetIndex)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDictionary(ms, sheetIndex);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTable(System.String,System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, string sheetName, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDataTable(inputStream, sheetName, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTable(System.String,System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, int sheetIndex, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDataTable(inputStream, sheetIndex, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTable(System.IO.Stream,System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, string sheetName, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sheetName);
        return ParseXlsxStreamAsDataTableCore(
            xlsxStream, workbook => workbook.Worksheets.TryGetWorksheet(sheetName, out var ws) ? ws : null, $"Worksheet '{sheetName}' was not found in the workbook.",
            useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTable(System.IO.Stream,System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, int sheetIndex, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNegative(sheetIndex);
        return ParseXlsxStreamAsDataTableCore(
            xlsxStream, workbook => workbook.Worksheets.Skip(sheetIndex).FirstOrDefault(), $"Worksheet index {sheetIndex} is out of range.", useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTable(System.Byte[],System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, string sheetName, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDataTable(ms, sheetName, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTable(System.Byte[],System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, int sheetIndex, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDataTable(ms, sheetIndex, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsAllSheets(System.String,System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxFileAsAllSheets(string xlsxFilePath, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsAllSheets(inputStream, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsAllSheets(System.IO.Stream,System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxStreamAsAllSheets(Stream xlsxStream, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Parsing all xlsx sheets as DataTables");
        var effectiveUseHeaderRow = useHeaderRow ?? Config.UseHeaderRow;
        using var workbook = new XLWorkbook(xlsxStream);
        var result = new Dictionary<string, DataTable.Models.DataTable>();
        foreach (var ws in workbook.Worksheets)
            result[ws.Name] = ConvertWorksheetToDataTable(ws, effectiveUseHeaderRow);

        return result;
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsAllSheets(System.Byte[],System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxBytesAsAllSheets(byte[] xlsxBytes, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsAllSheets(ms, useHeaderRow);
    }

    private IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionaryCore(
        Stream xlsxStream,
        Func<DataSet, System.Data.DataTable?> selectTable,
        string missingSheetMessage)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Parsing xlsx stream sheet as dictionary");
        using var reader = ExcelReaderFactory.CreateReader(xlsxStream);
        var dataSet = reader.AsDataSet(new() { ConfigureDataTable = _ => Config });
        var table = selectTable(dataSet);
        if (table == null)
            throw new ArgumentException(missingSheetMessage);

        return ConvertDataTableToDictionary(table);
    }

    private Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTableCore(
        Stream xlsxStream,
        Func<XLWorkbook, IXLWorksheet?> selectWorksheet,
        string missingSheetMessage,
        bool? useHeaderRow)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Parsing xlsx stream sheet as DataTable");
        try {
            using var workbook = new XLWorkbook(xlsxStream);
            var ws = selectWorksheet(workbook);
            if (ws == null)
                return Result<DataTable.Models.DataTable>.Failure(new ArgumentException(missingSheetMessage));

            return Result<DataTable.Models.DataTable>.Success(ConvertWorksheetToDataTable(ws, useHeaderRow ?? Config.UseHeaderRow));
        }
        catch (Exception ex) {
            return Result<DataTable.Models.DataTable>.Failure(ex);
        }
    }

    private static IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ConvertDataTableToDictionary(System.Data.DataTable table)
    {
        var result = new Dictionary<int, IReadOnlyDictionary<int, string>>();
        for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++) {
            var row = table.Rows[rowIndex];
            var rowData = new Dictionary<int, string>();
            for (var colIndex = 0; colIndex < table.Columns.Count; colIndex++) {
                var value = row[colIndex].ToString() ?? string.Empty;
                rowData[colIndex] = value;
            }

            result[rowIndex] = rowData;
        }

        return result;
    }

    private static Result<DataTable.Models.DataTable> ConvertWorkbookToDataTable(XLWorkbook workbook, bool useHeaderRow)
    {
        var ws = workbook.Worksheets.FirstOrDefault();
        if (ws == null) {
            var empty = new DataTable.Models.DataTable();
            return Result<DataTable.Models.DataTable>.Success(empty);
        }

        return Result<DataTable.Models.DataTable>.Success(ConvertWorksheetToDataTable(ws, useHeaderRow));
    }

    private static DataTable.Models.DataTable ConvertWorksheetToDataTable(IXLWorksheet ws, bool useHeaderRow)
    {
        var usedRange = ws.RangeUsed();
        if (usedRange == null)
            return new();

        var lastRow = usedRange.LastRow().RowNumber();
        var lastCol = usedRange.LastColumn().ColumnNumber();
        // Anchor (top-left) worksheet coordinate of each merged range -> its span. Covered cells stay empty.
        var mergeMap = new Dictionary<(int Row, int Col), (int ColSpan, int RowSpan)>();
        foreach (var range in ws.MergedRanges) {
            var first = range.RangeAddress.FirstAddress;
            var last = range.RangeAddress.LastAddress;
            mergeMap[(first.RowNumber, first.ColumnNumber)] = (last.ColumnNumber - first.ColumnNumber + 1, last.RowNumber - first.RowNumber + 1);
        }

        var dt = new DataTable.Models.DataTable();
        for (var col = 1; col <= lastCol; col++) {
            var cell = ws.Cell(1, col);
            var headerValue = useHeaderRow ? GetCellDisplayValue(cell) : $"Column{col - 1}";
            var headerCell = ExtractCellValue(cell, headerValue);
            if (useHeaderRow && mergeMap.TryGetValue((1, col), out var headerSpan))
                headerCell = headerCell with { ColSpan = headerSpan.ColSpan, RowSpan = headerSpan.RowSpan };

            dt.SetHeader(col - 1, headerCell.ToDataTableCell());
        }

        var startDataRow = useHeaderRow ? 2 : 1;
        for (var rowNum = startDataRow; rowNum <= lastRow; rowNum++) {
            var dataRow = dt.AddRow();
            for (var col = 1; col <= lastCol; col++) {
                var cell = ws.Cell(rowNum, col);
                var displayValue = GetCellDisplayValue(cell);
                var cellValue = ExtractCellValue(cell, displayValue);
                if (mergeMap.TryGetValue((rowNum, col), out var span))
                    cellValue = cellValue with { ColSpan = span.ColSpan, RowSpan = span.RowSpan };

                dataRow.SetCell(col - 1, cellValue.ToDataTableCell());
            }
        }

        return dt;
    }

    private static string GetCellDisplayValue(IXLCell cell)
    {
        try {
            return cell.GetFormattedString() ?? cell.GetString() ?? "";
        }
        catch {
            return cell.GetString() ?? "";
        }
    }

    private static XlsxCellValue ExtractCellValue(IXLCell cell, string displayValue)
    {
        var style = cell.Style;
        double? fontSize = style.Font.FontSize > 0 ? style.Font.FontSize : null;
        var fontName = style.Font.FontName;
        bool? fontBold = style.Font.Bold ? true : null;
        bool? fontItalic = style.Font.Italic ? true : null;
        bool? fontUnderline = style.Font.Underline != XLFontUnderlineValues.None ? true : null;
        bool? fontStrikethrough = style.Font.Strikethrough ? true : null;
        var fontColor = TryGetColorHex(style.Font.FontColor);
        var bgColor = TryGetColorHex(style.Fill.BackgroundColor);
        var hAlign = style.Alignment.Horizontal != XLAlignmentHorizontalValues.General ? style.Alignment.Horizontal.ToString() : null;
        var vAlign = style.Alignment.Vertical != XLAlignmentVerticalValues.Bottom ? style.Alignment.Vertical.ToString() : null;
        var numFormat = style.NumberFormat.Format;
        var numberFormat = !string.IsNullOrEmpty(numFormat) ? numFormat : null;
        int? textRotation = style.Alignment.TextRotation != 0 ? style.Alignment.TextRotation : null;
        bool? wrapText = style.Alignment.WrapText ? true : null;
        var borderTop = style.Border.TopBorder != XLBorderStyleValues.None ? style.Border.TopBorder.ToString() : null;
        var borderBottom = style.Border.BottomBorder != XLBorderStyleValues.None ? style.Border.BottomBorder.ToString() : null;
        var borderLeft = style.Border.LeftBorder != XLBorderStyleValues.None ? style.Border.LeftBorder.ToString() : null;
        var borderRight = style.Border.RightBorder != XLBorderStyleValues.None ? style.Border.RightBorder.ToString() : null;
        var borderColor = TryGetColorHex(style.Border.TopBorderColor);
        return new(
            displayValue, fontSize, string.IsNullOrEmpty(fontName) ? null : fontName, fontBold, fontItalic, fontUnderline, fontStrikethrough, fontColor, bgColor, hAlign, vAlign,
            numberFormat, textRotation, wrapText, borderTop, borderBottom, borderLeft, borderRight, borderColor);
    }

    private static string? TryGetColorHex(XLColor? color)
    {
        if (color == null || color.ColorType == XLColorType.Theme)
            return null;

        try {
            var c = color.Color;
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
        catch {
            return null;
        }
    }

#if !NETSTANDARD2_0
    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDictionaryAsync(System.String,System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(string xlsxFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        _logger.LogDebug("Parsing {ParsingXlsxPath} as dictionary", xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDictionaryAsync(inputStream, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDictionaryAsync(System.IO.Stream,System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxStreamAsDictionaryAsync(Stream xlsxStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Parsing xlsx stream as dictionary");
        ct.ThrowIfCancellationRequested();
        try {
            return await Task.Run(
                    () => {
                        ct.ThrowIfCancellationRequested();
                        using var reader = ExcelReaderFactory.CreateReader(xlsxStream);
                        var dataSet = reader.AsDataSet(new() { ConfigureDataTable = _ => Config });
                        ct.ThrowIfCancellationRequested();
                        if (dataSet.Tables.Count == 0)
                            return new Dictionary<int, IReadOnlyDictionary<int, string>>();

                        var table = dataSet.Tables[0];
                        return ConvertDataTableToDictionary(table);
                    }, ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("Parse operation was cancelled");
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to parse xlsx stream as dictionary");
            throw;
        }
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTableAsync(System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(string xlsxFilePath, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        _logger.LogDebug("Parsing {ParsingXlsxPath} as DataTable", xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDataTableAsync(inputStream, useHeaderRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableAsync(System.IO.Stream,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Parsing xlsx stream as DataTable");
        var effectiveUseHeaderRow = useHeaderRow ?? Config.UseHeaderRow;
        ct.ThrowIfCancellationRequested();
        try {
            return await Task.Run(
                    () => {
                        ct.ThrowIfCancellationRequested();
                        using var workbook = new XLWorkbook(xlsxStream);
                        ct.ThrowIfCancellationRequested();
                        return ConvertWorkbookToDataTable(workbook, effectiveUseHeaderRow);
                    }, ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("Parse operation was cancelled");
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to parse xlsx stream as DataTable");
            return Result<DataTable.Models.DataTable>.Failure(ex);
        }
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableAsync(System.Byte[],System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDataTableAsync(ms, useHeaderRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDictionaryAsync(System.Byte[],System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxBytesAsDictionaryAsync(byte[] xlsxBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDictionaryAsync(ms, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ListSheetNamesAsync(System.String,System.Threading.CancellationToken)' />
    public async Task<IReadOnlyList<string>> ListSheetNamesAsync(string xlsxFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ListSheetNamesAsync(inputStream, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ListSheetNamesAsync(System.IO.Stream,System.Threading.CancellationToken)' />
    public Task<IReadOnlyList<string>> ListSheetNamesAsync(Stream xlsxStream, CancellationToken ct = default)
        => GuardedRunAsync(() => ListSheetNames(xlsxStream), ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ListSheetNamesAsync(System.Byte[],System.Threading.CancellationToken)' />
    public async Task<IReadOnlyList<string>> ListSheetNamesAsync(byte[] xlsxBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ListSheetNamesAsync(ms, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDictionaryAsync(System.String,System.String,System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(string xlsxFilePath, string sheetName, CancellationToken ct =
 default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDictionaryAsync(inputStream, sheetName, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDictionaryAsync(System.String,System.Int32,System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(string xlsxFilePath, int sheetIndex, CancellationToken ct =
 default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDictionaryAsync(inputStream, sheetIndex, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDictionaryAsync(System.IO.Stream,System.String,System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxStreamAsDictionaryAsync(Stream xlsxStream, string sheetName, CancellationToken ct = default)
        => GuardedRunAsync(() => ParseXlsxStreamAsDictionary(xlsxStream, sheetName), ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDictionaryAsync(System.IO.Stream,System.Int32,System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxStreamAsDictionaryAsync(Stream xlsxStream, int sheetIndex, CancellationToken ct = default)
        => GuardedRunAsync(() => ParseXlsxStreamAsDictionary(xlsxStream, sheetIndex), ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDictionaryAsync(System.Byte[],System.String,System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxBytesAsDictionaryAsync(byte[] xlsxBytes, string sheetName, CancellationToken ct =
 default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDictionaryAsync(ms, sheetName, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDictionaryAsync(System.Byte[],System.Int32,System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxBytesAsDictionaryAsync(byte[] xlsxBytes, int sheetIndex, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDictionaryAsync(ms, sheetIndex, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTableAsync(System.String,System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(
        string xlsxFilePath,
        string sheetName,
        bool? useHeaderRow = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDataTableAsync(inputStream, sheetName, useHeaderRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTableAsync(System.String,System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(
        string xlsxFilePath,
        int sheetIndex,
        bool? useHeaderRow = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDataTableAsync(inputStream, sheetIndex, useHeaderRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableAsync(System.IO.Stream,System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, string sheetName, bool? useHeaderRow = null, CancellationToken ct = default)
        => GuardedRunAsync(() => ParseXlsxStreamAsDataTable(xlsxStream, sheetName, useHeaderRow), ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableAsync(System.IO.Stream,System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, int sheetIndex, bool? useHeaderRow = null, CancellationToken ct = default)
        => GuardedRunAsync(() => ParseXlsxStreamAsDataTable(xlsxStream, sheetIndex, useHeaderRow), ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableAsync(System.Byte[],System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, string sheetName, bool? useHeaderRow = null, CancellationToken ct =
 default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDataTableAsync(ms, sheetName, useHeaderRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableAsync(System.Byte[],System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, int sheetIndex, bool? useHeaderRow = null, CancellationToken ct =
 default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDataTableAsync(ms, sheetIndex, useHeaderRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsAllSheetsAsync(System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxFileAsAllSheetsAsync(
        string xlsxFilePath,
        bool? useHeaderRow = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsAllSheetsAsync(inputStream, useHeaderRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsAllSheetsAsync(System.IO.Stream,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxStreamAsAllSheetsAsync(Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct =
 default)
        => GuardedRunAsync(() => ParseXlsxStreamAsAllSheets(xlsxStream, useHeaderRow), ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsAllSheetsAsync(System.Byte[],System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxBytesAsAllSheetsAsync(
        byte[] xlsxBytes,
        bool? useHeaderRow = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsAllSheetsAsync(ms, useHeaderRow, ct).ConfigureAwait(false);
    }

    // Throw synchronously so an already-cancelled token surfaces the exact OperationCanceledException (not a derived TaskCanceledException).
    private async Task<TResult> GuardedRunAsync<TResult>(Func<TResult> work, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try {
            return await Task.Run(
                    () => {
                        ct.ThrowIfCancellationRequested();
                        return work();
                    }, ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("Parse operation was cancelled");
            throw;
        }
    }
#endif
}