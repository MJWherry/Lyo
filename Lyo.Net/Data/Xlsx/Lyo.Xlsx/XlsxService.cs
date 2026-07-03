using System.Data;
using System.Reflection;
using System.Text;
using ExcelDataReader;
using Lyo.DataTable.Models;
using Lyo.Exceptions;
using Lyo.Result;
using Lyo.Xlsx.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Xlsx;

/// <summary>Coordinates XLSX export (streaming OpenXML writer) and import (ExcelDataReader / ClosedXML), CSV conversion, and batch HTML helpers.</summary>
/// <remarks>Thread-safe for concurrent calls; configuration can be replaced via <see cref="SetExcelDataTableConfiguration" />.</remarks>
public class XlsxService : IXlsxService
{
    private readonly XlsxWriter _writer;
    private readonly XlsxReader _reader;
    private readonly ILogger<XlsxService> _logger;

    private ExcelDataTableConfiguration _excelDataTableConfiguration;

    /// <summary>Creates a service with optional logging and ExcelDataReader configuration (defaults to header row).</summary>
    public XlsxService(ILogger<XlsxService>? logger = null, ExcelDataTableConfiguration? excelDataTableConfiguration = null)
    {
        _logger = logger ?? NullLoggerFactory.Instance.CreateLogger<XlsxService>();
        _excelDataTableConfiguration = excelDataTableConfiguration ?? new ExcelDataTableConfiguration { UseHeaderRow = true };
        _writer = new(_logger);
        _reader = new(() => _excelDataTableConfiguration, _logger);
    }

    /// <inheritdoc cref='P:Lyo.Xlsx.Models.IXlsxService.Writer' />
    public IXlsxWriter Writer => _writer;

