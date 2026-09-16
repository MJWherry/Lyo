using System.Reflection;
using System.Text;
using Lyo.Result;

namespace Lyo.Toml;

/// <summary>Writes a typed value as TOML to a file, stream, string, or byte array.</summary>
public interface ITomlWriter
{
    /// <summary>Writes <paramref name="value" /> to <paramref name="tomlFilePath" />.</summary>
    void ExportToToml<T>(T value, string tomlFilePath);

    /// <summary>Writes <paramref name="value" /> to <paramref name="tomlStream" />. The stream is left open.</summary>
    void ExportToToml<T>(T value, Stream tomlStream);

    /// <summary>Serializes <paramref name="value" /> to a TOML string. A root array is written as <c>[[items]]</c>.</summary>
    string ExportToTomlString<T>(T value);

    /// <summary>Serializes <paramref name="value" /> to bytes. A null <paramref name="encoding" /> uses the service encoding.</summary>
    byte[] ExportToTomlBytes<T>(T value, Encoding? encoding = null);

    /// <summary>Writes <paramref name="value" /> into <paramref name="destination" />. Returns false when the buffer is too small.</summary>
    bool TryExport<T>(T value, Span<byte> destination, out int bytesWritten, Encoding? encoding = null);

    /// <summary>
    /// Writes one object per item. Each field is <paramref name="fields" /> in dictionary order. <c>Order</c> does not reorder these keys. A null formatter result is an empty
    /// string. A root array is still written as <c>[[items]]</c>.
    /// </summary>
    string ExportFormattedToTomlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields);

    /// <summary>Writes <see cref="ExportFormattedToTomlString{T}" /> to <paramref name="tomlStream" />. The stream is left open.</summary>
    void ExportFormattedToToml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream tomlStream);

    /// <summary>
    /// Writes one object per item. The key is the field name and the value is the property value, in dictionary order. <c>Order</c> does not reorder these keys.
    /// </summary>
    string ExportColumnsToTomlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns);

    /// <summary>Writes <see cref="ExportColumnsToTomlString{T}" /> to <paramref name="tomlStream" />. The stream is left open.</summary>
    void ExportColumnsToToml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream tomlStream);

    /// <summary>
    /// Writes <paramref name="dataTable" /> as <c>[[items]]</c> under <see cref="TomlOptions.ItemsKey" />. Header text is the key. A blank header is <c>Column{index}</c>. A repeated
    /// header throws. Empty cells stay empty strings. A footer is the last table.
    /// </summary>
    string ExportToTomlStringFromDataTable(DataTable.Models.DataTable dataTable);

    /// <summary>Writes <see cref="ExportToTomlStringFromDataTable" /> to <paramref name="tomlFilePath" />.</summary>
    void ExportToTomlFromDataTable(DataTable.Models.DataTable dataTable, string tomlFilePath);

    /// <summary>Writes <see cref="ExportToTomlStringFromDataTable" /> to <paramref name="tomlStream" />. The stream is left open.</summary>
    void ExportToTomlFromDataTable(DataTable.Models.DataTable dataTable, Stream tomlStream);

    /// <summary>Serializes <paramref name="dataTable" /> to bytes. A null <paramref name="encoding" /> uses the service encoding.</summary>
    byte[] ExportToTomlBytesFromDataTable(DataTable.Models.DataTable dataTable, Encoding? encoding = null);

#if !NETSTANDARD2_0
    /// <summary>Writes <paramref name="value" /> to <paramref name="tomlFilePath" />.</summary>
    Task ExportToTomlAsync<T>(T value, string tomlFilePath, CancellationToken ct = default);

    /// <summary>Writes <paramref name="value" /> to <paramref name="tomlStream" />. The stream is left open.</summary>
    Task ExportToTomlAsync<T>(T value, Stream tomlStream, CancellationToken ct = default);

    /// <summary>Serializes <paramref name="value" /> to a TOML string.</summary>
    Task<string> ExportToTomlStringAsync<T>(T value, CancellationToken ct = default);

    /// <summary>Serializes <paramref name="value" /> to UTF-8 TOML bytes.</summary>
    Task<byte[]> ExportToTomlBytesAsync<T>(T value, CancellationToken ct = default);

    /// <summary>Writes <see cref="ExportFormattedToTomlString{T}" /> to <paramref name="tomlStream" />. The stream is left open.</summary>
    Task ExportFormattedToTomlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream tomlStream, CancellationToken ct = default);

    /// <summary>Writes <see cref="ExportColumnsToTomlString{T}" /> to <paramref name="tomlStream" />. The stream is left open.</summary>
    Task ExportColumnsToTomlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream tomlStream, CancellationToken ct = default);

    /// <summary>Writes <see cref="ExportToTomlStringFromDataTable" /> to <paramref name="tomlFilePath" />.</summary>
    Task ExportToTomlFromDataTableAsync(DataTable.Models.DataTable dataTable, string tomlFilePath, CancellationToken ct = default);

    /// <summary>Writes <see cref="ExportToTomlStringFromDataTable" /> to <paramref name="tomlStream" />. The stream is left open.</summary>
    Task ExportToTomlFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream tomlStream, CancellationToken ct = default);

    /// <summary>Serializes <paramref name="dataTable" /> to a TOML string.</summary>
    Task<string> ExportToTomlStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    /// <summary>Serializes <paramref name="dataTable" /> to bytes. A null <paramref name="encoding" /> uses the service encoding.</summary>
    Task<byte[]> ExportToTomlBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, Encoding? encoding = null, CancellationToken ct = default);
#endif
}
