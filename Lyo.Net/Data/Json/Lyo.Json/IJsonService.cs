using System.Reflection;
using System.Text;
using Lyo.Result;

namespace Lyo.Json;

/// <summary>
/// Reads and writes a JSON document. <see cref="Reader" /> and <see cref="Writer" /> are the same operations this facade forwards to. One instance is safe for concurrent
/// calls.
/// </summary>
public interface IJsonService
{
    /// <summary>Owned reader.</summary>
    IJsonReader Reader { get; }

    /// <summary>Owned writer.</summary>
    IJsonWriter Writer { get; }

    /// <inheritdoc cref="IJsonReader.ParseFile{T}" />
    T ParseFile<T>(string jsonFilePath);

    /// <inheritdoc cref="IJsonReader.ParseStream{T}" />
    T ParseStream<T>(Stream jsonStream);

    /// <inheritdoc cref="IJsonReader.ParseBytes{T}" />
    T ParseBytes<T>(byte[] jsonBytes, Encoding? encoding = null);

    /// <inheritdoc cref="IJsonReader.Parse{T}(ReadOnlySpan{byte}, Encoding)" />
    T Parse<T>(ReadOnlySpan<byte> json, Encoding? encoding = null);

    /// <inheritdoc cref="IJsonReader.Parse{T}(ReadOnlySpan{char})" />
    T Parse<T>(ReadOnlySpan<char> json);

    /// <inheritdoc cref="IJsonReader.ParseString{T}" />
    T ParseString<T>(string json);

    /// <inheritdoc cref="IJsonReader.ParseNdjsonFile{T}" />
    List<T> ParseNdjsonFile<T>(string jsonFilePath);

    /// <inheritdoc cref="IJsonReader.ParseNdjsonStream{T}" />
    List<T> ParseNdjsonStream<T>(Stream jsonStream);

    /// <inheritdoc cref="IJsonReader.ParseNdjsonString{T}" />
    List<T> ParseNdjsonString<T>(string ndjson);

    /// <inheritdoc cref="IJsonReader.ParseNdjsonBytes{T}" />
    List<T> ParseNdjsonBytes<T>(byte[] ndjson);

    /// <inheritdoc cref="IJsonReader.ParseArrayAsResult{T}" />
    JsonImportResult<T> ParseArrayAsResult<T>(string json, string? rowsPath = null);

    /// <inheritdoc cref="IJsonReader.ParseNdjsonAsResult{T}" />
    JsonImportResult<T> ParseNdjsonAsResult<T>(string ndjson);

    /// <inheritdoc cref="IJsonReader.ParseStringAsDataTable" />
    Result<DataTable.Models.DataTable> ParseStringAsDataTable(string json, bool hasFooterRow = false);

    /// <inheritdoc cref="IJsonReader.ParseBytesAsDataTable" />
    Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] jsonBytes, bool hasFooterRow = false, Encoding? encoding = null);

    /// <inheritdoc cref="IJsonReader.ParseStreamAsDataTable" />
    Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream jsonStream, bool hasFooterRow = false);

    /// <inheritdoc cref="IJsonReader.ParseFileAsDataTable" />
    Result<DataTable.Models.DataTable> ParseFileAsDataTable(string jsonFilePath, bool hasFooterRow = false);

    /// <inheritdoc cref="IJsonWriter.ExportToJson{T}(T, string)" />
    void ExportToJson<T>(T value, string jsonFilePath);

    /// <inheritdoc cref="IJsonWriter.ExportToJson{T}(T, Stream)" />
    void ExportToJson<T>(T value, Stream jsonStream);

    /// <inheritdoc cref="IJsonWriter.ExportToJsonString{T}" />
    string ExportToJsonString<T>(T value);

    /// <inheritdoc cref="IJsonWriter.ExportToJsonBytes{T}" />
    byte[] ExportToJsonBytes<T>(T value, Encoding? encoding = null);

    /// <inheritdoc cref="IJsonWriter.TryExport{T}" />
    bool TryExport<T>(T value, Span<byte> destination, out int bytesWritten, Encoding? encoding = null);

    /// <inheritdoc cref="IJsonWriter.ExportNdjson{T}(IEnumerable{T}, string)" />
    void ExportNdjson<T>(IEnumerable<T> values, string jsonFilePath);

    /// <inheritdoc cref="IJsonWriter.ExportNdjson{T}(IEnumerable{T}, Stream)" />
    void ExportNdjson<T>(IEnumerable<T> values, Stream jsonStream);

    /// <inheritdoc cref="IJsonWriter.ExportNdjsonString{T}" />
    string ExportNdjsonString<T>(IEnumerable<T> values);

    /// <inheritdoc cref="IJsonWriter.ExportNdjsonBytes{T}" />
    byte[] ExportNdjsonBytes<T>(IEnumerable<T> values);

    /// <inheritdoc cref="IJsonWriter.ExportFormattedToJsonString{T}" />
    string ExportFormattedToJsonString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields);

    /// <inheritdoc cref="IJsonWriter.ExportFormattedToJson{T}" />
    void ExportFormattedToJson<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream jsonStream);

    /// <inheritdoc cref="IJsonWriter.ExportColumnsToJsonString{T}" />
    string ExportColumnsToJsonString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns);

    /// <inheritdoc cref="IJsonWriter.ExportColumnsToJson{T}" />
    void ExportColumnsToJson<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream jsonStream);

    /// <inheritdoc cref="IJsonWriter.ExportToJsonStringFromDataTable" />
    string ExportToJsonStringFromDataTable(DataTable.Models.DataTable dataTable);

    /// <inheritdoc cref="IJsonWriter.ExportToJsonFromDataTable(DataTable.Models.DataTable, string)" />
    void ExportToJsonFromDataTable(DataTable.Models.DataTable dataTable, string jsonFilePath);

    /// <inheritdoc cref="IJsonWriter.ExportToJsonFromDataTable(DataTable.Models.DataTable, Stream)" />
    void ExportToJsonFromDataTable(DataTable.Models.DataTable dataTable, Stream jsonStream);

    /// <inheritdoc cref="IJsonWriter.ExportToJsonBytesFromDataTable" />
    byte[] ExportToJsonBytesFromDataTable(DataTable.Models.DataTable dataTable, Encoding? encoding = null);

