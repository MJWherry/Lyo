using Lyo.Common;

namespace Lyo.Csv.Models;

/// <summary>Imports and parses data from CSV format. Supports parsing to strongly-typed objects, dictionaries, DataTables, streaming, and validation.</summary>
public interface ICsvImporter
{
    IEnumerable<T> ParseFile<T>(string csvFilePath);

    IEnumerable<T> ParseStream<T>(Stream csvStream);

    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseFileAsDictionary(string csvFilePath);

    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseStreamAsDictionary(Stream csvStream);

    Result<DataTable.Models.DataTable> ParseFileAsDataTable(string csvFilePath, bool? hasHeaderRow = null);

    Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream csvStream, bool? hasHeaderRow = null);

    Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] csvBytes, bool? hasHeaderRow = null);

    IEnumerable<T> ParseBytes<T>(byte[] csvBytes);

    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseBytesAsDictionary(byte[] csvBytes);

#if !NETSTANDARD2_0
    Task<List<T>> ParseFileAsync<T>(string csvFilePath, CancellationToken ct = default);

    Task<List<T>> ParseStreamAsync<T>(Stream csvStream, CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseFileAsDictionaryAsync(string csvFilePath, CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseStreamAsDictionaryAsync(Stream csvStream, CancellationToken ct = default);

    Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string csvFilePath, bool? hasHeaderRow = null, CancellationToken ct = default);

    Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream csvStream, bool? hasHeaderRow = null, CancellationToken ct = default);

    Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] csvBytes, bool? hasHeaderRow = null, CancellationToken ct = default);

    Task<List<T>> ParseBytesAsync<T>(byte[] csvBytes, CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseBytesAsDictionaryAsync(byte[] csvBytes, CancellationToken ct = default);

    IAsyncEnumerable<T> ParseFileStreamingAsync<T>(string csvFilePath, CsvParseOptions? options = null, CancellationToken ct = default);

    IAsyncEnumerable<T> ParseStreamStreamingAsync<T>(Stream csvStream, CsvParseOptions? options = null, CancellationToken ct = default);

    Task<List<T>> ParseFileWithOptionsAsync<T>(string csvFilePath, CsvParseOptions? options, CancellationToken ct = default);

    Task<List<T>> ParseStreamWithOptionsAsync<T>(Stream csvStream, CsvParseOptions? options, CancellationToken ct = default);

    Task<CsvStatistics> GetStatisticsAsync(string csvFilePath, CancellationToken ct = default);

    Task<CsvStatistics> GetStatisticsAsync(Stream csvStream, CancellationToken ct = default);

    Task ProcessFileInChunksAsync<T>(string csvFilePath, int chunkSize, Func<IEnumerable<T>, Task> processChunk, CsvParseOptions? options = null, CancellationToken ct = default);

    Task ProcessStreamInChunksAsync<T>(Stream csvStream, int chunkSize, Func<IEnumerable<T>, Task> processChunk, CsvParseOptions? options = null, CancellationToken ct = default);

    Task<ValidationResult> ValidateAsync(string csvFilePath, CsvSchema schema, CancellationToken ct = default);

    Task<ValidationResult> ValidateAsync(Stream csvStream, CsvSchema schema, CancellationToken ct = default);

    Task<List<T>> ParseFileWithMappingAsync<T>(string csvFilePath, List<ColumnMapping> columnMappings, CsvParseOptions? options = null, CancellationToken ct = default);

    Task<List<T>> ParseStreamWithMappingAsync<T>(Stream csvStream, List<ColumnMapping> columnMappings, CsvParseOptions? options = null, CancellationToken ct = default);

    Task<CsvComparisonResult> CompareFilesAsync(string file1, string file2, string? keyColumn = null, CancellationToken ct = default);
#endif
}