using System.Data;
using System.Reflection;
using System.Text;
using ExcelDataReader;
using Lyo.Common;
using Lyo.DataTable.Models;
using Lyo.Exceptions;
using Lyo.Xlsx.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Xlsx;

public class XlsxService : IXlsxService
{
    private readonly XlsxExporter _exporter;
    private readonly XlsxImporter _importer;
    private readonly ILogger<XlsxService> _logger;

    private ExcelDataTableConfiguration _excelDataTableConfiguration;

    public XlsxService(ILogger<XlsxService>? logger = null, ExcelDataTableConfiguration? excelDataTableConfiguration = null)
    {
        _logger = logger ?? NullLoggerFactory.Instance.CreateLogger<XlsxService>();
        _excelDataTableConfiguration = excelDataTableConfiguration ?? new ExcelDataTableConfiguration { UseHeaderRow = true };
        _exporter = new(_logger);
        _importer = new(() => _excelDataTableConfiguration, _logger);
    }

    /// <inheritdoc />
    public IXlsxExporter Exporter => _exporter;

    /// <inheritdoc />
    public IXlsxImporter Importer => _importer;

    /// <inheritdoc />
    /// <inheritdoc />
    public void ExportToXlsx<T>(IEnumerable<T> data, string xlsxFilePath, string? worksheetName = null) => _exporter.ExportToXlsx(data, xlsxFilePath, worksheetName);

    /// <inheritdoc />
    public void ExportToXlsx<T>(IEnumerable<T> data, Stream xlsxStream, string? worksheetName = null) => _exporter.ExportToXlsx(data, xlsxStream, worksheetName);

    /// <inheritdoc />
    public byte[] ExportToXlsxBytes<T>(IEnumerable<T> data, string? worksheetName = null) => _exporter.ExportToXlsxBytes(data, worksheetName);

