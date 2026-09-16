using System.Reflection;
using System.Text;
using Lyo.Result;

namespace Lyo.Xml;

/// <summary>Reads and writes XML, and converts it to and from JSON. Methods forward to <see cref="Reader" /> and <see cref="Writer" />.</summary>
public interface IXmlService
{
    /// <summary>Owned reader.</summary>
    IXmlReader Reader { get; }

    /// <summary>Owned writer.</summary>
    IXmlWriter Writer { get; }

    /// <inheritdoc cref="IXmlReader.ParseFile{T}" />
    T ParseFile<T>(string xmlFilePath);

    /// <inheritdoc cref="IXmlReader.ParseStream{T}" />
    T ParseStream<T>(Stream xmlStream);

    /// <inheritdoc cref="IXmlReader.ParseBytes{T}" />
    T ParseBytes<T>(byte[] xmlBytes, Encoding? encoding = null);

    /// <inheritdoc cref="IXmlReader.Parse{T}(ReadOnlySpan{byte}, Encoding)" />
    T Parse<T>(ReadOnlySpan<byte> xml, Encoding? encoding = null);

    /// <inheritdoc cref="IXmlReader.Parse{T}(ReadOnlySpan{char})" />
    T Parse<T>(ReadOnlySpan<char> xml);

    /// <inheritdoc cref="IXmlReader.ParseString{T}" />
    T ParseString<T>(string xml);

    /// <inheritdoc cref="IXmlWriter.ExportToXml{T}(T, string)" />
    void ExportToXml<T>(T value, string xmlFilePath);

    /// <inheritdoc cref="IXmlWriter.ExportToXml{T}(T, Stream)" />
    void ExportToXml<T>(T value, Stream xmlStream);

    /// <inheritdoc cref="IXmlWriter.ExportToXmlString{T}" />
    string ExportToXmlString<T>(T value);

    /// <inheritdoc cref="IXmlWriter.ExportToXmlBytes{T}" />
    byte[] ExportToXmlBytes<T>(T value, Encoding? encoding = null);

    /// <inheritdoc cref="IXmlWriter.TryExport{T}" />
    bool TryExport<T>(T value, Span<byte> destination, out int bytesWritten, Encoding? encoding = null);

    /// <inheritdoc cref="IXmlWriter.ExportFormattedToXmlString{T}" />
    string ExportFormattedToXmlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields);

    /// <inheritdoc cref="IXmlWriter.ExportFormattedToXml{T}" />
    void ExportFormattedToXml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream xmlStream);

    /// <inheritdoc cref="IXmlWriter.ExportColumnsToXmlString{T}" />
    string ExportColumnsToXmlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns);

    /// <inheritdoc cref="IXmlWriter.ExportColumnsToXml{T}" />
    void ExportColumnsToXml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream xmlStream);

    /// <inheritdoc cref="IXmlReader.ParseStringAsDataTable" />
    Result<DataTable.Models.DataTable> ParseStringAsDataTable(string xml, bool hasFooterRow = false);

    /// <inheritdoc cref="IXmlReader.ParseBytesAsDataTable" />
    Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] xmlBytes, bool hasFooterRow = false, Encoding? encoding = null);

    /// <inheritdoc cref="IXmlReader.ParseStreamAsDataTable" />
    Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream xmlStream, bool hasFooterRow = false);

    /// <inheritdoc cref="IXmlReader.ParseFileAsDataTable" />
    Result<DataTable.Models.DataTable> ParseFileAsDataTable(string xmlFilePath, bool hasFooterRow = false);

    /// <inheritdoc cref="IXmlWriter.ExportToXmlStringFromDataTable" />
    string ExportToXmlStringFromDataTable(DataTable.Models.DataTable dataTable);

    /// <inheritdoc cref="IXmlWriter.ExportToXmlFromDataTable(DataTable.Models.DataTable, string)" />
    void ExportToXmlFromDataTable(DataTable.Models.DataTable dataTable, string xmlFilePath);

    /// <inheritdoc cref="IXmlWriter.ExportToXmlFromDataTable(DataTable.Models.DataTable, Stream)" />
    void ExportToXmlFromDataTable(DataTable.Models.DataTable dataTable, Stream xmlStream);

    /// <inheritdoc cref="IXmlWriter.ExportToXmlBytesFromDataTable" />
    byte[] ExportToXmlBytesFromDataTable(DataTable.Models.DataTable dataTable, Encoding? encoding = null);

    /// <summary>Converts an XML file to a JSON file.</summary>
    void ConvertToJson(string xmlFilePath, string jsonFilePath);

    /// <summary>Converts an XML stream to a JSON stream. Both streams are left open.</summary>
    void ConvertToJson(Stream xmlStream, Stream jsonStream);

    /// <summary>Converts XML bytes to UTF-8 JSON bytes.</summary>
    byte[] ConvertToJsonBytes(byte[] xmlBytes);

    /// <summary>Converts an XML string to a JSON string using the Lyo JSON contract.</summary>
    string ConvertToJson(string xml);

    /// <summary>Converts a JSON file to an XML file. The root element is <c>value</c> because the JSON text has no CLR type.</summary>
    void ConvertFromJson(string jsonFilePath, string xmlFilePath);

    /// <summary>Converts a JSON stream to an XML stream. Both streams are left open.</summary>
    void ConvertFromJson(Stream jsonStream, Stream xmlStream);

    /// <summary>Converts JSON bytes to UTF-8 XML bytes.</summary>
    byte[] ConvertFromJsonBytes(byte[] jsonBytes);

    /// <summary>Converts a JSON string to an XML string. The root element is <c>value</c> because the JSON text has no CLR type.</summary>
    string ConvertFromJson(string json);

