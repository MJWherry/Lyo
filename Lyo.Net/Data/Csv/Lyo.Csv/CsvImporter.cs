using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;
using Lyo.Common;
using Lyo.Csv.Models;
using Lyo.DataTable.Models;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
#if NET10_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace Lyo.Csv;

internal sealed class CsvImporter : ICsvImporter
{
    private readonly List<Type> _classMapTypes;
    private readonly Func<CsvConfiguration> _getConfig;
    private readonly ILogger _logger;

    private CsvConfiguration Config => _getConfig();

    internal CsvImporter(Func<CsvConfiguration> getConfig, List<Type> classMapTypes, ILogger logger)
    {
        _getConfig = getConfig;
        _classMapTypes = classMapTypes;
        _logger = logger;
    }

    public IEnumerable<T> ParseFile<T>(string csvFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath, nameof(csvFilePath));
        _logger.LogDebug("Parsing {ParsingCsvPath} as {ParsingType}", csvFilePath, typeof(T).FullName);
        using var reader = new StreamReader(csvFilePath);
        return ParseReader<T>(reader);
    }

    public IEnumerable<T> ParseStream<T>(Stream csvStream)
    {
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        if (csvStream.CanSeek && csvStream.Position > 0)
            csvStream.Position = 0;

        _logger.LogDebug("Parsing csv stream as {ParsingType}", typeof(T).FullName);
        using var reader = new StreamReader(csvStream, Config.Encoding, true, 1024, true);
        return ParseReader<T>(reader);
    }

    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseFileAsDictionary(string csvFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath, nameof(csvFilePath));
        _logger.LogDebug("Parsing {ParsingCsvPath} as dictionary", csvFilePath);
        using var reader = new StreamReader(csvFilePath);
        return ParseReaderAsDictionary(reader);
    }

    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseStreamAsDictionary(Stream csvStream)
    {
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        if (csvStream.CanSeek && csvStream.Position > 0)
            csvStream.Position = 0;

        _logger.LogDebug("Parsing csv stream as dictionary");
        using var reader = new StreamReader(csvStream, Config.Encoding, true, 1024, true);
        return ParseReaderAsDictionary(reader);
    }

    public Result<DataTable.Models.DataTable> ParseFileAsDataTable(string csvFilePath, bool? hasHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath, nameof(csvFilePath));
        _logger.LogDebug("Parsing {ParsingCsvPath} as DataTable", csvFilePath);
        using var reader = new StreamReader(csvFilePath);
        return ParseReaderAsDataTable(reader, hasHeaderRow);
    }

    public Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream csvStream, bool? hasHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        if (csvStream.CanSeek && csvStream.Position > 0)
            csvStream.Position = 0;

        _logger.LogDebug("Parsing csv stream as DataTable");
        using var reader = new StreamReader(csvStream, Config.Encoding, true, 1024, true);
        return ParseReaderAsDataTable(reader, hasHeaderRow);
    }

    public Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] csvBytes, bool? hasHeaderRow = null)
    {
        ArgumentHelpers.ThrowIfNull(csvBytes, nameof(csvBytes));
        using var ms = new MemoryStream(csvBytes);
        return ParseStreamAsDataTable(ms, hasHeaderRow);
    }

    public IEnumerable<T> ParseBytes<T>(byte[] csvBytes)
    {
        ArgumentHelpers.ThrowIfNull(csvBytes, nameof(csvBytes));
        using var ms = new MemoryStream(csvBytes);
        return ParseStream<T>(ms);
    }

    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseBytesAsDictionary(byte[] csvBytes)
    {
        ArgumentHelpers.ThrowIfNull(csvBytes, nameof(csvBytes));
        using var ms = new MemoryStream(csvBytes);
        return ParseStreamAsDictionary(ms);
    }

    private void RegisterClassMaps(CsvReader csv)
    {
        foreach (var mapType in _classMapTypes)
            csv.Context.RegisterClassMap(mapType);
    }

    private IEnumerable<T> ParseReader<T>(TextReader reader)
    {
        using var csv = new CsvReader(reader, Config);
        RegisterClassMaps(csv);
        return csv.GetRecords<T>().ToList();
    }

    private IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseReaderAsDictionary(TextReader reader)
    {
        using var csv = new CsvReader(reader, Config);
        var result = new Dictionary<int, IReadOnlyDictionary<int, string>>();
        var rowIndex = 0;
        while (csv.Read()) {
            var rowData = new Dictionary<int, string>();
            for (var i = 0; i < csv.Context.Reader?.ColumnCount; i++) {
                var value = csv.GetField(i) ?? string.Empty;
                rowData[i] = value;
            }

            result[rowIndex] = rowData;
            rowIndex++;
        }

        return result;
    }

    private Result<DataTable.Models.DataTable> ParseReaderAsDataTable(TextReader reader, bool? hasHeaderRow)
    {
        var dict = ParseReaderAsDictionary(reader);
        var dt = DictToDataTable(dict, hasHeaderRow ?? Config.HasHeaderRecord);
        return Result<DataTable.Models.DataTable>.Success(dt);
    }

    private static DataTable.Models.DataTable DictToDataTable(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> dict, bool useFirstRowAsHeader)
    {
        IReadOnlyDictionary<int, string> headers;
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> rows;
        if (useFirstRowAsHeader && dict.Count > 0 && dict.TryGetValue(0, out var headerRow)) {
            headers = headerRow;
            var rowsDict = new Dictionary<int, IReadOnlyDictionary<int, string>>();
            for (var i = 1; i < dict.Count; i++)
                rowsDict[i - 1] = dict[i];

            rows = rowsDict;
        }
        else {
            var maxCol = dict.Values.Select(r => r.Count).DefaultIfEmpty(0).Max();
            headers = Enumerable.Range(0, maxCol).ToDictionary(i => i, i => $"Column{i}");
            var rowsDict = new Dictionary<int, IReadOnlyDictionary<int, string>>();
            foreach (var kv in dict)
                rowsDict[kv.Key] = kv.Value;

            rows = rowsDict;
        }

        var dt = new DataTable.Models.DataTable();
        foreach (var kv in headers)
            dt.SetHeader(kv.Key, DataTableCell.FromValue(kv.Value ?? ""));

        foreach (var rowKv in rows.OrderBy(r => r.Key)) {
            var dataRow = dt.AddRow();
            foreach (var colKv in rowKv.Value)
                dataRow.SetCell(colKv.Key, DataTableCell.FromValue(colKv.Value ?? ""));
        }

        return dt;
    }

