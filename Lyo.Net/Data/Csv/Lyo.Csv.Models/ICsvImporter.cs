using Lyo.Result;

namespace Lyo.Csv.Models;

/// <summary>Imports and parses CSV. Supports strongly-typed rows, row/column dictionaries, Lyo data tables, streaming, validation, and comparison.</summary>
public interface ICsvImporter
{
    /// <summary>Parses <paramref name="csvFilePath"/> lazily as <typeparamref name="T"/> rows.</summary>
    /// <typeparam name="T">Mapped row type.</typeparam>
    IEnumerable<T> ParseFile<T>(string csvFilePath);

    /// <summary>Parses <paramref name="csvStream"/> lazily as <typeparamref name="T"/> (seeks to start when possible).</summary>
    /// <typeparam name="T">Mapped row type.</typeparam>
    IEnumerable<T> ParseStream<T>(Stream csvStream);

    /// <summary>Parses a file into row index → column index → cell text.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseFileAsDictionary(string csvFilePath);

    /// <summary>Parses a stream into row index → column index → cell text.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseStreamAsDictionary(Stream csvStream);

    /// <summary>Parses a file into a Lyo data table. When <c>hasHeaderRow</c> is true the first row is headers; false uses synthetic names; null uses configuration.</summary>
    Result<DataTable.Models.DataTable> ParseFileAsDataTable(string csvFilePath, bool? hasHeaderRow = null);

    /// <summary>Parses a stream into a Lyo data table.</summary>
    Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream csvStream, bool? hasHeaderRow = null);

    /// <summary>Parses bytes into a Lyo data table.</summary>
    Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] csvBytes, bool? hasHeaderRow = null);

    /// <summary>Parses bytes lazily as <typeparamref name="T"/>.</summary>
    IEnumerable<T> ParseBytes<T>(byte[] csvBytes);

    /// <summary>Parses bytes into row/column dictionaries.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseBytesAsDictionary(byte[] csvBytes);

#if !NETSTANDARD2_0
    /// <summary>Parses a file into a list of <typeparamref name="T"/>.</summary>
    Task<List<T>> ParseFileAsync<T>(string csvFilePath, CancellationToken ct = default);

    /// <summary>Parses a stream into a list of <typeparamref name="T"/>.</summary>
    Task<List<T>> ParseStreamAsync<T>(Stream csvStream, CancellationToken ct = default);

    /// <summary>Parses a file into row/column dictionaries asynchronously.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseFileAsDictionaryAsync(string csvFilePath, CancellationToken ct = default);

    /// <summary>Parses a stream into row/column dictionaries asynchronously.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseStreamAsDictionaryAsync(Stream csvStream, CancellationToken ct = default);

    /// <summary>Parses a file into a Lyo data table asynchronously.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string csvFilePath, bool? hasHeaderRow = null, CancellationToken ct = default);

    /// <summary>Parses a stream into a Lyo data table asynchronously.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream csvStream, bool? hasHeaderRow = null, CancellationToken ct = default);

    /// <summary>Parses bytes into a Lyo data table asynchronously.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] csvBytes, bool? hasHeaderRow = null, CancellationToken ct = default);

    /// <summary>Parses bytes into a list of <typeparamref name="T"/> asynchronously.</summary>
    Task<List<T>> ParseBytesAsync<T>(byte[] csvBytes, CancellationToken ct = default);

    /// <summary>Parses bytes into row/column dictionaries asynchronously.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseBytesAsDictionaryAsync(byte[] csvBytes, CancellationToken ct = default);

    /// <summary>Parses a file as an async stream of rows with optional <paramref name="options"/>.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    IAsyncEnumerable<T> ParseFileStreamingAsync<T>(string csvFilePath, CsvParseOptions? options = null, CancellationToken ct = default);

    /// <summary>Parses a CSV stream as an async sequence of <typeparamref name="T"/>.</summary>
    IAsyncEnumerable<T> ParseStreamStreamingAsync<T>(Stream csvStream, CsvParseOptions? options = null, CancellationToken ct = default);

    /// <summary>Parses all rows into a list using <paramref name="options"/> (continue-on-error, filters, row caps).</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task<List<T>> ParseFileWithOptionsAsync<T>(string csvFilePath, CsvParseOptions? options, CancellationToken ct = default);

    /// <summary>Parses all rows from a stream into a list using <paramref name="options"/>.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task<List<T>> ParseStreamWithOptionsAsync<T>(Stream csvStream, CsvParseOptions? options, CancellationToken ct = default);

    /// <summary>Computes column and row statistics for a CSV file.</summary>
    Task<CsvStatistics> GetStatisticsAsync(string csvFilePath, CancellationToken ct = default);

    /// <summary>Computes column and row statistics for a CSV stream.</summary>
    Task<CsvStatistics> GetStatisticsAsync(Stream csvStream, CancellationToken ct = default);

    /// <summary>Reads a file in chunks of <paramref name="chunkSize"/> rows and invokes <paramref name="processChunk"/> for each.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ProcessFileInChunksAsync<T>(string csvFilePath, int chunkSize, Func<IEnumerable<T>, Task> processChunk, CsvParseOptions? options = null, CancellationToken ct = default);

    /// <summary>Reads a stream in chunks and invokes <paramref name="processChunk"/> for each.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task ProcessStreamInChunksAsync<T>(Stream csvStream, int chunkSize, Func<IEnumerable<T>, Task> processChunk, CsvParseOptions? options = null, CancellationToken ct = default);

    /// <summary>Validates a CSV file against <paramref name="schema"/>.</summary>
    Task<ValidationResult> ValidateAsync(string csvFilePath, CsvSchema schema, CancellationToken ct = default);

    /// <summary>Validates a CSV stream against <paramref name="schema"/>.</summary>
    Task<ValidationResult> ValidateAsync(Stream csvStream, CsvSchema schema, CancellationToken ct = default);

    /// <summary>Parses using explicit <paramref name="columnMappings"/>.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task<List<T>> ParseFileWithMappingAsync<T>(string csvFilePath, List<ColumnMapping> columnMappings, CsvParseOptions? options = null, CancellationToken ct = default);

    /// <summary>Parses a stream using explicit <paramref name="columnMappings"/>.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    Task<List<T>> ParseStreamWithMappingAsync<T>(Stream csvStream, List<ColumnMapping> columnMappings, CsvParseOptions? options = null, CancellationToken ct = default);

    /// <summary>Compares two CSV files; optional <paramref name="keyColumn"/> for row matching.</summary>
    Task<CsvComparisonResult> CompareFilesAsync(string file1, string file2, string? keyColumn = null, CancellationToken ct = default);
#endif
}
