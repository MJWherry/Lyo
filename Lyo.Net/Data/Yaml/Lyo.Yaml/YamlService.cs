using System.Reflection;
using System.Text;
using Lyo.Exceptions;
using Lyo.Result;

namespace Lyo.Yaml;

/// <summary>Facade over <see cref="YamlReader" /> and <see cref="YamlWriter" />, plus JSON conversion. One instance is safe for concurrent calls.</summary>
public sealed class YamlService : IYamlService
{
    private readonly YamlEngine _engine;
    private readonly YamlReader _reader;
    private readonly YamlWriter _writer;

    /// <summary>Shared service with the default options. Do not change it.</summary>
    public static YamlService Default { get; } = new();

    static YamlService() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>Builds a service. <paramref name="configure" /> runs before validation.</summary>
    public YamlService(Action<YamlOptions>? configure = null)
    {
        var options = new YamlOptions();
        configure?.Invoke(options);
        options.Validate();
        _engine = new(options);
        _reader = new(_engine);
        _writer = new(_engine);
    }

    /// <inheritdoc />
    public IYamlReader Reader => _reader;

    /// <inheritdoc />
    public IYamlWriter Writer => _writer;

    /// <inheritdoc />
    public T ParseFile<T>(string yamlFilePath) => _reader.ParseFile<T>(yamlFilePath);

    /// <inheritdoc />
    public T ParseStream<T>(Stream yamlStream) => _reader.ParseStream<T>(yamlStream);

    /// <inheritdoc />
    public T ParseBytes<T>(byte[] yamlBytes, Encoding? encoding = null) => _reader.ParseBytes<T>(yamlBytes, encoding);

    /// <inheritdoc />
    public T Parse<T>(ReadOnlySpan<byte> yaml, Encoding? encoding = null) => _reader.Parse<T>(yaml, encoding);

    /// <inheritdoc />
    public T Parse<T>(ReadOnlySpan<char> yaml) => _reader.Parse<T>(yaml);

