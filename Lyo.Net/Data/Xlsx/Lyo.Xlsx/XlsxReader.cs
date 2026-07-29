using System.Data;
using System.Runtime.InteropServices;
using ClosedXML.Excel;
using ClosedXML.Graphics;
using ExcelDataReader;
using Lyo.DataTable.Models;
using Lyo.Exceptions;
using Lyo.Result;
using Lyo.Xlsx.Models;
using Microsoft.Extensions.Logging;

namespace Lyo.Xlsx;

internal sealed class XlsxReader : IXlsxReader
{
    private readonly Func<ExcelDataTableConfiguration> _getConfig;
    private readonly Func<DataTablePoolingOptions> _getPooling;
    private readonly ILogger _logger;

    private ExcelDataTableConfiguration Config => _getConfig();

    private DataTablePoolingOptions Pooling => _getPooling();

    static XlsxReader()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            LoadOptions.DefaultGraphicEngine = new DefaultGraphicEngine("DejaVu Sans");
    }

    internal XlsxReader(Func<ExcelDataTableConfiguration> getConfig, ILogger logger, Func<DataTablePoolingOptions>? getPooling = null)
    {
        _getConfig = getConfig;
        _logger = logger;
        _getPooling = getPooling ?? (() => new());
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

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTableWithFormatting(System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTableWithFormatting(string xlsxFilePath, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        _logger.LogDebug("Parsing {ParsingXlsxPath} as DataTable with formatting", xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDataTableWithFormatting(inputStream, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTable(System.IO.Stream,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, bool? useHeaderRow = null)
        => ParseXlsxStreamAsDataTableCore(xlsxStream, useHeaderRow, includeFormatting: false);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableWithFormatting(System.IO.Stream,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTableWithFormatting(Stream xlsxStream, bool? useHeaderRow = null)
        => ParseXlsxStreamAsDataTableCore(xlsxStream, useHeaderRow, includeFormatting: true);

    private Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTableCore(Stream xlsxStream, bool? useHeaderRow, bool includeFormatting)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Parsing xlsx stream as DataTable (formatting={IncludeFormatting})", includeFormatting);
        try {
            using var workbook = new XLWorkbook(xlsxStream);
            return ConvertWorkbookToDataTable(workbook, useHeaderRow ?? Config.UseHeaderRow, includeFormatting);
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

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableWithFormatting(System.Byte[],System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTableWithFormatting(byte[] xlsxBytes, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDataTableWithFormatting(ms, useHeaderRow);
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
            useHeaderRow, includeFormatting: false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableWithFormatting(System.IO.Stream,System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTableWithFormatting(Stream xlsxStream, string sheetName, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sheetName);
        return ParseXlsxStreamAsDataTableCore(
            xlsxStream, workbook => workbook.Worksheets.TryGetWorksheet(sheetName, out var ws) ? ws : null, $"Worksheet '{sheetName}' was not found in the workbook.",
            useHeaderRow, includeFormatting: true);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTable(System.IO.Stream,System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, int sheetIndex, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNegative(sheetIndex);
        return ParseXlsxStreamAsDataTableCore(
            xlsxStream, workbook => workbook.Worksheets.Skip(sheetIndex).FirstOrDefault(), $"Worksheet index {sheetIndex} is out of range.", useHeaderRow,
            includeFormatting: false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableWithFormatting(System.IO.Stream,System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTableWithFormatting(Stream xlsxStream, int sheetIndex, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNegative(sheetIndex);
        return ParseXlsxStreamAsDataTableCore(
            xlsxStream, workbook => workbook.Worksheets.Skip(sheetIndex).FirstOrDefault(), $"Worksheet index {sheetIndex} is out of range.", useHeaderRow,
            includeFormatting: true);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTableWithFormatting(System.String,System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTableWithFormatting(string xlsxFilePath, string sheetName, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDataTableWithFormatting(inputStream, sheetName, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTableWithFormatting(System.String,System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTableWithFormatting(string xlsxFilePath, int sheetIndex, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDataTableWithFormatting(inputStream, sheetIndex, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTable(System.Byte[],System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, string sheetName, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDataTable(ms, sheetName, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableWithFormatting(System.Byte[],System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTableWithFormatting(byte[] xlsxBytes, string sheetName, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDataTableWithFormatting(ms, sheetName, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTable(System.Byte[],System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, int sheetIndex, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDataTable(ms, sheetIndex, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableWithFormatting(System.Byte[],System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTableWithFormatting(byte[] xlsxBytes, int sheetIndex, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDataTableWithFormatting(ms, sheetIndex, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsAllSheets(System.String,System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxFileAsAllSheets(string xlsxFilePath, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsAllSheets(inputStream, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsAllSheetsWithFormatting(System.String,System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxFileAsAllSheetsWithFormatting(string xlsxFilePath, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsAllSheetsWithFormatting(inputStream, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsAllSheets(System.IO.Stream,System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxStreamAsAllSheets(Stream xlsxStream, bool? useHeaderRow = null)
        => ParseXlsxStreamAsAllSheetsCore(xlsxStream, useHeaderRow, includeFormatting: false);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsAllSheetsWithFormatting(System.IO.Stream,System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxStreamAsAllSheetsWithFormatting(Stream xlsxStream, bool? useHeaderRow = null)
        => ParseXlsxStreamAsAllSheetsCore(xlsxStream, useHeaderRow, includeFormatting: true);

    private IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxStreamAsAllSheetsCore(Stream xlsxStream, bool? useHeaderRow, bool includeFormatting)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Parsing all xlsx sheets as DataTables (formatting={IncludeFormatting})", includeFormatting);
        var effectiveUseHeaderRow = useHeaderRow ?? Config.UseHeaderRow;
        using var workbook = new XLWorkbook(xlsxStream);
        var result = new Dictionary<string, DataTable.Models.DataTable>();
        foreach (var ws in workbook.Worksheets)
            result[ws.Name] = ConvertWorksheetToDataTable(ws, effectiveUseHeaderRow, includeFormatting);

        return result;
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsAllSheets(System.Byte[],System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxBytesAsAllSheets(byte[] xlsxBytes, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsAllSheets(ms, useHeaderRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsAllSheetsWithFormatting(System.Byte[],System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxBytesAsAllSheetsWithFormatting(byte[] xlsxBytes, bool? useHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsAllSheetsWithFormatting(ms, useHeaderRow);
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
        bool? useHeaderRow,
        bool includeFormatting)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Parsing xlsx stream sheet as DataTable (formatting={IncludeFormatting})", includeFormatting);
        try {
            using var workbook = new XLWorkbook(xlsxStream);
            var ws = selectWorksheet(workbook);
            if (ws == null)
                return Result<DataTable.Models.DataTable>.Failure(new ArgumentException(missingSheetMessage));

            return Result<DataTable.Models.DataTable>.Success(ConvertWorksheetToDataTable(ws, useHeaderRow ?? Config.UseHeaderRow, includeFormatting));
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

    private Result<DataTable.Models.DataTable> ConvertWorkbookToDataTable(XLWorkbook workbook, bool useHeaderRow, bool includeFormatting)
    {
        var ws = workbook.Worksheets.FirstOrDefault();
        if (ws == null) {
            var empty = new DataTable.Models.DataTable();
            return Result<DataTable.Models.DataTable>.Success(empty);
        }

        return Result<DataTable.Models.DataTable>.Success(ConvertWorksheetToDataTable(ws, useHeaderRow, includeFormatting));
    }

    private DataTable.Models.DataTable ConvertWorksheetToDataTable(IXLWorksheet ws, bool useHeaderRow, bool includeFormatting)
    {
        var usedRange = ws.RangeUsed();
        if (usedRange == null)
            return new();

        var lastRow = usedRange.LastRow().RowNumber();
        var lastCol = usedRange.LastColumn().ColumnNumber();
        var estimatedCells = lastRow * lastCol;
        var interner = new DataTableValueInterner(Pooling, estimatedCells);
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
            var headerValue = interner.Intern(useHeaderRow ? GetCellDisplayValue(cell) : $"Column{col - 1}");
            var colSpan = 1;
            var rowSpan = 1;
            if (useHeaderRow && mergeMap.TryGetValue((1, col), out var headerSpan)) {
                colSpan = headerSpan.ColSpan;
                rowSpan = headerSpan.RowSpan;
            }

            dt.SetHeader(col - 1, DataTableCell.FromValue(headerValue, colSpan, rowSpan));
            if (includeFormatting && useHeaderRow) {
                var format = interner.Intern(ExtractMeaningfulFormat(cell));
                if (format != null)
                    dt.SetFormat(-1, col - 1, format);
            }
        }

        var startDataRow = useHeaderRow ? 2 : 1;
        for (var rowNum = startDataRow; rowNum <= lastRow; rowNum++) {
            var dataRow = dt.AddRow();
            var tableRow = dt.Rows.Count - 1;
            for (var col = 1; col <= lastCol; col++) {
                var cell = ws.Cell(rowNum, col);
                var displayValue = interner.Intern(GetCellDisplayValue(cell));
                var colSpan = 1;
                var rowSpan = 1;
                if (mergeMap.TryGetValue((rowNum, col), out var span)) {
                    colSpan = span.ColSpan;
                    rowSpan = span.RowSpan;
                }

                dataRow.SetCell(col - 1, DataTableCell.FromValue(displayValue, colSpan, rowSpan));
                if (includeFormatting) {
                    var format = interner.Intern(ExtractMeaningfulFormat(cell));
                    if (format != null)
                        dt.SetFormat(tableRow, col - 1, format);
                }
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

    /// <summary>
    /// Extracts meaningful (non-default) formatting only. Theme colors and workbook defaults are ignored so unstyled sheets stay out of the format map.
    /// </summary>
    private static DataTableCellFormat? ExtractMeaningfulFormat(IXLCell cell)
    {
        var style = cell.Style;
        bool? fontBold = style.Font.Bold ? true : null;
        bool? fontItalic = style.Font.Italic ? true : null;
        bool? fontUnderline = style.Font.Underline != XLFontUnderlineValues.None ? true : null;
        bool? fontStrikethrough = style.Font.Strikethrough ? true : null;
        var fontColor = TryGetColorHex(style.Font.FontColor);
        var bgColor = TryGetColorHex(style.Fill.BackgroundColor);
        var hAlign = style.Alignment.Horizontal != XLAlignmentHorizontalValues.General ? style.Alignment.Horizontal.ToString() : null;
        var vAlign = style.Alignment.Vertical != XLAlignmentVerticalValues.Bottom && style.Alignment.Vertical != XLAlignmentVerticalValues.Center
            ? style.Alignment.Vertical.ToString()
            : null;
        // Ignore default bottom/center-ish vertical; keep Top explicitly.
        if (style.Alignment.Vertical == XLAlignmentVerticalValues.Top)
            vAlign = "Top";

        var numFormat = style.NumberFormat.Format;
        var numberFormat = !string.IsNullOrEmpty(numFormat) && numFormat != "General" ? numFormat : null;
        int? textRotation = style.Alignment.TextRotation != 0 ? style.Alignment.TextRotation : null;
        bool? wrapText = style.Alignment.WrapText ? true : null;
        var borderTop = style.Border.TopBorder != XLBorderStyleValues.None ? style.Border.TopBorder.ToString() : null;
        var borderBottom = style.Border.BottomBorder != XLBorderStyleValues.None ? style.Border.BottomBorder.ToString() : null;
        var borderLeft = style.Border.LeftBorder != XLBorderStyleValues.None ? style.Border.LeftBorder.ToString() : null;
        var borderRight = style.Border.RightBorder != XLBorderStyleValues.None ? style.Border.RightBorder.ToString() : null;
        var hasBorder = borderTop != null || borderBottom != null || borderLeft != null || borderRight != null;
        // Only keep border color when a border edge is actually set (ClosedXML still reports black otherwise).
        var borderColor = hasBorder ? NormalizeColor(TryGetColorHex(style.Border.TopBorderColor), defaultBlack: true) : null;
        // Skip default font size/name and workbook default black/white colors — ClosedXML reports them on every cell.
        var format = new DataTableCellFormat(
            FontSize: null,
            FontName: null,
            FontBold: fontBold,
            FontItalic: fontItalic,
            FontUnderline: fontUnderline,
            FontStrikethrough: fontStrikethrough,
            FontColor: NormalizeColor(fontColor, defaultBlack: true),
            BackgroundColor: NormalizeColor(bgColor, defaultWhite: true),
            HorizontalAlignment: hAlign,
            VerticalAlignment: vAlign,
            NumberFormat: numberFormat,
            TextRotation: textRotation,
            WrapText: wrapText,
            BorderTop: borderTop,
            BorderBottom: borderBottom,
            BorderLeft: borderLeft,
            BorderRight: borderRight,
            BorderColor: borderColor);
        return format.HasAny() ? format : null;
    }

    private static string? TryGetColorHex(XLColor? color)
    {
        if (color == null || color.ColorType == XLColorType.Theme)
            return null;

        // Indexed automatic/empty fills are not meaningful styles.
        if (color.ColorType == XLColorType.Indexed && (color.Indexed == 64 || color.Indexed == 65))
            return null;

        try {
            var c = color.Color;
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
        catch {
            return null;
        }
    }

    private static string? NormalizeColor(string? hex, bool defaultBlack = false, bool defaultWhite = false)
    {
        if (hex == null)
            return null;

        if (defaultBlack && (hex.Equals("#000000", StringComparison.OrdinalIgnoreCase) || hex.Equals("#FF000000", StringComparison.OrdinalIgnoreCase)))
            return null;

        if (defaultWhite && (hex.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase) || hex.Equals("#FFFFFFFF", StringComparison.OrdinalIgnoreCase)))
            return null;

        return hex;
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
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct = default)
        => ParseXlsxStreamAsDataTableAsyncCore(xlsxStream, useHeaderRow, includeFormatting: false, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableWithFormattingAsync(System.IO.Stream,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableWithFormattingAsync(Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct = default)
        => ParseXlsxStreamAsDataTableAsyncCore(xlsxStream, useHeaderRow, includeFormatting: true, ct);

    private async Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsyncCore(
        Stream xlsxStream, bool? useHeaderRow, bool includeFormatting, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Parsing xlsx stream as DataTable (formatting={IncludeFormatting})", includeFormatting);
        var effectiveUseHeaderRow = useHeaderRow ?? Config.UseHeaderRow;
        ct.ThrowIfCancellationRequested();
        try {
            return await Task.Run(
                    () => {
                        ct.ThrowIfCancellationRequested();
                        using var workbook = new XLWorkbook(xlsxStream);
                        ct.ThrowIfCancellationRequested();
                        return ConvertWorkbookToDataTable(workbook, effectiveUseHeaderRow, includeFormatting);
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

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTableWithFormattingAsync(System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableWithFormattingAsync(
        string xlsxFilePath, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDataTableWithFormattingAsync(inputStream, useHeaderRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableAsync(System.Byte[],System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDataTableAsync(ms, useHeaderRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableWithFormattingAsync(System.Byte[],System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableWithFormattingAsync(
        byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDataTableWithFormattingAsync(ms, useHeaderRow, ct).ConfigureAwait(false);
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

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableWithFormattingAsync(System.IO.Stream,System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableWithFormattingAsync(
        Stream xlsxStream, string sheetName, bool? useHeaderRow = null, CancellationToken ct = default)
        => GuardedRunAsync(() => ParseXlsxStreamAsDataTableWithFormatting(xlsxStream, sheetName, useHeaderRow), ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableAsync(System.IO.Stream,System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, int sheetIndex, bool? useHeaderRow = null, CancellationToken ct = default)
        => GuardedRunAsync(() => ParseXlsxStreamAsDataTable(xlsxStream, sheetIndex, useHeaderRow), ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableWithFormattingAsync(System.IO.Stream,System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableWithFormattingAsync(
        Stream xlsxStream, int sheetIndex, bool? useHeaderRow = null, CancellationToken ct = default)
        => GuardedRunAsync(() => ParseXlsxStreamAsDataTableWithFormatting(xlsxStream, sheetIndex, useHeaderRow), ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTableWithFormattingAsync(System.String,System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableWithFormattingAsync(
        string xlsxFilePath, string sheetName, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDataTableWithFormattingAsync(inputStream, sheetName, useHeaderRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTableWithFormattingAsync(System.String,System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableWithFormattingAsync(
        string xlsxFilePath, int sheetIndex, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDataTableWithFormattingAsync(inputStream, sheetIndex, useHeaderRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableAsync(System.Byte[],System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, string sheetName, bool? useHeaderRow = null, CancellationToken ct =
 default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDataTableAsync(ms, sheetName, useHeaderRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableWithFormattingAsync(System.Byte[],System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableWithFormattingAsync(
        byte[] xlsxBytes, string sheetName, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDataTableWithFormattingAsync(ms, sheetName, useHeaderRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableAsync(System.Byte[],System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, int sheetIndex, bool? useHeaderRow = null, CancellationToken ct =
 default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDataTableAsync(ms, sheetIndex, useHeaderRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableWithFormattingAsync(System.Byte[],System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableWithFormattingAsync(
        byte[] xlsxBytes, int sheetIndex, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDataTableWithFormattingAsync(ms, sheetIndex, useHeaderRow, ct).ConfigureAwait(false);
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

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsAllSheetsWithFormattingAsync(System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxFileAsAllSheetsWithFormattingAsync(
        string xlsxFilePath, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsAllSheetsWithFormattingAsync(inputStream, useHeaderRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsAllSheetsAsync(System.IO.Stream,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxStreamAsAllSheetsAsync(Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct =
 default)
        => GuardedRunAsync(() => ParseXlsxStreamAsAllSheets(xlsxStream, useHeaderRow), ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsAllSheetsWithFormattingAsync(System.IO.Stream,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxStreamAsAllSheetsWithFormattingAsync(
        Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct = default)
        => GuardedRunAsync(() => ParseXlsxStreamAsAllSheetsWithFormatting(xlsxStream, useHeaderRow), ct);

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

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsAllSheetsWithFormattingAsync(System.Byte[],System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxBytesAsAllSheetsWithFormattingAsync(
        byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsAllSheetsWithFormattingAsync(ms, useHeaderRow, ct).ConfigureAwait(false);
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