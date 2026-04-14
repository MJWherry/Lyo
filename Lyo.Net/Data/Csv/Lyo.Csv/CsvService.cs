using System.Globalization;
using System.Reflection;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Lyo.Common;
using Lyo.Csv.Models;
using Lyo.DataTable.Models;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Csv;

/// <summary>Service for reading and writing CSV files using CsvHelper. Provides synchronous and asynchronous methods for exporting data to CSV and parsing CSV files.</summary>
/// <remarks>This class is thread-safe and can be used concurrently from multiple threads. Each operation is independent and maintains no shared mutable state between method calls.</remarks>
public sealed class CsvService : ICsvService
{
    private readonly List<Type> _classMapTypes = [];
    private readonly CsvExporter _exporter;
    private readonly HttpClient? _httpClient;
    private readonly CsvImporter _importer;
    private readonly ILogger<CsvService> _logger;

    private CsvConfiguration _csvConfiguration;

    /// <summary>Initializes a new instance of the <see cref="CsvService" /> class.</summary>
    /// <param name="logger">Optional logger instance. If null, a null logger will be used.</param>
    /// <param name="csvConfiguration">Optional CSV configuration. If null, default configuration will be used.</param>
    /// <param name="httpClient">Optional HttpClient for ParseFromUrl. If null, a new HttpClient is used per request (not recommended for production).</param>
    public CsvService(ILogger<CsvService>? logger = null, CsvConfiguration? csvConfiguration = null, HttpClient? httpClient = null)
    {
        _logger = logger ?? NullLoggerFactory.Instance.CreateLogger<CsvService>();
        _httpClient = httpClient;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _csvConfiguration = csvConfiguration ?? new CsvConfiguration(CultureInfo.InvariantCulture) {
            MissingFieldFound = null,
            HeaderValidated = null,
            IgnoreBlankLines = true,
            BadDataFound = BadDataFound,
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => args.Header.Trim(),
            ShouldUseConstructorParameters = _ => false,
            DetectColumnCountChanges = true
        };

        _exporter = new(() => _csvConfiguration, _classMapTypes, _logger);
        _importer = new(() => _csvConfiguration, _classMapTypes, _logger);
    }

    /// <summary>Initializes a new instance of the <see cref="CsvService" /> class with a configuration builder.</summary>
    /// <param name="logger">Optional logger instance. If null, a null logger will be used.</param>
    /// <param name="configBuilder">Function that builds the CSV configuration. Must not be null.</param>
    /// <param name="httpClient">Optional HttpClient for ParseFromUrl.</param>
    /// <exception cref="ArgumentNullException">Thrown when configBuilder is null.</exception>
    public CsvService(ILogger<CsvService>? logger, Func<CsvConfiguration> configBuilder, HttpClient? httpClient = null)
    {
        ArgumentHelpers.ThrowIfNull(configBuilder, nameof(configBuilder));
        _logger = logger ?? NullLoggerFactory.Instance.CreateLogger<CsvService>();
        _httpClient = httpClient;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _csvConfiguration = configBuilder.Invoke();
        _exporter = new(() => _csvConfiguration, _classMapTypes, _logger);
        _importer = new(() => _csvConfiguration, _classMapTypes, _logger);
    }

    /// <inheritdoc />
    public ICsvExporter Exporter => _exporter;

    /// <inheritdoc />
    public ICsvImporter Importer => _importer;

    /// <inheritdoc />
    public void SetEncoding(Encoding encoding) => _csvConfiguration.Encoding = encoding;

    /// <inheritdoc />
    public void ExportToCsv<T>(IEnumerable<T> data, string csvFilePath) => _exporter.ExportToCsv(data, csvFilePath);

    /// <inheritdoc />
    public void ExportToCsvStream<T>(IEnumerable<T> data, Stream csvStream) => _exporter.ExportToCsvStream(data, csvStream);

    /// <inheritdoc />
    public void ExportToCsv<T>(IEnumerable<T> data, TextWriter writer) => _exporter.ExportToCsv(data, writer);

    /// <inheritdoc />
    public string ExportToCsvString<T>(IEnumerable<T> data) => _exporter.ExportToCsvString(data);