#if !NETSTANDARD2_0
    /// <inheritdoc cref="IJsonReader.ParseFileAsync{T}" />
    Task<T> ParseFileAsync<T>(string jsonFilePath, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonReader.ParseStreamAsync{T}" />
    Task<T> ParseStreamAsync<T>(Stream jsonStream, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonReader.ParseBytesAsync{T}" />
    Task<T> ParseBytesAsync<T>(byte[] jsonBytes, Encoding? encoding = null, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonReader.ParseStringAsync{T}" />
    Task<T> ParseStringAsync<T>(string json, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonReader.ParseNdjsonFileAsync{T}" />
    IAsyncEnumerable<T> ParseNdjsonFileAsync<T>(string jsonFilePath, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonReader.ParseNdjsonStreamAsync{T}" />
    IAsyncEnumerable<T> ParseNdjsonStreamAsync<T>(Stream jsonStream, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonWriter.ExportToJsonAsync{T}(T, string, CancellationToken)" />
    Task ExportToJsonAsync<T>(T value, string jsonFilePath, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonWriter.ExportToJsonAsync{T}(T, Stream, CancellationToken)" />
    Task ExportToJsonAsync<T>(T value, Stream jsonStream, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonWriter.ExportToJsonStringAsync{T}" />
    Task<string> ExportToJsonStringAsync<T>(T value, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonWriter.ExportToJsonBytesAsync{T}" />
    Task<byte[]> ExportToJsonBytesAsync<T>(T value, Encoding? encoding = null, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonWriter.ExportFormattedToJsonAsync{T}" />
    Task ExportFormattedToJsonAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream jsonStream, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonWriter.ExportColumnsToJsonAsync{T}" />
    Task ExportColumnsToJsonAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream jsonStream, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonWriter.ExportToJsonFromDataTableAsync(DataTable.Models.DataTable, string, CancellationToken)" />
    Task ExportToJsonFromDataTableAsync(DataTable.Models.DataTable dataTable, string jsonFilePath, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonWriter.ExportToJsonFromDataTableAsync(DataTable.Models.DataTable, Stream, CancellationToken)" />
    Task ExportToJsonFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream jsonStream, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonWriter.ExportToJsonStringFromDataTableAsync" />
    Task<string> ExportToJsonStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonWriter.ExportToJsonBytesFromDataTableAsync" />
    Task<byte[]> ExportToJsonBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, Encoding? encoding = null, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonReader.ParseStringAsDataTableAsync" />
    Task<Result<DataTable.Models.DataTable>> ParseStringAsDataTableAsync(string json, bool hasFooterRow = false, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonReader.ParseBytesAsDataTableAsync" />
    Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] jsonBytes, bool hasFooterRow = false, Encoding? encoding = null, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonReader.ParseStreamAsDataTableAsync" />
    Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream jsonStream, bool hasFooterRow = false, CancellationToken ct = default);

    /// <inheritdoc cref="IJsonReader.ParseFileAsDataTableAsync" />
    Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string jsonFilePath, bool hasFooterRow = false, CancellationToken ct = default);
#endif
}