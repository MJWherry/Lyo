using System.Reflection;
using System.Text;
using Lyo.Result;

namespace Lyo.Json;

/// <summary>Writes a typed value as JSON to a file, stream, string, byte array, or span.</summary>
public interface IJsonWriter
{
    /// <summary>Writes <paramref name="value" /> to <paramref name="jsonFilePath" /> with the service encoding.</summary>
    void ExportToJson<T>(T value, string jsonFilePath);

    /// <summary>Writes <paramref name="value" /> to <paramref name="jsonStream" /> with the service encoding. The stream is left open.</summary>
    void ExportToJson<T>(T value, Stream jsonStream);

    /// <summary>Serializes <paramref name="value" /> to a JSON string.</summary>
    string ExportToJsonString<T>(T value);

    /// <summary>Serializes <paramref name="value" /> to bytes. A null <paramref name="encoding" /> uses the service encoding.</summary>
    byte[] ExportToJsonBytes<T>(T value, Encoding? encoding = null);

    /// <summary>Writes <paramref name="value" /> into <paramref name="destination" />. Returns false when the buffer is too small.</summary>
    bool TryExport<T>(T value, Span<byte> destination, out int bytesWritten, Encoding? encoding = null);

    /// <summary>Writes one compact JSON value per line to <paramref name="jsonFilePath" />.</summary>
    void ExportNdjson<T>(IEnumerable<T> values, string jsonFilePath);

    /// <summary>Writes one compact JSON value per line to <paramref name="jsonStream" />. The stream is left open.</summary>
    void ExportNdjson<T>(IEnumerable<T> values, Stream jsonStream);

    /// <summary>Serializes <paramref name="values" /> to one compact JSON value per line.</summary>
    string ExportNdjsonString<T>(IEnumerable<T> values);

    /// <summary>Serializes <paramref name="values" /> to UTF-8 NDJSON bytes.</summary>
    byte[] ExportNdjsonBytes<T>(IEnumerable<T> values);

    /// <summary>
    /// Writes one object per item. Each field is <paramref name="fields" /> in dictionary order. <c>Order</c> does not reorder these keys. A null formatter result is an empty
    /// string.
    /// </summary>
    string ExportFormattedToJsonString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields);

    /// <summary>Writes <see cref="ExportFormattedToJsonString{T}" /> to <paramref name="jsonStream" />. The stream is left open.</summary>
    void ExportFormattedToJson<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream jsonStream);

    /// <summary>
    /// Writes one object per item. The key is the field name and the value is the property value, in dictionary order. <c>Order</c> does not reorder these keys.
    /// </summary>
    string ExportColumnsToJsonString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns);

    /// <summary>Writes <see cref="ExportColumnsToJsonString{T}" /> to <paramref name="jsonStream" />. The stream is left open.</summary>
    void ExportColumnsToJson<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream jsonStream);

    /// <summary>
    /// Writes <paramref name="dataTable" /> as a JSON array of objects. Header text is the key. A blank header is <c>Column{index}</c>. A repeated header throws. Empty cells stay
    /// empty strings. A footer is the last object. Naming, member order, and empty-string omission do not apply.
    /// </summary>
    string ExportToJsonStringFromDataTable(DataTable.Models.DataTable dataTable);

    /// <summary>Writes <see cref="ExportToJsonStringFromDataTable" /> to <paramref name="jsonFilePath" />.</summary>
    void ExportToJsonFromDataTable(DataTable.Models.DataTable dataTable, string jsonFilePath);

    /// <summary>Writes <see cref="ExportToJsonStringFromDataTable" /> to <paramref name="jsonStream" />. The stream is left open.</summary>
    void ExportToJsonFromDataTable(DataTable.Models.DataTable dataTable, Stream jsonStream);

    /// <summary>Serializes <paramref name="dataTable" /> to bytes. A null <paramref name="encoding" /> uses the service encoding.</summary>
    byte[] ExportToJsonBytesFromDataTable(DataTable.Models.DataTable dataTable, Encoding? encoding = null);

#if !NETSTANDARD2_0
    /// <summary>Writes <paramref name="value" /> to <paramref name="jsonFilePath" />.</summary>
    Task ExportToJsonAsync<T>(T value, string jsonFilePath, CancellationToken ct = default);

    /// <summary>Writes <paramref name="value" /> to <paramref name="jsonStream" />. The stream is left open.</summary>
    Task ExportToJsonAsync<T>(T value, Stream jsonStream, CancellationToken ct = default);

    /// <summary>Serializes <paramref name="value" /> to a JSON string.</summary>
    Task<string> ExportToJsonStringAsync<T>(T value, CancellationToken ct = default);

    /// <summary>Serializes <paramref name="value" /> to bytes. A null <paramref name="encoding" /> uses the service encoding.</summary>
    Task<byte[]> ExportToJsonBytesAsync<T>(T value, Encoding? encoding = null, CancellationToken ct = default);

    /// <summary>Writes <see cref="ExportFormattedToJsonString{T}" /> to <paramref name="jsonStream" />. The stream is left open.</summary>
    Task ExportFormattedToJsonAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream jsonStream, CancellationToken ct = default);

    /// <summary>Writes <see cref="ExportColumnsToJsonString{T}" /> to <paramref name="jsonStream" />. The stream is left open.</summary>
    Task ExportColumnsToJsonAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream jsonStream, CancellationToken ct = default);

    /// <summary>Writes <see cref="ExportToJsonStringFromDataTable" /> to <paramref name="jsonFilePath" />.</summary>
    Task ExportToJsonFromDataTableAsync(DataTable.Models.DataTable dataTable, string jsonFilePath, CancellationToken ct = default);

    /// <summary>Writes <see cref="ExportToJsonStringFromDataTable" /> to <paramref name="jsonStream" />. The stream is left open.</summary>
    Task ExportToJsonFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream jsonStream, CancellationToken ct = default);

    /// <summary>Serializes <paramref name="dataTable" /> to a JSON string.</summary>
    Task<string> ExportToJsonStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    /// <summary>Serializes <paramref name="dataTable" /> to bytes. A null <paramref name="encoding" /> uses the service encoding.</summary>
    Task<byte[]> ExportToJsonBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, Encoding? encoding = null, CancellationToken ct = default);
#endif
}
