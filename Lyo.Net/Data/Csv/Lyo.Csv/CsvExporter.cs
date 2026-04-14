using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;
using Lyo.Csv.Models;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;

namespace Lyo.Csv;

internal sealed class CsvExporter : ICsvExporter
{
    private readonly List<Type> _classMapTypes;
    private readonly Func<CsvConfiguration> _getConfig;
    private readonly ILogger _logger;

    private CsvConfiguration Config => _getConfig();

    internal CsvExporter(Func<CsvConfiguration> getConfig, List<Type> classMapTypes, ILogger logger)
    {
        _getConfig = getConfig;
        _classMapTypes = classMapTypes;
        _logger = logger;
    }

    public void ExportToCsv<T>(IEnumerable<T> data, string csvFilePath)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        _logger.LogDebug("Exporting {ExportType} to {ExportCsvPath}", typeof(T).FullName, csvFilePath);
        using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        ExportToCsv(data, writer);
    }

    public void ExportToCsvStream<T>(IEnumerable<T> data, Stream csvStream)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting {ExportType} to csv stream", typeof(T).FullName);
        using var writer = new StreamWriter(csvStream, Config.Encoding, 1024, true);
        ExportToCsv(data, writer);
        writer.Flush();
    }

    public void ExportToCsv<T>(IEnumerable<T> data, TextWriter writer)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        ArgumentHelpers.ThrowIfNull(writer, nameof(writer));
        _logger.LogDebug("Exporting {ExportType} to csv writer", typeof(T).FullName);
        using var csv = new CsvWriter(writer, Config);
        RegisterClassMaps(csv);
        csv.WriteRecords(data);
        csv.Flush();
    }

    public string ExportToCsvString<T>(IEnumerable<T> data)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        _logger.LogDebug("Exporting {ExportType} to csv string", typeof(T).FullName);
        using var writer = new StringWriter();
        ExportToCsv(data, writer);
        return writer.ToString();
    }

    public byte[] ExportToCsvBytes<T>(IEnumerable<T> data)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        _logger.LogDebug("Exporting {ExportType} to csv bytes", typeof(T).FullName);
        using var memoryStream = new MemoryStream();
        ExportToCsvStream(data, memoryStream);
        return memoryStream.ToArray();
    }

    public void ExportToCsv<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string csvFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties, nameof(selectedProperties));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        _logger.LogDebug("Exporting {ExportType} to {ExportCsvPath} with {PropertyCount} selected properties", typeof(T).FullName, csvFilePath, selectedProperties.Count);
        using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        ExportToCsv(data, selectedProperties, writer);
    }

    public void ExportToCsvStream<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream csvStream)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties, nameof(selectedProperties));
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting {ExportType} to csv stream with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        using var writer = new StreamWriter(csvStream, Config.Encoding, 1024, true);
        ExportToCsv(data, selectedProperties, writer);
        writer.Flush();
    }

    public void ExportToCsv<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, TextWriter writer)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties, nameof(selectedProperties));
        _logger.LogDebug("Exporting {ExportType} to csv writer with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        using var csv = new CsvWriter(writer, Config);
        foreach (var prop in selectedProperties)
            csv.WriteField(prop.Name);

        csv.NextRecord();
        foreach (var item in data) {
            foreach (var prop in selectedProperties) {
                var value = prop.GetValue(item);
                csv.WriteField(value);
            }

            csv.NextRecord();
        }

        csv.Flush();
    }

    public string ExportToCsvString<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties, nameof(selectedProperties));
        _logger.LogDebug("Exporting {ExportType} to csv string with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        using var writer = new StringWriter();
        ExportToCsv(data, selectedProperties, writer);
        return writer.ToString();
    }

    public byte[] ExportToCsvBytes<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties, nameof(selectedProperties));
        _logger.LogDebug("Exporting {ExportType} to csv bytes with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        using var memoryStream = new MemoryStream();
        ExportToCsvStream(data, selectedProperties, memoryStream);
        return memoryStream.ToArray();
    }

    public void ExportToCsvFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, string csvFilePath, bool hasHeaderRow = true)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        _logger.LogDebug("Exporting dictionary to {ExportCsvPath}", csvFilePath);
        using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        WriteDictionaryToCsv(data, writer, hasHeaderRow);
    }

    public void ExportToCsvStreamFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, Stream csvStream, bool hasHeaderRow = true)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting dictionary to csv stream");
        using var writer = new StreamWriter(csvStream, Config.Encoding, 1024, true);
        WriteDictionaryToCsv(data, writer, hasHeaderRow);
        writer.Flush();
    }

    public string ExportToCsvStringFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        using var writer = new StringWriter();
        WriteDictionaryToCsv(data, writer, hasHeaderRow);
        return writer.ToString();
    }

    public byte[] ExportToCsvBytesFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        using var ms = new MemoryStream();
        ExportToCsvStreamFromDictionary(data, ms, hasHeaderRow);
        return ms.ToArray();
    }

    public void ExportToCsvFromDataTable(DataTable.Models.DataTable dataTable, string csvFilePath)
    {
        ArgumentHelpers.ThrowIfNull(dataTable, nameof(dataTable));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        _logger.LogDebug("Exporting tabular to {ExportCsvPath}", csvFilePath);
        using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        WriteDataTableToCsv(dataTable, writer);
    }

    public void ExportToCsvStreamFromDataTable(DataTable.Models.DataTable dataTable, Stream csvStream)
    {
        ArgumentHelpers.ThrowIfNull(dataTable, nameof(dataTable));
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting tabular to csv stream");
        using var writer = new StreamWriter(csvStream, Config.Encoding, 1024, true);
        WriteDataTableToCsv(dataTable, writer);
        writer.Flush();
    }

    public string ExportToCsvStringFromDataTable(DataTable.Models.DataTable dataTable)
    {
        ArgumentHelpers.ThrowIfNull(dataTable, nameof(dataTable));
        using var writer = new StringWriter();
        WriteDataTableToCsv(dataTable, writer);
        return writer.ToString();
    }

    public byte[] ExportToCsvBytesFromDataTable(DataTable.Models.DataTable dataTable)
    {
        ArgumentHelpers.ThrowIfNull(dataTable, nameof(dataTable));
        using var ms = new MemoryStream();
        ExportToCsvStreamFromDataTable(dataTable, ms);
        return ms.ToArray();
    }

    private void RegisterClassMaps(CsvWriter csv)
    {
        foreach (var mapType in _classMapTypes)
            csv.Context.RegisterClassMap(mapType);
    }

    private void WriteDictionaryToCsv(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, TextWriter writer, bool hasHeaderRow)
    {
        if (data.Count == 0)
            return;

        var maxCol = data.Values.SelectMany(r => r.Keys).DefaultIfEmpty(-1).Max() + 1;
        maxCol = Math.Max(maxCol, 1);
        var orderedRows = data.OrderBy(kv => kv.Key).ToList();
        var firstRow = orderedRows[0].Value;
        if (hasHeaderRow && orderedRows.Count > 0) {
            for (var c = 0; c < maxCol; c++)
                writer.Write((c > 0 ? "," : "") + EscapeCsv(firstRow.TryGetValue(c, out var hv) ? hv ?? "" : ""));

            writer.WriteLine();
            orderedRows = orderedRows.Skip(1).ToList();
        }
        else {
            for (var c = 0; c < maxCol; c++)
                writer.Write((c > 0 ? "," : "") + EscapeCsv($"Column{c}"));

            writer.WriteLine();
        }

        foreach (var kv in orderedRows) {
            for (var c = 0; c < maxCol; c++)
                writer.Write((c > 0 ? "," : "") + EscapeCsv(kv.Value.TryGetValue(c, out var rv) ? rv ?? "" : ""));

            writer.WriteLine();
        }
    }

    private void WriteDataTableToCsv(DataTable.Models.DataTable dataTable, TextWriter writer)
    {
        var maxCol = dataTable.MaxColumn >= 0 ? dataTable.MaxColumn + 1 : 0;
        var orderedHeaders = dataTable.Headers.OrderBy(kv => kv.Key).ToList();
        for (var c = 0; c < maxCol; c++) {
            var header = orderedHeaders.FirstOrDefault(h => h.Key == c).Value;
            writer.Write((c > 0 ? "," : "") + EscapeCsv(header?.DisplayValue ?? ""));
        }

        writer.WriteLine();
        foreach (var row in dataTable.Rows) {
            for (var c = 0; c < maxCol; c++) {
                var cell = row.Cells.TryGetValue(c, out var cellVal) ? cellVal : null;
                writer.Write((c > 0 ? "," : "") + EscapeCsv(cell?.DisplayValue ?? ""));
            }

            writer.WriteLine();
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
    public async Task ExportToCsvAsync<T>(IEnumerable<T> data, string csvFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        _logger.LogDebug("Exporting {ExportType} to {ExportCsvPath}", typeof(T).FullName, csvFilePath);
        await using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        await ExportToCsvAsync(data, writer, ct).ConfigureAwait(false);
    }

    public async Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting {ExportType} to csv stream", typeof(T).FullName);
        await using var writer = new StreamWriter(csvStream, Config.Encoding, 1024, true);
        await ExportToCsvAsync(data, writer, ct).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    public async Task ExportToCsvAsync<T>(IEnumerable<T> data, TextWriter writer, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        ArgumentHelpers.ThrowIfNull(writer, nameof(writer));
        _logger.LogDebug("Exporting {ExportType} to csv writer", typeof(T).FullName);
        await using var csv = new CsvWriter(writer, Config);
        RegisterClassMaps(csv);
        await csv.WriteRecordsAsync(data, ct).ConfigureAwait(false);
        await csv.FlushAsync().ConfigureAwait(false);
    }

    public async Task<string> ExportToCsvStringAsync<T>(IEnumerable<T> data, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        _logger.LogDebug("Exporting {ExportType} to csv string", typeof(T).FullName);
        using var writer = new StringWriter();
        await ExportToCsvAsync(data, writer, ct).ConfigureAwait(false);
        return writer.ToString();
    }

    public async Task<byte[]> ExportToCsvBytesAsync<T>(IEnumerable<T> data, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        _logger.LogDebug("Exporting {ExportType} to csv bytes", typeof(T).FullName);
        await using var memoryStream = new MemoryStream();
        await ExportToCsvStreamAsync(data, memoryStream, ct).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    public async Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string csvFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties, nameof(selectedProperties));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        _logger.LogDebug("Exporting {ExportType} to {ExportCsvPath} with {PropertyCount} selected properties", typeof(T).FullName, csvFilePath, selectedProperties.Count);
        await using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        await ExportToCsvAsync(data, selectedProperties, writer, ct).ConfigureAwait(false);
    }

    public async Task ExportToCsvFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        string csvFilePath,
        bool hasHeaderRow = true,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        _logger.LogDebug("Exporting dictionary to {ExportCsvPath}", csvFilePath);
        await using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        await WriteDictionaryToCsvAsync(data, writer, hasHeaderRow, ct).ConfigureAwait(false);
    }

    public async Task ExportToCsvStreamFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        Stream csvStream,
        bool hasHeaderRow = true,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting dictionary to csv stream");
        await using var writer = new StreamWriter(csvStream, Config.Encoding, 1024, true);
        await WriteDictionaryToCsvAsync(data, writer, hasHeaderRow, ct).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    public async Task<string> ExportToCsvStringFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        bool hasHeaderRow = true,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        using var writer = new StringWriter();
        await WriteDictionaryToCsvAsync(data, writer, hasHeaderRow, ct).ConfigureAwait(false);
        return writer.ToString();
    }

    public async Task<byte[]> ExportToCsvBytesFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        bool hasHeaderRow = true,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        await using var ms = new MemoryStream();
        await ExportToCsvStreamFromDictionaryAsync(data, ms, hasHeaderRow, ct).ConfigureAwait(false);
        return ms.ToArray();
    }

    public async Task ExportToCsvFromDataTableAsync(DataTable.Models.DataTable dataTable, string csvFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataTable, nameof(dataTable));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        _logger.LogDebug("Exporting tabular to {ExportCsvPath}", csvFilePath);
        await using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        await WriteDataTableToCsvAsync(dataTable, writer, ct).ConfigureAwait(false);
    }

    public async Task ExportToCsvStreamFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataTable, nameof(dataTable));
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting tabular to csv stream");
        await using var writer = new StreamWriter(csvStream, Config.Encoding, 1024, true);
        await WriteDataTableToCsvAsync(dataTable, writer, ct).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    public async Task<string> ExportToCsvStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataTable, nameof(dataTable));
        using var writer = new StringWriter();
        await WriteDataTableToCsvAsync(dataTable, writer, ct).ConfigureAwait(false);
        return writer.ToString();
    }

    public async Task<byte[]> ExportToCsvBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataTable, nameof(dataTable));
        await using var ms = new MemoryStream();
        await ExportToCsvStreamFromDataTableAsync(dataTable, ms, ct).ConfigureAwait(false);
        return ms.ToArray();
    }

    private async Task WriteDictionaryToCsvAsync(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, TextWriter writer, bool hasHeaderRow, CancellationToken ct)
    {
        if (data.Count == 0)
            return;

        var maxCol = data.Values.SelectMany(r => r.Keys).DefaultIfEmpty(-1).Max() + 1;
        maxCol = Math.Max(maxCol, 1);
        var orderedRows = data.OrderBy(kv => kv.Key).ToList();
        var firstRow = orderedRows[0].Value;
        if (hasHeaderRow && orderedRows.Count > 0) {
            for (var c = 0; c < maxCol; c++)
                await writer.WriteAsync((c > 0 ? "," : "") + EscapeCsv(firstRow.TryGetValue(c, out var hv) ? hv ?? "" : "")).ConfigureAwait(false);

            await writer.WriteLineAsync().ConfigureAwait(false);
            orderedRows = orderedRows.Skip(1).ToList();
        }
        else {
            for (var c = 0; c < maxCol; c++)
                await writer.WriteAsync((c > 0 ? "," : "") + EscapeCsv($"Column{c}")).ConfigureAwait(false);

            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        foreach (var kv in orderedRows) {
            ct.ThrowIfCancellationRequested();
            for (var c = 0; c < maxCol; c++)
                await writer.WriteAsync((c > 0 ? "," : "") + EscapeCsv(kv.Value.TryGetValue(c, out var rv) ? rv ?? "" : "")).ConfigureAwait(false);

            await writer.WriteLineAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteDataTableToCsvAsync(DataTable.Models.DataTable dataTable, TextWriter writer, CancellationToken ct)
    {
        var maxCol = dataTable.MaxColumn >= 0 ? dataTable.MaxColumn + 1 : 0;
        var orderedHeaders = dataTable.Headers.OrderBy(kv => kv.Key).ToList();
        for (var c = 0; c < maxCol; c++) {
            var header = orderedHeaders.FirstOrDefault(h => h.Key == c).Value;
            await writer.WriteAsync((c > 0 ? "," : "") + EscapeCsv(header?.DisplayValue ?? "")).ConfigureAwait(false);
        }

        await writer.WriteLineAsync().ConfigureAwait(false);
        foreach (var row in dataTable.Rows) {
            ct.ThrowIfCancellationRequested();
            for (var c = 0; c < maxCol; c++) {
                var cell = row.Cells.TryGetValue(c, out var cellVal) ? cellVal : null;
                await writer.WriteAsync((c > 0 ? "," : "") + EscapeCsv(cell?.DisplayValue ?? "")).ConfigureAwait(false);
            }

            await writer.WriteLineAsync().ConfigureAwait(false);
        }
    }

    public async Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties, nameof(selectedProperties));
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting {ExportType} to csv stream with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        await using var writer = new StreamWriter(csvStream, Config.Encoding, 1024, true);
        await ExportToCsvAsync(data, selectedProperties, writer, ct).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    public async Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, TextWriter writer, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties, nameof(selectedProperties));
        _logger.LogDebug("Exporting {ExportType} to csv writer with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        await using var csv = new CsvWriter(writer, Config);
        foreach (var prop in selectedProperties)
            csv.WriteField(prop.Name);

        await csv.NextRecordAsync().ConfigureAwait(false);
        foreach (var item in data) {
            ct.ThrowIfCancellationRequested();
            foreach (var prop in selectedProperties) {
                var value = prop.GetValue(item);
                csv.WriteField(value);
            }

            await csv.NextRecordAsync().ConfigureAwait(false);
        }

        await csv.FlushAsync().ConfigureAwait(false);
    }

    public async Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, PropertyInfo> columns, Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        ArgumentHelpers.ThrowIfNullOrEmpty(columns, nameof(columns));
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting {ExportType} to csv stream with {ColumnCount} custom columns", typeof(T).FullName, columns.Count);
        await using var writer = new StreamWriter(csvStream, Config.Encoding, 1024, true);
        await ExportToCsvAsync(data, columns, writer, ct).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    public async Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, PropertyInfo> columns, TextWriter writer, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(columns, nameof(columns));
        _logger.LogDebug("Exporting {ExportType} to csv writer with {ColumnCount} custom columns", typeof(T).FullName, columns.Count);
        await using var csv = new CsvWriter(writer, Config);
        foreach (var header in columns.Keys)
            csv.WriteField(header);

        await csv.NextRecordAsync().ConfigureAwait(false);
        foreach (var item in data) {
            ct.ThrowIfCancellationRequested();
            foreach (var prop in columns.Values) {
                var value = prop.GetValue(item);
                csv.WriteField(value);
            }

            await csv.NextRecordAsync().ConfigureAwait(false);
        }

        await csv.FlushAsync().ConfigureAwait(false);
    }

    public async Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, Func<T, string>> columnFormatters, Stream csvStream, CancellationToken ct =
 default)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        ArgumentHelpers.ThrowIfNullOrEmpty(columnFormatters, nameof(columnFormatters));
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting {ExportType} to csv stream with {ColumnCount} formatter columns", typeof(T).FullName, columnFormatters.Count);
        await using var writer = new StreamWriter(csvStream, Config.Encoding, 1024, true);
        await ExportToCsvAsync(data, columnFormatters, writer, ct).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    public async Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, Func<T, string>> columnFormatters, TextWriter writer, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(columnFormatters, nameof(columnFormatters));
        _logger.LogDebug("Exporting {ExportType} to csv writer with {ColumnCount} formatter columns", typeof(T).FullName, columnFormatters.Count);
        await using var csv = new CsvWriter(writer, Config);
        foreach (var header in columnFormatters.Keys)
            csv.WriteField(header);

        await csv.NextRecordAsync().ConfigureAwait(false);
        var formatters = columnFormatters.Values.ToList();
        foreach (var item in data) {
            ct.ThrowIfCancellationRequested();
            foreach (var formatter in formatters) {
                var value = formatter(item);
                csv.WriteField(value);
            }

            await csv.NextRecordAsync().ConfigureAwait(false);
        }

        await csv.FlushAsync().ConfigureAwait(false);
    }

    public async Task<string> ExportToCsvStringAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties, nameof(selectedProperties));
        _logger.LogDebug("Exporting {ExportType} to csv string with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        using var writer = new StringWriter();
        await ExportToCsvAsync(data, selectedProperties, writer, ct).ConfigureAwait(false);
        return writer.ToString();
    }

    public async Task<byte[]> ExportToCsvBytesAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties, nameof(selectedProperties));
        _logger.LogDebug("Exporting {ExportType} to csv bytes with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        await using var memoryStream = new MemoryStream();
        await ExportToCsvStreamAsync(data, selectedProperties, memoryStream, ct).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    public async Task ExportToCsvWithProgressAsync<T>(IEnumerable<T> data, string csvFilePath, IProgress<CsvProgress>? progress, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        var dataList = data.ToList();
        var totalRows = dataList.Count;
        await using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        await using var csv = new CsvWriter(writer, Config);
        RegisterClassMaps(csv);
        long rowsProcessed = 0;
        foreach (var item in dataList) {
            ct.ThrowIfCancellationRequested();
            csv.WriteRecord(item);
            await csv.NextRecordAsync().ConfigureAwait(false);
            rowsProcessed++;
            if ((progress != null && rowsProcessed % 100 == 0) || rowsProcessed == totalRows)
                progress!.Report(new() { RowsProcessed = rowsProcessed, TotalRows = totalRows, Operation = "Exporting" });
        }

        await csv.FlushAsync().ConfigureAwait(false);
    }

    public async Task ExportToCsvStreamWithProgressAsync<T>(IEnumerable<T> data, Stream csvStream, IProgress<CsvProgress>? progress, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        var dataList = data.ToList();
        var totalRows = dataList.Count;
        await using var writer = new StreamWriter(csvStream, Config.Encoding, 1024, true);
        await using var csv = new CsvWriter(writer, Config);
        RegisterClassMaps(csv);
        long rowsProcessed = 0;
        foreach (var item in dataList) {
            ct.ThrowIfCancellationRequested();
            csv.WriteRecord(item);
            await csv.NextRecordAsync().ConfigureAwait(false);
            rowsProcessed++;
            if (progress != null && (rowsProcessed % 100 == 0 || rowsProcessed == totalRows))
                progress.Report(new() { RowsProcessed = rowsProcessed, TotalRows = totalRows, Operation = "Exporting" });
        }

        await csv.FlushAsync().ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    public async Task AppendToCsvAsync<T>(IEnumerable<T> data, string csvFilePath, bool includeHeaderIfMissing = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        var fileExists = File.Exists(csvFilePath);
        var fileIsEmpty = fileExists && new FileInfo(csvFilePath).Length == 0;
        await using var stream = new FileStream(csvFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        await using var writer = new StreamWriter(stream, Config.Encoding);
        await using var csv = new CsvWriter(writer, Config);
        RegisterClassMaps(csv);
        if ((!fileExists || fileIsEmpty) && includeHeaderIfMissing) {
            csv.WriteHeader<T>();
            await csv.NextRecordAsync().ConfigureAwait(false);
        }

        foreach (var item in data) {
            ct.ThrowIfCancellationRequested();
            csv.WriteRecord(item);
            await csv.NextRecordAsync().ConfigureAwait(false);
        }

        await csv.FlushAsync().ConfigureAwait(false);
    }
#endif
}