#if !NETSTANDARD2_0
    public async Task<List<T>> ParseFileAsync<T>(string csvFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath, nameof(csvFilePath));
        _logger.LogDebug("Parsing {ParsingCsvPath} as {ParsingType}", csvFilePath, typeof(T).FullName);
        await using var stream = File.OpenRead(csvFilePath);
        return await ParseStreamAsync<T>(stream, ct).ConfigureAwait(false);
    }

    public async Task<List<T>> ParseStreamAsync<T>(Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        if (csvStream.CanSeek && csvStream.Position > 0)
            csvStream.Position = 0;

        _logger.LogDebug("Parsing csv stream as {ParsingType}", typeof(T).FullName);
        using var reader = new StreamReader(csvStream, Config.Encoding, true, 1024, true);
        using var csv = new CsvReader(reader, Config);
        RegisterClassMaps(csv);
        var records = new List<T>();
        await foreach (var record in csv.GetRecordsAsync<T>(ct).ConfigureAwait(false))
            records.Add(record);

        return records;
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseFileAsDictionaryAsync(string csvFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath, nameof(csvFilePath));
        _logger.LogDebug("Parsing {ParsingCsvPath} as dictionary", csvFilePath);
        await using var stream = File.OpenRead(csvFilePath);
        return await ParseStreamAsDictionaryAsync(stream, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseStreamAsDictionaryAsync(Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        if (csvStream.CanSeek && csvStream.Position > 0)
            csvStream.Position = 0;

        _logger.LogDebug("Parsing csv stream as dictionary");
        using var reader = new StreamReader(csvStream, Config.Encoding, true, 1024, true);
        return await ParseReaderAsDictionaryAsync(reader, ct).ConfigureAwait(false);
    }

    public async Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string csvFilePath, bool? hasHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath, nameof(csvFilePath));
        _logger.LogDebug("Parsing {ParsingCsvPath} as DataTable", csvFilePath);
        await using var stream = File.OpenRead(csvFilePath);
        return await ParseStreamAsDataTableAsync(stream, hasHeaderRow, ct).ConfigureAwait(false);
    }

    public async Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream csvStream, bool? hasHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        if (csvStream.CanSeek && csvStream.Position > 0)
            csvStream.Position = 0;

        _logger.LogDebug("Parsing csv stream as DataTable");
        using var reader = new StreamReader(csvStream, Config.Encoding, true, 1024, true);
        var dict = await ParseReaderAsDictionaryAsync(reader, ct).ConfigureAwait(false);
        var dt = DictToDataTable(dict, hasHeaderRow ?? Config.HasHeaderRecord);
        return Result<DataTable.Models.DataTable>.Success(dt);
    }

    public async Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] csvBytes, bool? hasHeaderRow = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvBytes, nameof(csvBytes));
        using var ms = new MemoryStream(csvBytes);
        return await ParseStreamAsDataTableAsync(ms, hasHeaderRow, ct).ConfigureAwait(false);
    }

    public async Task<List<T>> ParseBytesAsync<T>(byte[] csvBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvBytes, nameof(csvBytes));
        using var ms = new MemoryStream(csvBytes);
        return await ParseStreamAsync<T>(ms, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseBytesAsDictionaryAsync(byte[] csvBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvBytes, nameof(csvBytes));
        using var ms = new MemoryStream(csvBytes);
        return await ParseStreamAsDictionaryAsync(ms, ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseReaderAsDictionaryAsync(TextReader reader, CancellationToken ct = default)
    {
        using var csv = new CsvReader(reader, Config);
        var result = new Dictionary<int, IReadOnlyDictionary<int, string>>();
        var rowIndex = 0;
        while (await csv.ReadAsync().ConfigureAwait(false)) {
            ct.ThrowIfCancellationRequested();
            var rowData = new Dictionary<int, string>();
            for (var i = 0; i < csv.Context.Reader?.ColumnCount; i++) {
                var value = csv.GetField(i) ?? string.Empty;
                rowData[i] = value;
            }

            result[rowIndex] = rowData;
            rowIndex++;
        }

        return result;
    }

#if NET10_0_OR_GREATER
    public async IAsyncEnumerable<T> ParseFileStreamingAsync<T>(string csvFilePath, CsvParseOptions? options = null, [EnumeratorCancellation] CancellationToken ct = default)
#else
    public async IAsyncEnumerable<T> ParseFileStreamingAsync<T>(string csvFilePath, CsvParseOptions? options = null, CancellationToken ct = default)
#endif
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath, nameof(csvFilePath));
        await using var stream = File.OpenRead(csvFilePath);
        await foreach (var record in ParseStreamStreamingAsync<T>(stream, options, ct).ConfigureAwait(false))
            yield return record;
    }

