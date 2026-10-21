using System.Reflection;
using System.Text;
using Lyo.Exceptions;
using Lyo.Result;

namespace Lyo.Xml;

/// <summary>Facade over <see cref="XmlReader" /> and <see cref="XmlWriter" />, plus JSON conversion. One instance is safe for concurrent calls.</summary>
public sealed class XmlService : IXmlService
{
    private readonly XmlEngine _engine;
    private readonly XmlReader _reader;
    private readonly XmlWriter _writer;

    /// <summary>Shared service with the default options. Do not change it.</summary>
    public static XmlService Default { get; } = new();

    static XmlService() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>Builds a service. <paramref name="configure" /> runs before validation.</summary>
    public XmlService(Action<XmlOptions>? configure = null)
    {
        var options = new XmlOptions();
        configure?.Invoke(options);
        options.Validate();
        _engine = new(options);
        _reader = new(_engine);
        _writer = new(_engine);
    }

    /// <inheritdoc />
    public IXmlReader Reader => _reader;

    /// <inheritdoc />
    public IXmlWriter Writer => _writer;

    /// <inheritdoc />
    public T ParseFile<T>(string xmlFilePath) => _reader.ParseFile<T>(xmlFilePath);

    /// <inheritdoc />
    public T ParseStream<T>(Stream xmlStream) => _reader.ParseStream<T>(xmlStream);

    /// <inheritdoc />
    public T ParseBytes<T>(byte[] xmlBytes, Encoding? encoding = null) => _reader.ParseBytes<T>(xmlBytes, encoding);

    /// <inheritdoc />
    public T Parse<T>(ReadOnlySpan<byte> xml, Encoding? encoding = null) => _reader.Parse<T>(xml, encoding);

    /// <inheritdoc />
    public T Parse<T>(ReadOnlySpan<char> xml) => _reader.Parse<T>(xml);

    /// <inheritdoc />
    public T ParseString<T>(string xml) => _reader.ParseString<T>(xml);

    /// <inheritdoc />
    public void ExportToXml<T>(T value, string xmlFilePath) => _writer.ExportToXml(value, xmlFilePath);

    /// <inheritdoc />
    public void ExportToXml<T>(T value, Stream xmlStream) => _writer.ExportToXml(value, xmlStream);

    /// <inheritdoc />
    public string ExportToXmlString<T>(T value) => _writer.ExportToXmlString(value);

    /// <inheritdoc />
    public byte[] ExportToXmlBytes<T>(T value, Encoding? encoding = null) => _writer.ExportToXmlBytes(value, encoding);

    /// <inheritdoc />
    public bool TryExport<T>(T value, Span<byte> destination, out int bytesWritten, Encoding? encoding = null) => _writer.TryExport(value, destination, out bytesWritten, encoding);

