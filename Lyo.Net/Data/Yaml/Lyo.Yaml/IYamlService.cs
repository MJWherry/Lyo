using System.Reflection;
using System.Text;
using Lyo.Result;

namespace Lyo.Yaml;

/// <summary>Reads and writes YAML, and converts it to and from JSON. Methods forward to <see cref="Reader" /> and <see cref="Writer" />.</summary>
public interface IYamlService
{
    /// <summary>Owned reader.</summary>
    IYamlReader Reader { get; }

    /// <summary>Owned writer.</summary>
    IYamlWriter Writer { get; }

    /// <inheritdoc cref="IYamlReader.ParseFile{T}" />
    T ParseFile<T>(string yamlFilePath);

    /// <inheritdoc cref="IYamlReader.ParseStream{T}" />
    T ParseStream<T>(Stream yamlStream);

    /// <inheritdoc cref="IYamlReader.ParseBytes{T}" />
    T ParseBytes<T>(byte[] yamlBytes, Encoding? encoding = null);

    /// <inheritdoc cref="IYamlReader.Parse{T}(ReadOnlySpan{byte}, Encoding)" />
    T Parse<T>(ReadOnlySpan<byte> yaml, Encoding? encoding = null);

    /// <inheritdoc cref="IYamlReader.Parse{T}(ReadOnlySpan{char})" />
    T Parse<T>(ReadOnlySpan<char> yaml);

    /// <inheritdoc cref="IYamlReader.ParseString{T}" />
    T ParseString<T>(string yaml);

    /// <inheritdoc cref="IYamlReader.ParseStringAsDataTable" />
    Result<DataTable.Models.DataTable> ParseStringAsDataTable(string yaml, bool hasFooterRow = false);

    /// <inheritdoc cref="IYamlReader.ParseBytesAsDataTable" />
    Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] yamlBytes, bool hasFooterRow = false, Encoding? encoding = null);

    /// <inheritdoc cref="IYamlReader.ParseStreamAsDataTable" />
    Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream yamlStream, bool hasFooterRow = false);

    /// <inheritdoc cref="IYamlReader.ParseFileAsDataTable" />
    Result<DataTable.Models.DataTable> ParseFileAsDataTable(string yamlFilePath, bool hasFooterRow = false);

    /// <inheritdoc cref="IYamlWriter.ExportToYaml{T}(T, string)" />
    void ExportToYaml<T>(T value, string yamlFilePath);

    /// <inheritdoc cref="IYamlWriter.ExportToYaml{T}(T, Stream)" />
    void ExportToYaml<T>(T value, Stream yamlStream);

    /// <inheritdoc cref="IYamlWriter.ExportToYamlString{T}" />
    string ExportToYamlString<T>(T value);

    /// <inheritdoc cref="IYamlWriter.ExportToYamlBytes{T}" />
    byte[] ExportToYamlBytes<T>(T value, Encoding? encoding = null);

    /// <inheritdoc cref="IYamlWriter.TryExport{T}" />
    bool TryExport<T>(T value, Span<byte> destination, out int bytesWritten, Encoding? encoding = null);

    /// <inheritdoc cref="IYamlWriter.ExportFormattedToYamlString{T}" />
    string ExportFormattedToYamlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields);

    /// <inheritdoc cref="IYamlWriter.ExportFormattedToYaml{T}" />
    void ExportFormattedToYaml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream yamlStream);

    /// <inheritdoc cref="IYamlWriter.ExportColumnsToYamlString{T}" />
    string ExportColumnsToYamlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns);

    /// <inheritdoc cref="IYamlWriter.ExportColumnsToYaml{T}" />
    void ExportColumnsToYaml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream yamlStream);

    /// <inheritdoc cref="IYamlWriter.ExportToYamlStringFromDataTable" />
    string ExportToYamlStringFromDataTable(DataTable.Models.DataTable dataTable);

    /// <inheritdoc cref="IYamlWriter.ExportToYamlFromDataTable(DataTable.Models.DataTable, string)" />
    void ExportToYamlFromDataTable(DataTable.Models.DataTable dataTable, string yamlFilePath);

    /// <inheritdoc cref="IYamlWriter.ExportToYamlFromDataTable(DataTable.Models.DataTable, Stream)" />
    void ExportToYamlFromDataTable(DataTable.Models.DataTable dataTable, Stream yamlStream);

    /// <inheritdoc cref="IYamlWriter.ExportToYamlBytesFromDataTable" />
    byte[] ExportToYamlBytesFromDataTable(DataTable.Models.DataTable dataTable, Encoding? encoding = null);

    /// <summary>Converts a YAML file to a JSON file.</summary>
    void ConvertToJson(string yamlFilePath, string jsonFilePath);

    /// <summary>Converts a YAML stream to a JSON stream. Both streams are left open.</summary>
    void ConvertToJson(Stream yamlStream, Stream jsonStream);

    /// <summary>Converts YAML bytes to UTF-8 JSON bytes.</summary>
    byte[] ConvertToJsonBytes(byte[] yamlBytes);

    /// <summary>Converts a YAML string to a JSON string using the Lyo JSON contract.</summary>
    string ConvertToJson(string yaml);

    /// <summary>Converts a JSON file to a YAML file.</summary>
    void ConvertFromJson(string jsonFilePath, string yamlFilePath);

    /// <summary>Converts a JSON stream to a YAML stream. Both streams are left open.</summary>
    void ConvertFromJson(Stream jsonStream, Stream yamlStream);

    /// <summary>Converts JSON bytes to UTF-8 YAML bytes.</summary>
    byte[] ConvertFromJsonBytes(byte[] jsonBytes);

    /// <summary>Converts a JSON string to a YAML string.</summary>
    string ConvertFromJson(string json);