    /// <inheritdoc />
    public byte[] ExportToCsvBytes<T>(IEnumerable<T> data) => _exporter.ExportToCsvBytes(data);

    /// <inheritdoc />
    public void ExportToCsv<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string csvFilePath)
        => _exporter.ExportToCsv(data, selectedProperties, csvFilePath);

    /// <inheritdoc />
    public void ExportToCsvStream<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream csvStream)
        => _exporter.ExportToCsvStream(data, selectedProperties, csvStream);

    /// <inheritdoc />
    public void ExportToCsv<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, TextWriter writer) => _exporter.ExportToCsv(data, selectedProperties, writer);

    /// <inheritdoc />
    public string ExportToCsvString<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties) => _exporter.ExportToCsvString(data, selectedProperties);

    /// <inheritdoc />
    public byte[] ExportToCsvBytes<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties) => _exporter.ExportToCsvBytes(data, selectedProperties);

    /// <inheritdoc />
    public void ExportToCsvFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, string csvFilePath, bool hasHeaderRow = true)
        => _exporter.ExportToCsvFromDictionary(data, csvFilePath, hasHeaderRow);

    /// <inheritdoc />
    public void ExportToCsvStreamFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, Stream csvStream, bool hasHeaderRow = true)
        => _exporter.ExportToCsvStreamFromDictionary(data, csvStream, hasHeaderRow);

    /// <inheritdoc />
    public string ExportToCsvStringFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true)
        => _exporter.ExportToCsvStringFromDictionary(data, hasHeaderRow);

    /// <inheritdoc />
    public byte[] ExportToCsvBytesFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true)
        => _exporter.ExportToCsvBytesFromDictionary(data, hasHeaderRow);

    /// <inheritdoc />
    public void ExportToCsvFromDataTable(DataTable.Models.DataTable dataTable, string csvFilePath) => _exporter.ExportToCsvFromDataTable(dataTable, csvFilePath);

    /// <inheritdoc />
    public void ExportToCsvStreamFromDataTable(DataTable.Models.DataTable dataTable, Stream csvStream) => _exporter.ExportToCsvStreamFromDataTable(dataTable, csvStream);

    /// <inheritdoc />
    public string ExportToCsvStringFromDataTable(DataTable.Models.DataTable dataTable) => _exporter.ExportToCsvStringFromDataTable(dataTable);

    /// <inheritdoc />
    public byte[] ExportToCsvBytesFromDataTable(DataTable.Models.DataTable dataTable) => _exporter.ExportToCsvBytesFromDataTable(dataTable);

    /// <inheritdoc />
    public IEnumerable<T> ParseFile<T>(string csvFilePath) => _importer.ParseFile<T>(csvFilePath);

    /// <inheritdoc />
    public IEnumerable<T> ParseStream<T>(Stream csvStream) => _importer.ParseStream<T>(csvStream);

    /// <inheritdoc />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseFileAsDictionary(string csvFilePath) => _importer.ParseFileAsDictionary(csvFilePath);

    /// <inheritdoc />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseStreamAsDictionary(Stream csvStream) => _importer.ParseStreamAsDictionary(csvStream);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseFileAsDataTable(string csvFilePath, bool? hasHeaderRow = null) => _importer.ParseFileAsDataTable(csvFilePath, hasHeaderRow);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream csvStream, bool? hasHeaderRow = null) => _importer.ParseStreamAsDataTable(csvStream, hasHeaderRow);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] csvBytes, bool? hasHeaderRow = null) => _importer.ParseBytesAsDataTable(csvBytes, hasHeaderRow);

    /// <inheritdoc />
    public string ExportToHtmlTable(byte[] csvBytes, bool? hasHeaderRow = null) => DataTableToHtml.ToHtmlDocument(ParseBytesAsDataTable(csvBytes, hasHeaderRow).ValueOrThrow());

    /// <inheritdoc />
    public IEnumerable<T> ParseBytes<T>(byte[] csvBytes) => _importer.ParseBytes<T>(csvBytes);

    /// <inheritdoc />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseBytesAsDictionary(byte[] csvBytes) => _importer.ParseBytesAsDictionary(csvBytes);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseFromUrlAsDataTable(string url, bool? hasHeaderRow = null)
        => ParseFromUrlAsDataTableAsync(url, hasHeaderRow).GetAwaiter().GetResult();

    /// <inheritdoc />
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseFromUrlAsDictionary(string url) => ParseFromUrlAsDictionaryAsync(url).GetAwaiter().GetResult();

    /// <inheritdoc />
    public IEnumerable<T> ParseFromUrl<T>(string url) => ParseFromUrlAsync<T>(url).GetAwaiter().GetResult();

    /// <inheritdoc />
    public IReadOnlyList<Result<DataTable.Models.DataTable>> BatchParseFilesAsDataTable(IEnumerable<string> csvFilePaths, bool? hasHeaderRow = null)
        => BatchParseFilesAsDataTableAsync(csvFilePaths, hasHeaderRow).GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<Result<DataTable.Models.DataTable>> ParseFromUrlAsDataTableAsync(string url, bool? hasHeaderRow = null, CancellationToken ct = default)
    {
        var bytes = await FetchBytesFromUrlAsync(url, ct).ConfigureAwait(false);
        return _importer.ParseBytesAsDataTable(bytes, hasHeaderRow);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseFromUrlAsDictionaryAsync(string url, CancellationToken ct = default)
    {
        var bytes = await FetchBytesFromUrlAsync(url, ct).ConfigureAwait(false);
#if NETSTANDARD2_0
        return _importer.ParseBytesAsDictionary(bytes);
#else
        return await _importer.ParseBytesAsDictionaryAsync(bytes, ct).ConfigureAwait(false);
#endif
    }

    /// <inheritdoc />
    public async Task<List<T>> ParseFromUrlAsync<T>(string url, CancellationToken ct = default)
    {
        var bytes = await FetchBytesFromUrlAsync(url, ct).ConfigureAwait(false);
#if NETSTANDARD2_0
        return _importer.ParseBytes<T>(bytes).ToList();
#else
        return await _importer.ParseBytesAsync<T>(bytes, ct).ConfigureAwait(false);
#endif
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Result<DataTable.Models.DataTable>>> BatchParseFilesAsDataTableAsync(
        IEnumerable<string> csvFilePaths,
        bool? hasHeaderRow = null,
        CancellationToken ct = default)
    {
        var paths = csvFilePaths.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(paths, nameof(csvFilePaths));
        var results = new List<Result<DataTable.Models.DataTable>>();
        foreach (var path in paths) {
            ct.ThrowIfCancellationRequested();
#if NETSTANDARD2_0
            results.Add(_importer.ParseFileAsDataTable(path, hasHeaderRow));
#else
            results.Add(await _importer.ParseFileAsDataTableAsync(path, hasHeaderRow, ct).ConfigureAwait(false));
#endif
        }

        return results;
    }

    /// <summary>Registers a custom class map for CSV mapping configuration.</summary>
    /// <typeparam name="TMap">The type of class map to register, must inherit from ClassMap.</typeparam>
    /// <returns>The service instance for method chaining.</returns>
    public ICsvService RegisterClassMap<TMap>()
        where TMap : ClassMap
    {
        _classMapTypes.Add(typeof(TMap));
        return this;
    }

    /// <summary>Sets the CSV configuration to use for reading and writing CSV files.</summary>
    /// <param name="csvConfiguration">The CSV configuration to use. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when csvConfiguration is null.</exception>
    public void SetCsvConfiguration(CsvConfiguration csvConfiguration) => _csvConfiguration = csvConfiguration;

    private void BadDataFound(BadDataFoundArgs args)
        => _logger.LogWarning(
            "Bad data found Row={BadCsvRow} Column={BadCsvColumn} RawValue='{BadCsvRawValue}'", args.Context.Parser?.Row, args.Context.Reader?.CurrentIndex, args.RawRecord);

    private async Task<byte[]> FetchBytesFromUrlAsync(string url, CancellationToken ct)
    {
        UriHelpers.GetValidWebUri(url, nameof(url));
        var client = _httpClient ?? new HttpClient();
        try {
#if NETSTANDARD2_0
            return await client.GetByteArrayAsync(url).ConfigureAwait(false);
#else
            return await client.GetByteArrayAsync(url, ct).ConfigureAwait(false);
#endif
        }
        finally {
            if (_httpClient == null)
                client.Dispose();
        }
    }

#if !NETSTANDARD2_0
    /// <inheritdoc />
    public Task ExportToCsvAsync<T>(IEnumerable<T> data, string csvFilePath, CancellationToken ct = default) => _exporter.ExportToCsvAsync(data, csvFilePath, ct);

    /// <inheritdoc />
    public Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, Stream csvStream, CancellationToken ct = default) => _exporter.ExportToCsvStreamAsync(data, csvStream, ct);

    /// <inheritdoc />
    public Task ExportToCsvAsync<T>(IEnumerable<T> data, TextWriter writer, CancellationToken ct = default) => _exporter.ExportToCsvAsync(data, writer, ct);

    /// <inheritdoc />
    public Task<string> ExportToCsvStringAsync<T>(IEnumerable<T> data, CancellationToken ct = default) => _exporter.ExportToCsvStringAsync(data, ct);

    /// <inheritdoc />
    public Task<byte[]> ExportToCsvBytesAsync<T>(IEnumerable<T> data, CancellationToken ct = default) => _exporter.ExportToCsvBytesAsync(data, ct);

    /// <inheritdoc />
    public Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string csvFilePath, CancellationToken ct = default)
        => _exporter.ExportToCsvAsync(data, selectedProperties, csvFilePath, ct);

    public Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream csvStream, CancellationToken ct = default)
        => _exporter.ExportToCsvStreamAsync(data, selectedProperties, csvStream, ct);

    /// <inheritdoc />
    public Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, PropertyInfo> columns, Stream csvStream, CancellationToken ct = default)
        => _exporter.ExportToCsvStreamAsync(data, columns, csvStream, ct);

    /// <inheritdoc />
    public Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, Func<T, string>> columnFormatters, Stream csvStream, CancellationToken ct = default)
        => _exporter.ExportToCsvStreamAsync(data, columnFormatters, csvStream, ct);

    public Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, TextWriter writer, CancellationToken ct = default)
        => _exporter.ExportToCsvAsync(data, selectedProperties, writer, ct);

    public Task<string> ExportToCsvStringAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, CancellationToken ct = default)
        => _exporter.ExportToCsvStringAsync(data, selectedProperties, ct);

    public Task<byte[]> ExportToCsvBytesAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, CancellationToken ct = default)
        => _exporter.ExportToCsvBytesAsync(data, selectedProperties, ct);

    /// <inheritdoc />
    public Task ExportToCsvFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        string csvFilePath,
        bool hasHeaderRow = true,
        CancellationToken ct = default)
        => _exporter.ExportToCsvFromDictionaryAsync(data, csvFilePath, hasHeaderRow, ct);

    /// <inheritdoc />
    public Task ExportToCsvStreamFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        Stream csvStream,
        bool hasHeaderRow = true,
        CancellationToken ct = default)
        => _exporter.ExportToCsvStreamFromDictionaryAsync(data, csvStream, hasHeaderRow, ct);

    public Task<string> ExportToCsvStringFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        bool hasHeaderRow = true,
        CancellationToken ct = default)
        => _exporter.ExportToCsvStringFromDictionaryAsync(data, hasHeaderRow, ct);

    public Task<byte[]> ExportToCsvBytesFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        bool hasHeaderRow = true,
        CancellationToken ct = default)
        => _exporter.ExportToCsvBytesFromDictionaryAsync(data, hasHeaderRow, ct);

    /// <inheritdoc />
    public Task ExportToCsvFromDataTableAsync(DataTable.Models.DataTable dataTable, string csvFilePath, CancellationToken ct = default)
        => _exporter.ExportToCsvFromDataTableAsync(dataTable, csvFilePath, ct);

    /// <inheritdoc />
    public Task ExportToCsvStreamFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream csvStream, CancellationToken ct = default)
        => _exporter.ExportToCsvStreamFromDataTableAsync(dataTable, csvStream, ct);

    public Task<string> ExportToCsvStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
        => _exporter.ExportToCsvStringFromDataTableAsync(dataTable, ct);

    public Task<byte[]> ExportToCsvBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
        => _exporter.ExportToCsvBytesFromDataTableAsync(dataTable, ct);

    /// <inheritdoc />
    public Task ExportToCsvWithProgressAsync<T>(IEnumerable<T> data, string csvFilePath, IProgress<CsvProgress>? progress, CancellationToken ct = default)
        => _exporter.ExportToCsvWithProgressAsync(data, csvFilePath, progress, ct);

    public Task ExportToCsvStreamWithProgressAsync<T>(IEnumerable<T> data, Stream csvStream, IProgress<CsvProgress>? progress, CancellationToken ct = default)
        => _exporter.ExportToCsvStreamWithProgressAsync(data, csvStream, progress, ct);

    /// <inheritdoc />
    public Task AppendToCsvAsync<T>(IEnumerable<T> data, string csvFilePath, bool includeHeaderIfMissing = false, CancellationToken ct = default)
        => _exporter.AppendToCsvAsync(data, csvFilePath, includeHeaderIfMissing, ct);

    /// <inheritdoc />
    public Task<List<T>> ParseFileAsync<T>(string csvFilePath, CancellationToken ct = default) => _importer.ParseFileAsync<T>(csvFilePath, ct);

    public Task<List<T>> ParseStreamAsync<T>(Stream csvStream, CancellationToken ct = default) => _importer.ParseStreamAsync<T>(csvStream, ct);

    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseFileAsDictionaryAsync(string csvFilePath, CancellationToken ct = default)
        => _importer.ParseFileAsDictionaryAsync(csvFilePath, ct);

    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseStreamAsDictionaryAsync(Stream csvStream, CancellationToken ct = default)
        => _importer.ParseStreamAsDictionaryAsync(csvStream, ct);

    public Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string csvFilePath, bool? hasHeaderRow = null, CancellationToken ct = default)
        => _importer.ParseFileAsDataTableAsync(csvFilePath, hasHeaderRow, ct);

    public Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream csvStream, bool? hasHeaderRow = null, CancellationToken ct = default)
        => _importer.ParseStreamAsDataTableAsync(csvStream, hasHeaderRow, ct);

    public Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] csvBytes, bool? hasHeaderRow = null, CancellationToken ct = default)
        => _importer.ParseBytesAsDataTableAsync(csvBytes, hasHeaderRow, ct);

    public async Task<string> ExportToHtmlTableAsync(byte[] csvBytes, bool? hasHeaderRow = null, CancellationToken ct = default)
    {
        var result = await _importer.ParseBytesAsDataTableAsync(csvBytes, hasHeaderRow, ct).ConfigureAwait(false);
        return DataTableToHtml.ToHtmlDocument(result.ValueOrThrow());
    }

    public Task<List<T>> ParseBytesAsync<T>(byte[] csvBytes, CancellationToken ct = default) => _importer.ParseBytesAsync<T>(csvBytes, ct);

    public Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseBytesAsDictionaryAsync(byte[] csvBytes, CancellationToken ct = default)
        => _importer.ParseBytesAsDictionaryAsync(csvBytes, ct);

    public IAsyncEnumerable<T> ParseFileStreamingAsync<T>(string csvFilePath, CsvParseOptions? options = null, CancellationToken ct = default)
        => _importer.ParseFileStreamingAsync<T>(csvFilePath, options, ct);

    public IAsyncEnumerable<T> ParseStreamStreamingAsync<T>(Stream csvStream, CsvParseOptions? options = null, CancellationToken ct = default)
        => _importer.ParseStreamStreamingAsync<T>(csvStream, options, ct);

    public Task<List<T>> ParseFileWithOptionsAsync<T>(string csvFilePath, CsvParseOptions? options, CancellationToken ct = default)
        => _importer.ParseFileWithOptionsAsync<T>(csvFilePath, options, ct);

    public Task<List<T>> ParseStreamWithOptionsAsync<T>(Stream csvStream, CsvParseOptions? options, CancellationToken ct = default)
        => _importer.ParseStreamWithOptionsAsync<T>(csvStream, options, ct);

    public Task<CsvStatistics> GetStatisticsAsync(string csvFilePath, CancellationToken ct = default) => _importer.GetStatisticsAsync(csvFilePath, ct);

    public Task<CsvStatistics> GetStatisticsAsync(Stream csvStream, CancellationToken ct = default) => _importer.GetStatisticsAsync(csvStream, ct);

    public Task ProcessFileInChunksAsync<T>(
        string csvFilePath,
        int chunkSize,
        Func<IEnumerable<T>, Task> processChunk,
        CsvParseOptions? options = null,
        CancellationToken ct = default)
        => _importer.ProcessFileInChunksAsync(csvFilePath, chunkSize, processChunk, options, ct);

    public Task ProcessStreamInChunksAsync<T>(
        Stream csvStream,
        int chunkSize,
        Func<IEnumerable<T>, Task> processChunk,
        CsvParseOptions? options = null,
        CancellationToken ct = default)
        => _importer.ProcessStreamInChunksAsync(csvStream, chunkSize, processChunk, options, ct);

    public Task<ValidationResult> ValidateAsync(string csvFilePath, CsvSchema schema, CancellationToken ct = default) => _importer.ValidateAsync(csvFilePath, schema, ct);

    public Task<ValidationResult> ValidateAsync(Stream csvStream, CsvSchema schema, CancellationToken ct = default) => _importer.ValidateAsync(csvStream, schema, ct);

    public Task<List<T>> ParseFileWithMappingAsync<T>(string csvFilePath, List<ColumnMapping> columnMappings, CsvParseOptions? options = null, CancellationToken ct = default)
        => _importer.ParseFileWithMappingAsync<T>(csvFilePath, columnMappings, options, ct);

    public Task<List<T>> ParseStreamWithMappingAsync<T>(Stream csvStream, List<ColumnMapping> columnMappings, CsvParseOptions? options = null, CancellationToken ct = default)
        => _importer.ParseStreamWithMappingAsync<T>(csvStream, columnMappings, options, ct);

    public Task<CsvComparisonResult> CompareFilesAsync(string file1, string file2, string? keyColumn = null, CancellationToken ct = default)
        => _importer.CompareFilesAsync(file1, file2, keyColumn, ct);

    // Composite operations (use both reader and writer)
    public async Task CombineCsvFilesAsync(IEnumerable<string> inputFiles, string outputFile, bool includeHeaders = true, CancellationToken ct = default)
    {
        var fileList = inputFiles.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(fileList, nameof(inputFiles));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputFile, nameof(outputFile));
        await using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.Read);
        await using var outputWriter = new StreamWriter(outputStream, _csvConfiguration.Encoding);
        await using var outputCsv = new CsvWriter(outputWriter, _csvConfiguration);
        var firstFile = true;
        foreach (var inputFile in fileList) {
            ArgumentHelpers.ThrowIfFileNotFound(inputFile, nameof(inputFiles));
            if (firstFile) {
                var headers = await ReadHeaderRowAsync(inputFile).ConfigureAwait(false);
                if (headers != null)
                    await WriteHeaderRowAsync(outputCsv, headers).ConfigureAwait(false);
            }

            await using var inputStream = File.OpenRead(inputFile);
            using var inputReader = new StreamReader(inputStream, _csvConfiguration.Encoding);
            using var inputCsv = new CsvReader(inputReader, _csvConfiguration);
            await CopyDataRowsAsync(inputCsv, outputCsv, ct).ConfigureAwait(false);
            firstFile = false;
        }

        await outputCsv.FlushAsync().ConfigureAwait(false);
    }

    public async Task SplitCsvFileAsync(string inputFile, int rowsPerFile, string outputDirectory, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(inputFile, nameof(inputFile));
        ArgumentHelpers.ThrowIfFileNotFound(inputFile, nameof(inputFile));
        ArgumentHelpers.ThrowIfNegativeOrZero(rowsPerFile, nameof(rowsPerFile));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputDirectory, nameof(outputDirectory));
        ExceptionThrower.ThrowIfDirectoryNotFound(outputDirectory, nameof(outputDirectory));
        var baseFileName = Path.GetFileNameWithoutExtension(inputFile);
        var extension = ".csv";
        var fileNumber = 1;
        var rowCount = 0;
        var headers = await ReadHeaderRowAsync(inputFile).ConfigureAwait(false);
        await using var inputStream = File.OpenRead(inputFile);
        using var inputReader = new StreamReader(inputStream, _csvConfiguration.Encoding);
        using var inputCsv = new CsvReader(inputReader, _csvConfiguration);
        if (_csvConfiguration.HasHeaderRecord)
            await inputCsv.ReadAsync().ConfigureAwait(false);

        StreamWriter? outputWriter = null;
        CsvWriter? outputCsv = null;
        try {
            while (await inputCsv.ReadAsync().ConfigureAwait(false)) {
                ct.ThrowIfCancellationRequested();
                if (rowCount % rowsPerFile == 0) {
                    if (outputCsv != null) {
                        await outputCsv.FlushAsync().ConfigureAwait(false);
                        await outputCsv.DisposeAsync().ConfigureAwait(false);
                    }

                    if (outputWriter != null)
                        await outputWriter.DisposeAsync().ConfigureAwait(false);

                    var outputPath = Path.Combine(outputDirectory, $"{baseFileName}_{fileNumber}{extension}");
                    outputWriter = new(outputPath, false, _csvConfiguration.Encoding);
                    outputCsv = new(outputWriter, _csvConfiguration);
                    if (headers != null)
                        await WriteHeaderRowAsync(outputCsv, headers).ConfigureAwait(false);

                    fileNumber++;
                }

                var columnCount = inputCsv.Context.Reader?.ColumnCount ?? 0;
                for (var i = 0; i < columnCount; i++)
                    outputCsv!.WriteField(inputCsv.GetField(i));

                await outputCsv!.NextRecordAsync().ConfigureAwait(false);
                rowCount++;
            }
        }
        finally {
            if (outputCsv != null) {
                await outputCsv.FlushAsync().ConfigureAwait(false);
                await outputCsv.DisposeAsync().ConfigureAwait(false);
            }

            outputWriter?.Dispose();
        }
    }

    private async Task<string[]?> ReadHeaderRowAsync(string filePath)
    {
        if (!_csvConfiguration.HasHeaderRecord)
            return null;

        string? headerLine;
        using (var reader = new StreamReader(filePath, _csvConfiguration.Encoding))
            headerLine = await reader.ReadLineAsync().ConfigureAwait(false);

        if (string.IsNullOrEmpty(headerLine))
            return null;

        using var headerStringReader = new StringReader(headerLine);
        using var headerCsv = new CsvReader(headerStringReader, _csvConfiguration);
        if (!await headerCsv.ReadAsync().ConfigureAwait(false))
            return null;

        var columnCount = headerCsv.Context.Reader?.ColumnCount ?? 0;
        var headers = new string[columnCount];
        for (var i = 0; i < columnCount; i++)
            headers[i] = headerCsv.GetField(i) ?? $"Column{i}";

        return headers;
    }

    private static async Task WriteHeaderRowAsync(CsvWriter csv, string[] headers)
    {
        foreach (var header in headers)
            csv.WriteField(header);

        await csv.NextRecordAsync().ConfigureAwait(false);
    }

    private async Task CopyDataRowsAsync(CsvReader reader, CsvWriter writer, CancellationToken ct = default)
    {
        if (_csvConfiguration.HasHeaderRecord)
            await reader.ReadAsync().ConfigureAwait(false);

        while (await reader.ReadAsync().ConfigureAwait(false)) {
            ct.ThrowIfCancellationRequested();
            var columnCount = reader.Context.Reader?.ColumnCount ?? 0;
            for (var i = 0; i < columnCount; i++)
                writer.WriteField(reader.GetField(i));

            await writer.NextRecordAsync().ConfigureAwait(false);
        }
    }
#endif
}