#if !NETSTANDARD2_0
    /// <inheritdoc cref="IXmlReader.ParseFileAsync{T}" />
    Task<T> ParseFileAsync<T>(string xmlFilePath, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlReader.ParseStreamAsync{T}" />
    Task<T> ParseStreamAsync<T>(Stream xmlStream, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlReader.ParseBytesAsync{T}" />
    Task<T> ParseBytesAsync<T>(byte[] xmlBytes, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlReader.ParseStringAsync{T}" />
    Task<T> ParseStringAsync<T>(string xml, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlWriter.ExportToXmlAsync{T}(T, string, CancellationToken)" />
    Task ExportToXmlAsync<T>(T value, string xmlFilePath, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlWriter.ExportToXmlAsync{T}(T, Stream, CancellationToken)" />
    Task ExportToXmlAsync<T>(T value, Stream xmlStream, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlWriter.ExportToXmlStringAsync{T}" />
    Task<string> ExportToXmlStringAsync<T>(T value, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlWriter.ExportToXmlBytesAsync{T}" />
    Task<byte[]> ExportToXmlBytesAsync<T>(T value, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlWriter.ExportFormattedToXmlAsync{T}" />
    Task ExportFormattedToXmlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream xmlStream, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlWriter.ExportColumnsToXmlAsync{T}" />
    Task ExportColumnsToXmlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream xmlStream, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlReader.ParseStringAsDataTableAsync" />
    Task<Result<DataTable.Models.DataTable>> ParseStringAsDataTableAsync(string xml, bool hasFooterRow = false, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlReader.ParseBytesAsDataTableAsync" />
    Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] xmlBytes, bool hasFooterRow = false, Encoding? encoding = null, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlReader.ParseStreamAsDataTableAsync" />
    Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream xmlStream, bool hasFooterRow = false, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlReader.ParseFileAsDataTableAsync" />
    Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string xmlFilePath, bool hasFooterRow = false, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlWriter.ExportToXmlStringFromDataTableAsync" />
    Task<string> ExportToXmlStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlWriter.ExportToXmlFromDataTableAsync(DataTable.Models.DataTable, string, CancellationToken)" />
    Task ExportToXmlFromDataTableAsync(DataTable.Models.DataTable dataTable, string xmlFilePath, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlWriter.ExportToXmlFromDataTableAsync(DataTable.Models.DataTable, Stream, CancellationToken)" />
    Task ExportToXmlFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream xmlStream, CancellationToken ct = default);

    /// <inheritdoc cref="IXmlWriter.ExportToXmlBytesFromDataTableAsync" />
    Task<byte[]> ExportToXmlBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, Encoding? encoding = null, CancellationToken ct = default);
#endif
}