    /// <inheritdoc />
    public string ExportFormattedToXmlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields) => _writer.ExportFormattedToXmlString(items, fields);

    /// <inheritdoc />
    public void ExportFormattedToXml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream xmlStream)
        => _writer.ExportFormattedToXml(items, fields, xmlStream);

    /// <inheritdoc />
    public string ExportColumnsToXmlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns) => _writer.ExportColumnsToXmlString(items, columns);

    /// <inheritdoc />
    public void ExportColumnsToXml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream xmlStream)
        => _writer.ExportColumnsToXml(items, columns, xmlStream);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStringAsDataTable(string xml, bool hasFooterRow = false) => _reader.ParseStringAsDataTable(xml, hasFooterRow);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] xmlBytes, bool hasFooterRow = false, Encoding? encoding = null)
        => _reader.ParseBytesAsDataTable(xmlBytes, hasFooterRow, encoding);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream xmlStream, bool hasFooterRow = false) => _reader.ParseStreamAsDataTable(xmlStream, hasFooterRow);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseFileAsDataTable(string xmlFilePath, bool hasFooterRow = false) => _reader.ParseFileAsDataTable(xmlFilePath, hasFooterRow);

    /// <inheritdoc />
    public string ExportToXmlStringFromDataTable(DataTable.Models.DataTable dataTable) => _writer.ExportToXmlStringFromDataTable(dataTable);

    /// <inheritdoc />
    public void ExportToXmlFromDataTable(DataTable.Models.DataTable dataTable, string xmlFilePath) => _writer.ExportToXmlFromDataTable(dataTable, xmlFilePath);

    /// <inheritdoc />
    public void ExportToXmlFromDataTable(DataTable.Models.DataTable dataTable, Stream xmlStream) => _writer.ExportToXmlFromDataTable(dataTable, xmlStream);

    /// <inheritdoc />
    public byte[] ExportToXmlBytesFromDataTable(DataTable.Models.DataTable dataTable, Encoding? encoding = null) => _writer.ExportToXmlBytesFromDataTable(dataTable, encoding);

    /// <inheritdoc />
    public void ConvertToJson(string xmlFilePath, string jsonFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xmlFilePath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        File.WriteAllText(jsonFilePath, ConvertToJson(File.ReadAllText(xmlFilePath)));
    }

    /// <inheritdoc />
    public void ConvertToJson(Stream xmlStream, Stream jsonStream)
    {
        ArgumentHelpers.ThrowIfNull(xmlStream);
        ArgumentHelpers.ThrowIfNull(jsonStream);
        var bytes = Encoding.UTF8.GetBytes(ConvertToJson(Read(xmlStream)));
        jsonStream.Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc />
    public byte[] ConvertToJsonBytes(byte[] xmlBytes)
    {
        ArgumentHelpers.ThrowIfNull(xmlBytes);
        return Encoding.UTF8.GetBytes(ConvertToJson(Encoding.UTF8.GetString(xmlBytes)));
    }

    /// <inheritdoc />
    public string ConvertToJson(string xml)
    {
        ArgumentHelpers.ThrowIfNull(xml);
        return _engine.ToJson(xml);
    }

    /// <inheritdoc />
    public void ConvertFromJson(string jsonFilePath, string xmlFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xmlFilePath);
        File.WriteAllBytes(xmlFilePath, _engine.Encoding.GetBytes(ConvertFromJson(File.ReadAllText(jsonFilePath))));
    }

    /// <inheritdoc />
    public void ConvertFromJson(Stream jsonStream, Stream xmlStream)
    {
        ArgumentHelpers.ThrowIfNull(jsonStream);
        ArgumentHelpers.ThrowIfNull(xmlStream);
        var bytes = _engine.Encoding.GetBytes(ConvertFromJson(Read(jsonStream)));
        xmlStream.Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc />
    public byte[] ConvertFromJsonBytes(byte[] jsonBytes)
    {
        ArgumentHelpers.ThrowIfNull(jsonBytes);
        return Encoding.UTF8.GetBytes(ConvertFromJson(Encoding.UTF8.GetString(jsonBytes)));
    }

    /// <inheritdoc />
    public string ConvertFromJson(string json)
    {
        ArgumentHelpers.ThrowIfNull(json);
        return _engine.FromJson(json);
    }

    private static string Read(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
        return reader.ReadToEnd();
    }

#if !NETSTANDARD2_0
    /// <inheritdoc />
    public Task<T> ParseFileAsync<T>(string xmlFilePath, CancellationToken ct = default) => _reader.ParseFileAsync<T>(xmlFilePath, ct);

    /// <inheritdoc />
    public Task<T> ParseStreamAsync<T>(Stream xmlStream, CancellationToken ct = default) => _reader.ParseStreamAsync<T>(xmlStream, ct);

    /// <inheritdoc />
    public Task<T> ParseBytesAsync<T>(byte[] xmlBytes, CancellationToken ct = default) => _reader.ParseBytesAsync<T>(xmlBytes, ct);

    /// <inheritdoc />
    public Task<T> ParseStringAsync<T>(string xml, CancellationToken ct = default) => _reader.ParseStringAsync<T>(xml, ct);

    /// <inheritdoc />
    public Task ExportToXmlAsync<T>(T value, string xmlFilePath, CancellationToken ct = default) => _writer.ExportToXmlAsync(value, xmlFilePath, ct);

    /// <inheritdoc />
    public Task ExportToXmlAsync<T>(T value, Stream xmlStream, CancellationToken ct = default) => _writer.ExportToXmlAsync(value, xmlStream, ct);

    /// <inheritdoc />
    public Task<string> ExportToXmlStringAsync<T>(T value, CancellationToken ct = default) => _writer.ExportToXmlStringAsync(value, ct);

    /// <inheritdoc />
    public Task<byte[]> ExportToXmlBytesAsync<T>(T value, CancellationToken ct = default) => _writer.ExportToXmlBytesAsync(value, ct);

    /// <inheritdoc />
    public Task ExportFormattedToXmlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream xmlStream, CancellationToken ct = default)
        => _writer.ExportFormattedToXmlAsync(items, fields, xmlStream, ct);

    /// <inheritdoc />
    public Task ExportColumnsToXmlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream xmlStream, CancellationToken ct = default)
        => _writer.ExportColumnsToXmlAsync(items, columns, xmlStream, ct);

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseStringAsDataTableAsync(string xml, bool hasFooterRow = false, CancellationToken ct = default)
        => _reader.ParseStringAsDataTableAsync(xml, hasFooterRow, ct);

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(
        byte[] xmlBytes,
        bool hasFooterRow = false,
        Encoding? encoding = null,
        CancellationToken ct = default)
        => _reader.ParseBytesAsDataTableAsync(xmlBytes, hasFooterRow, encoding, ct);

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream xmlStream, bool hasFooterRow = false, CancellationToken ct = default)
        => _reader.ParseStreamAsDataTableAsync(xmlStream, hasFooterRow, ct);

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string xmlFilePath, bool hasFooterRow = false, CancellationToken ct = default)
        => _reader.ParseFileAsDataTableAsync(xmlFilePath, hasFooterRow, ct);

    /// <inheritdoc />
    public Task<string> ExportToXmlStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
        => _writer.ExportToXmlStringFromDataTableAsync(dataTable, ct);

    /// <inheritdoc />
    public Task ExportToXmlFromDataTableAsync(DataTable.Models.DataTable dataTable, string xmlFilePath, CancellationToken ct = default)
        => _writer.ExportToXmlFromDataTableAsync(dataTable, xmlFilePath, ct);

    /// <inheritdoc />
    public Task ExportToXmlFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream xmlStream, CancellationToken ct = default)
        => _writer.ExportToXmlFromDataTableAsync(dataTable, xmlStream, ct);

    /// <inheritdoc />
    public Task<byte[]> ExportToXmlBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, Encoding? encoding = null, CancellationToken ct = default)
        => _writer.ExportToXmlBytesFromDataTableAsync(dataTable, encoding, ct);
#endif
}