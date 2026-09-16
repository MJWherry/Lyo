using System.Reflection;
using System.Text;
using Lyo.Result;

namespace Lyo.Toml;

/// <summary>Reads and writes TOML, and converts it to and from JSON. Methods forward to <see cref="Reader" /> and <see cref="Writer" />.</summary>
public interface ITomlService
{
    /// <summary>Owned reader.</summary>
    ITomlReader Reader { get; }

    /// <summary>Owned writer.</summary>
    ITomlWriter Writer { get; }

    /// <inheritdoc cref="ITomlReader.ParseFile{T}" />
    T ParseFile<T>(string tomlFilePath);

    /// <inheritdoc cref="ITomlReader.ParseStream{T}" />
    T ParseStream<T>(Stream tomlStream);

    /// <inheritdoc cref="ITomlReader.ParseBytes{T}" />
    T ParseBytes<T>(byte[] tomlBytes, Encoding? encoding = null);

    /// <inheritdoc cref="ITomlReader.Parse{T}(ReadOnlySpan{byte}, Encoding)" />
    T Parse<T>(ReadOnlySpan<byte> toml, Encoding? encoding = null);

    /// <inheritdoc cref="ITomlReader.Parse{T}(ReadOnlySpan{char})" />
    T Parse<T>(ReadOnlySpan<char> toml);

    /// <inheritdoc cref="ITomlReader.ParseString{T}" />
    T ParseString<T>(string toml);

    /// <inheritdoc cref="ITomlReader.ParseStringAsDataTable" />
    Result<DataTable.Models.DataTable> ParseStringAsDataTable(string toml, bool hasFooterRow = false);

    /// <inheritdoc cref="ITomlReader.ParseBytesAsDataTable" />
    Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] tomlBytes, bool hasFooterRow = false, Encoding? encoding = null);

    /// <inheritdoc cref="ITomlReader.ParseStreamAsDataTable" />
    Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream tomlStream, bool hasFooterRow = false);

    /// <inheritdoc cref="ITomlReader.ParseFileAsDataTable" />
    Result<DataTable.Models.DataTable> ParseFileAsDataTable(string tomlFilePath, bool hasFooterRow = false);

    /// <inheritdoc cref="ITomlWriter.ExportToToml{T}(T, string)" />
    void ExportToToml<T>(T value, string tomlFilePath);

    /// <inheritdoc cref="ITomlWriter.ExportToToml{T}(T, Stream)" />
    void ExportToToml<T>(T value, Stream tomlStream);

    /// <inheritdoc cref="ITomlWriter.ExportToTomlString{T}" />
    string ExportToTomlString<T>(T value);

    /// <inheritdoc cref="ITomlWriter.ExportToTomlBytes{T}" />
    byte[] ExportToTomlBytes<T>(T value, Encoding? encoding = null);

    /// <inheritdoc cref="ITomlWriter.TryExport{T}" />
    bool TryExport<T>(T value, Span<byte> destination, out int bytesWritten, Encoding? encoding = null);

    /// <inheritdoc cref="ITomlWriter.ExportFormattedToTomlString{T}" />
    string ExportFormattedToTomlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields);

    /// <inheritdoc cref="ITomlWriter.ExportFormattedToToml{T}" />
    void ExportFormattedToToml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream tomlStream);

    /// <inheritdoc cref="ITomlWriter.ExportColumnsToTomlString{T}" />
    string ExportColumnsToTomlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns);

    /// <inheritdoc cref="ITomlWriter.ExportColumnsToToml{T}" />
    void ExportColumnsToToml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream tomlStream);

    /// <inheritdoc cref="ITomlWriter.ExportToTomlStringFromDataTable" />
    string ExportToTomlStringFromDataTable(DataTable.Models.DataTable dataTable);

    /// <inheritdoc cref="ITomlWriter.ExportToTomlFromDataTable(DataTable.Models.DataTable, string)" />
    void ExportToTomlFromDataTable(DataTable.Models.DataTable dataTable, string tomlFilePath);

    /// <inheritdoc cref="ITomlWriter.ExportToTomlFromDataTable(DataTable.Models.DataTable, Stream)" />
    void ExportToTomlFromDataTable(DataTable.Models.DataTable dataTable, Stream tomlStream);

    /// <inheritdoc cref="ITomlWriter.ExportToTomlBytesFromDataTable" />
    byte[] ExportToTomlBytesFromDataTable(DataTable.Models.DataTable dataTable, Encoding? encoding = null);

    /// <summary>Converts a TOML file to a JSON file.</summary>
    void ConvertToJson(string tomlFilePath, string jsonFilePath);

    /// <summary>Converts a TOML stream to a JSON stream. Both streams are left open.</summary>
    void ConvertToJson(Stream tomlStream, Stream jsonStream);

    /// <summary>Converts TOML bytes to UTF-8 JSON bytes.</summary>
    byte[] ConvertToJsonBytes(byte[] tomlBytes);

    /// <summary>Converts a TOML string to a JSON string using the Lyo JSON contract.</summary>
    string ConvertToJson(string toml);

    /// <summary>Converts a JSON file to a TOML file. A JSON array becomes <c>[[items]]</c>.</summary>
    void ConvertFromJson(string jsonFilePath, string tomlFilePath);

    /// <summary>Converts a JSON stream to a TOML stream. Both streams are left open.</summary>
    void ConvertFromJson(Stream jsonStream, Stream tomlStream);

    /// <summary>Converts JSON bytes to UTF-8 TOML bytes.</summary>
    byte[] ConvertFromJsonBytes(byte[] jsonBytes);

    /// <summary>Converts a JSON string to a TOML string. A JSON array becomes <c>[[items]]</c>.</summary>
    string ConvertFromJson(string json);

