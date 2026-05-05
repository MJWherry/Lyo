using System.Reflection;
using System.Text;
using Lyo.Result;

namespace Lyo.Csv.Models;

/// <summary>Service for reading, writing, and processing CSV files with support for export, import, validation, and batch operations.</summary>
public interface ICsvService
{
    /// <summary>Gets the CSV exporter for writing data to CSV format.</summary>
    ICsvExporter Exporter { get; }

    /// <summary>Gets the CSV importer for reading/parsing data from CSV format.</summary>
    ICsvImporter Importer { get; }

    /// <summary>Sets the encoding used for reading and writing CSV files.</summary>
    /// <param name="encoding">The encoding to use. Must not be null.</param>
    void SetEncoding(Encoding encoding);

    /// <summary>Exports rows to a CSV file using default or registered class maps.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    /// <param name="data">Rows to write.</param>
    /// <param name="csvFilePath">Destination file path.</param>
    void ExportToCsv<T>(IEnumerable<T> data, string csvFilePath);

    /// <summary>Exports rows to a CSV stream.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToCsvStream<T>(IEnumerable<T> data, Stream csvStream);

    /// <summary>Exports rows to a text writer (CSV content).</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToCsv<T>(IEnumerable<T> data, TextWriter writer);

    /// <summary>Serializes rows to a CSV string.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    /// <returns>CSV text.</returns>
    string ExportToCsvString<T>(IEnumerable<T> data);

    /// <summary>Serializes rows to CSV bytes using the configured encoding.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    /// <returns>CSV payload.</returns>
    byte[] ExportToCsvBytes<T>(IEnumerable<T> data);

    /// <summary>Exports only the given properties as columns to a CSV file.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToCsv<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string csvFilePath);

    /// <summary>Exports only the given properties as columns to a CSV stream.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToCsvStream<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream csvStream);

    /// <summary>Exports only the given properties as columns to a text writer.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    void ExportToCsv<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, TextWriter writer);

    /// <summary>Builds a CSV string including only the selected properties.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    string ExportToCsvString<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties);

    /// <summary>Builds CSV bytes including only the selected properties.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    byte[] ExportToCsvBytes<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties);

    /// <summary>Exports a row/column dictionary map to a CSV file.</summary>
    /// <param name="data">Row index → column index → cell text.</param>
    /// <param name="csvFilePath">Destination path.</param>
    /// <param name="hasHeaderRow">When true, writes a header row.</param>
    void ExportToCsvFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, string csvFilePath, bool hasHeaderRow = true);

    /// <summary>Exports a row/column dictionary map to a CSV stream.</summary>
    void ExportToCsvStreamFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, Stream csvStream, bool hasHeaderRow = true);

    /// <summary>Serializes a dictionary map to a CSV string.</summary>
    string ExportToCsvStringFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true);

    /// <summary>Serializes a dictionary map to CSV bytes.</summary>
    byte[] ExportToCsvBytesFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true);

    /// <summary>Exports a data table to a CSV file.</summary>
    void ExportToCsvFromDataTable(DataTable.Models.DataTable dataTable, string csvFilePath);

    /// <summary>Exports a data table to a CSV stream.</summary>
    void ExportToCsvStreamFromDataTable(DataTable.Models.DataTable dataTable, Stream csvStream);

    /// <summary>Exports a data table to a CSV string.</summary>
    string ExportToCsvStringFromDataTable(DataTable.Models.DataTable dataTable);

    /// <summary>Exports a data table to CSV bytes.</summary>
    byte[] ExportToCsvBytesFromDataTable(DataTable.Models.DataTable dataTable);

    /// <summary>Parses a CSV file row-by-row as <typeparamref name="T"/> (lazy enumeration).</summary>
    /// <typeparam name="T">Mapped row type.</typeparam>
    IEnumerable<T> ParseFile<T>(string csvFilePath);

    /// <summary>Parses a CSV stream row-by-row as <typeparamref name="T"/>.</summary>
    IEnumerable<T> ParseStream<T>(Stream csvStream);

    /// <summary>Parses a CSV file into a nested dictionary (row → column → cell text).</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseFileAsDictionary(string csvFilePath);

    /// <summary>Parses a CSV stream into a nested dictionary (row → column → cell text).</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseStreamAsDictionary(Stream csvStream);

    /// <summary>Parses a CSV file into a mutable data table with optional header interpretation.</summary>
    /// <param name="csvFilePath">Path to the CSV file.</param>
    /// <param name="hasHeaderRow">When true, first row is headers. When false, synthetic names. When null, uses configuration.</param>
    Result<DataTable.Models.DataTable> ParseFileAsDataTable(string csvFilePath, bool? hasHeaderRow = null);

    /// <summary>Parses a CSV stream into a mutable data table.</summary>
    Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream csvStream, bool? hasHeaderRow = null);

    /// <summary>Parses CSV bytes into a mutable data table.</summary>
    Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] csvBytes, bool? hasHeaderRow = null);

    /// <summary>Exports CSV bytes to an HTML document containing a table.</summary>
    /// <param name="csvBytes">Raw CSV file bytes.</param>
    /// <param name="hasHeaderRow">When true, first row is headers. When false, uses Column0, Column1, etc. When null, uses CsvService configuration.</param>
    /// <returns>Complete HTML document string with table.</returns>
    string ExportToHtmlTable(byte[] csvBytes, bool? hasHeaderRow = null);

    /// <summary>Parses CSV bytes row-by-row as <typeparamref name="T"/>.</summary>
    IEnumerable<T> ParseBytes<T>(byte[] csvBytes);

    /// <summary>Parses CSV bytes into a nested dictionary (row → column → cell text).</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseBytesAsDictionary(byte[] csvBytes);

    /// <summary>Downloads CSV from a URL and parses it into a data table (blocking).</summary>
    Result<DataTable.Models.DataTable> ParseFromUrlAsDataTable(string url, bool? hasHeaderRow = null);

    /// <summary>Downloads CSV from a URL and parses it as a nested dictionary.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseFromUrlAsDictionary(string url);

    /// <summary>Downloads CSV from a URL and maps rows to <typeparamref name="T"/> (blocking).</summary>
    IEnumerable<T> ParseFromUrl<T>(string url);