    /// <inheritdoc />
    public T ParseString<T>(string yaml) => _reader.ParseString<T>(yaml);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStringAsDataTable(string yaml, bool hasFooterRow = false) => _reader.ParseStringAsDataTable(yaml, hasFooterRow);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] yamlBytes, bool hasFooterRow = false, Encoding? encoding = null)
        => _reader.ParseBytesAsDataTable(yamlBytes, hasFooterRow, encoding);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream yamlStream, bool hasFooterRow = false) => _reader.ParseStreamAsDataTable(yamlStream, hasFooterRow);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseFileAsDataTable(string yamlFilePath, bool hasFooterRow = false) => _reader.ParseFileAsDataTable(yamlFilePath, hasFooterRow);

    /// <inheritdoc />
    public void ExportToYaml<T>(T value, string yamlFilePath) => _writer.ExportToYaml(value, yamlFilePath);

    /// <inheritdoc />
    public void ExportToYaml<T>(T value, Stream yamlStream) => _writer.ExportToYaml(value, yamlStream);

    /// <inheritdoc />
    public string ExportToYamlString<T>(T value) => _writer.ExportToYamlString(value);

    /// <inheritdoc />
    public byte[] ExportToYamlBytes<T>(T value, Encoding? encoding = null) => _writer.ExportToYamlBytes(value, encoding);

    /// <inheritdoc />
    public bool TryExport<T>(T value, Span<byte> destination, out int bytesWritten, Encoding? encoding = null) => _writer.TryExport(value, destination, out bytesWritten, encoding);

    /// <inheritdoc />
    public string ExportFormattedToYamlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields) => _writer.ExportFormattedToYamlString(items, fields);

    /// <inheritdoc />
    public void ExportFormattedToYaml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream yamlStream)
        => _writer.ExportFormattedToYaml(items, fields, yamlStream);

    /// <inheritdoc />
    public string ExportColumnsToYamlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns) => _writer.ExportColumnsToYamlString(items, columns);

    /// <inheritdoc />
    public void ExportColumnsToYaml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream yamlStream)
        => _writer.ExportColumnsToYaml(items, columns, yamlStream);

    /// <inheritdoc />
    public string ExportToYamlStringFromDataTable(DataTable.Models.DataTable dataTable) => _writer.ExportToYamlStringFromDataTable(dataTable);

    /// <inheritdoc />
    public void ExportToYamlFromDataTable(DataTable.Models.DataTable dataTable, string yamlFilePath) => _writer.ExportToYamlFromDataTable(dataTable, yamlFilePath);

    /// <inheritdoc />
    public void ExportToYamlFromDataTable(DataTable.Models.DataTable dataTable, Stream yamlStream) => _writer.ExportToYamlFromDataTable(dataTable, yamlStream);

    /// <inheritdoc />
    public byte[] ExportToYamlBytesFromDataTable(DataTable.Models.DataTable dataTable, Encoding? encoding = null) => _writer.ExportToYamlBytesFromDataTable(dataTable, encoding);

    /// <inheritdoc />
    public void ConvertToJson(string yamlFilePath, string jsonFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(yamlFilePath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        File.WriteAllText(jsonFilePath, ConvertToJson(File.ReadAllText(yamlFilePath)));
    }

    /// <inheritdoc />
    public void ConvertToJson(Stream yamlStream, Stream jsonStream)
    {
        ArgumentHelpers.ThrowIfNull(yamlStream);
        ArgumentHelpers.ThrowIfNull(jsonStream);
        var json = ConvertToJson(Read(yamlStream));
        var bytes = Encoding.UTF8.GetBytes(json);
        jsonStream.Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc />
    public byte[] ConvertToJsonBytes(byte[] yamlBytes)
    {
        ArgumentHelpers.ThrowIfNull(yamlBytes);
        return Encoding.UTF8.GetBytes(ConvertToJson(Encoding.UTF8.GetString(yamlBytes)));
    }

    /// <inheritdoc />
    public string ConvertToJson(string yaml)
    {
        ArgumentHelpers.ThrowIfNull(yaml);
        return _engine.ToJson(yaml);
    }

    /// <inheritdoc />
    public void ConvertFromJson(string jsonFilePath, string yamlFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(yamlFilePath);
        File.WriteAllBytes(yamlFilePath, _engine.Encoding.GetBytes(ConvertFromJson(File.ReadAllText(jsonFilePath))));
    }

    /// <inheritdoc />
    public void ConvertFromJson(Stream jsonStream, Stream yamlStream)
    {
        ArgumentHelpers.ThrowIfNull(jsonStream);
        ArgumentHelpers.ThrowIfNull(yamlStream);
        var bytes = _engine.Encoding.GetBytes(ConvertFromJson(Read(jsonStream)));
        yamlStream.Write(bytes, 0, bytes.Length);
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
    public Task<T> ParseFileAsync<T>(string yamlFilePath, CancellationToken ct = default) => _reader.ParseFileAsync<T>(yamlFilePath, ct);

    /// <inheritdoc />
    public Task<T> ParseStreamAsync<T>(Stream yamlStream, CancellationToken ct = default) => _reader.ParseStreamAsync<T>(yamlStream, ct);

    /// <inheritdoc />
    public Task<T> ParseBytesAsync<T>(byte[] yamlBytes, CancellationToken ct = default) => _reader.ParseBytesAsync<T>(yamlBytes, ct);

    /// <inheritdoc />
    public Task<T> ParseStringAsync<T>(string yaml, CancellationToken ct = default) => _reader.ParseStringAsync<T>(yaml, ct);

    /// <inheritdoc />
    public Task ExportToYamlAsync<T>(T value, string yamlFilePath, CancellationToken ct = default) => _writer.ExportToYamlAsync(value, yamlFilePath, ct);

    /// <inheritdoc />
    public Task ExportToYamlAsync<T>(T value, Stream yamlStream, CancellationToken ct = default) => _writer.ExportToYamlAsync(value, yamlStream, ct);

    /// <inheritdoc />
    public Task<string> ExportToYamlStringAsync<T>(T value, CancellationToken ct = default) => _writer.ExportToYamlStringAsync(value, ct);

    /// <inheritdoc />
    public Task<byte[]> ExportToYamlBytesAsync<T>(T value, CancellationToken ct = default) => _writer.ExportToYamlBytesAsync(value, ct);

    /// <inheritdoc />
    public Task ExportFormattedToYamlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream yamlStream, CancellationToken ct = default)
        => _writer.ExportFormattedToYamlAsync(items, fields, yamlStream, ct);

    /// <inheritdoc />
    public Task ExportColumnsToYamlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream yamlStream, CancellationToken ct = default)
        => _writer.ExportColumnsToYamlAsync(items, columns, yamlStream, ct);

    /// <inheritdoc />
    public Task ExportToYamlFromDataTableAsync(DataTable.Models.DataTable dataTable, string yamlFilePath, CancellationToken ct = default)
        => _writer.ExportToYamlFromDataTableAsync(dataTable, yamlFilePath, ct);

    /// <inheritdoc />
    public Task ExportToYamlFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream yamlStream, CancellationToken ct = default)
        => _writer.ExportToYamlFromDataTableAsync(dataTable, yamlStream, ct);

    /// <inheritdoc />
    public Task<string> ExportToYamlStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
        => _writer.ExportToYamlStringFromDataTableAsync(dataTable, ct);

    /// <inheritdoc />
    public Task<byte[]> ExportToYamlBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, Encoding? encoding = null, CancellationToken ct = default)
        => _writer.ExportToYamlBytesFromDataTableAsync(dataTable, encoding, ct);

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseStringAsDataTableAsync(string yaml, bool hasFooterRow = false, CancellationToken ct = default)
        => _reader.ParseStringAsDataTableAsync(yaml, hasFooterRow, ct);

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(
        byte[] yamlBytes,
        bool hasFooterRow = false,
        Encoding? encoding = null,
        CancellationToken ct = default)
        => _reader.ParseBytesAsDataTableAsync(yamlBytes, hasFooterRow, encoding, ct);

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream yamlStream, bool hasFooterRow = false, CancellationToken ct = default)
        => _reader.ParseStreamAsDataTableAsync(yamlStream, hasFooterRow, ct);

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string yamlFilePath, bool hasFooterRow = false, CancellationToken ct = default)
        => _reader.ParseFileAsDataTableAsync(yamlFilePath, hasFooterRow, ct);
#endif
}