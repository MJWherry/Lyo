using System.Globalization;
using System.Reflection;
using Lyo.Csv.Models;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
#if NETSTANDARD2_0
using Lyo.Common;
#endif

namespace Lyo.Csv;

internal sealed class CsvWriter : ICsvWriter
{
    private readonly Func<CsvOptions> _getOptions;
    private readonly ILogger _logger;

    private CsvOptions Config => _getOptions();

    internal CsvWriter(Func<CsvOptions> getOptions, ILogger logger)
    {
        _getOptions = getOptions;
        _logger = logger;
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsv``1(System.Collections.Generic.IEnumerable{``0},System.String)' />
    public void ExportToCsv<T>(IEnumerable<T> data, string csvFilePath)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        _logger.LogDebug("Exporting {ExportType} to {ExportCsvPath}", typeof(T).FullName, csvFilePath);
        using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        ExportToCsv(data, writer);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStream``1(System.Collections.Generic.IEnumerable{``0},System.IO.Stream)' />
    public void ExportToCsvStream<T>(IEnumerable<T> data, Stream csvStream)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting {ExportType} to csv stream", typeof(T).FullName);
        using var writer = new StreamWriter(csvStream, Config.Encoding, 8192, true);
        ExportToCsv(data, writer);
        writer.Flush();
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsv``1(System.Collections.Generic.IEnumerable{``0},System.IO.TextWriter)' />
    public void ExportToCsv<T>(IEnumerable<T> data, TextWriter writer)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(writer);
        _logger.LogDebug("Exporting {ExportType} to csv writer", typeof(T).FullName);
        var options = Config;
        var map = CsvTypeBinder.GetMap<T>();
        using var csv = new CsvTextWriter(writer, options);
        if (options.HasHeaderRecord) {
            csv.WriteFields(CsvTypeBinder.GetHeaders(map));
            csv.NextRecord();
        }

        foreach (var item in data) {
            csv.WriteFields(CsvTypeBinder.GetFieldValues(item, map, options.Culture));
            csv.NextRecord();
        }

        csv.Flush();
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvString``1(System.Collections.Generic.IEnumerable{``0})' />
    public string ExportToCsvString<T>(IEnumerable<T> data)
    {
        ArgumentHelpers.ThrowIfNull(data);
        _logger.LogDebug("Exporting {ExportType} to csv string", typeof(T).FullName);
        using var writer = new StringWriter();
        ExportToCsv(data, writer);
        return writer.ToString();
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvBytes``1(System.Collections.Generic.IEnumerable{``0})' />
    public byte[] ExportToCsvBytes<T>(IEnumerable<T> data)
    {
        ArgumentHelpers.ThrowIfNull(data);
        _logger.LogDebug("Exporting {ExportType} to csv bytes", typeof(T).FullName);
        using var memoryStream = new MemoryStream();
        ExportToCsvStream(data, memoryStream);
        return memoryStream.ToArray();
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsv``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.String)' />
    public void ExportToCsv<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string csvFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        _logger.LogDebug("Exporting {ExportType} to {ExportCsvPath} with {PropertyCount} selected properties", typeof(T).FullName, csvFilePath, selectedProperties.Count);
        using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        ExportToCsv(data, selectedProperties, writer);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStream``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.IO.Stream)' />
    public void ExportToCsvStream<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream csvStream)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties);
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting {ExportType} to csv stream with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        using var writer = new StreamWriter(csvStream, Config.Encoding, 8192, true);
        ExportToCsv(data, selectedProperties, writer);
        writer.Flush();
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsv``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.IO.TextWriter)' />
    public void ExportToCsv<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, TextWriter writer)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties);
        _logger.LogDebug("Exporting {ExportType} to csv writer with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        var options = Config;
        using var csv = new CsvTextWriter(writer, options);
        foreach (var prop in selectedProperties)
            csv.WriteField(prop.Name);

        csv.NextRecord();
        foreach (var item in data) {
            foreach (var prop in selectedProperties)
                csv.WriteField(FormatValue(prop.GetValue(item), options.Culture));

            csv.NextRecord();
        }

        csv.Flush();
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvString``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo})' />
    public string ExportToCsvString<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties);
        _logger.LogDebug("Exporting {ExportType} to csv string with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        using var writer = new StringWriter();
        ExportToCsv(data, selectedProperties, writer);
        return writer.ToString();
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvBytes``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo})' />
    public byte[] ExportToCsvBytes<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties);
        _logger.LogDebug("Exporting {ExportType} to csv bytes with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        using var memoryStream = new MemoryStream();
        ExportToCsvStream(data, selectedProperties, memoryStream);
        return memoryStream.ToArray();
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvFromDictionary(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.String,System.Boolean,System.Boolean)' />
    public void ExportToCsvFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, string csvFilePath, bool hasHeaderRow = true, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        _logger.LogDebug("Exporting dictionary to {ExportCsvPath}", csvFilePath);
        using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        WriteDictionaryToCsv(data, writer, hasHeaderRow, hasFooterRow);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStreamFromDictionary(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.IO.Stream,System.Boolean,System.Boolean)' />
    public void ExportToCsvStreamFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, Stream csvStream, bool hasHeaderRow = true, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting dictionary to csv stream");
        using var writer = new StreamWriter(csvStream, Config.Encoding, 8192, true);
        WriteDictionaryToCsv(data, writer, hasHeaderRow, hasFooterRow);
        writer.Flush();
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStringFromDictionary(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.Boolean,System.Boolean)' />
    public string ExportToCsvStringFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(data);
        using var writer = new StringWriter();
        WriteDictionaryToCsv(data, writer, hasHeaderRow, hasFooterRow);
        return writer.ToString();
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvBytesFromDictionary(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.Boolean,System.Boolean)' />
    public byte[] ExportToCsvBytesFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(data);
        using var ms = new MemoryStream();
        ExportToCsvStreamFromDictionary(data, ms, hasHeaderRow, hasFooterRow);
        return ms.ToArray();
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvFromDataTable(Lyo.DataTable.Models.DataTable,System.String)' />
    public void ExportToCsvFromDataTable(DataTable.Models.DataTable dataTable, string csvFilePath)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        _logger.LogDebug("Exporting tabular to {ExportCsvPath}", csvFilePath);
        using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        WriteDataTableToCsv(dataTable, writer);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStreamFromDataTable(Lyo.DataTable.Models.DataTable,System.IO.Stream)' />
    public void ExportToCsvStreamFromDataTable(DataTable.Models.DataTable dataTable, Stream csvStream)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting tabular to csv stream");
        using var writer = new StreamWriter(csvStream, Config.Encoding, 8192, true);
        WriteDataTableToCsv(dataTable, writer);
        writer.Flush();
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStringFromDataTable(Lyo.DataTable.Models.DataTable)' />
    public string ExportToCsvStringFromDataTable(DataTable.Models.DataTable dataTable)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        using var writer = new StringWriter();
        WriteDataTableToCsv(dataTable, writer);
        return writer.ToString();
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvBytesFromDataTable(Lyo.DataTable.Models.DataTable)' />
    public byte[] ExportToCsvBytesFromDataTable(DataTable.Models.DataTable dataTable)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        using var ms = new MemoryStream();
        ExportToCsvStreamFromDataTable(dataTable, ms);
        return ms.ToArray();
    }

    private void WriteDictionaryToCsv(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, TextWriter writer, bool hasHeaderRow, bool hasFooterRow)
    {
        if (data.Count == 0)
            return;

        var options = Config;
        var maxCol = data.Values.SelectMany(r => r.Keys).DefaultIfEmpty(-1).Max() + 1;
        maxCol = Math.Max(maxCol, 1);
        var orderedRows = data.OrderBy(kv => kv.Key).ToList();
        var firstRow = orderedRows[0].Value;
        using var csv = new CsvTextWriter(writer, options);
        if (hasHeaderRow && orderedRows.Count > 0) {
            for (var c = 0; c < maxCol; c++)
                csv.WriteField(firstRow.GetValueOrDefault(c, ""));

            csv.NextRecord();
            orderedRows = orderedRows.Skip(1).ToList();
        }
        else {
            for (var c = 0; c < maxCol; c++)
                csv.WriteField($"Column{c}");

            csv.NextRecord();
        }

        IReadOnlyDictionary<int, string>? footerRow = null;
        if (hasFooterRow && orderedRows.Count > 0) {
            footerRow = orderedRows[^1].Value;
            orderedRows = orderedRows.Take(orderedRows.Count - 1).ToList();
        }

        foreach (var kv in orderedRows) {
            for (var c = 0; c < maxCol; c++)
                csv.WriteField(kv.Value.GetValueOrDefault(c, ""));

            csv.NextRecord();
        }

        if (footerRow != null) {
            for (var c = 0; c < maxCol; c++)
                csv.WriteField(footerRow.GetValueOrDefault(c, ""));

            csv.NextRecord();
        }

        csv.Flush();
    }

    private void WriteDataTableToCsv(DataTable.Models.DataTable dataTable, TextWriter writer)
    {
        var options = Config;
        var maxCol = dataTable.MaxColumn >= 0 ? dataTable.MaxColumn + 1 : 0;
        var orderedHeaders = dataTable.Headers.OrderBy(kv => kv.Key).ToList();
        using var csv = new CsvTextWriter(writer, options);
        for (var c = 0; c < maxCol; c++) {
            var header = orderedHeaders.FirstOrDefault(h => h.Key == c).Value;
            csv.WriteField(header?.DisplayValue ?? "");
        }

        csv.NextRecord();
        foreach (var row in dataTable.Rows) {
            for (var c = 0; c < maxCol; c++) {
                var cell = row.Cells.TryGetValue(c, out var cellVal) ? cellVal : null;
                csv.WriteField(cell?.DisplayValue ?? "");
            }

            csv.NextRecord();
        }

        if (dataTable.Footer.Count > 0) {
            var orderedFooters = dataTable.Footer.OrderBy(kv => kv.Key).ToList();
            for (var c = 0; c < maxCol; c++) {
                var footer = orderedFooters.FirstOrDefault(f => f.Key == c).Value;
                csv.WriteField(footer?.DisplayValue ?? "");
            }

            csv.NextRecord();
        }

        csv.Flush();
    }

    private static string FormatValue(object? value, CultureInfo culture)
        => value switch {
            null => "",
            IFormattable f => f.ToString(null, culture) ?? "",
            var o => o.ToString() ?? ""
        };