    /// <inheritdoc cref='P:Lyo.Xlsx.Models.IXlsxService.Reader' />
    public IXlsxReader Reader => _reader;

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsx``1(System.Collections.Generic.IEnumerable{``0},System.String,System.String)' />
    public void ExportToXlsx<T>(IEnumerable<T> data, string xlsxFilePath, string? worksheetName = null) => _writer.ExportToXlsx(data, xlsxFilePath, worksheetName);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsx``1(System.Collections.Generic.IEnumerable{``0},System.IO.Stream,System.String)' />
    public void ExportToXlsx<T>(IEnumerable<T> data, Stream xlsxStream, string? worksheetName = null) => _writer.ExportToXlsx(data, xlsxStream, worksheetName);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxBytes``1(System.Collections.Generic.IEnumerable{``0},System.String)' />
    public byte[] ExportToXlsxBytes<T>(IEnumerable<T> data, string? worksheetName = null) => _writer.ExportToXlsxBytes(data, worksheetName);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsx``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.String,System.String)' />
    public void ExportToXlsx<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string xlsxFilePath, string? worksheetName = null)
        => _writer.ExportToXlsx(data, selectedProperties, xlsxFilePath, worksheetName);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsx``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.IO.Stream,System.String)' />
    public void ExportToXlsx<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream xlsxStream, string? worksheetName = null)
        => _writer.ExportToXlsx(data, selectedProperties, xlsxStream, worksheetName);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxBytes``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.String)' />
    public byte[] ExportToXlsxBytes<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string? worksheetName = null)
        => _writer.ExportToXlsxBytes(data, selectedProperties, worksheetName);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsx``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}},System.String)' />
    public void ExportToXlsx<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, string xlsxFilePath) => _writer.ExportToXlsx(dataSets, xlsxFilePath);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsx``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}},System.IO.Stream)' />
    public void ExportToXlsx<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, Stream xlsxStream) => _writer.ExportToXlsx(dataSets, xlsxStream);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxBytes``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}})' />
    public byte[] ExportToXlsxBytes<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets) => _writer.ExportToXlsxBytes(dataSets);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxFileAsDictionary(System.String)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath) => _reader.ParseXlsxFileAsDictionary(xlsxFilePath);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxStreamAsDictionary(System.IO.Stream)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream) => _reader.ParseXlsxStreamAsDictionary(xlsxStream);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxFileAsDataTable(System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, bool? useHeaderRow = null)
        => _reader.ParseXlsxFileAsDataTable(xlsxFilePath, useHeaderRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxStreamAsDataTable(System.IO.Stream,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, bool? useHeaderRow = null)
        => _reader.ParseXlsxStreamAsDataTable(xlsxStream, useHeaderRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxBytesAsDataTable(System.Byte[],System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, bool? useHeaderRow = null)
        => _reader.ParseXlsxBytesAsDataTable(xlsxBytes, useHeaderRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToHtmlTable(System.Byte[],System.Nullable{System.Boolean})' />
    public string ExportToHtmlTable(byte[] xlsxBytes, bool? useHeaderRow = null)
        => DataTableToHtml.ToHtmlDocument(ParseXlsxBytesAsDataTable(xlsxBytes, useHeaderRow).ValueOrThrow());

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxBytesAsDictionary(System.Byte[])' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes) => _reader.ParseXlsxBytesAsDictionary(xlsxBytes);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.BatchParseFilesAsDataTable(System.Collections.Generic.IEnumerable{System.String},System.Nullable{System.Boolean})' />
    public IReadOnlyList<Result<DataTable.Models.DataTable>> BatchParseFilesAsDataTable(IEnumerable<string> xlsxFilePaths, bool? useHeaderRow = null)
    {
        var paths = xlsxFilePaths.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(paths, nameof(xlsxFilePaths));
        var results = new List<Result<DataTable.Models.DataTable>>();
        foreach (var path in paths)
            results.Add(_reader.ParseXlsxFileAsDataTable(path, useHeaderRow));

        return results;
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.CreateDocumentWriter(System.IO.Stream)' />
    public IXlsxDocumentWriter CreateDocumentWriter(Stream xlsxStream) => _writer.CreateDocumentWriter(xlsxStream);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.CreateDocumentWriter(System.String)' />
    public IXlsxDocumentWriter CreateDocumentWriter(string xlsxFilePath) => _writer.CreateDocumentWriter(xlsxFilePath);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ListSheetNames(System.String)' />
    public IReadOnlyList<string> ListSheetNames(string xlsxFilePath) => _reader.ListSheetNames(xlsxFilePath);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ListSheetNames(System.IO.Stream)' />
    public IReadOnlyList<string> ListSheetNames(Stream xlsxStream) => _reader.ListSheetNames(xlsxStream);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ListSheetNames(System.Byte[])' />
    public IReadOnlyList<string> ListSheetNames(byte[] xlsxBytes) => _reader.ListSheetNames(xlsxBytes);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxFileAsDictionary(System.String,System.String)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath, string sheetName)
        => _reader.ParseXlsxFileAsDictionary(xlsxFilePath, sheetName);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxFileAsDictionary(System.String,System.Int32)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath, int sheetIndex)
        => _reader.ParseXlsxFileAsDictionary(xlsxFilePath, sheetIndex);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxStreamAsDictionary(System.IO.Stream,System.String)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream, string sheetName)
        => _reader.ParseXlsxStreamAsDictionary(xlsxStream, sheetName);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxStreamAsDictionary(System.IO.Stream,System.Int32)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream, int sheetIndex)
        => _reader.ParseXlsxStreamAsDictionary(xlsxStream, sheetIndex);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxBytesAsDictionary(System.Byte[],System.String)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes, string sheetName)
        => _reader.ParseXlsxBytesAsDictionary(xlsxBytes, sheetName);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxBytesAsDictionary(System.Byte[],System.Int32)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes, int sheetIndex)
        => _reader.ParseXlsxBytesAsDictionary(xlsxBytes, sheetIndex);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxFileAsDataTable(System.String,System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, string sheetName, bool? useHeaderRow = null)
        => _reader.ParseXlsxFileAsDataTable(xlsxFilePath, sheetName, useHeaderRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxFileAsDataTable(System.String,System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, int sheetIndex, bool? useHeaderRow = null)
        => _reader.ParseXlsxFileAsDataTable(xlsxFilePath, sheetIndex, useHeaderRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxStreamAsDataTable(System.IO.Stream,System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, string sheetName, bool? useHeaderRow = null)
        => _reader.ParseXlsxStreamAsDataTable(xlsxStream, sheetName, useHeaderRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxStreamAsDataTable(System.IO.Stream,System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, int sheetIndex, bool? useHeaderRow = null)
        => _reader.ParseXlsxStreamAsDataTable(xlsxStream, sheetIndex, useHeaderRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxBytesAsDataTable(System.Byte[],System.String,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, string sheetName, bool? useHeaderRow = null)
        => _reader.ParseXlsxBytesAsDataTable(xlsxBytes, sheetName, useHeaderRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxBytesAsDataTable(System.Byte[],System.Int32,System.Nullable{System.Boolean})' />
    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, int sheetIndex, bool? useHeaderRow = null)
        => _reader.ParseXlsxBytesAsDataTable(xlsxBytes, sheetIndex, useHeaderRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxFileAsAllSheets(System.String,System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxFileAsAllSheets(string xlsxFilePath, bool? useHeaderRow = null)
        => _reader.ParseXlsxFileAsAllSheets(xlsxFilePath, useHeaderRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxStreamAsAllSheets(System.IO.Stream,System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxStreamAsAllSheets(Stream xlsxStream, bool? useHeaderRow = null)
        => _reader.ParseXlsxStreamAsAllSheets(xlsxStream, useHeaderRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxBytesAsAllSheets(System.Byte[],System.Nullable{System.Boolean})' />
    public IReadOnlyDictionary<string, DataTable.Models.DataTable> ParseXlsxBytesAsAllSheets(byte[] xlsxBytes, bool? useHeaderRow = null)
        => _reader.ParseXlsxBytesAsAllSheets(xlsxBytes, useHeaderRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ConvertXlsxToCsv(System.String,System.String,System.Text.Encoding)' />
    public void ConvertXlsxToCsv(string xlsxPath, string outputCsvPath, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxPath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputCsvPath);
        _logger.LogDebug("Converting {ConvertingXlsxPath} to {ConvertedCsvPath}", xlsxPath, outputCsvPath);
        using var inputStream = File.Open(xlsxPath, FileMode.Open, FileAccess.Read);
        using var outputStream = File.Create(outputCsvPath);
        ConvertXlsxToCsv(inputStream, outputStream, encoding);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ConvertXlsxToCsv(System.IO.Stream,System.IO.Stream,System.Text.Encoding)' />
    public void ConvertXlsxToCsv(Stream inputStream, Stream outputStream, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(inputStream);
        ArgumentHelpers.ThrowIfNull(outputStream);
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        _logger.LogDebug("Converting xlsx stream to csv stream");
        using var reader = ExcelReaderFactory.CreateReader(inputStream);
        var dataSet = reader.AsDataSet(new() { ConfigureDataTable = _ => _excelDataTableConfiguration });
        OperationHelpers.ThrowIf(dataSet.Tables.Count == 0, "The XLSX file contains no worksheets.");
        var table = dataSet.Tables[0];
        using var writer = new StreamWriter(outputStream, encoding ?? Encoding.UTF8, 1024, true);
        WriteDataTableToCsv(table, writer);
        writer.Flush();
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ConvertXlsxToCsv(System.Byte[],System.String,System.Text.Encoding)' />
    public void ConvertXlsxToCsv(byte[] xlsxBytes, string outputCsvPath, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputCsvPath);
        using var inputStream = new MemoryStream(xlsxBytes);
        using var outputStream = File.Create(outputCsvPath);
        ConvertXlsxToCsv(inputStream, outputStream, encoding);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ConvertXlsxToCsv(System.Byte[],System.IO.Stream,System.Text.Encoding)' />
    public void ConvertXlsxToCsv(byte[] xlsxBytes, Stream outputStream, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        ArgumentHelpers.ThrowIfNull(outputStream);
        using var inputStream = new MemoryStream(xlsxBytes);
        ConvertXlsxToCsv(inputStream, outputStream, encoding);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ConvertXlsxToCsvBytes(System.Byte[],System.Text.Encoding)' />
    public byte[] ConvertXlsxToCsvBytes(byte[] xlsxBytes, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        using var inputStream = new MemoryStream(xlsxBytes);
        return ConvertXlsxToCsvBytes(inputStream, encoding);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ConvertXlsxToCsvBytes(System.IO.Stream,System.Text.Encoding)' />
    public byte[] ConvertXlsxToCsvBytes(Stream inputStream, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(inputStream);
        using var outputStream = new MemoryStream();
        ConvertXlsxToCsv(inputStream, outputStream, encoding);
        return outputStream.ToArray();
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxFromDictionary(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.String,System.Boolean)' />
    public void ExportToXlsxFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, string xlsxFilePath, bool useHeaderRow = true)
        => _writer.ExportToXlsxFromDictionary(data, xlsxFilePath, useHeaderRow);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxFromDictionary(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.IO.Stream,System.Boolean)' />
    public void ExportToXlsxFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, Stream xlsxStream, bool useHeaderRow = true)
        => _writer.ExportToXlsxFromDictionary(data, xlsxStream, useHeaderRow);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxBytesFromDictionary(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.Boolean)' />
    public byte[] ExportToXlsxBytesFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool useHeaderRow = true)
        => _writer.ExportToXlsxBytesFromDictionary(data, useHeaderRow);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxFromDataTable(Lyo.DataTable.Models.DataTable,System.String)' />
    public void ExportToXlsxFromDataTable(DataTable.Models.DataTable dataTable, string xlsxFilePath) => _writer.ExportToXlsxFromDataTable(dataTable, xlsxFilePath);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxFromDataTable(Lyo.DataTable.Models.DataTable,System.IO.Stream)' />
    public void ExportToXlsxFromDataTable(DataTable.Models.DataTable dataTable, Stream xlsxStream) => _writer.ExportToXlsxFromDataTable(dataTable, xlsxStream);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxBytesFromDataTable(Lyo.DataTable.Models.DataTable)' />
    public byte[] ExportToXlsxBytesFromDataTable(DataTable.Models.DataTable dataTable) => _writer.ExportToXlsxBytesFromDataTable(dataTable);

    /// <summary>Replaces the ExcelDataReader data-table configuration used for CSV conversion and parsing.</summary>
    public void SetExcelDataTableConfiguration(ExcelDataTableConfiguration excelDataTableConfiguration)
    {
        ArgumentHelpers.ThrowIfNull(excelDataTableConfiguration);
        _excelDataTableConfiguration = excelDataTableConfiguration;
    }

    private static void WriteDataTableToCsv(System.Data.DataTable table, StreamWriter writer)
    {
        var headers = table.Columns.Cast<DataColumn>().Select(col => EscapeCsv(col.ColumnName));
        writer.WriteLine(string.Join(",", headers));
        foreach (DataRow row in table.Rows) {
            var fields = row.ItemArray.Select(field => EscapeCsv(field?.ToString()));
            writer.WriteLine(string.Join(",", fields));
        }
    }

    private static string EscapeCsv(string? s)
    {
        if (s is null || string.IsNullOrEmpty(s))
            return "";

        if (s.Contains(',') || s.Contains('"') || s.Contains('\r') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";

        return s;
    }

#if !NETSTANDARD2_0
    // Export Async - delegate to Writer
    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.String,System.String,System.Threading.CancellationToken)' />
    public Task ExportToXlsxAsync<T>(IEnumerable<T> data, string xlsxFilePath, string? worksheetName = null, CancellationToken ct = default)
        => _writer.ExportToXlsxAsync(data, xlsxFilePath, worksheetName, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.IO.Stream,System.String,System.Threading.CancellationToken)' />
    public Task ExportToXlsxAsync<T>(IEnumerable<T> data, Stream xlsxStream, string? worksheetName = null, CancellationToken ct = default)
        => _writer.ExportToXlsxAsync(data, xlsxStream, worksheetName, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxBytesAsync``1(System.Collections.Generic.IEnumerable{``0},System.String,System.Threading.CancellationToken)' />
    public Task<byte[]> ExportToXlsxBytesAsync<T>(IEnumerable<T> data, string? worksheetName = null, CancellationToken ct = default)
        => _writer.ExportToXlsxBytesAsync(data, worksheetName, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.String,System.String,System.Threading.CancellationToken)' />
    public Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyList<PropertyInfo> selectedProperties,
        string xlsxFilePath,
        string? worksheetName = null,
        CancellationToken ct = default)
        => _writer.ExportToXlsxAsync(data, selectedProperties, xlsxFilePath, worksheetName, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.IO.Stream,System.String,System.Threading.CancellationToken)' />
    public Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyList<PropertyInfo> selectedProperties,
        Stream xlsxStream,
        string? worksheetName = null,
        CancellationToken ct = default)
        => _writer.ExportToXlsxAsync(data, selectedProperties, xlsxStream, worksheetName, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyDictionary{System.String,System.Reflection.PropertyInfo},System.IO.Stream,System.String,System.Threading.CancellationToken)' />
    public Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyDictionary<string, PropertyInfo> columns,
        Stream xlsxStream,
        string? worksheetName = null,
        CancellationToken ct = default)
        => _writer.ExportToXlsxAsync(data, columns, xlsxStream, worksheetName, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyDictionary{System.String,System.Func{``0,System.String}},System.IO.Stream,System.String,System.Threading.CancellationToken)' />
    public Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyDictionary<string, Func<T, string>> columnFormatters,
        Stream xlsxStream,
        string? worksheetName = null,
        CancellationToken ct = default)
        => _writer.ExportToXlsxAsync(data, columnFormatters, xlsxStream, worksheetName, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxBytesAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.String,System.Threading.CancellationToken)' />
    public Task<byte[]> ExportToXlsxBytesAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string? worksheetName = null, CancellationToken ct = default)
        => _writer.ExportToXlsxBytesAsync(data, selectedProperties, worksheetName, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxAsync``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}},System.String,System.Threading.CancellationToken)' />
    public Task ExportToXlsxAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, string xlsxFilePath, CancellationToken ct = default)
        => _writer.ExportToXlsxAsync(dataSets, xlsxFilePath, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxAsync``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}},System.IO.Stream,System.Threading.CancellationToken)' />
    public Task ExportToXlsxAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, Stream xlsxStream, CancellationToken ct = default)
        => _writer.ExportToXlsxAsync(dataSets, xlsxStream, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxBytesAsync``1(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IEnumerable{``0}},System.Threading.CancellationToken)' />
    public Task<byte[]> ExportToXlsxBytesAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, CancellationToken ct = default)
        => _writer.ExportToXlsxBytesAsync(dataSets, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxFromDictionaryAsync(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.String,System.Boolean,System.Threading.CancellationToken)' />
    public Task ExportToXlsxFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        string xlsxFilePath,
        bool useHeaderRow = true,
        CancellationToken ct = default)
        => _writer.ExportToXlsxFromDictionaryAsync(data, xlsxFilePath, useHeaderRow, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxFromDictionaryAsync(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.IO.Stream,System.Boolean,System.Threading.CancellationToken)' />
    public Task ExportToXlsxFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        Stream xlsxStream,
        bool useHeaderRow = true,
        CancellationToken ct = default)
        => _writer.ExportToXlsxFromDictionaryAsync(data, xlsxStream, useHeaderRow, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxBytesFromDictionaryAsync(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.Boolean,System.Threading.CancellationToken)' />
    public Task<byte[]> ExportToXlsxBytesFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        bool useHeaderRow = true,
        CancellationToken ct = default)
        => _writer.ExportToXlsxBytesFromDictionaryAsync(data, useHeaderRow, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxFromDataTableAsync(Lyo.DataTable.Models.DataTable,System.String,System.Threading.CancellationToken)' />
    public Task ExportToXlsxFromDataTableAsync(DataTable.Models.DataTable dataTable, string xlsxFilePath, CancellationToken ct = default)
        => _writer.ExportToXlsxFromDataTableAsync(dataTable, xlsxFilePath, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxFromDataTableAsync(Lyo.DataTable.Models.DataTable,System.IO.Stream,System.Threading.CancellationToken)' />
    public Task ExportToXlsxFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream xlsxStream, CancellationToken ct = default)
        => _writer.ExportToXlsxFromDataTableAsync(dataTable, xlsxStream, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToXlsxBytesFromDataTableAsync(Lyo.DataTable.Models.DataTable,System.Threading.CancellationToken)' />
    public Task<byte[]> ExportToXlsxBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
        => _writer.ExportToXlsxBytesFromDataTableAsync(dataTable, ct);

    // Import Async - delegate to Reader
    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxFileAsDictionaryAsync(System.String,System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(string xlsxFilePath, CancellationToken ct = default)
        => _reader.ParseXlsxFileAsDictionaryAsync(xlsxFilePath, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxStreamAsDictionaryAsync(System.IO.Stream,System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxStreamAsDictionaryAsync(Stream xlsxStream, CancellationToken ct = default)
        => _reader.ParseXlsxStreamAsDictionaryAsync(xlsxStream, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxFileAsDataTableAsync(System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(string xlsxFilePath, bool? useHeaderRow = null, CancellationToken ct = default)
        => _reader.ParseXlsxFileAsDataTableAsync(xlsxFilePath, useHeaderRow, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxStreamAsDataTableAsync(System.IO.Stream,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct = default)
        => _reader.ParseXlsxStreamAsDataTableAsync(xlsxStream, useHeaderRow, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxBytesAsDataTableAsync(System.Byte[],System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default)
        => _reader.ParseXlsxBytesAsDataTableAsync(xlsxBytes, useHeaderRow, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ExportToHtmlTableAsync(System.Byte[],System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<string> ExportToHtmlTableAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        var result = await _reader.ParseXlsxBytesAsDataTableAsync(xlsxBytes, useHeaderRow, ct).ConfigureAwait(false);
        return DataTableToHtml.ToHtmlDocument(result.ValueOrThrow());
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxBytesAsDictionaryAsync(System.Byte[],System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxBytesAsDictionaryAsync(byte[] xlsxBytes, CancellationToken ct = default)
        => _reader.ParseXlsxBytesAsDictionaryAsync(xlsxBytes, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ListSheetNamesAsync(System.String,System.Threading.CancellationToken)' />
    public Task<IReadOnlyList<string>> ListSheetNamesAsync(string xlsxFilePath, CancellationToken ct = default) => _reader.ListSheetNamesAsync(xlsxFilePath, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ListSheetNamesAsync(System.IO.Stream,System.Threading.CancellationToken)' />
    public Task<IReadOnlyList<string>> ListSheetNamesAsync(Stream xlsxStream, CancellationToken ct = default) => _reader.ListSheetNamesAsync(xlsxStream, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ListSheetNamesAsync(System.Byte[],System.Threading.CancellationToken)' />
    public Task<IReadOnlyList<string>> ListSheetNamesAsync(byte[] xlsxBytes, CancellationToken ct = default) => _reader.ListSheetNamesAsync(xlsxBytes, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxFileAsDictionaryAsync(System.String,System.String,System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(string xlsxFilePath, string sheetName, CancellationToken ct = default)
        => _reader.ParseXlsxFileAsDictionaryAsync(xlsxFilePath, sheetName, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxFileAsDictionaryAsync(System.String,System.Int32,System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(string xlsxFilePath, int sheetIndex, CancellationToken ct = default)
        => _reader.ParseXlsxFileAsDictionaryAsync(xlsxFilePath, sheetIndex, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxStreamAsDictionaryAsync(System.IO.Stream,System.String,System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxStreamAsDictionaryAsync(Stream xlsxStream, string sheetName, CancellationToken ct = default)
        => _reader.ParseXlsxStreamAsDictionaryAsync(xlsxStream, sheetName, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxStreamAsDictionaryAsync(System.IO.Stream,System.Int32,System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxStreamAsDictionaryAsync(Stream xlsxStream, int sheetIndex, CancellationToken ct = default)
        => _reader.ParseXlsxStreamAsDictionaryAsync(xlsxStream, sheetIndex, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxBytesAsDictionaryAsync(System.Byte[],System.String,System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxBytesAsDictionaryAsync(byte[] xlsxBytes, string sheetName, CancellationToken ct = default)
        => _reader.ParseXlsxBytesAsDictionaryAsync(xlsxBytes, sheetName, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxBytesAsDictionaryAsync(System.Byte[],System.Int32,System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxBytesAsDictionaryAsync(byte[] xlsxBytes, int sheetIndex, CancellationToken ct = default)
        => _reader.ParseXlsxBytesAsDictionaryAsync(xlsxBytes, sheetIndex, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxFileAsDataTableAsync(System.String,System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(string xlsxFilePath, string sheetName, bool? useHeaderRow = null, CancellationToken ct = default)
        => _reader.ParseXlsxFileAsDataTableAsync(xlsxFilePath, sheetName, useHeaderRow, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxFileAsDataTableAsync(System.String,System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(string xlsxFilePath, int sheetIndex, bool? useHeaderRow = null, CancellationToken ct = default)
        => _reader.ParseXlsxFileAsDataTableAsync(xlsxFilePath, sheetIndex, useHeaderRow, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxStreamAsDataTableAsync(System.IO.Stream,System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, string sheetName, bool? useHeaderRow = null, CancellationToken ct = default)
        => _reader.ParseXlsxStreamAsDataTableAsync(xlsxStream, sheetName, useHeaderRow, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxStreamAsDataTableAsync(System.IO.Stream,System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, int sheetIndex, bool? useHeaderRow = null, CancellationToken ct = default)
        => _reader.ParseXlsxStreamAsDataTableAsync(xlsxStream, sheetIndex, useHeaderRow, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxBytesAsDataTableAsync(System.Byte[],System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, string sheetName, bool? useHeaderRow = null, CancellationToken ct = default)
        => _reader.ParseXlsxBytesAsDataTableAsync(xlsxBytes, sheetName, useHeaderRow, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxBytesAsDataTableAsync(System.Byte[],System.Int32,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, int sheetIndex, bool? useHeaderRow = null, CancellationToken ct = default)
        => _reader.ParseXlsxBytesAsDataTableAsync(xlsxBytes, sheetIndex, useHeaderRow, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxFileAsAllSheetsAsync(System.String,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxFileAsAllSheetsAsync(string xlsxFilePath, bool? useHeaderRow = null, CancellationToken ct = default)
        => _reader.ParseXlsxFileAsAllSheetsAsync(xlsxFilePath, useHeaderRow, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxStreamAsAllSheetsAsync(System.IO.Stream,System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxStreamAsAllSheetsAsync(Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct = default)
        => _reader.ParseXlsxStreamAsAllSheetsAsync(xlsxStream, useHeaderRow, ct);

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ParseXlsxBytesAsAllSheetsAsync(System.Byte[],System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public Task<IReadOnlyDictionary<string, DataTable.Models.DataTable>> ParseXlsxBytesAsAllSheetsAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default)
        => _reader.ParseXlsxBytesAsAllSheetsAsync(xlsxBytes, useHeaderRow, ct);

    /// <inheritdoc
    ///     cref='M:Lyo.Xlsx.Models.IXlsxService.BatchParseFilesAsDataTableAsync(System.Collections.Generic.IEnumerable{System.String},System.Nullable{System.Boolean},System.Threading.CancellationToken)' />
    public async Task<IReadOnlyList<Result<DataTable.Models.DataTable>>> BatchParseFilesAsDataTableAsync(
        IEnumerable<string> xlsxFilePaths,
        bool? useHeaderRow = null,
        CancellationToken ct = default)
    {
        var paths = xlsxFilePaths.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(paths, nameof(xlsxFilePaths));
        var results = new List<Result<DataTable.Models.DataTable>>();
        foreach (var path in paths) {
            ct.ThrowIfCancellationRequested();
            results.Add(await _reader.ParseXlsxFileAsDataTableAsync(path, useHeaderRow, ct).ConfigureAwait(false));
        }

        return results;
    }

    // Conversion Async
    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ConvertXlsxToCsvAsync(System.String,System.String,System.Text.Encoding,System.Threading.CancellationToken)' />
    public async Task ConvertXlsxToCsvAsync(string xlsxPath, string outputCsvPath, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxPath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputCsvPath);
        _logger.LogDebug("Converting {ConvertingXlsxPath} to {ConvertedCsvPath}", xlsxPath, outputCsvPath);
        await using var inputStream = File.Open(xlsxPath, FileMode.Open, FileAccess.Read);
        await using var outputStream = File.Create(outputCsvPath);
        await ConvertXlsxToCsvAsync(inputStream, outputStream, encoding, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ConvertXlsxToCsvAsync(System.IO.Stream,System.IO.Stream,System.Text.Encoding,System.Threading.CancellationToken)' />
    public async Task ConvertXlsxToCsvAsync(Stream inputStream, Stream outputStream, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputStream);
        ArgumentHelpers.ThrowIfNull(outputStream);
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        _logger.LogDebug("Converting xlsx stream to csv stream");
        using var reader = ExcelReaderFactory.CreateReader(inputStream);
        var dataSet = reader.AsDataSet(new() { ConfigureDataTable = _ => _excelDataTableConfiguration });
        OperationHelpers.ThrowIf(dataSet.Tables.Count == 0, "The XLSX file contains no worksheets.");
        var table = dataSet.Tables[0];
        await using var writer = new StreamWriter(outputStream, encoding, 1024, true);
        await WriteDataTableToCsvAsync(table, writer, ct).ConfigureAwait(false);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ConvertXlsxToCsvAsync(System.Byte[],System.String,System.Text.Encoding,System.Threading.CancellationToken)' />
    public async Task ConvertXlsxToCsvAsync(byte[] xlsxBytes, string outputCsvPath, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputCsvPath);
        await using var inputStream = new MemoryStream(xlsxBytes);
        await using var outputStream = File.Create(outputCsvPath);
        await ConvertXlsxToCsvAsync(inputStream, outputStream, encoding, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ConvertXlsxToCsvAsync(System.Byte[],System.IO.Stream,System.Text.Encoding,System.Threading.CancellationToken)' />
    public async Task ConvertXlsxToCsvAsync(byte[] xlsxBytes, Stream outputStream, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        ArgumentHelpers.ThrowIfNull(outputStream);
        await using var inputStream = new MemoryStream(xlsxBytes);
        await ConvertXlsxToCsvAsync(inputStream, outputStream, encoding, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ConvertXlsxToCsvBytesAsync(System.Byte[],System.Text.Encoding,System.Threading.CancellationToken)' />
    public async Task<byte[]> ConvertXlsxToCsvBytesAsync(byte[] xlsxBytes, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes);
        await using var inputStream = new MemoryStream(xlsxBytes);
        return await ConvertXlsxToCsvBytesAsync(inputStream, encoding, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Xlsx.Models.IXlsxService.ConvertXlsxToCsvBytesAsync(System.IO.Stream,System.Text.Encoding,System.Threading.CancellationToken)' />
    public async Task<byte[]> ConvertXlsxToCsvBytesAsync(Stream inputStream, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputStream);
        await using var outputStream = new MemoryStream();
        await ConvertXlsxToCsvAsync(inputStream, outputStream, encoding, ct).ConfigureAwait(false);
        return outputStream.ToArray();
    }

    private static async Task WriteDataTableToCsvAsync(System.Data.DataTable table, StreamWriter writer, CancellationToken ct)
    {
        var headers = table.Columns.Cast<DataColumn>().Select(col => EscapeCsv(col.ColumnName));
        await writer.WriteLineAsync(string.Join(",", headers)).ConfigureAwait(false);
        foreach (DataRow row in table.Rows) {
            ct.ThrowIfCancellationRequested();
            var fields = row.ItemArray.Select(field => EscapeCsv(field?.ToString()));
            await writer.WriteLineAsync(string.Join(",", fields)).ConfigureAwait(false);
        }
    }
#endif
}