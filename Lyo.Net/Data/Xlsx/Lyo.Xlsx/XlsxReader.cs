using System.Data;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        _logger.LogDebug("Parsing {ParsingXlsxPath} as DataTable", xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDataTable(inputStream, useHeaderRow, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTableWithFormatting(System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTableWithFormatting(string xlsxFilePath, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        _logger.LogDebug("Parsing {ParsingXlsxPath} as DataTable with formatting", xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDataTableWithFormatting(inputStream, useHeaderRow, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTable(System.IO.Stream,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, bool? useHeaderRow = null, bool useFooterRow = false)
        => ParseXlsxStreamAsDataTableCore(xlsxStream, useHeaderRow, false, useFooterRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableWithFormatting(System.IO.Stream,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTableWithFormatting(Stream xlsxStream, bool? useHeaderRow = null, bool useFooterRow = false)
        => ParseXlsxStreamAsDataTableCore(xlsxStream, useHeaderRow, true, useFooterRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTable(System.Byte[],System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDataTable(ms, useHeaderRow, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableWithFormatting(System.Byte[],System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTableWithFormatting(byte[] xlsxBytes, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDataTableWithFormatting(ms, useHeaderRow, useFooterRow);
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
    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, string sheetName, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDataTable(inputStream, sheetName, useHeaderRow, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTable(System.String,System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, int sheetIndex, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDataTable(inputStream, sheetIndex, useHeaderRow, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTable(System.IO.Stream,System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, string sheetName, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sheetName);
        return ParseXlsxStreamAsDataTableCore(
            xlsxStream, workbook => workbook.Worksheets.TryGetWorksheet(sheetName, out var ws) ? ws : null, $"Worksheet '{sheetName}' was not found in the workbook.", useHeaderRow,
            false, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableWithFormatting(System.IO.Stream,System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTableWithFormatting(Stream xlsxStream, string sheetName, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sheetName);
        return ParseXlsxStreamAsDataTableCore(
            xlsxStream, workbook => workbook.Worksheets.TryGetWorksheet(sheetName, out var ws) ? ws : null, $"Worksheet '{sheetName}' was not found in the workbook.", useHeaderRow,
            true, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTable(System.IO.Stream,System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, int sheetIndex, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNegative(sheetIndex);
        return ParseXlsxStreamAsDataTableCore(
            xlsxStream, workbook => workbook.Worksheets.Skip(sheetIndex).FirstOrDefault(), $"Worksheet index {sheetIndex} is out of range.", useHeaderRow, false, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableWithFormatting(System.IO.Stream,System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTableWithFormatting(Stream xlsxStream, int sheetIndex, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNegative(sheetIndex);
        return ParseXlsxStreamAsDataTableCore(
            xlsxStream, workbook => workbook.Worksheets.Skip(sheetIndex).FirstOrDefault(), $"Worksheet index {sheetIndex} is out of range.", useHeaderRow, true, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTableWithFormatting(System.String,System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTableWithFormatting(string xlsxFilePath, string sheetName, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDataTableWithFormatting(inputStream, sheetName, useHeaderRow, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTableWithFormatting(System.String,System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTableWithFormatting(string xlsxFilePath, int sheetIndex, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsDataTableWithFormatting(inputStream, sheetIndex, useHeaderRow, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTable(System.Byte[],System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, string sheetName, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDataTable(ms, sheetName, useHeaderRow, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableWithFormatting(System.Byte[],System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTableWithFormatting(byte[] xlsxBytes, string sheetName, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDataTableWithFormatting(ms, sheetName, useHeaderRow, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTable(System.Byte[],System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, int sheetIndex, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDataTable(ms, sheetIndex, useHeaderRow, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableWithFormatting(System.Byte[],System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTableWithFormatting(byte[] xlsxBytes, int sheetIndex, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsDataTableWithFormatting(ms, sheetIndex, useHeaderRow, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsAllSheets(System.String,System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxFileAsAllSheets(string xlsxFilePath, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsAllSheets(inputStream, useHeaderRow, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsAllSheetsWithFormatting(System.String,System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxFileAsAllSheetsWithFormatting(string xlsxFilePath, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        using var inputStream = File.OpenRead(xlsxFilePath);
        return ParseXlsxStreamAsAllSheetsWithFormatting(inputStream, useHeaderRow, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsAllSheets(System.IO.Stream,System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxStreamAsAllSheets(Stream xlsxStream, bool? useHeaderRow = null, bool useFooterRow = false)
        => ParseXlsxStreamAsAllSheetsCore(xlsxStream, useHeaderRow, false, useFooterRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsAllSheetsWithFormatting(System.IO.Stream,System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxStreamAsAllSheetsWithFormatting(Stream xlsxStream, bool? useHeaderRow = null, bool useFooterRow = false)
        => ParseXlsxStreamAsAllSheetsCore(xlsxStream, useHeaderRow, true, useFooterRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsAllSheets(System.Byte[],System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxBytesAsAllSheets(byte[] xlsxBytes, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsAllSheets(ms, useHeaderRow, useFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsAllSheetsWithFormatting(System.Byte[],System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxBytesAsAllSheetsWithFormatting(byte[] xlsxBytes, bool? useHeaderRow = null, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return ParseXlsxStreamAsAllSheetsWithFormatting(ms, useHeaderRow, useFooterRow);
    }

    private Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTableCore(Stream xlsxStream, bool? useHeaderRow, bool includeFormatting, bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Parsing xlsx stream as DataTable (formatting={IncludeFormatting})", includeFormatting);
        try {
            using var workbook = new XLWorkbook(xlsxStream);
            return ConvertWorkbookToDataTable(workbook, useHeaderRow ?? Config.UseHeaderRow, includeFormatting, useFooterRow);
        }
        catch (Exception ex) {
            return Result<DataTable.Models.DataTable>.Failure(ex);
        }
    }

    private IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxStreamAsAllSheetsCore(
        Stream xlsxStream,
        bool? useHeaderRow,
        bool includeFormatting,
        bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Parsing all xlsx sheets as DataTables (formatting={IncludeFormatting})", includeFormatting);
        var effectiveUseHeaderRow = useHeaderRow ?? Config.UseHeaderRow;
        using var workbook = new XLWorkbook(xlsxStream);
        var result = new Dictionary<string, DataTable.Models.DataTable>();
        foreach (var ws in workbook.Worksheets)
            result[ws.Name] = ConvertWorksheetToDataTable(ws, effectiveUseHeaderRow, includeFormatting, useFooterRow);

        return result;
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
        bool includeFormatting,
        bool useFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Parsing xlsx stream sheet as DataTable (formatting={IncludeFormatting})", includeFormatting);
        try {
            using var workbook = new XLWorkbook(xlsxStream);
            var ws = selectWorksheet(workbook);
            if (ws == null)
                return Result<DataTable.Models.DataTable>.Failure(new ArgumentException(missingSheetMessage));

            return Result<DataTable.Models.DataTable>.Success(ConvertWorksheetToDataTable(ws, useHeaderRow ?? Config.UseHeaderRow, includeFormatting, useFooterRow));
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

    private Result<DataTable.Models.DataTable> ConvertWorkbookToDataTable(XLWorkbook workbook, bool useHeaderRow, bool includeFormatting, bool useFooterRow = false)
    {
        var ws = workbook.Worksheets.FirstOrDefault();
        if (ws == null) {
            var empty = new DataTable.Models.DataTable();
            return Result<DataTable.Models.DataTable>.Success(empty);
        }

        return Result<DataTable.Models.DataTable>.Success(ConvertWorksheetToDataTable(ws, useHeaderRow, includeFormatting, useFooterRow));
    }

    private DataTable.Models.DataTable ConvertWorksheetToDataTable(IXLWorksheet ws, bool useHeaderRow, bool includeFormatting, bool useFooterRow = false)
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
        var endDataRow = lastRow;
        var footerSheetRow = (int?)null;
        if (useFooterRow && lastRow >= startDataRow) {
            footerSheetRow = lastRow;
            endDataRow = lastRow - 1;
        }

        for (var rowNum = startDataRow; rowNum <= endDataRow; rowNum++) {
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

        if (footerSheetRow is { } footerRowNum) {
            for (var col = 1; col <= lastCol; col++) {
                var cell = ws.Cell(footerRowNum, col);
                var displayValue = interner.Intern(GetCellDisplayValue(cell));
                var colSpan = 1;
                var rowSpan = 1;
                if (mergeMap.TryGetValue((footerRowNum, col), out var span)) {
                    colSpan = span.ColSpan;
                    rowSpan = span.RowSpan;
                }

                var footerCell = DataTableCell.FromValue(displayValue, colSpan, rowSpan);
                if (includeFormatting) {
                    var format = interner.Intern(ExtractMeaningfulFormat(cell));
                    dt.SetFooter(col - 1, footerCell, format);
                }
                else
                    dt.SetFooter(col - 1, footerCell);
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

    /// <summary>Extracts meaningful (non-default) formatting only. Theme colors and workbook defaults are ignored so unstyled sheets stay out of the format map.</summary>
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
        var borderColor = hasBorder ? NormalizeColor(TryGetColorHex(style.Border.TopBorderColor), true) : null;
        // Skip default font size/name and workbook default black/white colors — ClosedXML reports them on every cell.
        var format = new DataTableCellFormat(
            null, null, fontBold, fontItalic, fontUnderline, fontStrikethrough, NormalizeColor(fontColor, true), NormalizeColor(bgColor, defaultWhite: true), hAlign, vAlign,
            numberFormat, textRotation, wrapText, borderTop, borderBottom, borderLeft, borderRight, borderColor);

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
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(
        string xlsxFilePath,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        _logger.LogDebug("Parsing {ParsingXlsxPath} as DataTable", xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDataTableAsync(inputStream, useHeaderRow, useFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableAsync(System.IO.Stream,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(
        Stream xlsxStream,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
        => ParseXlsxStreamAsDataTableAsyncCore(xlsxStream, useHeaderRow, false, useFooterRow, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableWithFormattingAsync(System.IO.Stream,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableWithFormattingAsync(
        Stream xlsxStream,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
        => ParseXlsxStreamAsDataTableAsyncCore(xlsxStream, useHeaderRow, true, useFooterRow, ct);

    private async Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsyncCore(
        Stream xlsxStream,
        bool? useHeaderRow,
        bool includeFormatting,
        bool useFooterRow,
        CancellationToken ct)
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
                        return ConvertWorkbookToDataTable(workbook, effectiveUseHeaderRow, includeFormatting, useFooterRow);
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
        string xlsxFilePath,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDataTableWithFormattingAsync(inputStream, useHeaderRow, useFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableAsync(System.Byte[],System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(
        byte[] xlsxBytes,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDataTableAsync(ms, useHeaderRow, useFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableWithFormattingAsync(System.Byte[],System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableWithFormattingAsync(
        byte[] xlsxBytes,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDataTableWithFormattingAsync(ms, useHeaderRow, useFooterRow, ct).ConfigureAwait(false);
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
    public Task<IReadOnlyList<string>> ListSheetNamesAsync(Stream xlsxStream, CancellationToken ct = default) => GuardedRunAsync(() => ListSheetNames(xlsxStream), ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ListSheetNamesAsync(System.Byte[],System.Threading.CancellationToken)' />
    public async Task<IReadOnlyList<string>> ListSheetNamesAsync(byte[] xlsxBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ListSheetNamesAsync(ms, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDictionaryAsync(System.String,System.String,System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(
        string xlsxFilePath,
        string sheetName,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDictionaryAsync(inputStream, sheetName, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDictionaryAsync(System.String,System.Int32,System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(
        string xlsxFilePath,
        int sheetIndex,
        CancellationToken ct = default)
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
    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxBytesAsDictionaryAsync(
        byte[] xlsxBytes,
        string sheetName,
        CancellationToken ct = default)
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
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDataTableAsync(inputStream, sheetName, useHeaderRow, useFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTableAsync(System.String,System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(
        string xlsxFilePath,
        int sheetIndex,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDataTableAsync(inputStream, sheetIndex, useHeaderRow, useFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableAsync(System.IO.Stream,System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(
        Stream xlsxStream,
        string sheetName,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
        => GuardedRunAsync(() => ParseXlsxStreamAsDataTable(xlsxStream, sheetName, useHeaderRow, useFooterRow), ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableWithFormattingAsync(System.IO.Stream,System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableWithFormattingAsync(
        Stream xlsxStream,
        string sheetName,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
        => GuardedRunAsync(() => ParseXlsxStreamAsDataTableWithFormatting(xlsxStream, sheetName, useHeaderRow, useFooterRow), ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableAsync(System.IO.Stream,System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(
        Stream xlsxStream,
        int sheetIndex,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
        => GuardedRunAsync(() => ParseXlsxStreamAsDataTable(xlsxStream, sheetIndex, useHeaderRow, useFooterRow), ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsDataTableWithFormattingAsync(System.IO.Stream,System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableWithFormattingAsync(
        Stream xlsxStream,
        int sheetIndex,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
        => GuardedRunAsync(() => ParseXlsxStreamAsDataTableWithFormatting(xlsxStream, sheetIndex, useHeaderRow, useFooterRow), ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTableWithFormattingAsync(System.String,System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableWithFormattingAsync(
        string xlsxFilePath,
        string sheetName,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDataTableWithFormattingAsync(inputStream, sheetName, useHeaderRow, useFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsDataTableWithFormattingAsync(System.String,System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableWithFormattingAsync(
        string xlsxFilePath,
        int sheetIndex,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsDataTableWithFormattingAsync(inputStream, sheetIndex, useHeaderRow, useFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableAsync(System.Byte[],System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(
        byte[] xlsxBytes,
        string sheetName,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDataTableAsync(ms, sheetName, useHeaderRow, useFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableWithFormattingAsync(System.Byte[],System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableWithFormattingAsync(
        byte[] xlsxBytes,
        string sheetName,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDataTableWithFormattingAsync(ms, sheetName, useHeaderRow, useFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableAsync(System.Byte[],System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(
        byte[] xlsxBytes,
        int sheetIndex,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDataTableAsync(ms, sheetIndex, useHeaderRow, useFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsDataTableWithFormattingAsync(System.Byte[],System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableWithFormattingAsync(
        byte[] xlsxBytes,
        int sheetIndex,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsDataTableWithFormattingAsync(ms, sheetIndex, useHeaderRow, useFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsAllSheetsAsync(System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxFileAsAllSheetsAsync(
        string xlsxFilePath,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsAllSheetsAsync(inputStream, useHeaderRow, useFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileAsAllSheetsWithFormattingAsync(System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxFileAsAllSheetsWithFormattingAsync(
        string xlsxFilePath,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        return await ParseXlsxStreamAsAllSheetsWithFormattingAsync(inputStream, useHeaderRow, useFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsAllSheetsAsync(System.IO.Stream,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxStreamAsAllSheetsAsync(
        Stream xlsxStream,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
        => GuardedRunAsync(() => ParseXlsxStreamAsAllSheets(xlsxStream, useHeaderRow, useFooterRow), ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamAsAllSheetsWithFormattingAsync(System.IO.Stream,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxStreamAsAllSheetsWithFormattingAsync(
        Stream xlsxStream,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
        => GuardedRunAsync(() => ParseXlsxStreamAsAllSheetsWithFormatting(xlsxStream, useHeaderRow, useFooterRow), ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsAllSheetsAsync(System.Byte[],System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxBytesAsAllSheetsAsync(
        byte[] xlsxBytes,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsAllSheetsAsync(ms, useHeaderRow, useFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxBytesAsAllSheetsWithFormattingAsync(System.Byte[],System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxBytesAsAllSheetsWithFormattingAsync(
        byte[] xlsxBytes,
        bool? useHeaderRow = null,
        bool useFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var ms = new MemoryStream(xlsxBytes);
        return await ParseXlsxStreamAsAllSheetsWithFormattingAsync(ms, useHeaderRow, useFooterRow, ct).ConfigureAwait(false);
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

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileRowsStreamingAsync(System.String,System.Threading.CancellationToken)' />
    public async IAsyncEnumerable<IReadOnlyList<string>> ParseXlsxFileRowsStreamingAsync(string xlsxFilePath, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        _logger.LogDebug("Streaming rows from {ParsingXlsxPath}", xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        await foreach (var row in ParseXlsxStreamRowsStreamingAsync(inputStream, ct).ConfigureAwait(false))
            yield return row;
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamRowsStreamingAsync(System.IO.Stream,System.Threading.CancellationToken)' />
    public async IAsyncEnumerable<IReadOnlyList<string>> ParseXlsxStreamRowsStreamingAsync(Stream xlsxStream, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Streaming xlsx rows from stream");
        using var reader = ExcelReaderFactory.CreateReader(xlsxStream);
        await foreach (var row in EnumerateStringRowsAsync(reader, ct).ConfigureAwait(false))
            yield return row;
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileRowsStreamingAsync(System.String,System.String,System.Threading.CancellationToken)' />
    public async IAsyncEnumerable<IReadOnlyList<string>> ParseXlsxFileRowsStreamingAsync(
        string xlsxFilePath,
        string sheetName,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sheetName);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        await foreach (var row in ParseXlsxStreamRowsStreamingAsync(inputStream, sheetName, ct).ConfigureAwait(false))
            yield return row;
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileRowsStreamingAsync(System.String,System.Int32,System.Threading.CancellationToken)' />
    public async IAsyncEnumerable<IReadOnlyList<string>> ParseXlsxFileRowsStreamingAsync(
        string xlsxFilePath,
        int sheetIndex,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        ArgumentHelpers.ThrowIfNegative(sheetIndex);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        await foreach (var row in ParseXlsxStreamRowsStreamingAsync(inputStream, sheetIndex, ct).ConfigureAwait(false))
            yield return row;
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamRowsStreamingAsync(System.IO.Stream,System.String,System.Threading.CancellationToken)' />
    public async IAsyncEnumerable<IReadOnlyList<string>> ParseXlsxStreamRowsStreamingAsync(
        Stream xlsxStream,
        string sheetName,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sheetName);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        using var reader = ExcelReaderFactory.CreateReader(xlsxStream);
        SelectSheetByName(reader, sheetName);
        await foreach (var row in EnumerateStringRowsAsync(reader, ct).ConfigureAwait(false))
            yield return row;
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamRowsStreamingAsync(System.IO.Stream,System.Int32,System.Threading.CancellationToken)' />
    public async IAsyncEnumerable<IReadOnlyList<string>> ParseXlsxStreamRowsStreamingAsync(
        Stream xlsxStream,
        int sheetIndex,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        ArgumentHelpers.ThrowIfNegative(sheetIndex);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        using var reader = ExcelReaderFactory.CreateReader(xlsxStream);
        SelectSheetByIndex(reader, sheetIndex);
        await foreach (var row in EnumerateStringRowsAsync(reader, ct).ConfigureAwait(false))
            yield return row;
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxFileStreamingAsync``1(System.String,System.Threading.CancellationToken)' />
    public async IAsyncEnumerable<T> ParseXlsxFileStreamingAsync<T>(string xlsxFilePath, [EnumeratorCancellation] CancellationToken ct = default)
        where T : new()
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxFilePath);
        _logger.LogDebug("Streaming typed rows from {ParsingXlsxPath}", xlsxFilePath);
        await using var inputStream = File.OpenRead(xlsxFilePath);
        await foreach (var row in ParseXlsxStreamStreamingAsync<T>(inputStream, ct).ConfigureAwait(false))
            yield return row;
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxReader.ParseXlsxStreamStreamingAsync``1(System.IO.Stream,System.Threading.CancellationToken)' />
    public async IAsyncEnumerable<T> ParseXlsxStreamStreamingAsync<T>(Stream xlsxStream, [EnumeratorCancellation] CancellationToken ct = default)
        where T : new()
    {
        ArgumentHelpers.ThrowIfNull(xlsxStream);
        OperationHelpers.ThrowIfNotReadable(xlsxStream, $"Stream '{nameof(xlsxStream)}' must be readable.");
        _logger.LogDebug("Streaming typed {RowType} rows from xlsx stream", typeof(T).FullName);
        using var reader = ExcelReaderFactory.CreateReader(xlsxStream);
        var useHeaderRow = Config.UseHeaderRow;
        var writable = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanWrite).ToArray();
        PropertyInfo?[]? columnBindings = null;
        var isFirst = true;
        while (reader.Read()) {
            ct.ThrowIfCancellationRequested();
            if (isFirst && useHeaderRow) {
                columnBindings = BindHeadersToProperties(reader, writable);
                isFirst = false;
                continue;
            }

            if (isFirst) {
                columnBindings = BindPositionalProperties(writable, reader.FieldCount);
                isFirst = false;
            }

            yield return BindTypedRow<T>(reader, columnBindings!);

            await Task.Yield();
        }

        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<IReadOnlyList<string>> EnumerateStringRowsAsync(IExcelDataReader reader, [EnumeratorCancellation] CancellationToken ct)
    {
        while (reader.Read()) {
            ct.ThrowIfCancellationRequested();
            yield return ReadCurrentRow(reader);

            await Task.Yield();
        }

        await Task.CompletedTask;
    }

    private static IReadOnlyList<string> ReadCurrentRow(IExcelDataReader reader)
    {
        var cells = new string[reader.FieldCount];
        for (var i = 0; i < reader.FieldCount; i++)
            cells[i] = reader.GetValue(i)?.ToString() ?? string.Empty;

        return cells;
    }

    private static void SelectSheetByName(IExcelDataReader reader, string sheetName)
    {
        do {
            if (string.Equals(reader.Name, sheetName, StringComparison.OrdinalIgnoreCase))
                return;
        } while (reader.NextResult());

        throw new ArgumentException($"Worksheet '{sheetName}' was not found in the workbook.");
    }

    private static void SelectSheetByIndex(IExcelDataReader reader, int sheetIndex)
    {
        var index = 0;
        while (index < sheetIndex) {
            if (!reader.NextResult())
                throw new ArgumentException($"Worksheet index {sheetIndex} is out of range.");

            index++;
        }
    }

    private static PropertyInfo?[] BindHeadersToProperties(IExcelDataReader reader, PropertyInfo[] writable)
    {
        var byName = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in writable)
            byName.TryAdd(property.Name, property);

        var bindings = new PropertyInfo?[reader.FieldCount];
        for (var i = 0; i < reader.FieldCount; i++) {
            var header = reader.GetValue(i)?.ToString();
            if (!string.IsNullOrWhiteSpace(header) && byName.TryGetValue(header, out var property))
                bindings[i] = property;
        }

        return bindings;
    }

    private static PropertyInfo?[] BindPositionalProperties(PropertyInfo[] writable, int fieldCount)
    {
        var bindings = new PropertyInfo?[fieldCount];
        var count = Math.Min(writable.Length, fieldCount);
        for (var i = 0; i < count; i++)
            bindings[i] = writable[i];

        return bindings;
    }

    private static T BindTypedRow<T>(IExcelDataReader reader, PropertyInfo?[] columnBindings)
        where T : new()
    {
        var item = new T();
        var count = Math.Min(columnBindings.Length, reader.FieldCount);
        for (var i = 0; i < count; i++) {
            var property = columnBindings[i];
            if (property == null)
                continue;

            SetPropertyValue(property, item, reader.GetValue(i));
        }

        return item;
    }

    private static void SetPropertyValue(PropertyInfo property, object target, object? raw)
    {
        if (raw is null or DBNull) {
            if (!property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) != null)
                property.SetValue(target, null);

            return;
        }

        var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        try {
            if (targetType == typeof(string)) {
                property.SetValue(target, raw.ToString());
                return;
            }

            if (targetType.IsEnum) {
                property.SetValue(target, Enum.Parse(targetType, raw.ToString() ?? string.Empty, true));
                return;
            }

            if (raw.GetType() == targetType) {
                property.SetValue(target, raw);
                return;
            }

            property.SetValue(target, Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture));
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException) {
            var text = raw.ToString();
            if (string.IsNullOrWhiteSpace(text))
                return;

            property.SetValue(target, Convert.ChangeType(text, targetType, CultureInfo.InvariantCulture));
        }
    }
#endif
}