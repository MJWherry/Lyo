using System.Reflection;
using System.Text;
using Lyo.Common;

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

    void ExportToCsv<T>(IEnumerable<T> data, string csvFilePath);

    void ExportToCsvStream<T>(IEnumerable<T> data, Stream csvStream);

    void ExportToCsv<T>(IEnumerable<T> data, TextWriter writer);

    string ExportToCsvString<T>(IEnumerable<T> data);

    byte[] ExportToCsvBytes<T>(IEnumerable<T> data);

    void ExportToCsv<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string csvFilePath);

    void ExportToCsvStream<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream csvStream);

    void ExportToCsv<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, TextWriter writer);

    string ExportToCsvString<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties);

    byte[] ExportToCsvBytes<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties);

    void ExportToCsvFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, string csvFilePath, bool hasHeaderRow = true);

    void ExportToCsvStreamFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, Stream csvStream, bool hasHeaderRow = true);

    string ExportToCsvStringFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true);

    byte[] ExportToCsvBytesFromDictionary(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true);

    void ExportToCsvFromDataTable(DataTable.Models.DataTable dataTable, string csvFilePath);

    void ExportToCsvStreamFromDataTable(DataTable.Models.DataTable dataTable, Stream csvStream);

    string ExportToCsvStringFromDataTable(DataTable.Models.DataTable dataTable);

    byte[] ExportToCsvBytesFromDataTable(DataTable.Models.DataTable dataTable);

    IEnumerable<T> ParseFile<T>(string csvFilePath);

    IEnumerable<T> ParseStream<T>(Stream csvStream);

    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseFileAsDictionary(string csvFilePath);

    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseStreamAsDictionary(Stream csvStream);

    Result<DataTable.Models.DataTable> ParseFileAsDataTable(string csvFilePath, bool? hasHeaderRow = null);

    Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream csvStream, bool? hasHeaderRow = null);

    Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] csvBytes, bool? hasHeaderRow = null);

    /// <summary>Exports CSV bytes to an HTML document containing a table.</summary>
    /// <param name="csvBytes">Raw CSV file bytes.</param>
    /// <param name="hasHeaderRow">When true, first row is headers. When false, uses Column0, Column1, etc. When null, uses CsvService configuration.</param>
    /// <returns>Complete HTML document string with table.</returns>
    string ExportToHtmlTable(byte[] csvBytes, bool? hasHeaderRow = null);

    IEnumerable<T> ParseBytes<T>(byte[] csvBytes);

    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseBytesAsDictionary(byte[] csvBytes);

    Result<DataTable.Models.DataTable> ParseFromUrlAsDataTable(string url, bool? hasHeaderRow = null);

    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseFromUrlAsDictionary(string url);

    IEnumerable<T> ParseFromUrl<T>(string url);