#if !NETSTANDARD2_0
    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvAsync``1(System.Collections.Generic.IEnumerable{``0},System.String,System.Threading.CancellationToken)' />
    public async Task ExportToCsvAsync<T>(IEnumerable<T> data, string csvFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        _logger.LogDebug("Exporting {ExportType} to {ExportCsvPath}", typeof(T).FullName, csvFilePath);
        await using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        await ExportToCsvAsync(data, writer, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStreamAsync``1(System.Collections.Generic.IEnumerable{``0},System.IO.Stream,System.Threading.CancellationToken)' />
    public async Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting {ExportType} to csv stream", typeof(T).FullName);
        await using var writer = new StreamWriter(csvStream, Config.Encoding, 8192, true);
        await ExportToCsvAsync(data, writer, ct).ConfigureAwait(false);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvAsync``1(System.Collections.Generic.IEnumerable{``0},System.IO.TextWriter,System.Threading.CancellationToken)' />
    public async Task ExportToCsvAsync<T>(IEnumerable<T> data, TextWriter writer, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(writer);
        _logger.LogDebug("Exporting {ExportType} to csv writer", typeof(T).FullName);
        var options = Config;
        var map = CsvTypeBinder.GetMap<T>();
        await using var csv = new CsvTextWriter(writer, options);
        if (options.HasHeaderRecord) {
            await csv.WriteFieldsAsync(CsvTypeBinder.GetHeaders(map), ct).ConfigureAwait(false);
            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        foreach (var item in data) {
            ct.ThrowIfCancellationRequested();
            await csv.WriteFieldsAsync(CsvTypeBinder.GetFieldValues(item, map, options.Culture), ct).ConfigureAwait(false);
            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        await csv.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Asynchronously streams <paramref name="data" /> to <paramref name="csvFilePath" /> without buffering the full sequence.</summary>
    public async Task ExportToCsvAsync<T>(IAsyncEnumerable<T> data, string csvFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        _logger.LogDebug("Exporting {ExportType} to {ExportCsvPath}", typeof(T).FullName, csvFilePath);
        await using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        await ExportToCsvAsync(data, writer, ct).ConfigureAwait(false);
    }

    /// <summary>Asynchronously streams <paramref name="data" /> to <paramref name="csvStream" /> without buffering the full sequence.</summary>
    public async Task ExportToCsvStreamAsync<T>(IAsyncEnumerable<T> data, Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting {ExportType} to csv stream", typeof(T).FullName);
        await using var writer = new StreamWriter(csvStream, Config.Encoding, 8192, true);
        await ExportToCsvAsync(data, writer, ct).ConfigureAwait(false);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Asynchronously streams <paramref name="data" /> to <paramref name="writer" /> without buffering the full sequence.</summary>
    public async Task ExportToCsvAsync<T>(IAsyncEnumerable<T> data, TextWriter writer, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(writer);
        _logger.LogDebug("Exporting {ExportType} to csv writer", typeof(T).FullName);
        var options = Config;
        var map = CsvTypeBinder.GetMap<T>();
        await using var csv = new CsvTextWriter(writer, options);
        if (options.HasHeaderRecord) {
            await csv.WriteFieldsAsync(CsvTypeBinder.GetHeaders(map), ct).ConfigureAwait(false);
            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        await foreach (var item in data.WithCancellation(ct).ConfigureAwait(false)) {
            await csv.WriteFieldsAsync(CsvTypeBinder.GetFieldValues(item, map, options.Culture), ct).ConfigureAwait(false);
            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        await csv.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStringAsync``1(System.Collections.Generic.IEnumerable{``0},System.Threading.CancellationToken)' />
    public async Task<string> ExportToCsvStringAsync<T>(IEnumerable<T> data, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        _logger.LogDebug("Exporting {ExportType} to csv string", typeof(T).FullName);
        await using var writer = new StringWriter();
        await ExportToCsvAsync(data, writer, ct).ConfigureAwait(false);
        return writer.ToString();
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvBytesAsync``1(System.Collections.Generic.IEnumerable{``0},System.Threading.CancellationToken)' />
    public async Task<byte[]> ExportToCsvBytesAsync<T>(IEnumerable<T> data, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        _logger.LogDebug("Exporting {ExportType} to csv bytes", typeof(T).FullName);
        await using var memoryStream = new MemoryStream();
        await ExportToCsvStreamAsync(data, memoryStream, ct).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.String,System.Threading.CancellationToken)' />
    public async Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string csvFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        _logger.LogDebug("Exporting {ExportType} to {ExportCsvPath} with {PropertyCount} selected properties", typeof(T).FullName, csvFilePath, selectedProperties.Count);
        await using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        await ExportToCsvAsync(data, selectedProperties, writer, ct).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvFromDictionaryAsync(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.String,System.Boolean,System.Boolean,System.Threading.CancellationToken)' />
    public async Task ExportToCsvFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        string csvFilePath,
        bool hasHeaderRow = true,
        bool hasFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        _logger.LogDebug("Exporting dictionary to {ExportCsvPath}", csvFilePath);
        await using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        await WriteDictionaryToCsvAsync(data, writer, hasHeaderRow, hasFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStreamFromDictionaryAsync(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.IO.Stream,System.Boolean,System.Boolean,System.Threading.CancellationToken)' />
    public async Task ExportToCsvStreamFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        Stream csvStream,
        bool hasHeaderRow = true,
        bool hasFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting dictionary to csv stream");
        await using var writer = new StreamWriter(csvStream, Config.Encoding, 8192, true);
        await WriteDictionaryToCsvAsync(data, writer, hasHeaderRow, hasFooterRow, ct).ConfigureAwait(false);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStringFromDictionaryAsync(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.Boolean,System.Boolean,System.Threading.CancellationToken)' />
    public async Task<string> ExportToCsvStringFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        bool hasHeaderRow = true,
        bool hasFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        await using var writer = new StringWriter();
        await WriteDictionaryToCsvAsync(data, writer, hasHeaderRow, hasFooterRow, ct).ConfigureAwait(false);
        return writer.ToString();
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvBytesFromDictionaryAsync(System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.Collections.Generic.IReadOnlyDictionary{System.Int32,System.String}},System.Boolean,System.Boolean,System.Threading.CancellationToken)' />
    public async Task<byte[]> ExportToCsvBytesFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        bool hasHeaderRow = true,
        bool hasFooterRow = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        await using var ms = new MemoryStream();
        await ExportToCsvStreamFromDictionaryAsync(data, ms, hasHeaderRow, hasFooterRow, ct).ConfigureAwait(false);
        return ms.ToArray();
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvFromDataTableAsync(Lyo.DataTable.Models.DataTable,System.String,System.Threading.CancellationToken)' />
    public async Task ExportToCsvFromDataTableAsync(DataTable.Models.DataTable dataTable, string csvFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        _logger.LogDebug("Exporting tabular to {ExportCsvPath}", csvFilePath);
        await using var writer = new StreamWriter(csvFilePath, false, Config.Encoding);
        await WriteDataTableToCsvAsync(dataTable, writer, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStreamFromDataTableAsync(Lyo.DataTable.Models.DataTable,System.IO.Stream,System.Threading.CancellationToken)' />
    public async Task ExportToCsvStreamFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting tabular to csv stream");
        await using var writer = new StreamWriter(csvStream, Config.Encoding, 8192, true);
        await WriteDataTableToCsvAsync(dataTable, writer, ct).ConfigureAwait(false);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStringFromDataTableAsync(Lyo.DataTable.Models.DataTable,System.Threading.CancellationToken)' />
    public async Task<string> ExportToCsvStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        await using var writer = new StringWriter();
        await WriteDataTableToCsvAsync(dataTable, writer, ct).ConfigureAwait(false);
        return writer.ToString();
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvBytesFromDataTableAsync(Lyo.DataTable.Models.DataTable,System.Threading.CancellationToken)' />
    public async Task<byte[]> ExportToCsvBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        await using var ms = new MemoryStream();
        await ExportToCsvStreamFromDataTableAsync(dataTable, ms, ct).ConfigureAwait(false);
        return ms.ToArray();
    }

    private async Task WriteDictionaryToCsvAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        TextWriter writer,
        bool hasHeaderRow,
        bool hasFooterRow,
        CancellationToken ct)
    {
        if (data.Count == 0)
            return;

        var options = Config;
        var maxCol = data.Values.SelectMany(r => r.Keys).DefaultIfEmpty(-1).Max() + 1;
        maxCol = Math.Max(maxCol, 1);
        var orderedRows = data.OrderBy(kv => kv.Key).ToList();
        var firstRow = orderedRows[0].Value;
        await using var csv = new CsvTextWriter(writer, options);
        if (hasHeaderRow && orderedRows.Count > 0) {
            for (var c = 0; c < maxCol; c++)
                await csv.WriteFieldAsync(firstRow.GetValueOrDefault(c, ""), ct).ConfigureAwait(false);

            await csv.NextRecordAsync(ct).ConfigureAwait(false);
            orderedRows = orderedRows.Skip(1).ToList();
        }
        else {
            for (var c = 0; c < maxCol; c++)
                await csv.WriteFieldAsync($"Column{c}", ct).ConfigureAwait(false);

            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        IReadOnlyDictionary<int, string>? footerRow = null;
        if (hasFooterRow && orderedRows.Count > 0) {
            footerRow = orderedRows[^1].Value;
            orderedRows = orderedRows.Take(orderedRows.Count - 1).ToList();
        }

        foreach (var kv in orderedRows) {
            ct.ThrowIfCancellationRequested();
            for (var c = 0; c < maxCol; c++)
                await csv.WriteFieldAsync(kv.Value.GetValueOrDefault(c, ""), ct).ConfigureAwait(false);

            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        if (footerRow != null) {
            for (var c = 0; c < maxCol; c++)
                await csv.WriteFieldAsync(footerRow.GetValueOrDefault(c, ""), ct).ConfigureAwait(false);

            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        await csv.FlushAsync(ct).ConfigureAwait(false);
    }

    private async Task WriteDataTableToCsvAsync(DataTable.Models.DataTable dataTable, TextWriter writer, CancellationToken ct)
    {
        var options = Config;
        var maxCol = dataTable.MaxColumn >= 0 ? dataTable.MaxColumn + 1 : 0;
        var orderedHeaders = dataTable.Headers.OrderBy(kv => kv.Key).ToList();
        await using var csv = new CsvTextWriter(writer, options);
        for (var c = 0; c < maxCol; c++) {
            var header = orderedHeaders.FirstOrDefault(h => h.Key == c).Value;
            await csv.WriteFieldAsync(header?.DisplayValue ?? "", ct).ConfigureAwait(false);
        }

        await csv.NextRecordAsync(ct).ConfigureAwait(false);
        foreach (var row in dataTable.Rows) {
            ct.ThrowIfCancellationRequested();
            for (var c = 0; c < maxCol; c++) {
                var cell = row.Cells!.GetValueOrDefault(c, null);
                await csv.WriteFieldAsync(cell?.DisplayValue ?? "", ct).ConfigureAwait(false);
            }

            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        if (dataTable.Footer.Count > 0) {
            var orderedFooters = dataTable.Footer.OrderBy(kv => kv.Key).ToList();
            for (var c = 0; c < maxCol; c++) {
                var footer = orderedFooters.FirstOrDefault(f => f.Key == c).Value;
                await csv.WriteFieldAsync(footer?.DisplayValue ?? "", ct).ConfigureAwait(false);
            }

            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        await csv.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStreamAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.IO.Stream,System.Threading.CancellationToken)' />
    public async Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties);
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting {ExportType} to csv stream with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        await using var writer = new StreamWriter(csvStream, Config.Encoding, 8192, true);
        await ExportToCsvAsync(data, selectedProperties, writer, ct).ConfigureAwait(false);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Asynchronously streams selected properties from <paramref name="data" /> without buffering the full sequence.</summary>
    public async Task ExportToCsvStreamAsync<T>(IAsyncEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties);
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting {ExportType} to csv stream with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        var options = Config;
        await using var writer = new StreamWriter(csvStream, options.Encoding, 8192, true);
        await using var csv = new CsvTextWriter(writer, options);
        foreach (var prop in selectedProperties)
            await csv.WriteFieldAsync(prop.Name, ct).ConfigureAwait(false);

        await csv.NextRecordAsync(ct).ConfigureAwait(false);
        await foreach (var item in data.WithCancellation(ct).ConfigureAwait(false)) {
            foreach (var prop in selectedProperties)
                await csv.WriteFieldAsync(FormatValue(prop.GetValue(item), options.Culture), ct).ConfigureAwait(false);

            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        await csv.FlushAsync(ct).ConfigureAwait(false);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.IO.TextWriter,System.Threading.CancellationToken)' />
    public async Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, TextWriter writer, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties);
        _logger.LogDebug("Exporting {ExportType} to csv writer with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        var options = Config;
        await using var csv = new CsvTextWriter(writer, options);
        foreach (var prop in selectedProperties)
            await csv.WriteFieldAsync(prop.Name, ct).ConfigureAwait(false);

        await csv.NextRecordAsync(ct).ConfigureAwait(false);
        foreach (var item in data) {
            ct.ThrowIfCancellationRequested();
            foreach (var prop in selectedProperties)
                await csv.WriteFieldAsync(FormatValue(prop.GetValue(item), options.Culture), ct).ConfigureAwait(false);

            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        await csv.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStreamAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyDictionary{System.String,System.Reflection.PropertyInfo},System.IO.Stream,System.Threading.CancellationToken)' />
    public async Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, PropertyInfo> columns, Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrEmpty(columns);
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting {ExportType} to csv stream with {ColumnCount} custom columns", typeof(T).FullName, columns.Count);
        await using var writer = new StreamWriter(csvStream, Config.Encoding, 8192, true);
        await ExportToCsvAsync(data, columns, writer, ct).ConfigureAwait(false);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Asynchronously streams custom columns from <paramref name="data" /> without buffering the full sequence.</summary>
    public async Task ExportToCsvStreamAsync<T>(IAsyncEnumerable<T> data, IReadOnlyDictionary<string, PropertyInfo> columns, Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrEmpty(columns);
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting {ExportType} to csv stream with {ColumnCount} custom columns", typeof(T).FullName, columns.Count);
        var options = Config;
        await using var writer = new StreamWriter(csvStream, options.Encoding, 8192, true);
        await using var csv = new CsvTextWriter(writer, options);
        foreach (var header in columns.Keys)
            await csv.WriteFieldAsync(header, ct).ConfigureAwait(false);

        await csv.NextRecordAsync(ct).ConfigureAwait(false);
        await foreach (var item in data.WithCancellation(ct).ConfigureAwait(false)) {
            foreach (var prop in columns.Values)
                await csv.WriteFieldAsync(FormatValue(prop.GetValue(item), options.Culture), ct).ConfigureAwait(false);

            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        await csv.FlushAsync(ct).ConfigureAwait(false);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Asynchronously writes rows using custom column headers (property map) to <paramref name="writer" />.</summary>
    public async Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, PropertyInfo> columns, TextWriter writer, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(columns);
        _logger.LogDebug("Exporting {ExportType} to csv writer with {ColumnCount} custom columns", typeof(T).FullName, columns.Count);
        var options = Config;
        await using var csv = new CsvTextWriter(writer, options);
        foreach (var header in columns.Keys)
            await csv.WriteFieldAsync(header, ct).ConfigureAwait(false);

        await csv.NextRecordAsync(ct).ConfigureAwait(false);
        foreach (var item in data) {
            ct.ThrowIfCancellationRequested();
            foreach (var prop in columns.Values)
                await csv.WriteFieldAsync(FormatValue(prop.GetValue(item), options.Culture), ct).ConfigureAwait(false);

            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        await csv.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStreamAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyDictionary{System.String,System.Func{``0,System.String}},System.IO.Stream,System.Threading.CancellationToken)' />
    public async Task ExportToCsvStreamAsync<T>(
        IEnumerable<T> data,
        IReadOnlyDictionary<string, Func<T, string>> columnFormatters,
        Stream csvStream,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrEmpty(columnFormatters);
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting {ExportType} to csv stream with {ColumnCount} formatter columns", typeof(T).FullName, columnFormatters.Count);
        await using var writer = new StreamWriter(csvStream, Config.Encoding, 8192, true);
        await ExportToCsvAsync(data, columnFormatters, writer, ct).ConfigureAwait(false);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Asynchronously streams formatter columns from <paramref name="data" /> without buffering the full sequence.</summary>
    public async Task ExportToCsvStreamAsync<T>(
        IAsyncEnumerable<T> data,
        IReadOnlyDictionary<string, Func<T, string>> columnFormatters,
        Stream csvStream,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrEmpty(columnFormatters);
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        _logger.LogDebug("Exporting {ExportType} to csv stream with {ColumnCount} formatter columns", typeof(T).FullName, columnFormatters.Count);
        var options = Config;
        await using var writer = new StreamWriter(csvStream, options.Encoding, 8192, true);
        await using var csv = new CsvTextWriter(writer, options);
        foreach (var header in columnFormatters.Keys)
            await csv.WriteFieldAsync(header, ct).ConfigureAwait(false);

        await csv.NextRecordAsync(ct).ConfigureAwait(false);
        var formatters = columnFormatters.Values.ToList();
        await foreach (var item in data.WithCancellation(ct).ConfigureAwait(false)) {
            foreach (var formatter in formatters)
                await csv.WriteFieldAsync(formatter(item), ct).ConfigureAwait(false);

            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        await csv.FlushAsync(ct).ConfigureAwait(false);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Asynchronously writes rows using per-column formatters to <paramref name="writer" />.</summary>
    public async Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, Func<T, string>> columnFormatters, TextWriter writer, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(columnFormatters);
        _logger.LogDebug("Exporting {ExportType} to csv writer with {ColumnCount} formatter columns", typeof(T).FullName, columnFormatters.Count);
        var options = Config;
        await using var csv = new CsvTextWriter(writer, options);
        foreach (var header in columnFormatters.Keys)
            await csv.WriteFieldAsync(header, ct).ConfigureAwait(false);

        await csv.NextRecordAsync(ct).ConfigureAwait(false);
        var formatters = columnFormatters.Values.ToList();
        foreach (var item in data) {
            ct.ThrowIfCancellationRequested();
            foreach (var formatter in formatters)
                await csv.WriteFieldAsync(formatter(item), ct).ConfigureAwait(false);

            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        await csv.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStringAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.Threading.CancellationToken)' />
    public async Task<string> ExportToCsvStringAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties);
        _logger.LogDebug("Exporting {ExportType} to csv string with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        await using var writer = new StringWriter();
        await ExportToCsvAsync(data, selectedProperties, writer, ct).ConfigureAwait(false);
        return writer.ToString();
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvBytesAsync``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IReadOnlyList{System.Reflection.PropertyInfo},System.Threading.CancellationToken)' />
    public async Task<byte[]> ExportToCsvBytesAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(selectedProperties);
        _logger.LogDebug("Exporting {ExportType} to csv bytes with {PropertyCount} selected properties", typeof(T).FullName, selectedProperties.Count);
        await using var memoryStream = new MemoryStream();
        await ExportToCsvStreamAsync(data, selectedProperties, memoryStream, ct).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvWithProgressAsync``1(System.Collections.Generic.IEnumerable{``0},System.String,System.IProgress{Lyo.Csv.Models.CsvProgress},System.Threading.CancellationToken)' />
    public async Task ExportToCsvWithProgressAsync<T>(IEnumerable<T> data, string csvFilePath, IProgress<CsvProgress>? progress, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        var dataList = data.ToList();
        var totalRows = dataList.Count;
        var options = Config;
        var map = CsvTypeBinder.GetMap<T>();
        await using var writer = new StreamWriter(csvFilePath, false, options.Encoding);
        await using var csv = new CsvTextWriter(writer, options);
        if (options.HasHeaderRecord) {
            await csv.WriteFieldsAsync(CsvTypeBinder.GetHeaders(map), ct).ConfigureAwait(false);
            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        long rowsProcessed = 0;
        foreach (var item in dataList) {
            ct.ThrowIfCancellationRequested();
            await csv.WriteFieldsAsync(CsvTypeBinder.GetFieldValues(item, map, options.Culture), ct).ConfigureAwait(false);
            await csv.NextRecordAsync(ct).ConfigureAwait(false);
            rowsProcessed++;
            if ((progress != null && rowsProcessed % 100 == 0) || rowsProcessed == totalRows)
                progress!.Report(new() { RowsProcessed = rowsProcessed, TotalRows = totalRows, Operation = "Exporting" });
        }

        await csv.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvWriter.ExportToCsvStreamWithProgressAsync``1(System.Collections.Generic.IEnumerable{``0},System.IO.Stream,System.IProgress{Lyo.Csv.Models.CsvProgress},System.Threading.CancellationToken)' />
    public async Task ExportToCsvStreamWithProgressAsync<T>(IEnumerable<T> data, Stream csvStream, IProgress<CsvProgress>? progress, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotWritable(csvStream, $"Stream '{nameof(csvStream)}' must be writable.");
        var dataList = data.ToList();
        var totalRows = dataList.Count;
        var options = Config;
        var map = CsvTypeBinder.GetMap<T>();
        await using var writer = new StreamWriter(csvStream, options.Encoding, 8192, true);
        await using var csv = new CsvTextWriter(writer, options);
        if (options.HasHeaderRecord) {
            await csv.WriteFieldsAsync(CsvTypeBinder.GetHeaders(map), ct).ConfigureAwait(false);
            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        long rowsProcessed = 0;
        foreach (var item in dataList) {
            ct.ThrowIfCancellationRequested();
            await csv.WriteFieldsAsync(CsvTypeBinder.GetFieldValues(item, map, options.Culture), ct).ConfigureAwait(false);
            await csv.NextRecordAsync(ct).ConfigureAwait(false);
            rowsProcessed++;
            if (progress != null && (rowsProcessed % 100 == 0 || rowsProcessed == totalRows))
                progress.Report(new() { RowsProcessed = rowsProcessed, TotalRows = totalRows, Operation = "Exporting" });
        }

        await csv.FlushAsync(ct).ConfigureAwait(false);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvWriter.AppendToCsvAsync``1(System.Collections.Generic.IEnumerable{``0},System.String,System.Boolean,System.Threading.CancellationToken)' />
    public async Task AppendToCsvAsync<T>(IEnumerable<T> data, string csvFilePath, bool includeHeaderIfMissing = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        var fileExists = File.Exists(csvFilePath);
        var fileIsEmpty = fileExists && new FileInfo(csvFilePath).Length == 0;
        var options = Config;
        var map = CsvTypeBinder.GetMap<T>();
        await using var stream = new FileStream(csvFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        await using var writer = new StreamWriter(stream, options.Encoding);
        await using var csv = new CsvTextWriter(writer, options);
        if ((!fileExists || fileIsEmpty) && includeHeaderIfMissing) {
            await csv.WriteFieldsAsync(CsvTypeBinder.GetHeaders(map), ct).ConfigureAwait(false);
            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        foreach (var item in data) {
            ct.ThrowIfCancellationRequested();
            await csv.WriteFieldsAsync(CsvTypeBinder.GetFieldValues(item, map, options.Culture), ct).ConfigureAwait(false);
            await csv.NextRecordAsync(ct).ConfigureAwait(false);
        }

        await csv.FlushAsync(ct).ConfigureAwait(false);
    }
#endif
}