    /// <inheritdoc />
    public void ExportToXlsx<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string xlsxFilePath, string? worksheetName = null)
        => _exporter.ExportToXlsx(data, selectedProperties, xlsxFilePath, worksheetName);

    /// <inheritdoc />
    public void ExportToXlsx<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream xlsxStream, string? worksheetName = null)
        => _exporter.ExportToXlsx(data, selectedProperties, xlsxStream, worksheetName);

    /// <inheritdoc />
    public byte[] ExportToXlsxBytes<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string? worksheetName = null)
        => _exporter.ExportToXlsxBytes(data, selectedProperties, worksheetName);

    /// <inheritdoc />
    public void ExportToXlsx<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, string xlsxFilePath) => _exporter.ExportToXlsx(dataSets, xlsxFilePath);

    /// <inheritdoc />
    public void ExportToXlsx<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, Stream xlsxStream) => _exporter.ExportToXlsx(dataSets, xlsxStream);

    /// <inheritdoc />
    public byte[] ExportToXlsxBytes<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets) => _exporter.ExportToXlsxBytes(dataSets);

    /// <inheritdoc />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath) => _importer.ParseXlsxFileAsDictionary(xlsxFilePath);

    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream) => _importer.ParseXlsxStreamAsDictionary(xlsxStream);

    public Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, bool? useHeaderRow = null)
        => _importer.ParseXlsxFileAsDataTable(xlsxFilePath, useHeaderRow);

    public Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, bool? useHeaderRow = null)
        => _importer.ParseXlsxStreamAsDataTable(xlsxStream, useHeaderRow);

    public Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, bool? useHeaderRow = null)
        => _importer.ParseXlsxBytesAsDataTable(xlsxBytes, useHeaderRow);

    public string ExportToHtmlTable(byte[] xlsxBytes, bool? useHeaderRow = null)
        => DataTableToHtml.ToHtmlDocument(ParseXlsxBytesAsDataTable(xlsxBytes, useHeaderRow).ValueOrThrow());

    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes) => _importer.ParseXlsxBytesAsDictionary(xlsxBytes);

    public IReadOnlyList<Result<DataTable.Models.DataTable>> BatchParseFilesAsDataTable(IEnumerable<string> xlsxFilePaths, bool? useHeaderRow = null)
    {
        var paths = xlsxFilePaths.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(paths, nameof(xlsxFilePaths));
        var results = new List<Result<DataTable.Models.DataTable>>();
        foreach (var path in paths)
            results.Add(_importer.ParseXlsxFileAsDataTable(path, useHeaderRow));

        return results;
    }

    /// <inheritdoc />
    public void ConvertXlsxToCsv(string xlsxPath, string outputCsvPath, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxPath, nameof(xlsxPath));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputCsvPath, nameof(outputCsvPath));
        _logger.LogDebug("Converting {ConvertingXlsxPath} to {ConvertedCsvPath}", xlsxPath, outputCsvPath);
        using var inputStream = File.Open(xlsxPath, FileMode.Open, FileAccess.Read);
        using var outputStream = File.Create(outputCsvPath);
        ConvertXlsxToCsv(inputStream, outputStream, encoding);
    }

    public void ConvertXlsxToCsv(Stream inputStream, Stream outputStream, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(inputStream, nameof(inputStream));
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        _logger.LogDebug("Converting xlsx stream to csv stream");
        using var reader = ExcelReaderFactory.CreateReader(inputStream);
        var dataSet = reader.AsDataSet(new() { ConfigureDataTable = _ => _excelDataTableConfiguration });
        OperationHelpers.ThrowIf(dataSet.Tables.Count == 0, "The XLSX file contains no worksheets.");
        var table = dataSet.Tables[0]!;
        using var writer = new StreamWriter(outputStream, encoding ?? Encoding.UTF8, 1024, true);
        WriteDataTableToCsv(table, writer);
        writer.Flush();
    }

    public void ConvertXlsxToCsv(byte[] xlsxBytes, string outputCsvPath, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes, nameof(xlsxBytes));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputCsvPath, nameof(outputCsvPath));
        using var inputStream = new MemoryStream(xlsxBytes);
        using var outputStream = File.Create(outputCsvPath);
        ConvertXlsxToCsv(inputStream, outputStream, encoding);
    }

    public void ConvertXlsxToCsv(byte[] xlsxBytes, Stream outputStream, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes, nameof(xlsxBytes));
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        using var inputStream = new MemoryStream(xlsxBytes);
        ConvertXlsxToCsv(inputStream, outputStream, encoding);
    }

    public byte[] ConvertXlsxToCsvBytes(byte[] xlsxBytes, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes, nameof(xlsxBytes));
        using var inputStream = new MemoryStream(xlsxBytes);
        return ConvertXlsxToCsvBytes(inputStream, encoding);
    }

    public byte[] ConvertXlsxToCsvBytes(Stream inputStream, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(inputStream, nameof(inputStream));
        using var outputStream = new MemoryStream();
        ConvertXlsxToCsv(inputStream, outputStream, encoding);
        return outputStream.ToArray();
    }

    public void SetExcelDataTableConfiguration(ExcelDataTableConfiguration excelDataTableConfiguration)
    {
        ArgumentHelpers.ThrowIfNull(excelDataTableConfiguration, nameof(excelDataTableConfiguration));
        _excelDataTableConfiguration = excelDataTableConfiguration;
    }

    public void ExportToXlsxFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, string xlsxFilePath, bool useHeaderRow = true)
        => _exporter.ExportToXlsxFromDictionary(data, xlsxFilePath, useHeaderRow);

    public void ExportToXlsxFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, Stream xlsxStream, bool useHeaderRow = true)
        => _exporter.ExportToXlsxFromDictionary(data, xlsxStream, useHeaderRow);

    public byte[] ExportToXlsxBytesFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool useHeaderRow = true)
        => _exporter.ExportToXlsxBytesFromDictionary(data, useHeaderRow);

    public void ExportToXlsxFromDataTable(DataTable.Models.DataTable dataTable, string xlsxFilePath) => _exporter.ExportToXlsxFromDataTable(dataTable, xlsxFilePath);

    public void ExportToXlsxFromDataTable(DataTable.Models.DataTable dataTable, Stream xlsxStream) => _exporter.ExportToXlsxFromDataTable(dataTable, xlsxStream);

    public byte[] ExportToXlsxBytesFromDataTable(DataTable.Models.DataTable dataTable) => _exporter.ExportToXlsxBytesFromDataTable(dataTable);

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
    // Export Async - delegate to Exporter
    public Task ExportToXlsxAsync<T>(IEnumerable<T> data, string xlsxFilePath, string? worksheetName = null, CancellationToken ct = default)
        => _exporter.ExportToXlsxAsync(data, xlsxFilePath, worksheetName, ct);

    public Task ExportToXlsxAsync<T>(IEnumerable<T> data, Stream xlsxStream, string? worksheetName = null, CancellationToken ct = default)
        => _exporter.ExportToXlsxAsync(data, xlsxStream, worksheetName, ct);

    public Task<byte[]> ExportToXlsxBytesAsync<T>(IEnumerable<T> data, string? worksheetName = null, CancellationToken ct = default)
        => _exporter.ExportToXlsxBytesAsync(data, worksheetName, ct);

    public Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyList<PropertyInfo> selectedProperties,
        string xlsxFilePath,
        string? worksheetName = null,
        CancellationToken ct = default)
        => _exporter.ExportToXlsxAsync(data, selectedProperties, xlsxFilePath, worksheetName, ct);

    public Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyList<PropertyInfo> selectedProperties,
        Stream xlsxStream,
        string? worksheetName = null,
        CancellationToken ct = default)
        => _exporter.ExportToXlsxAsync(data, selectedProperties, xlsxStream, worksheetName, ct);

    public Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyDictionary<string, PropertyInfo> columns,
        Stream xlsxStream,
        string? worksheetName = null,
        CancellationToken ct = default)
        => _exporter.ExportToXlsxAsync(data, columns, xlsxStream, worksheetName, ct);

    public Task ExportToXlsxAsync<T>(
        IEnumerable<T> data,
        IReadOnlyDictionary<string, Func<T, string>> columnFormatters,
        Stream xlsxStream,
        string? worksheetName = null,
        CancellationToken ct = default)
        => _exporter.ExportToXlsxAsync(data, columnFormatters, xlsxStream, worksheetName, ct);

    public Task<byte[]> ExportToXlsxBytesAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string? worksheetName = null, CancellationToken ct = default)
        => _exporter.ExportToXlsxBytesAsync(data, selectedProperties, worksheetName, ct);

    public Task ExportToXlsxAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, string xlsxFilePath, CancellationToken ct = default)
        => _exporter.ExportToXlsxAsync(dataSets, xlsxFilePath, ct);

    public Task ExportToXlsxAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, Stream xlsxStream, CancellationToken ct = default)
        => _exporter.ExportToXlsxAsync(dataSets, xlsxStream, ct);

    public Task<byte[]> ExportToXlsxBytesAsync<T>(IReadOnlyDictionary<string, IEnumerable<T>> dataSets, CancellationToken ct = default)
        => _exporter.ExportToXlsxBytesAsync(dataSets, ct);

    public Task ExportToXlsxFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        string xlsxFilePath,
        bool useHeaderRow = true,
        CancellationToken ct = default)
        => _exporter.ExportToXlsxFromDictionaryAsync(data, xlsxFilePath, useHeaderRow, ct);

    public Task ExportToXlsxFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        Stream xlsxStream,
        bool useHeaderRow = true,
        CancellationToken ct = default)
        => _exporter.ExportToXlsxFromDictionaryAsync(data, xlsxStream, useHeaderRow, ct);

    public Task<byte[]> ExportToXlsxBytesFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        bool useHeaderRow = true,
        CancellationToken ct = default)
        => _exporter.ExportToXlsxBytesFromDictionaryAsync(data, useHeaderRow, ct);

    public Task ExportToXlsxFromDataTableAsync(DataTable.Models.DataTable dataTable, string xlsxFilePath, CancellationToken ct = default)
        => _exporter.ExportToXlsxFromDataTableAsync(dataTable, xlsxFilePath, ct);

    public Task ExportToXlsxFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream xlsxStream, CancellationToken ct = default)
        => _exporter.ExportToXlsxFromDataTableAsync(dataTable, xlsxStream, ct);

    public Task<byte[]> ExportToXlsxBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
        => _exporter.ExportToXlsxBytesFromDataTableAsync(dataTable, ct);

    // Import Async - delegate to Importer
    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(string xlsxFilePath, CancellationToken ct = default)
        => _importer.ParseXlsxFileAsDictionaryAsync(xlsxFilePath, ct);

    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxStreamAsDictionaryAsync(Stream xlsxStream, CancellationToken ct = default)
        => _importer.ParseXlsxStreamAsDictionaryAsync(xlsxStream, ct);

    public Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(string xlsxFilePath, bool? useHeaderRow = null, CancellationToken ct = default)
        => _importer.ParseXlsxFileAsDataTableAsync(xlsxFilePath, useHeaderRow, ct);

    public Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct = default)
        => _importer.ParseXlsxStreamAsDataTableAsync(xlsxStream, useHeaderRow, ct);

    public Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default)
        => _importer.ParseXlsxBytesAsDataTableAsync(xlsxBytes, useHeaderRow, ct);

    public async Task<string> ExportToHtmlTableAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default)
    {
        var result = await _importer.ParseXlsxBytesAsDataTableAsync(xlsxBytes, useHeaderRow, ct).ConfigureAwait(false);
        return DataTableToHtml.ToHtmlDocument(result.ValueOrThrow());
    }

    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxBytesAsDictionaryAsync(byte[] xlsxBytes, CancellationToken ct = default)
        => _importer.ParseXlsxBytesAsDictionaryAsync(xlsxBytes, ct);

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
            results.Add(await _importer.ParseXlsxFileAsDataTableAsync(path, useHeaderRow, ct).ConfigureAwait(false));
        }

        return results;
    }

    // Conversion Async
    public async Task ConvertXlsxToCsvAsync(string xlsxPath, string outputCsvPath, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(xlsxPath, nameof(xlsxPath));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputCsvPath, nameof(outputCsvPath));
        _logger.LogDebug("Converting {ConvertingXlsxPath} to {ConvertedCsvPath}", xlsxPath, outputCsvPath);
        await using var inputStream = File.Open(xlsxPath, FileMode.Open, FileAccess.Read);
        await using var outputStream = File.Create(outputCsvPath);
        await ConvertXlsxToCsvAsync(inputStream, outputStream, encoding, ct).ConfigureAwait(false);
    }

    public async Task ConvertXlsxToCsvAsync(Stream inputStream, Stream outputStream, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputStream, nameof(inputStream));
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        _logger.LogDebug("Converting xlsx stream to csv stream");
        using var reader = ExcelReaderFactory.CreateReader(inputStream);
        var dataSet = reader.AsDataSet(new() { ConfigureDataTable = _ => _excelDataTableConfiguration });
        OperationHelpers.ThrowIf(dataSet.Tables.Count == 0, "The XLSX file contains no worksheets.");
        var table = dataSet.Tables[0]!;
        await using var writer = new StreamWriter(outputStream, encoding, 1024, true);
        await WriteDataTableToCsvAsync(table, writer, ct).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    public async Task ConvertXlsxToCsvAsync(byte[] xlsxBytes, string outputCsvPath, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes, nameof(xlsxBytes));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputCsvPath, nameof(outputCsvPath));
        await using var inputStream = new MemoryStream(xlsxBytes);
        await using var outputStream = File.Create(outputCsvPath);
        await ConvertXlsxToCsvAsync(inputStream, outputStream, encoding, ct).ConfigureAwait(false);
    }

    public async Task ConvertXlsxToCsvAsync(byte[] xlsxBytes, Stream outputStream, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes, nameof(xlsxBytes));
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        await using var inputStream = new MemoryStream(xlsxBytes);
        await ConvertXlsxToCsvAsync(inputStream, outputStream, encoding, ct).ConfigureAwait(false);
    }

    public async Task<byte[]> ConvertXlsxToCsvBytesAsync(byte[] xlsxBytes, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xlsxBytes, nameof(xlsxBytes));
        await using var inputStream = new MemoryStream(xlsxBytes);
        return await ConvertXlsxToCsvBytesAsync(inputStream, encoding, ct).ConfigureAwait(false);
    }

    public async Task<byte[]> ConvertXlsxToCsvBytesAsync(Stream inputStream, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputStream, nameof(inputStream));
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