#if !NETSTANDARD2_0
    /// <inheritdoc cref="IYamlReader.ParseFileAsync{T}" />
    Task<T> ParseFileAsync<T>(string yamlFilePath, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlReader.ParseStreamAsync{T}" />
    Task<T> ParseStreamAsync<T>(Stream yamlStream, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlReader.ParseBytesAsync{T}" />
    Task<T> ParseBytesAsync<T>(byte[] yamlBytes, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlReader.ParseStringAsync{T}" />
    Task<T> ParseStringAsync<T>(string yaml, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlWriter.ExportToYamlAsync{T}(T, string, CancellationToken)" />
    Task ExportToYamlAsync<T>(T value, string yamlFilePath, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlWriter.ExportToYamlAsync{T}(T, Stream, CancellationToken)" />
    Task ExportToYamlAsync<T>(T value, Stream yamlStream, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlWriter.ExportToYamlStringAsync{T}" />
    Task<string> ExportToYamlStringAsync<T>(T value, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlWriter.ExportToYamlBytesAsync{T}" />
    Task<byte[]> ExportToYamlBytesAsync<T>(T value, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlWriter.ExportFormattedToYamlAsync{T}" />
    Task ExportFormattedToYamlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream yamlStream, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlWriter.ExportColumnsToYamlAsync{T}" />
    Task ExportColumnsToYamlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream yamlStream, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlWriter.ExportToYamlFromDataTableAsync(DataTable.Models.DataTable, string, CancellationToken)" />
    Task ExportToYamlFromDataTableAsync(DataTable.Models.DataTable dataTable, string yamlFilePath, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlWriter.ExportToYamlFromDataTableAsync(DataTable.Models.DataTable, Stream, CancellationToken)" />
    Task ExportToYamlFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream yamlStream, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlWriter.ExportToYamlStringFromDataTableAsync" />
    Task<string> ExportToYamlStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlWriter.ExportToYamlBytesFromDataTableAsync" />
    Task<byte[]> ExportToYamlBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, Encoding? encoding = null, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlReader.ParseStringAsDataTableAsync" />
    Task<Result<DataTable.Models.DataTable>> ParseStringAsDataTableAsync(string yaml, bool hasFooterRow = false, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlReader.ParseBytesAsDataTableAsync" />
    Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] yamlBytes, bool hasFooterRow = false, Encoding? encoding = null, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlReader.ParseStreamAsDataTableAsync" />
    Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream yamlStream, bool hasFooterRow = false, CancellationToken ct = default);

    /// <inheritdoc cref="IYamlReader.ParseFileAsDataTableAsync" />
    Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string yamlFilePath, bool hasFooterRow = false, CancellationToken ct = default);
#endif
}