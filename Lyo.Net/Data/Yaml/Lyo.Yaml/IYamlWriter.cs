using System.Reflection;
using System.Text;

namespace Lyo.Yaml;

/// <summary>Writes a typed value as YAML to a file, stream, string, or byte array.</summary>
public interface IYamlWriter
{
    /// <summary>Writes <paramref name="value" /> to <paramref name="yamlFilePath" />.</summary>
    void ExportToYaml<T>(T value, string yamlFilePath);

    /// <summary>Writes <paramref name="value" /> to <paramref name="yamlStream" />. The stream is left open.</summary>
    void ExportToYaml<T>(T value, Stream yamlStream);

    /// <summary>Serializes <paramref name="value" /> to a YAML string.</summary>
    string ExportToYamlString<T>(T value);

    /// <summary>Serializes <paramref name="value" /> to bytes. A null <paramref name="encoding" /> uses the service encoding.</summary>
    byte[] ExportToYamlBytes<T>(T value, Encoding? encoding = null);

    /// <summary>Writes <paramref name="value" /> into <paramref name="destination" />. Returns false when the buffer is too small.</summary>
    bool TryExport<T>(T value, Span<byte> destination, out int bytesWritten, Encoding? encoding = null);

    /// <summary>
    /// Writes one object per item. Each field is <paramref name="fields" /> in dictionary order. <c>Order</c> does not reorder these keys. A null formatter result is an empty
    /// string.
    /// </summary>
    string ExportFormattedToYamlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields);

    /// <summary>Writes <see cref="ExportFormattedToYamlString{T}" /> to <paramref name="yamlStream" />. The stream is left open.</summary>
    void ExportFormattedToYaml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream yamlStream);

    /// <summary>Writes one object per item. The key is the field name and the value is the property value, in dictionary order. <c>Order</c> does not reorder these keys.</summary>
    string ExportColumnsToYamlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns);

    /// <summary>Writes <see cref="ExportColumnsToYamlString{T}" /> to <paramref name="yamlStream" />. The stream is left open.</summary>
    void ExportColumnsToYaml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream yamlStream);

    /// <summary>
    /// Writes <paramref name="dataTable" /> as a sequence of maps. Header text is the key. A blank header is <c>Column{index}</c>. A repeated header throws. Empty cells stay
    /// empty strings. A footer is the last map.
    /// </summary>
    string ExportToYamlStringFromDataTable(DataTable.Models.DataTable dataTable);

    /// <summary>Writes <see cref="ExportToYamlStringFromDataTable" /> to <paramref name="yamlFilePath" />.</summary>
    void ExportToYamlFromDataTable(DataTable.Models.DataTable dataTable, string yamlFilePath);

    /// <summary>Writes <see cref="ExportToYamlStringFromDataTable" /> to <paramref name="yamlStream" />. The stream is left open.</summary>
    void ExportToYamlFromDataTable(DataTable.Models.DataTable dataTable, Stream yamlStream);

    /// <summary>Serializes <paramref name="dataTable" /> to bytes. A null <paramref name="encoding" /> uses the service encoding.</summary>
    byte[] ExportToYamlBytesFromDataTable(DataTable.Models.DataTable dataTable, Encoding? encoding = null);

#if !NETSTANDARD2_0
    /// <summary>Writes <paramref name="value" /> to <paramref name="yamlFilePath" />.</summary>
    Task ExportToYamlAsync<T>(T value, string yamlFilePath, CancellationToken ct = default);

    /// <summary>Writes <paramref name="value" /> to <paramref name="yamlStream" />. The stream is left open.</summary>
    Task ExportToYamlAsync<T>(T value, Stream yamlStream, CancellationToken ct = default);

    /// <summary>Serializes <paramref name="value" /> to a YAML string.</summary>
    Task<string> ExportToYamlStringAsync<T>(T value, CancellationToken ct = default);

    /// <summary>Serializes <paramref name="value" /> to UTF-8 YAML bytes.</summary>
    Task<byte[]> ExportToYamlBytesAsync<T>(T value, CancellationToken ct = default);

    /// <summary>Writes <see cref="ExportFormattedToYamlString{T}" /> to <paramref name="yamlStream" />. The stream is left open.</summary>
    Task ExportFormattedToYamlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream yamlStream, CancellationToken ct = default);

    /// <summary>Writes <see cref="ExportColumnsToYamlString{T}" /> to <paramref name="yamlStream" />. The stream is left open.</summary>
    Task ExportColumnsToYamlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream yamlStream, CancellationToken ct = default);

    /// <summary>Writes <see cref="ExportToYamlStringFromDataTable" /> to <paramref name="yamlFilePath" />.</summary>
    Task ExportToYamlFromDataTableAsync(DataTable.Models.DataTable dataTable, string yamlFilePath, CancellationToken ct = default);

    /// <summary>Writes <see cref="ExportToYamlStringFromDataTable" /> to <paramref name="yamlStream" />. The stream is left open.</summary>
    Task ExportToYamlFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream yamlStream, CancellationToken ct = default);

    /// <summary>Serializes <paramref name="dataTable" /> to a YAML string.</summary>
    Task<string> ExportToYamlStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    /// <summary>Serializes <paramref name="dataTable" /> to bytes. A null <paramref name="encoding" /> uses the service encoding.</summary>
    Task<byte[]> ExportToYamlBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, Encoding? encoding = null, CancellationToken ct = default);
#endif
}