#if !NETSTANDARD2_0
    /// <inheritdoc cref="ITomlReader.ParseFileAsync{T}" />
    Task<T> ParseFileAsync<T>(string tomlFilePath, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlReader.ParseStreamAsync{T}" />
    Task<T> ParseStreamAsync<T>(Stream tomlStream, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlReader.ParseBytesAsync{T}" />
    Task<T> ParseBytesAsync<T>(byte[] tomlBytes, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlReader.ParseStringAsync{T}" />
    Task<T> ParseStringAsync<T>(string toml, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlWriter.ExportToTomlAsync{T}(T, string, CancellationToken)" />
    Task ExportToTomlAsync<T>(T value, string tomlFilePath, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlWriter.ExportToTomlAsync{T}(T, Stream, CancellationToken)" />
    Task ExportToTomlAsync<T>(T value, Stream tomlStream, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlWriter.ExportToTomlStringAsync{T}" />
    Task<string> ExportToTomlStringAsync<T>(T value, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlWriter.ExportToTomlBytesAsync{T}" />
    Task<byte[]> ExportToTomlBytesAsync<T>(T value, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlWriter.ExportFormattedToTomlAsync{T}" />
    Task ExportFormattedToTomlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream tomlStream, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlWriter.ExportColumnsToTomlAsync{T}" />
    Task ExportColumnsToTomlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream tomlStream, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlWriter.ExportToTomlFromDataTableAsync(DataTable.Models.DataTable, string, CancellationToken)" />
    Task ExportToTomlFromDataTableAsync(DataTable.Models.DataTable dataTable, string tomlFilePath, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlWriter.ExportToTomlFromDataTableAsync(DataTable.Models.DataTable, Stream, CancellationToken)" />
    Task ExportToTomlFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream tomlStream, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlWriter.ExportToTomlStringFromDataTableAsync" />
    Task<string> ExportToTomlStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlWriter.ExportToTomlBytesFromDataTableAsync" />
    Task<byte[]> ExportToTomlBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, Encoding? encoding = null, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlReader.ParseStringAsDataTableAsync" />
    Task<Result<DataTable.Models.DataTable>> ParseStringAsDataTableAsync(string toml, bool hasFooterRow = false, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlReader.ParseBytesAsDataTableAsync" />
    Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] tomlBytes, bool hasFooterRow = false, Encoding? encoding = null, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlReader.ParseStreamAsDataTableAsync" />
    Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream tomlStream, bool hasFooterRow = false, CancellationToken ct = default);

    /// <inheritdoc cref="ITomlReader.ParseFileAsDataTableAsync" />
    Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string tomlFilePath, bool hasFooterRow = false, CancellationToken ct = default);
#endif
}