#if NET10_0_OR_GREATER
    public async IAsyncEnumerable<T> ParseStreamStreamingAsync<T>(Stream csvStream, CsvParseOptions? options = null, [EnumeratorCancellation] CancellationToken ct = default)
#else
    public async IAsyncEnumerable<T> ParseStreamStreamingAsync<T>(Stream csvStream, CsvParseOptions? options = null, CancellationToken ct = default)
#endif
    {
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        if (csvStream.CanSeek && csvStream.Position > 0)
            csvStream.Position = 0;

        using var reader = new StreamReader(csvStream, Config.Encoding, true, 1024, true);
        using var csv = new CsvReader(reader, Config);
        RegisterClassMaps(csv);
        var rowNumber = 0;
        var maxRows = options?.MaxRows;
        while (await csv.ReadAsync().ConfigureAwait(false)) {
            ct.ThrowIfCancellationRequested();
            if (maxRows.HasValue && rowNumber >= maxRows.Value)
                yield break;

            T? record = default;
            Exception? parseException = null;
            if (options?.ContinueOnError == true) {
                try {
                    record = csv.GetRecord<T>();
                }
                catch (Exception ex) {
                    parseException = ex;
                }
            }
            else
                record = csv.GetRecord<T>();

            if (parseException != null) {
                var error = new CsvParseError {
                    RowNumber = rowNumber + 1,
                    RawRecord = csv.Context.Parser?.RawRecord,
                    Exception = parseException,
                    ColumnIndex = csv.Context.Reader?.CurrentIndex
                };

                options?.OnError?.Invoke(error);
                continue;
            }

            if (record == null)
                continue;

            if (options?.RowFilter != null) {
                var rowDict = new Dictionary<string, string>();
                for (var i = 0; i < csv.Context.Reader?.ColumnCount; i++) {
                    var header = csv.Context.Reader?.HeaderRecord?[i] ?? $"Column{i}";
                    rowDict[header] = csv.GetField(i) ?? string.Empty;
                }

                if (!options.RowFilter(rowDict))
                    continue;
            }

            yield return record;

            rowNumber++;
        }
    }

    public async Task<List<T>> ParseFileWithOptionsAsync<T>(string csvFilePath, CsvParseOptions? options, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath, nameof(csvFilePath));
        await using var stream = File.OpenRead(csvFilePath);
        return await ParseStreamWithOptionsAsync<T>(stream, options, ct).ConfigureAwait(false);
    }

    public async Task<List<T>> ParseStreamWithOptionsAsync<T>(Stream csvStream, CsvParseOptions? options, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        if (csvStream.CanSeek && csvStream.Position > 0)
            csvStream.Position = 0;

        using var reader = new StreamReader(csvStream, Config.Encoding, true, 1024, true);
        using var csv = new CsvReader(reader, Config);
        RegisterClassMaps(csv);
        var records = new List<T>();
        var rowNumber = 0;
        var maxRows = options?.MaxRows;
        while (await csv.ReadAsync().ConfigureAwait(false)) {
            ct.ThrowIfCancellationRequested();
            if (maxRows.HasValue && rowNumber >= maxRows.Value)
                break;

            try {
                var record = csv.GetRecord<T>();
                if (record != null) {
                    if (options?.RowFilter != null) {
                        var rowDict = new Dictionary<string, string>();
                        for (var i = 0; i < csv.Context.Reader?.ColumnCount; i++) {
                            var header = csv.Context.Reader?.HeaderRecord?[i] ?? $"Column{i}";
                            rowDict[header] = csv.GetField(i) ?? string.Empty;
                        }

                        if (!options.RowFilter(rowDict))
                            continue;
                    }

                    records.Add(record);
                    rowNumber++;
                }
            }
            catch (Exception ex) when (options?.ContinueOnError == true) {
                var error = new CsvParseError {
                    RowNumber = rowNumber + 1,
                    RawRecord = csv.Context.Parser?.RawRecord,
                    Exception = ex,
                    ColumnIndex = csv.Context.Reader?.CurrentIndex
                };

                options.OnError?.Invoke(error);
            }
            catch when (options?.ContinueOnError != true) {
                throw;
            }
        }

        return records;
    }

    public async Task<CsvStatistics> GetStatisticsAsync(string csvFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath, nameof(csvFilePath));
        var fileInfo = new FileInfo(csvFilePath);
        await using var stream = File.OpenRead(csvFilePath);
        var stats = await GetStatisticsAsync(stream, ct).ConfigureAwait(false);
        stats.FileSizeBytes = fileInfo.Length;
        return stats;
    }

    public async Task<CsvStatistics> GetStatisticsAsync(Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        if (csvStream.CanSeek && csvStream.Position > 0)
            csvStream.Position = 0;

        var stats = new CsvStatistics {
            DetectedEncoding = Config.Encoding, DetectedDelimiter = !string.IsNullOrEmpty(Config.Delimiter) && Config.Delimiter.Length > 0 ? Config.Delimiter[0] : null
        };

        stats.HasHeaderRow = Config.HasHeaderRecord;
        string[]? headerArray = null;
        if (stats.HasHeaderRow && csvStream.CanSeek) {
            var originalPosition = csvStream.Position;
            try {
                csvStream.Position = 0;
                using var headerReader = new StreamReader(csvStream, Config.Encoding, true, 1024, true);
                var headerLine = await headerReader.ReadLineAsync().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(headerLine)) {
                    using var headerStringReader = new StringReader(headerLine);
                    using var headerCsv = new CsvReader(headerStringReader, Config);
                    if (await headerCsv.ReadAsync().ConfigureAwait(false)) {
                        headerArray = new string[headerCsv.Context.Reader?.ColumnCount ?? 0];
                        for (var i = 0; i < headerArray.Length; i++)
                            headerArray[i] = headerCsv.GetField(i) ?? $"Column{i}";
                    }
                }
            }
            finally {
                csvStream.Position = originalPosition;
            }
        }

        using var reader = new StreamReader(csvStream, Config.Encoding, true, 1024, true);
        using var csv = new CsvReader(reader, Config);
        if (headerArray != null && headerArray.Length > 0) {
            stats.Headers = headerArray.ToList();
            stats.ColumnCount = stats.Headers.Count;
        }
        else if (stats.HasHeaderRow) {
            if (await csv.ReadAsync().ConfigureAwait(false)) {
                if (csv.Context.Reader?.HeaderRecord != null && csv.Context.Reader.HeaderRecord.Length > 0) {
                    stats.Headers = csv.Context.Reader.HeaderRecord.ToList();
                    stats.ColumnCount = stats.Headers.Count;
                }
                else {
                    stats.ColumnCount = csv.Context.Reader?.ColumnCount ?? 0;
                    for (var i = 0; i < stats.ColumnCount; i++)
                        stats.Headers.Add(csv.GetField(i) ?? $"Column{i}");
                }
            }
        }
        else {
            if (await csv.ReadAsync().ConfigureAwait(false)) {
                stats.ColumnCount = csv.Context.Reader?.ColumnCount ?? 0;
                for (var i = 0; i < stats.ColumnCount; i++)
                    stats.Headers.Add($"Column{i}");
            }
        }

        if (csvStream.CanSeek) {
            csvStream.Position = 0;
            reader.DiscardBufferedData();
        }

        using var reader2 = new StreamReader(csvStream, Config.Encoding, true, 1024, true);
        using var csv2 = new CsvReader(reader2, Config);
        var sampleCount = 0;
        var rowCount = 0;
        while (await csv2.ReadAsync().ConfigureAwait(false) && sampleCount < 5) {
            ct.ThrowIfCancellationRequested();
            if (stats.HasHeaderRow && rowCount == 0) {
                rowCount++;
                continue;
            }

            var rowDict = new Dictionary<string, string>();
            for (var i = 0; i < stats.ColumnCount; i++) {
                var header = i < stats.Headers.Count ? stats.Headers[i] : $"Column{i}";
                var value = csv2.GetField(i) ?? string.Empty;
                rowDict[header] = value;
                if (!stats.InferredColumnTypes.ContainsKey(i)) {
                    if (int.TryParse(value, out var _))
                        stats.InferredColumnTypes[i] = typeof(int);
                    else if (decimal.TryParse(value, out var _))
                        stats.InferredColumnTypes[i] = typeof(decimal);
                    else if (DateTime.TryParse(value, out var _))
                        stats.InferredColumnTypes[i] = typeof(DateTime);
                    else
                        stats.InferredColumnTypes[i] = typeof(string);
                }
            }

            stats.SampleRows.Add(rowDict);
            sampleCount++;
            rowCount++;
        }

        while (await csv2.ReadAsync().ConfigureAwait(false)) {
            ct.ThrowIfCancellationRequested();
            rowCount++;
        }

        stats.RowCount = stats.HasHeaderRow ? rowCount - 1 : rowCount;
        return stats;
    }

    public async Task ProcessFileInChunksAsync<T>(
        string csvFilePath,
        int chunkSize,
        Func<IEnumerable<T>, Task> processChunk,
        CsvParseOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath, nameof(csvFilePath));
        ArgumentHelpers.ThrowIfNegativeOrZero(chunkSize, nameof(chunkSize));
        await using var stream = File.OpenRead(csvFilePath);
        await ProcessStreamInChunksAsync(stream, chunkSize, processChunk, options, ct).ConfigureAwait(false);
    }

    public async Task ProcessStreamInChunksAsync<T>(
        Stream csvStream,
        int chunkSize,
        Func<IEnumerable<T>, Task> processChunk,
        CsvParseOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        ArgumentHelpers.ThrowIfNull(processChunk, nameof(processChunk));
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        ArgumentHelpers.ThrowIfNegativeOrZero(chunkSize, nameof(chunkSize));
        if (csvStream.CanSeek && csvStream.Position > 0)
            csvStream.Position = 0;

        var chunk = new List<T>();
        await foreach (var record in ParseStreamStreamingAsync<T>(csvStream, options, ct).ConfigureAwait(false)) {
            chunk.Add(record);
            if (chunk.Count >= chunkSize) {
                await processChunk(chunk).ConfigureAwait(false);
                chunk.Clear();
            }
        }

        if (chunk.Count > 0)
            await processChunk(chunk).ConfigureAwait(false);
    }

    public async Task<ValidationResult> ValidateAsync(string csvFilePath, CsvSchema schema, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath, nameof(csvFilePath));
        await using var stream = File.OpenRead(csvFilePath);
        return await ValidateAsync(stream, schema, ct).ConfigureAwait(false);
    }

    public async Task<ValidationResult> ValidateAsync(Stream csvStream, CsvSchema schema, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        ArgumentHelpers.ThrowIfNull(schema, nameof(schema));
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        if (csvStream.CanSeek && csvStream.Position > 0)
            csvStream.Position = 0;

        using var reader = new StreamReader(csvStream, Config.Encoding, true, 1024, true);
        using var csv = new CsvReader(reader, Config);
        List<string> headers;
        if (Config.HasHeaderRecord) {
            if (!await csv.ReadAsync().ConfigureAwait(false))
                return new(false, ["CSV file is empty"]);

            if (csv.Context.Reader?.HeaderRecord != null && csv.Context.Reader.HeaderRecord.Length > 0)
                headers = csv.Context.Reader.HeaderRecord.ToList();
            else {
                headers = new();
                var columnCount = csv.Context.Reader?.ColumnCount ?? 0;
                for (var i = 0; i < columnCount; i++)
                    headers.Add(csv.GetField(i) ?? $"Column{i}");
            }
        }
        else {
            if (!await csv.ReadAsync().ConfigureAwait(false))
                return new(false, ["CSV file is empty"]);

            headers = new();
            var columnCount = csv.Context.Reader?.ColumnCount ?? 0;
            for (var i = 0; i < columnCount; i++)
                headers.Add($"Column{i}");
        }

        var schemaColumnNames = schema.Columns.Select(c => c.Name).ToHashSet();
        var errors = new List<string>();
        if (schema.RequireAllColumns) {
            foreach (var column in schema.Columns) {
                if (!headers.Contains(column.Name, StringComparer.OrdinalIgnoreCase))
                    errors.Add($"Required column '{column.Name}' is missing");
            }
        }

        if (!schema.AllowExtraColumns) {
            foreach (var header in headers) {
                if (!schemaColumnNames.Contains(header, StringComparer.OrdinalIgnoreCase))
                    errors.Add($"Unexpected column '{header}' found");
            }
        }

        var rowNumber = 1;
        while (await csv.ReadAsync().ConfigureAwait(false)) {
            ct.ThrowIfCancellationRequested();
            rowNumber++;
            foreach (var column in schema.Columns) {
                var columnIndex = headers.FindIndex(header => header.Equals(column.Name, StringComparison.OrdinalIgnoreCase));
                if (columnIndex < 0) {
                    if (column.Required && schema.RequireAllColumns)
                        errors.Add($"Row {rowNumber}: Required column '{column.Name}' is missing");

                    continue;
                }

                var value = csv.GetField(columnIndex) ?? string.Empty;
                if (column.Required && string.IsNullOrWhiteSpace(value))
                    errors.Add($"Row {rowNumber}: Required column '{column.Name}' is empty");

                if (!string.IsNullOrWhiteSpace(value) && column.Validator != null && !column.Validator(value))
                    errors.Add(column.ValidationErrorMessage ?? $"Row {rowNumber}: Column '{column.Name}' failed validation");
            }
        }

        return new(!errors.Any(), errors);
    }

    public async Task<List<T>> ParseFileWithMappingAsync<T>(string csvFilePath, List<ColumnMapping> columnMappings, CsvParseOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath, nameof(csvFilePath));
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath, nameof(csvFilePath));
        ArgumentHelpers.ThrowIfNullOrEmpty(columnMappings, nameof(columnMappings));
        await using var stream = File.OpenRead(csvFilePath);
        return await ParseStreamWithMappingAsync<T>(stream, columnMappings, options, ct).ConfigureAwait(false);
    }

    public async Task<List<T>> ParseStreamWithMappingAsync<T>(Stream csvStream, List<ColumnMapping> columnMappings, CsvParseOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvStream, nameof(csvStream));
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        ArgumentHelpers.ThrowIfNullOrEmpty(columnMappings, nameof(columnMappings));
        if (csvStream.CanSeek && csvStream.Position > 0)
            csvStream.Position = 0;

        var records = new List<T>();
        using var reader = new StreamReader(csvStream, Config.Encoding, true, 1024, true);
        using var csv = new CsvReader(reader, Config);
        List<string> headers;
        if (Config.HasHeaderRecord) {
            if (!await csv.ReadAsync().ConfigureAwait(false))
                return records;

            if (csv.Context.Reader?.HeaderRecord != null && csv.Context.Reader.HeaderRecord.Length > 0)
                headers = csv.Context.Reader.HeaderRecord.ToList();
            else {
                headers = new();
                var columnCount = csv.Context.Reader?.ColumnCount ?? 0;
                for (var i = 0; i < columnCount; i++)
                    headers.Add(csv.GetField(i) ?? $"Column{i}");
            }
        }
        else {
            if (!await csv.ReadAsync().ConfigureAwait(false))
                return records;

            headers = new();
            var columnCount = csv.Context.Reader?.ColumnCount ?? 0;
            for (var i = 0; i < columnCount; i++)
                headers.Add($"Column{i}");
        }

        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var rowNumber = 0;
        var maxRows = options?.MaxRows;
        while (await csv.ReadAsync().ConfigureAwait(false)) {
            ct.ThrowIfCancellationRequested();
            if (maxRows.HasValue && rowNumber >= maxRows.Value)
                break;

            try {
                var instance = Activator.CreateInstance<T>();
                foreach (var mapping in columnMappings) {
                    var columnIndex = headers.FindIndex(h => h.Equals(mapping.SourceColumn, StringComparison.OrdinalIgnoreCase));
                    var value = columnIndex >= 0 ? csv.GetField(columnIndex) : null;
                    if (string.IsNullOrWhiteSpace(value) && mapping.DefaultValue != null)
                        value = mapping.DefaultValue.ToString();

                    if (!string.IsNullOrWhiteSpace(value)) {
                        var prop = properties.FirstOrDefault(p => p.Name.Equals(mapping.TargetProperty, StringComparison.OrdinalIgnoreCase));
                        if (prop != null && prop.CanWrite) {
                            var finalValue = mapping.Transformer != null ? mapping.Transformer(value) : value;
                            if (finalValue != null && prop.PropertyType.IsAssignableFrom(finalValue.GetType()))
                                prop.SetValue(instance, finalValue);
                            else if (finalValue != null) {
                                try {
                                    var converted = Convert.ChangeType(finalValue, prop.PropertyType);
                                    prop.SetValue(instance, converted);
                                }
                                catch {
                                    // Conversion failed, skip
                                }
                            }
                        }
                    }
                }

                records.Add(instance);
                rowNumber++;
            }
            catch (Exception ex) when (options?.ContinueOnError == true) {
                var error = new CsvParseError { RowNumber = rowNumber + 1, RawRecord = csv.Context.Parser?.RawRecord, Exception = ex };
                options.OnError?.Invoke(error);
            }
            catch when (options?.ContinueOnError != true) {
                throw;
            }
        }

        return records;
    }

    public async Task<CsvComparisonResult> CompareFilesAsync(string file1, string file2, string? keyColumn = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(file1, nameof(file1));
        ArgumentHelpers.ThrowIfFileNotFound(file1, nameof(file1));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(file2, nameof(file2));
        ArgumentHelpers.ThrowIfFileNotFound(file2, nameof(file2));
        var dict1 = await ParseFileAsDictionaryAsync(file1, ct).ConfigureAwait(false);
        var dict2 = await ParseFileAsDictionaryAsync(file2, ct).ConfigureAwait(false);
        var result = new CsvComparisonResult {
            RowCount1 = dict1.Count,
            RowCount2 = dict2.Count,
            ColumnCount1 = dict1.Values.FirstOrDefault()?.Count ?? 0,
            ColumnCount2 = dict2.Values.FirstOrDefault()?.Count ?? 0
        };

        if (string.IsNullOrWhiteSpace(keyColumn)) {
            var maxRows = Math.Max(dict1.Count, dict2.Count);
            for (var i = 0; i < maxRows; i++) {
                var hasRow1 = dict1.ContainsKey(i);
                var hasRow2 = dict2.ContainsKey(i);
                if (!hasRow1 && hasRow2)
                    result.Differences.Add(new(i, DifferenceType.Added));
                else if (hasRow1 && !hasRow2)
                    result.Differences.Add(new(i, DifferenceType.Removed));
                else if (hasRow1 && hasRow2) {
                    var row1 = dict1[i];
                    var row2 = dict2[i];
                    var maxCols = Math.Max(row1.Count, row2.Count);
                    for (var j = 0; j < maxCols; j++) {
                        var val1 = row1.GetValueOrDefault(j);
                        var val2 = row2.GetValueOrDefault(j);
                        if (val1 != val2)
                            result.Differences.Add(new(i, DifferenceType.Modified, $"Column{j}", val1, val2));
                    }
                }
            }
        }
        else {
            var keyIndex1 = -1;
            var keyIndex2 = -1;
            if (dict1.TryGetValue(0, out var value)) {
                foreach (var kvp in value) {
                    if (!kvp.Value.Equals(keyColumn, StringComparison.OrdinalIgnoreCase))
                        continue;

                    keyIndex1 = kvp.Key;
                    break;
                }
            }

            if (dict2.TryGetValue(0, out var value1)) {
                foreach (var kvp in value1) {
                    if (!kvp.Value.Equals(keyColumn, StringComparison.OrdinalIgnoreCase))
                        continue;

                    keyIndex2 = kvp.Key;
                    break;
                }
            }

            if (keyIndex1 < 0 || keyIndex2 < 0)
                result.Differences.Add(new(0, DifferenceType.Modified, keyColumn, keyIndex1 >= 0 ? "Found" : "Not found", keyIndex2 >= 0 ? "Found" : "Not found"));
            else {
                var keys1 = dict1.Skip(1).ToDictionary(r => r.Value[keyIndex1], r => r.Key);
                var keys2 = dict2.Skip(1).ToDictionary(r => r.Value[keyIndex2], r => r.Key);
                var allKeys = keys1.Keys.Union(keys2.Keys).Distinct();
                foreach (var key in allKeys) {
                    var hasKey1 = keys1.ContainsKey(key);
                    var hasKey2 = keys2.ContainsKey(key);
                    if (!hasKey1 && hasKey2)
                        result.Differences.Add(new(keys2[key], DifferenceType.Added));
                    else if (hasKey1 && !hasKey2)
                        result.Differences.Add(new(keys1[key], DifferenceType.Removed));
                    else if (hasKey1 && hasKey2) {
                        var row1 = dict1[keys1[key]];
                        var row2 = dict2[keys2[key]];
                        foreach (var kvp in row1) {
                            if (!row2.ContainsKey(kvp.Key) || row2[kvp.Key] != kvp.Value)
                                result.Differences.Add(new(keys1[key], DifferenceType.Modified, $"Column{kvp.Key}", kvp.Value, row2.ContainsKey(kvp.Key) ? row2[kvp.Key] : null));
                        }
                    }
                }
            }
        }

        result.AreIdentical = result.Differences.Count == 0;
        return result;
    }
#endif
}