#if !NETSTANDARD2_0
    /// <summary>Exports rows to a CSV file asynchronously.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ExportToCsvAsync<T>(IEnumerable<T> data, string csvFilePath, CancellationToken ct = default);

    /// <summary>Exports rows to a CSV stream asynchronously.</summary>
    Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, Stream csvStream, CancellationToken ct = default);

    /// <summary>Exports rows to a text writer asynchronously.</summary>
    Task ExportToCsvAsync<T>(IEnumerable<T> data, TextWriter writer, CancellationToken ct = default);

    /// <summary>Serializes rows to a CSV string asynchronously.</summary>
    Task<string> ExportToCsvStringAsync<T>(IEnumerable<T> data, CancellationToken ct = default);

    /// <summary>Serializes rows to CSV bytes asynchronously.</summary>
    Task<byte[]> ExportToCsvBytesAsync<T>(IEnumerable<T> data, CancellationToken ct = default);

    /// <summary>Exports selected properties to a CSV file asynchronously.</summary>
    Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string csvFilePath, CancellationToken ct = default);

    /// <summary>Exports selected properties to a CSV stream asynchronously.</summary>
    Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream csvStream, CancellationToken ct = default);

    /// <summary>Exports data to a CSV stream with custom column headers. Key = header text, Value = property to read.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    /// <param name="data">Rows to write.</param>
    /// <param name="columns">Header label to property mapping.</param>
    /// <param name="csvStream">Destination stream.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, PropertyInfo> columns, Stream csvStream, CancellationToken ct = default);

    /// <summary>Exports data to a CSV stream with formatter delegates. Key = header text, Value = cell value factory.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    /// <param name="data">Rows to write.</param>
    /// <param name="columnFormatters">Header label to cell text.</param>
    /// <param name="csvStream">Destination stream.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, Func<T, string>> columnFormatters, Stream csvStream, CancellationToken ct = default);

    /// <summary>Exports selected properties to a text writer asynchronously.</summary>
    Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, TextWriter writer, CancellationToken ct = default);

    /// <summary>Builds a CSV string for selected properties asynchronously.</summary>
    Task<string> ExportToCsvStringAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, CancellationToken ct = default);

    /// <summary>Builds CSV bytes for selected properties asynchronously.</summary>
    Task<byte[]> ExportToCsvBytesAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, CancellationToken ct = default);

    /// <summary>Exports a dictionary map to a CSV file asynchronously.</summary>
    Task ExportToCsvFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        string csvFilePath,
        bool hasHeaderRow = true,
        CancellationToken ct = default);

    /// <summary>Exports a dictionary map to a CSV stream asynchronously.</summary>
    Task ExportToCsvStreamFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        Stream csvStream,
        bool hasHeaderRow = true,
        CancellationToken ct = default);

    /// <summary>Serializes a dictionary map to a CSV string asynchronously.</summary>
    Task<string> ExportToCsvStringFromDictionaryAsync(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true, CancellationToken ct = default);

    /// <summary>Serializes a dictionary map to CSV bytes asynchronously.</summary>
    Task<byte[]> ExportToCsvBytesFromDictionaryAsync(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true, CancellationToken ct = default);

    /// <summary>Exports a data table to a CSV file asynchronously.</summary>
    Task ExportToCsvFromDataTableAsync(DataTable.Models.DataTable dataTable, string csvFilePath, CancellationToken ct = default);

    /// <summary>Exports a data table to a CSV stream asynchronously.</summary>
    Task ExportToCsvStreamFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream csvStream, CancellationToken ct = default);

    /// <summary>Exports a data table to a CSV string asynchronously.</summary>
    Task<string> ExportToCsvStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    /// <summary>Exports a data table to CSV bytes asynchronously.</summary>
    Task<byte[]> ExportToCsvBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    /// <summary>Parses a CSV file into a list of <typeparamref name="T"/>.</summary>
    Task<List<T>> ParseFileAsync<T>(string csvFilePath, CancellationToken ct = default);

    /// <summary>Parses a CSV stream into a list of <typeparamref name="T"/>.</summary>
    Task<List<T>> ParseStreamAsync<T>(Stream csvStream, CancellationToken ct = default);

    /// <summary>Parses a CSV file into a nested dictionary asynchronously.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseFileAsDictionaryAsync(string csvFilePath, CancellationToken ct = default);

    /// <summary>Parses a CSV stream into a nested dictionary asynchronously.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseStreamAsDictionaryAsync(Stream csvStream, CancellationToken ct = default);

    /// <summary>Parses a CSV file into a data table asynchronously.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string csvFilePath, bool? hasHeaderRow = null, CancellationToken ct = default);

    /// <summary>Parses a CSV stream into a data table asynchronously.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream csvStream, bool? hasHeaderRow = null, CancellationToken ct = default);

    /// <summary>Parses CSV bytes into a data table asynchronously.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] csvBytes, bool? hasHeaderRow = null, CancellationToken ct = default);

    /// <summary>Exports CSV bytes to an HTML document containing a table.</summary>
    /// <param name="csvBytes">Raw CSV file bytes.</param>
    /// <param name="hasHeaderRow">When true, first row is headers. When false, uses Column0, Column1, etc. When null, uses CsvService configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Complete HTML document string with table.</returns>
    Task<string> ExportToHtmlTableAsync(byte[] csvBytes, bool? hasHeaderRow = null, CancellationToken ct = default);

    /// <summary>Parses CSV bytes into a list of <typeparamref name="T"/>.</summary>
    Task<List<T>> ParseBytesAsync<T>(byte[] csvBytes, CancellationToken ct = default);

    /// <summary>Parses CSV bytes into a nested dictionary asynchronously.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseBytesAsDictionaryAsync(byte[] csvBytes, CancellationToken ct = default);

    /// <summary>Downloads CSV from a URL and parses it into a data table.</summary>
    /// <param name="url">HTTP(S) URL to the CSV resource.</param>
    /// <param name="hasHeaderRow">When true, first row is headers. When false, synthetic column names. When null, uses service configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<DataTable.Models.DataTable>> ParseFromUrlAsDataTableAsync(string url, bool? hasHeaderRow = null, CancellationToken ct = default);

    /// <summary>Downloads CSV from a URL and parses it into a row/column dictionary map.</summary>
    /// <param name="url">HTTP(S) URL to the CSV resource.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseFromUrlAsDictionaryAsync(string url, CancellationToken ct = default);

    /// <summary>Downloads CSV from a URL and materializes rows as <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">Row type to map.</typeparam>
    /// <param name="url">HTTP(S) URL to the CSV resource.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<List<T>> ParseFromUrlAsync<T>(string url, CancellationToken ct = default);

    /// <summary>Parses multiple CSV files to data tables, returning one result per path (same order as <paramref name="csvFilePaths"/>).</summary>
    /// <param name="csvFilePaths">Paths to CSV files on disk.</param>
    /// <param name="hasHeaderRow">When true, first row is headers. When false, synthetic column names. When null, uses service configuration.</param>
    IReadOnlyList<Result<DataTable.Models.DataTable>> BatchParseFilesAsDataTable(IEnumerable<string> csvFilePaths, bool? hasHeaderRow = null);

    /// <summary>Parses multiple CSV files to data tables asynchronously, returning one result per path (same order as <paramref name="csvFilePaths"/>).</summary>
    /// <param name="csvFilePaths">Paths to CSV files on disk.</param>
    /// <param name="hasHeaderRow">When true, first row is headers. When false, synthetic column names. When null, uses service configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<Result<DataTable.Models.DataTable>>> BatchParseFilesAsDataTableAsync(
        IEnumerable<string> csvFilePaths,
        bool? hasHeaderRow = null,
        CancellationToken ct = default);

    /// <summary>Parses a CSV file as an async sequence of rows (bounded memory).</summary>
    IAsyncEnumerable<T> ParseFileStreamingAsync<T>(string csvFilePath, CsvParseOptions? options = null, CancellationToken ct = default);

    /// <summary>Parses a CSV stream as an async sequence of rows.</summary>
    IAsyncEnumerable<T> ParseStreamStreamingAsync<T>(Stream csvStream, CsvParseOptions? options = null, CancellationToken ct = default);

    /// <summary>Exports rows to a CSV file with progress callbacks.</summary>
    Task ExportToCsvWithProgressAsync<T>(IEnumerable<T> data, string csvFilePath, IProgress<CsvProgress>? progress, CancellationToken ct = default);

    /// <summary>Exports rows to a CSV stream with progress callbacks.</summary>
    Task ExportToCsvStreamWithProgressAsync<T>(IEnumerable<T> data, Stream csvStream, IProgress<CsvProgress>? progress, CancellationToken ct = default);

    /// <summary>Parses a CSV file with fine-grained parse options (errors per row).</summary>
    Task<List<T>> ParseFileWithOptionsAsync<T>(string csvFilePath, CsvParseOptions? options, CancellationToken ct = default);

    /// <summary>Parses a CSV stream with fine-grained parse options.</summary>
    Task<List<T>> ParseStreamWithOptionsAsync<T>(Stream csvStream, CsvParseOptions? options, CancellationToken ct = default);

    /// <summary>Computes row/column statistics for a CSV file.</summary>
    Task<CsvStatistics> GetStatisticsAsync(string csvFilePath, CancellationToken ct = default);

    /// <summary>Computes row/column statistics for a CSV stream.</summary>
    Task<CsvStatistics> GetStatisticsAsync(Stream csvStream, CancellationToken ct = default);

    /// <summary>Reads a CSV file in chunks and invokes <paramref name="processChunk"/> for each chunk.</summary>
    Task ProcessFileInChunksAsync<T>(string csvFilePath, int chunkSize, Func<IEnumerable<T>, Task> processChunk, CsvParseOptions? options = null, CancellationToken ct = default);

    /// <summary>Reads a CSV stream in chunks and invokes <paramref name="processChunk"/> for each chunk.</summary>
    Task ProcessStreamInChunksAsync<T>(Stream csvStream, int chunkSize, Func<IEnumerable<T>, Task> processChunk, CsvParseOptions? options = null, CancellationToken ct = default);

    /// <summary>Validates a CSV file against a column schema.</summary>
    Task<ValidationResult> ValidateAsync(string csvFilePath, CsvSchema schema, CancellationToken ct = default);

    /// <summary>Validates a CSV stream against a column schema.</summary>
    Task<ValidationResult> ValidateAsync(Stream csvStream, CsvSchema schema, CancellationToken ct = default);

    /// <summary>Parses a CSV file using explicit column mappings.</summary>
    Task<List<T>> ParseFileWithMappingAsync<T>(string csvFilePath, List<ColumnMapping> columnMappings, CsvParseOptions? options = null, CancellationToken ct = default);

    /// <summary>Parses a CSV stream using explicit column mappings.</summary>
    Task<List<T>> ParseStreamWithMappingAsync<T>(Stream csvStream, List<ColumnMapping> columnMappings, CsvParseOptions? options = null, CancellationToken ct = default);

    /// <summary>Compares two CSV files and returns structural differences.</summary>
    Task<CsvComparisonResult> CompareFilesAsync(string file1, string file2, string? keyColumn = null, CancellationToken ct = default);

    /// <summary>Appends rows to an existing CSV file (optionally writing the header if the file is new).</summary>
    Task AppendToCsvAsync<T>(IEnumerable<T> data, string csvFilePath, bool includeHeaderIfMissing = false, CancellationToken ct = default);

    /// <summary>Concatenates multiple CSV files into one, optionally repeating headers.</summary>
    Task CombineCsvFilesAsync(IEnumerable<string> inputFiles, string outputFile, bool includeHeaders = true, CancellationToken ct = default);

    /// <summary>Splits one CSV file into multiple files with at most <paramref name="rowsPerFile"/> data rows each.</summary>
    Task SplitCsvFileAsync(string inputFile, int rowsPerFile, string outputDirectory, CancellationToken ct = default);
#endif
}