#if !NETSTANDARD2_0
    Task ExportToCsvAsync<T>(IEnumerable<T> data, string csvFilePath, CancellationToken ct = default);

    Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, Stream csvStream, CancellationToken ct = default);

    Task ExportToCsvAsync<T>(IEnumerable<T> data, TextWriter writer, CancellationToken ct = default);

    Task<string> ExportToCsvStringAsync<T>(IEnumerable<T> data, CancellationToken ct = default);

    Task<byte[]> ExportToCsvBytesAsync<T>(IEnumerable<T> data, CancellationToken ct = default);

    Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, string csvFilePath, CancellationToken ct = default);

    Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, Stream csvStream, CancellationToken ct = default);

    /// <summary>Exports data to CSV stream with custom column headers. Key = header text, Value = property to read.</summary>
    Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, PropertyInfo> columns, Stream csvStream, CancellationToken ct = default);

    /// <summary>Exports data to CSV stream with formatter delegates. Key = header text, Value = function that returns the cell value for each row.</summary>
    Task ExportToCsvStreamAsync<T>(IEnumerable<T> data, IReadOnlyDictionary<string, Func<T, string>> columnFormatters, Stream csvStream, CancellationToken ct = default);

    Task ExportToCsvAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, TextWriter writer, CancellationToken ct = default);

    Task<string> ExportToCsvStringAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, CancellationToken ct = default);

    Task<byte[]> ExportToCsvBytesAsync<T>(IEnumerable<T> data, IReadOnlyList<PropertyInfo> selectedProperties, CancellationToken ct = default);

    Task ExportToCsvFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        string csvFilePath,
        bool hasHeaderRow = true,
        CancellationToken ct = default);

    Task ExportToCsvStreamFromDictionaryAsync(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data,
        Stream csvStream,
        bool hasHeaderRow = true,
        CancellationToken ct = default);

    Task<string> ExportToCsvStringFromDictionaryAsync(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true, CancellationToken ct = default);

    Task<byte[]> ExportToCsvBytesFromDictionaryAsync(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> data, bool hasHeaderRow = true, CancellationToken ct = default);

    Task ExportToCsvFromDataTableAsync(DataTable.Models.DataTable dataTable, string csvFilePath, CancellationToken ct = default);

    Task ExportToCsvStreamFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream csvStream, CancellationToken ct = default);

    Task<string> ExportToCsvStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    Task<byte[]> ExportToCsvBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    Task<List<T>> ParseFileAsync<T>(string csvFilePath, CancellationToken ct = default);

    Task<List<T>> ParseStreamAsync<T>(Stream csvStream, CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseFileAsDictionaryAsync(string csvFilePath, CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseStreamAsDictionaryAsync(Stream csvStream, CancellationToken ct = default);

    Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string csvFilePath, bool? hasHeaderRow = null, CancellationToken ct = default);

    Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream csvStream, bool? hasHeaderRow = null, CancellationToken ct = default);

    Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] csvBytes, bool? hasHeaderRow = null, CancellationToken ct = default);

    /// <summary>Exports CSV bytes to an HTML document containing a table.</summary>
    /// <param name="csvBytes">Raw CSV file bytes.</param>
    /// <param name="hasHeaderRow">When true, first row is headers. When false, uses Column0, Column1, etc. When null, uses CsvService configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Complete HTML document string with table.</returns>
    Task<string> ExportToHtmlTableAsync(byte[] csvBytes, bool? hasHeaderRow = null, CancellationToken ct = default);

    Task<List<T>> ParseBytesAsync<T>(byte[] csvBytes, CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseBytesAsDictionaryAsync(byte[] csvBytes, CancellationToken ct = default);

    Task<Result<DataTable.Models.DataTable>> ParseFromUrlAsDataTableAsync(string url, bool? hasHeaderRow = null, CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseFromUrlAsDictionaryAsync(string url, CancellationToken ct = default);

    Task<List<T>> ParseFromUrlAsync<T>(string url, CancellationToken ct = default);

    IReadOnlyList<Result<DataTable.Models.DataTable>> BatchParseFilesAsDataTable(IEnumerable<string> csvFilePaths, bool? hasHeaderRow = null);

    Task<IReadOnlyList<Result<DataTable.Models.DataTable>>> BatchParseFilesAsDataTableAsync(
        IEnumerable<string> csvFilePaths,
        bool? hasHeaderRow = null,
        CancellationToken ct = default);

    // Streaming parsing (memory efficient)
    IAsyncEnumerable<T> ParseFileStreamingAsync<T>(string csvFilePath, CsvParseOptions? options = null, CancellationToken ct = default);

    IAsyncEnumerable<T> ParseStreamStreamingAsync<T>(Stream csvStream, CsvParseOptions? options = null, CancellationToken ct = default);

    // Progress reporting
    Task ExportToCsvWithProgressAsync<T>(IEnumerable<T> data, string csvFilePath, IProgress<CsvProgress>? progress, CancellationToken ct = default);

    Task ExportToCsvStreamWithProgressAsync<T>(IEnumerable<T> data, Stream csvStream, IProgress<CsvProgress>? progress, CancellationToken ct = default);

    // Row-level error handling
    Task<List<T>> ParseFileWithOptionsAsync<T>(string csvFilePath, CsvParseOptions? options, CancellationToken ct = default);

    Task<List<T>> ParseStreamWithOptionsAsync<T>(Stream csvStream, CsvParseOptions? options, CancellationToken ct = default);

    // CSV statistics
    Task<CsvStatistics> GetStatisticsAsync(string csvFilePath, CancellationToken ct = default);

    Task<CsvStatistics> GetStatisticsAsync(Stream csvStream, CancellationToken ct = default);

    // Chunked/batch processing
    Task ProcessFileInChunksAsync<T>(string csvFilePath, int chunkSize, Func<IEnumerable<T>, Task> processChunk, CsvParseOptions? options = null, CancellationToken ct = default);

    Task ProcessStreamInChunksAsync<T>(Stream csvStream, int chunkSize, Func<IEnumerable<T>, Task> processChunk, CsvParseOptions? options = null, CancellationToken ct = default);

    // CSV validation
    Task<ValidationResult> ValidateAsync(string csvFilePath, CsvSchema schema, CancellationToken ct = default);

    Task<ValidationResult> ValidateAsync(Stream csvStream, CsvSchema schema, CancellationToken ct = default);

    // Column mapping
    Task<List<T>> ParseFileWithMappingAsync<T>(string csvFilePath, List<ColumnMapping> columnMappings, CsvParseOptions? options = null, CancellationToken ct = default);

    Task<List<T>> ParseStreamWithMappingAsync<T>(Stream csvStream, List<ColumnMapping> columnMappings, CsvParseOptions? options = null, CancellationToken ct = default);

    // CSV comparison
    Task<CsvComparisonResult> CompareFilesAsync(string file1, string file2, string? keyColumn = null, CancellationToken ct = default);

    // Append to CSV
    Task AppendToCsvAsync<T>(IEnumerable<T> data, string csvFilePath, bool includeHeaderIfMissing = false, CancellationToken ct = default);

    // Multiple file operations
    Task CombineCsvFilesAsync(IEnumerable<string> inputFiles, string outputFile, bool includeHeaders = true, CancellationToken ct = default);

    Task SplitCsvFileAsync(string inputFile, int rowsPerFile, string outputDirectory, CancellationToken ct = default);
#endif
}