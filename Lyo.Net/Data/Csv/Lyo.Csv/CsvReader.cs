using Lyo.Common.Extensions;
using Lyo.Csv.Models;
using Lyo.DataTable.Models;
using Lyo.Exceptions;
using Lyo.Result;
using Microsoft.Extensions.Logging;
#if !NETSTANDARD2_0
using System.Reflection;
#if NET10_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
#endif

namespace Lyo.Csv;

internal sealed class CsvReader : ICsvReader
{
    private readonly Func<CsvOptions> _getOptions;
    private readonly Func<DataTablePoolingOptions> _getPooling;
    private readonly ILogger _logger;

    private CsvOptions Options => _getOptions();
    private DataTablePoolingOptions Pooling => _getPooling();

    internal CsvReader(Func<CsvOptions> getOptions, ILogger logger, Func<DataTablePoolingOptions>? getPooling = null)
    {
        _getOptions = getOptions;
        _logger = logger;
        _getPooling = getPooling ?? CsvOptions.CreateDefaultPooling;
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseFile``1(System.String)' />
    public IEnumerable<T> ParseFile<T>(string csvFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath);
        _logger.LogDebug("Parsing {ParsingCsvPath} as {ParsingType}", csvFilePath, typeof(T).FullName);
        var options = Options;
        using var reader = new StreamReader(csvFilePath, options.Encoding, detectEncodingFromByteOrderMarks: true);
        return ParseReader<T>(reader, options);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseStream``1(System.IO.Stream)' />
    public IEnumerable<T> ParseStream<T>(Stream csvStream)
    {
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        csvStream.MoveToStart();
        _logger.LogDebug("Parsing csv stream as {ParsingType}", typeof(T).FullName);
        var options = Options;
        using var reader = CreateStreamReader(csvStream, options);
        return ParseReader<T>(reader, options);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseFileAsDictionary(System.String)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseFileAsDictionary(string csvFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath);
        _logger.LogDebug("Parsing {ParsingCsvPath} as dictionary", csvFilePath);
        var options = Options;
        using var reader = new StreamReader(csvFilePath, options.Encoding, detectEncodingFromByteOrderMarks: true);
        return ParseReaderAsDictionary(reader, options);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseStreamAsDictionary(System.IO.Stream)' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseStreamAsDictionary(Stream csvStream)
    {
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        csvStream.MoveToStart();
        _logger.LogDebug("Parsing csv stream as dictionary");
        var options = Options;
        using var reader = CreateStreamReader(csvStream, options);
        return ParseReaderAsDictionary(reader, options);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseFileAsDataTable(System.String,System.Nullable{System.Boolean},System.Boolean)' />
    public Result<DataTable.Models.DataTable> ParseFileAsDataTable(string csvFilePath, bool? hasHeaderRow = null, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath);
        _logger.LogDebug("Parsing {ParsingCsvPath} as DataTable", csvFilePath);
        var options = Options;
        using var reader = new StreamReader(csvFilePath, options.Encoding, detectEncodingFromByteOrderMarks: true);
        return ParseReaderAsDataTable(reader, options, hasHeaderRow, hasFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseStreamAsDataTable(System.IO.Stream,System.Nullable{System.Boolean},System.Boolean)' />
    public Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream csvStream, bool? hasHeaderRow = null, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        csvStream.MoveToStart();
        _logger.LogDebug("Parsing csv stream as DataTable");
        var options = Options;
        using var reader = CreateStreamReader(csvStream, options);
        return ParseReaderAsDataTable(reader, options, hasHeaderRow, hasFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseBytesAsDataTable(System.Byte[],System.Nullable{System.Boolean},System.Boolean)' />
    public Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] csvBytes, bool? hasHeaderRow = null, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(csvBytes);
        using var ms = new MemoryStream(csvBytes);
        return ParseStreamAsDataTable(ms, hasHeaderRow, hasFooterRow);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseBytes``1(System.Byte[])' />
    public IEnumerable<T> ParseBytes<T>(byte[] csvBytes)
    {
        ArgumentHelpers.ThrowIfNull(csvBytes);
        using var ms = new MemoryStream(csvBytes);
        return ParseStream<T>(ms);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseBytesAsDictionary(System.Byte[])' />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseBytesAsDictionary(byte[] csvBytes)
    {
        ArgumentHelpers.ThrowIfNull(csvBytes);
        using var ms = new MemoryStream(csvBytes);
        return ParseStreamAsDictionary(ms);
    }

    private IEnumerable<T> ParseReader<T>(TextReader reader, CsvOptions options)
    {
        using var csv = new CsvTextReader(reader, options);
        var records = new List<T>();
        if (options.HasHeaderRecord) {
            var headerRow = csv.ReadRow();
            if (headerRow is null)
                return records;

            var headers = headerRow.ToArray();
            while (csv.ReadRow() is { } fields)
                records.Add(CsvTypeBinder.CreateAndBind<T>(headers, fields, options.Culture));
        }
        else {
            var map = CsvTypeBinder.GetMap<T>();
            while (csv.ReadRow() is { } fields) {
                var instance = (T)map.Factory();
                CsvTypeBinder.BindByOrdinal(instance!, map, fields, options.Culture);
                records.Add(instance);
            }
        }

        return records;
    }

    private IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseReaderAsDictionary(TextReader reader, CsvOptions options)
    {
        using var csv = new CsvTextReader(reader, options);
        var result = new Dictionary<int, IReadOnlyDictionary<int, string>>();
        var rowIndex = 0;
        while (csv.ReadRow() is { } fields) {
            var rowData = new Dictionary<int, string>();
            for (var i = 0; i < fields.Count; i++)
                rowData[i] = fields[i];

            result[rowIndex] = rowData;
            rowIndex++;
        }

        return result;
    }

    private Result<DataTable.Models.DataTable> ParseReaderAsDataTable(TextReader reader, CsvOptions options, bool? hasHeaderRow, bool hasFooterRow = false, DataTablePoolingOptions? pooling = null)
    {
        var dict = ParseReaderAsDictionary(reader, options);
        var dt = DictToDataTable(dict, hasHeaderRow ?? options.HasHeaderRecord, hasFooterRow, pooling);
        return Result<DataTable.Models.DataTable>.Success(dt);
    }

    /// <summary>
    /// Builds a DataTable from a row/column dictionary.
    /// Cell-count estimate is <c>cols × (rows + 1)</c> after the full CSV has been buffered (no mid-parse pooling flip).
    /// </summary>
    private DataTable.Models.DataTable DictToDataTable(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> dict,
        bool useFirstRowAsHeader,
        bool useLastRowAsFooter = false,
        DataTablePoolingOptions? pooling = null)
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

        IReadOnlyDictionary<int, string>? footerRow = null;
        if (useLastRowAsFooter && rows.Count > 0) {
            var ordered = rows.OrderBy(r => r.Key).ToList();
            footerRow = ordered[ordered.Count - 1].Value;
            var bodyDict = new Dictionary<int, IReadOnlyDictionary<int, string>>();
            for (var i = 0; i < ordered.Count - 1; i++)
                bodyDict[i] = ordered[i].Value;

            rows = bodyDict;
        }

        var colCount = headers.Count > 0 ? headers.Keys.Max() + 1 : 0;
        if (footerRow != null && footerRow.Count > 0)
            colCount = Math.Max(colCount, footerRow.Keys.DefaultIfEmpty(-1).Max() + 1);

        var estimatedCells = Math.Max(colCount, 1) * (rows.Count + 1 + (footerRow != null ? 1 : 0));
        var interner = new DataTableValueInterner(pooling ?? Pooling, estimatedCells);
        var dt = new DataTable.Models.DataTable();
        foreach (var kv in headers)
            dt.SetHeader(kv.Key, DataTableCell.FromValue(interner.Intern(kv.Value)));

        foreach (var rowKv in rows.OrderBy(r => r.Key)) {
            var dataRow = dt.AddRow();
            foreach (var colKv in rowKv.Value)
                dataRow.SetCell(colKv.Key, DataTableCell.FromValue(interner.Intern(colKv.Value)));
        }

        if (footerRow != null) {
            foreach (var colKv in footerRow)
                dt.SetFooter(colKv.Key, DataTableCell.FromValue(interner.Intern(colKv.Value)));
        }

        return dt;
    }

    private static StreamReader CreateStreamReader(Stream csvStream, CsvOptions options)
        => new(csvStream, options.Encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 8192, leaveOpen: true);

#if !NETSTANDARD2_0
    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseFileAsync``1(System.String,System.Threading.CancellationToken)' />
    public async Task<List<T>> ParseFileAsync<T>(string csvFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath);
        _logger.LogDebug("Parsing {ParsingCsvPath} as {ParsingType}", csvFilePath, typeof(T).FullName);
        await using var stream = File.OpenRead(csvFilePath);
        return await ParseStreamAsync<T>(stream, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseStreamAsync``1(System.IO.Stream,System.Threading.CancellationToken)' />
    public async Task<List<T>> ParseStreamAsync<T>(Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        csvStream.MoveToStart();
        _logger.LogDebug("Parsing csv stream as {ParsingType}", typeof(T).FullName);
        var options = Options;
        using var reader = CreateStreamReader(csvStream, options);
        using var csv = new CsvTextReader(reader, options);
        var records = new List<T>();
        if (options.HasHeaderRecord) {
            var headerRow = await csv.ReadRowAsync(ct).ConfigureAwait(false);
            if (headerRow is null)
                return records;

            var headers = headerRow.ToArray();
            while (await csv.ReadRowAsync(ct).ConfigureAwait(false) is { } fields)
                records.Add(CsvTypeBinder.CreateAndBind<T>(headers, fields, options.Culture));
        }
        else {
            var map = CsvTypeBinder.GetMap<T>();
            while (await csv.ReadRowAsync(ct).ConfigureAwait(false) is { } fields) {
                var instance = (T)map.Factory();
                CsvTypeBinder.BindByOrdinal(instance!, map, fields, options.Culture);
                records.Add(instance);
            }
        }

        return records;
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseFileAsDictionaryAsync(System.String,System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseFileAsDictionaryAsync(string csvFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath);
        _logger.LogDebug("Parsing {ParsingCsvPath} as dictionary", csvFilePath);
        await using var stream = File.OpenRead(csvFilePath);
        return await ParseStreamAsDictionaryAsync(stream, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseStreamAsDictionaryAsync(System.IO.Stream,System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseStreamAsDictionaryAsync(Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        csvStream.MoveToStart();
        _logger.LogDebug("Parsing csv stream as dictionary");
        var options = Options;
        using var reader = CreateStreamReader(csvStream, options);
        return await ParseReaderAsDictionaryAsync(reader, options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseFileAsDataTableAsync(System.String,System.Nullable{System.Boolean},System.Boolean,System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string csvFilePath, bool? hasHeaderRow = null, bool hasFooterRow = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath);
        _logger.LogDebug("Parsing {ParsingCsvPath} as DataTable", csvFilePath);
        await using var stream = File.OpenRead(csvFilePath);
        return await ParseStreamAsDataTableAsync(stream, hasHeaderRow, hasFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseStreamAsDataTableAsync(System.IO.Stream,System.Nullable{System.Boolean},System.Boolean,System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream csvStream, bool? hasHeaderRow = null, bool hasFooterRow = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        csvStream.MoveToStart();
        _logger.LogDebug("Parsing csv stream as DataTable");
        var options = Options;
        using var reader = CreateStreamReader(csvStream, options);
        var dict = await ParseReaderAsDictionaryAsync(reader, options, ct).ConfigureAwait(false);
        var dt = DictToDataTable(dict, hasHeaderRow ?? options.HasHeaderRecord, hasFooterRow);
        return Result<DataTable.Models.DataTable>.Success(dt);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseBytesAsDataTableAsync(System.Byte[],System.Nullable{System.Boolean},System.Boolean,System.Threading.CancellationToken)' />
    public async Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] csvBytes, bool? hasHeaderRow = null, bool hasFooterRow = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvBytes);
        using var ms = new MemoryStream(csvBytes);
        return await ParseStreamAsDataTableAsync(ms, hasHeaderRow, hasFooterRow, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseBytesAsync``1(System.Byte[],System.Threading.CancellationToken)' />
    public async Task<List<T>> ParseBytesAsync<T>(byte[] csvBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvBytes);
        using var ms = new MemoryStream(csvBytes);
        return await ParseStreamAsync<T>(ms, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseBytesAsDictionaryAsync(System.Byte[],System.Threading.CancellationToken)' />
    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseBytesAsDictionaryAsync(byte[] csvBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvBytes);
        using var ms = new MemoryStream(csvBytes);
        return await ParseStreamAsDictionaryAsync(ms, ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseReaderAsDictionaryAsync(TextReader reader, CsvOptions options, CancellationToken ct = default)
    {
        using var csv = new CsvTextReader(reader, options);
        var result = new Dictionary<int, IReadOnlyDictionary<int, string>>();
        var rowIndex = 0;
        while (await csv.ReadRowAsync(ct).ConfigureAwait(false) is { } fields) {
            var rowData = new Dictionary<int, string>();
            for (var i = 0; i < fields.Count; i++)
                rowData[i] = fields[i];

            result[rowIndex] = rowData;
            rowIndex++;
        }

        return result;
    }

#if NET10_0_OR_GREATER
    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseFileStreamingAsync``1(System.String,Lyo.Csv.Models.CsvParseOptions,System.Threading.CancellationToken)' />
    public async IAsyncEnumerable<T> ParseFileStreamingAsync<T>(string csvFilePath, CsvParseOptions? options = null, [EnumeratorCancellation] CancellationToken ct = default)
#else
    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseFileStreamingAsync``1(System.String,Lyo.Csv.Models.CsvParseOptions,System.Threading.CancellationToken)' />
    public async IAsyncEnumerable<T> ParseFileStreamingAsync<T>(string csvFilePath, CsvParseOptions? options = null, CancellationToken ct = default)
#endif
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath);
        await using var stream = File.OpenRead(csvFilePath);
        await foreach (var record in ParseStreamStreamingAsync<T>(stream, options, ct).ConfigureAwait(false))
            yield return record;
    }

#if NET10_0_OR_GREATER
    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseStreamStreamingAsync``1(System.IO.Stream,Lyo.Csv.Models.CsvParseOptions,System.Threading.CancellationToken)' />
    public async IAsyncEnumerable<T> ParseStreamStreamingAsync<T>(Stream csvStream, CsvParseOptions? options = null, [EnumeratorCancellation] CancellationToken ct = default)
#else
    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseStreamStreamingAsync``1(System.IO.Stream,Lyo.Csv.Models.CsvParseOptions,System.Threading.CancellationToken)' />
    public async IAsyncEnumerable<T> ParseStreamStreamingAsync<T>(Stream csvStream, CsvParseOptions? options = null, CancellationToken ct = default)
#endif
    {
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        csvStream.MoveToStart();
        var csvOptions = Options;
        using var reader = CreateStreamReader(csvStream, csvOptions);
        using var csv = new CsvTextReader(reader, csvOptions);
        IReadOnlyList<string>? headers = null;
        CsvTypeBinder.TypeMap? map = null;
        if (csvOptions.HasHeaderRecord) {
            var headerRow = await csv.ReadRowAsync(ct).ConfigureAwait(false);
            if (headerRow is null)
                yield break;

            headers = headerRow.ToArray();
        }
        else
            map = CsvTypeBinder.GetMap<T>();

        var rowNumber = 0;
        var maxRows = options?.MaxRows;
        while (await csv.ReadRowAsync(ct).ConfigureAwait(false) is { } fields) {
            if (maxRows.HasValue && rowNumber >= maxRows.Value)
                yield break;

            T? record = default;
            Exception? parseException = null;
            if (options?.ContinueOnError == true) {
                try {
                    record = BindRecord<T>(csvOptions, headers, map, fields);
                }
                catch (Exception ex) {
                    parseException = ex;
                }
            }
            else
                record = BindRecord<T>(csvOptions, headers, map, fields);

            if (parseException != null) {
                var error = new CsvParseError {
                    RowNumber = rowNumber + 1,
                    RawRecord = csv.RawRecord,
                    Exception = parseException
                };

                options?.OnError?.Invoke(error);
                continue;
            }

            if (record == null)
                continue;

            if (options?.RowFilter != null) {
                var rowDict = BuildRowFilterDictionary(headers, fields);
                if (!options.RowFilter(rowDict))
                    continue;
            }

            yield return record;
            rowNumber++;
        }
    }

#if NET10_0_OR_GREATER
    /// <summary>Yields each physical CSV row from a file without materializing the full file.</summary>
    public async IAsyncEnumerable<IReadOnlyList<string>> ParseFileRowsStreamingAsync(string csvFilePath, [EnumeratorCancellation] CancellationToken ct = default)
#else
    /// <summary>Yields each physical CSV row from a file without materializing the full file.</summary>
    public async IAsyncEnumerable<IReadOnlyList<string>> ParseFileRowsStreamingAsync(string csvFilePath, CancellationToken ct = default)
#endif
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath);
        await using var stream = File.OpenRead(csvFilePath);
        await foreach (var row in ParseStreamRowsStreamingAsync(stream, ct).ConfigureAwait(false))
            yield return row;
    }

#if NET10_0_OR_GREATER
    /// <summary>Yields each physical CSV row from a stream without materializing all rows.</summary>
    public async IAsyncEnumerable<IReadOnlyList<string>> ParseStreamRowsStreamingAsync(Stream csvStream, [EnumeratorCancellation] CancellationToken ct = default)
#else
    /// <summary>Yields each physical CSV row from a stream without materializing all rows.</summary>
    public async IAsyncEnumerable<IReadOnlyList<string>> ParseStreamRowsStreamingAsync(Stream csvStream, CancellationToken ct = default)
#endif
    {
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        csvStream.MoveToStart();
        var options = Options;
        using var reader = CreateStreamReader(csvStream, options);
        using var csv = new CsvTextReader(reader, options);
        while (await csv.ReadRowAsync(ct).ConfigureAwait(false) is { } fields)
            yield return fields.ToArray();
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseFileWithOptionsAsync``1(System.String,Lyo.Csv.Models.CsvParseOptions,System.Threading.CancellationToken)' />
    public async Task<List<T>> ParseFileWithOptionsAsync<T>(string csvFilePath, CsvParseOptions? options, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath);
        await using var stream = File.OpenRead(csvFilePath);
        return await ParseStreamWithOptionsAsync<T>(stream, options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ParseStreamWithOptionsAsync``1(System.IO.Stream,Lyo.Csv.Models.CsvParseOptions,System.Threading.CancellationToken)' />
    public async Task<List<T>> ParseStreamWithOptionsAsync<T>(Stream csvStream, CsvParseOptions? options, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        csvStream.MoveToStart();
        var csvOptions = Options;
        using var reader = CreateStreamReader(csvStream, csvOptions);
        using var csv = new CsvTextReader(reader, csvOptions);
        IReadOnlyList<string>? headers = null;
        CsvTypeBinder.TypeMap? map = null;
        if (csvOptions.HasHeaderRecord) {
            var headerRow = await csv.ReadRowAsync(ct).ConfigureAwait(false);
            if (headerRow is null)
                return [];

            headers = headerRow.ToArray();
        }
        else
            map = CsvTypeBinder.GetMap<T>();

        var records = new List<T>();
        var rowNumber = 0;
        var maxRows = options?.MaxRows;
        while (await csv.ReadRowAsync(ct).ConfigureAwait(false) is { } fields) {
            if (maxRows.HasValue && rowNumber >= maxRows.Value)
                break;

            try {
                var record = BindRecord<T>(csvOptions, headers, map, fields);
                if (record != null) {
                    if (options?.RowFilter != null) {
                        var rowDict = BuildRowFilterDictionary(headers, fields);
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
                    RawRecord = csv.RawRecord,
                    Exception = ex
                };

                options.OnError?.Invoke(error);
            }
            catch when (options?.ContinueOnError != true) {
                throw;
            }
        }

        return records;
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.GetStatisticsAsync(System.String,System.Threading.CancellationToken)' />
    public async Task<CsvStatistics> GetStatisticsAsync(string csvFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath);
        var fileInfo = new FileInfo(csvFilePath);
        await using var stream = File.OpenRead(csvFilePath);
        var stats = await GetStatisticsAsync(stream, ct).ConfigureAwait(false);
        stats.FileSizeBytes = fileInfo.Length;
        return stats;
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.GetStatisticsAsync(System.IO.Stream,System.Threading.CancellationToken)' />
    public async Task<CsvStatistics> GetStatisticsAsync(Stream csvStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        csvStream.MoveToStart();
        var options = Options;
        var stats = new CsvStatistics {
            DetectedEncoding = options.Encoding,
            DetectedDelimiter = options.Delimiter[0],
            HasHeaderRow = options.HasHeaderRecord
        };

        using var reader = CreateStreamReader(csvStream, options);
        using var csv = new CsvTextReader(reader, options);
        var firstRow = await csv.ReadRowAsync(ct).ConfigureAwait(false);
        if (firstRow is null)
            return stats;

        if (stats.HasHeaderRow) {
            stats.Headers = [..firstRow];
            stats.ColumnCount = firstRow.Count;
        }
        else {
            stats.ColumnCount = firstRow.Count;
            for (var i = 0; i < stats.ColumnCount; i++)
                stats.Headers.Add($"Column{i}");
        }

        var sampleCount = 0;
        var dataRowCount = 0;
        if (!stats.HasHeaderRow) {
            AddSampleRow(stats, firstRow, ref sampleCount);
            dataRowCount++;
        }

        while (await csv.ReadRowAsync(ct).ConfigureAwait(false) is { } row) {
            ct.ThrowIfCancellationRequested();
            if (sampleCount < 5)
                AddSampleRow(stats, row, ref sampleCount);

            dataRowCount++;
        }

        stats.RowCount = dataRowCount;
        return stats;
    }

    private static void AddSampleRow(CsvStatistics stats, IReadOnlyList<string> fields, ref int sampleCount)
    {
        var rowDict = new Dictionary<string, string>();
        for (var i = 0; i < stats.ColumnCount; i++) {
            var header = i < stats.Headers.Count ? stats.Headers[i] : $"Column{i}";
            var value = i < fields.Count ? fields[i] : string.Empty;
            rowDict[header] = value;
            if (!stats.InferredColumnTypes.ContainsKey(i)) {
                if (int.TryParse(value, out _))
                    stats.InferredColumnTypes[i] = typeof(int);
                else if (decimal.TryParse(value, out _))
                    stats.InferredColumnTypes[i] = typeof(decimal);
                else if (DateTime.TryParse(value, out _))
                    stats.InferredColumnTypes[i] = typeof(DateTime);
                else
                    stats.InferredColumnTypes[i] = typeof(string);
            }
        }

        stats.SampleRows.Add(rowDict);
        sampleCount++;
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvReader.ProcessFileInChunksAsync``1(System.String,System.Int32,System.Func{System.Collections.Generic.IEnumerable{``0},System.Threading.Tasks.Task},Lyo.Csv.Models.CsvParseOptions,System.Threading.CancellationToken)' />
    public async Task ProcessFileInChunksAsync<T>(
        string csvFilePath,
        int chunkSize,
        Func<IEnumerable<T>, Task> processChunk,
        CsvParseOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath);
        ArgumentHelpers.ThrowIfNegativeOrZero(chunkSize);
        await using var stream = File.OpenRead(csvFilePath);
        await ProcessStreamInChunksAsync(stream, chunkSize, processChunk, options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvReader.ProcessStreamInChunksAsync``1(System.IO.Stream,System.Int32,System.Func{System.Collections.Generic.IEnumerable{``0},System.Threading.Tasks.Task},Lyo.Csv.Models.CsvParseOptions,System.Threading.CancellationToken)' />
    public async Task ProcessStreamInChunksAsync<T>(
        Stream csvStream,
        int chunkSize,
        Func<IEnumerable<T>, Task> processChunk,
        CsvParseOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvStream);
        ArgumentHelpers.ThrowIfNull(processChunk);
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        ArgumentHelpers.ThrowIfNegativeOrZero(chunkSize);
        csvStream.MoveToStart();
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

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ValidateAsync(System.String,Lyo.Csv.Models.CsvSchema,System.Threading.CancellationToken)' />
    public async Task<ValidationResult> ValidateAsync(string csvFilePath, CsvSchema schema, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath);
        await using var stream = File.OpenRead(csvFilePath);
        return await ValidateAsync(stream, schema, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.ValidateAsync(System.IO.Stream,Lyo.Csv.Models.CsvSchema,System.Threading.CancellationToken)' />
    public async Task<ValidationResult> ValidateAsync(Stream csvStream, CsvSchema schema, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvStream);
        ArgumentHelpers.ThrowIfNull(schema);
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        csvStream.MoveToStart();
        var options = Options;
        using var reader = CreateStreamReader(csvStream, options);
        using var csv = new CsvTextReader(reader, options);
        List<string> headers;
        if (options.HasHeaderRecord) {
            var headerRow = await csv.ReadRowAsync(ct).ConfigureAwait(false);
            if (headerRow is null)
                return new(false, ["CSV file is empty"]);

            headers = [..headerRow];
        }
        else {
            var firstRow = await csv.ReadRowAsync(ct).ConfigureAwait(false);
            if (firstRow is null)
                return new(false, ["CSV file is empty"]);

            headers = [];
            for (var i = 0; i < firstRow.Count; i++)
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
        while (await csv.ReadRowAsync(ct).ConfigureAwait(false) is { } fields) {
            ct.ThrowIfCancellationRequested();
            rowNumber++;
            foreach (var column in schema.Columns) {
                var columnIndex = headers.FindIndex(header => header.Equals(column.Name, StringComparison.OrdinalIgnoreCase));
                if (columnIndex < 0) {
                    if (column.Required && schema.RequireAllColumns)
                        errors.Add($"Row {rowNumber}: Required column '{column.Name}' is missing");

                    continue;
                }

                var value = columnIndex < fields.Count ? fields[columnIndex] : string.Empty;
                if (column.Required && string.IsNullOrWhiteSpace(value))
                    errors.Add($"Row {rowNumber}: Required column '{column.Name}' is empty");

                if (!string.IsNullOrWhiteSpace(value) && column.Validator != null && !column.Validator(value))
                    errors.Add(column.ValidationErrorMessage ?? $"Row {rowNumber}: Column '{column.Name}' failed validation");
            }
        }

        return new(!errors.Any(), errors);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvReader.ParseFileWithMappingAsync``1(System.String,System.Collections.Generic.List{Lyo.Csv.Models.ColumnMapping},Lyo.Csv.Models.CsvParseOptions,System.Threading.CancellationToken)' />
    public async Task<List<T>> ParseFileWithMappingAsync<T>(string csvFilePath, List<ColumnMapping> columnMappings, CsvParseOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(csvFilePath);
        ArgumentHelpers.ThrowIfFileNotFound(csvFilePath);
        ArgumentHelpers.ThrowIfNullOrEmpty(columnMappings);
        await using var stream = File.OpenRead(csvFilePath);
        return await ParseStreamWithMappingAsync<T>(stream, columnMappings, options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc
    ///     cref='M:Lyo.Csv.Models.ICsvReader.ParseStreamWithMappingAsync``1(System.IO.Stream,System.Collections.Generic.List{Lyo.Csv.Models.ColumnMapping},Lyo.Csv.Models.CsvParseOptions,System.Threading.CancellationToken)' />
    public async Task<List<T>> ParseStreamWithMappingAsync<T>(Stream csvStream, List<ColumnMapping> columnMappings, CsvParseOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(csvStream);
        OperationHelpers.ThrowIfNotReadable(csvStream, $"Stream '{nameof(csvStream)}' must be readable.");
        ArgumentHelpers.ThrowIfNullOrEmpty(columnMappings);
        csvStream.MoveToStart();
        var records = new List<T>();
        var csvOptions = Options;
        using var reader = CreateStreamReader(csvStream, csvOptions);
        using var csv = new CsvTextReader(reader, csvOptions);
        List<string> headers;
        if (csvOptions.HasHeaderRecord) {
            var headerRow = await csv.ReadRowAsync(ct).ConfigureAwait(false);
            if (headerRow is null)
                return records;

            headers = [..headerRow];
        }
        else {
            var firstRow = await csv.ReadRowAsync(ct).ConfigureAwait(false);
            if (firstRow is null)
                return records;

            headers = [];
            for (var i = 0; i < firstRow.Count; i++)
                headers.Add($"Column{i}");
        }

        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var rowNumber = 0;
        var maxRows = options?.MaxRows;
        while (await csv.ReadRowAsync(ct).ConfigureAwait(false) is { } fields) {
            ct.ThrowIfCancellationRequested();
            if (maxRows.HasValue && rowNumber >= maxRows.Value)
                break;

            try {
                var instance = (T)CsvTypeBinder.GetMap<T>().Factory();
                foreach (var mapping in columnMappings) {
                    var columnIndex = headers.FindIndex(h => h.Equals(mapping.SourceColumn, StringComparison.OrdinalIgnoreCase));
                    var value = columnIndex >= 0 && columnIndex < fields.Count ? fields[columnIndex] : null;
                    if (string.IsNullOrWhiteSpace(value) && mapping.DefaultValue != null)
                        value = mapping.DefaultValue.ToString();

                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    var prop = properties.FirstOrDefault(p => p.Name.Equals(mapping.TargetProperty, StringComparison.OrdinalIgnoreCase));
                    if (prop == null || !prop.CanWrite)
                        continue;

                    var finalValue = mapping.Transformer != null ? mapping.Transformer(value) : value;
                    if (prop.PropertyType.IsInstanceOfType(finalValue))
                        prop.SetValue(instance, finalValue);
                    else {
                        try {
                            var converted = Convert.ChangeType(finalValue, prop.PropertyType);
                            prop.SetValue(instance, converted);
                        }
                        catch {
                            // Conversion failed, skip
                        }
                    }
                }

                records.Add(instance);
                rowNumber++;
            }
            catch (Exception ex) when (options?.ContinueOnError == true) {
                var error = new CsvParseError { RowNumber = rowNumber + 1, RawRecord = csv.RawRecord, Exception = ex };
                options.OnError?.Invoke(error);
            }
            catch when (options?.ContinueOnError != true) {
                throw;
            }
        }

        return records;
    }

    /// <inheritdoc cref='M:Lyo.Csv.Models.ICsvReader.CompareFilesAsync(System.String,System.String,System.String,System.Threading.CancellationToken)' />
    public async Task<CsvComparisonResult> CompareFilesAsync(string file1, string file2, string? keyColumn = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(file1);
        ArgumentHelpers.ThrowIfFileNotFound(file1);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(file2);
        ArgumentHelpers.ThrowIfFileNotFound(file2);
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

    private static T BindRecord<T>(CsvOptions options, IReadOnlyList<string>? headers, CsvTypeBinder.TypeMap? map, IReadOnlyList<string> fields)
    {
        if (options.HasHeaderRecord) {
            ArgumentHelpers.ThrowIfNull(headers);
            return CsvTypeBinder.CreateAndBind<T>(headers, fields, options.Culture);
        }

        ArgumentHelpers.ThrowIfNull(map);
        var instance = (T)map.Factory();
        CsvTypeBinder.BindByOrdinal(instance!, map, fields, options.Culture);
        return instance;
    }

    private static Dictionary<string, string> BuildRowFilterDictionary(IReadOnlyList<string>? headers, IReadOnlyList<string> fields)
    {
        var rowDict = new Dictionary<string, string>();
        for (var i = 0; i < fields.Count; i++) {
            var header = headers != null && i < headers.Count ? headers[i] : $"Column{i}";
            rowDict[header] = fields[i];
        }

        return rowDict;
    }
#endif
}
