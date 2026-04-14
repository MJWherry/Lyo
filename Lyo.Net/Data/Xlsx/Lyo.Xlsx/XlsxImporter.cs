using System.Runtime.InteropServices;
using ClosedXML.Excel;
using ClosedXML.Graphics;
using ExcelDataReader;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Xlsx.Models;
using Microsoft.Extensions.Logging;

namespace Lyo.Xlsx;

internal sealed class XlsxImporter : IXlsxImporter
{
    private readonly Func<ExcelDataTableConfiguration> _getConfig;
    private readonly ILogger _logger;

    private ExcelDataTableConfiguration Config => _getConfig();

    static XlsxImporter()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            LoadOptions.DefaultGraphicEngine = new DefaultGraphicEngine("DejaVu Sans");
    }

    internal XlsxImporter(Func<ExcelDataTableConfiguration> getConfig, ILogger logger)
    {
        _getConfig = getConfig;
        _logger = logger;
    }

    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath, nameof(xlsxFilePath));
        _logger.LogDebug("Parsing {ParsingXlsxPath} as dictionary", xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDictionary(inputStream);
    }

    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream, nameof(xlsxStream));
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Parsing xlsx stream as dictionary");
        using var reader = ExcelReaderFactory.CreateReader(xlsxStream);
        var dataSet = reader.AsDataSet(new() { ConfigureDataTable = _ => Config });
        if (dataSet.Tables.Count == 0)
            return new Dictionary<int, IReadOnlyDictionary<int, string>>();

        var table = dataSet.Tables[0]!;
        return ConvertDataTableToDictionary(table);
    }

    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath, nameof(xlsxFilePath));
        _logger.LogDebug("Parsing {ParsingXlsxPath} as DataTable", xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDataTable(inputStream, useHeaderRow);
    }

    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream, nameof(xlsxStream));
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

    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes, nameof(xlsxBytes));
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDataTable(ms, useHeaderRow);
    }

    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes, nameof(xlsxBytes));
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDictionary(ms);
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

        var usedRange = ws.RangeUsed();
        if (usedRange == null) {
            var empty = new DataTable.Models.DataTable();
            return Result<DataTable.Models.DataTable>.Success(empty);
        }

        var lastRow = usedRange.LastRow().RowNumber();
        var lastCol = usedRange.LastColumn().ColumnNumber();
        var dt = new DataTable.Models.DataTable();
        for (var col = 1; col <= lastCol; col++) {
            var cell = ws.Cell(1, col);
            var headerValue = useHeaderRow ? GetCellDisplayValue(cell) : $"Column{col - 1}";
            dt.SetHeader(col - 1, ExtractCellValue(cell, headerValue).ToDataTableCell());
        }

        var startDataRow = useHeaderRow ? 2 : 1;
        for (var rowNum = startDataRow; rowNum <= lastRow; rowNum++) {
            var dataRow = dt.AddRow();
            for (var col = 1; col <= lastCol; col++) {
                var cell = ws.Cell(rowNum, col);
                var displayValue = GetCellDisplayValue(cell);
                dataRow.SetCell(col - 1, ExtractCellValue(cell, displayValue).ToDataTableCell());
            }
        }

        return Result<DataTable.Models.DataTable>.Success(dt);
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
        if (color == null)
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
    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(string xlsxFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath, nameof(xlsxFilePath));
        _logger.LogDebug("Parsing {ParsingXlsxPath} as dictionary", xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDictionaryAsync(inputStream, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxStreamAsDictionaryAsync(Stream xlsxStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream, nameof(xlsxStream));
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

                        var table = dataSet.Tables[0]!;
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

    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(string xlsxFilePath, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath, nameof(xlsxFilePath));
        _logger.LogDebug("Parsing {ParsingXlsxPath} as DataTable", xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDataTableAsync(inputStream, useHeaderRow, ct).ConfigureAwait(false);
    }

    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream, nameof(xlsxStream));
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

    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes, nameof(xlsxBytes));
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDataTableAsync(ms, useHeaderRow, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxBytesAsDictionaryAsync(byte[] xlsxBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes, nameof(xlsxBytes));
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDictionaryAsync(ms, ct).ConfigureAwait(false);
    }
#endif
}