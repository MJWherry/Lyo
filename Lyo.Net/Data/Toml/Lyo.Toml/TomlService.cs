using System.Reflection;
using System.Text;
using Lyo.Exceptions;
using Lyo.Result;

namespace Lyo.Toml;

/// <summary>Facade over <see cref="TomlReader" /> and <see cref="TomlWriter" />, plus JSON conversion. One instance is safe for concurrent calls.</summary>
public sealed class TomlService : ITomlService
{
    /// <summary>Shared service with the default options. Do not change it.</summary>
    public static TomlService Default { get; } = new();

    private readonly TomlEngine _engine;
    private readonly TomlReader _reader;
    private readonly TomlWriter _writer;

    static TomlService() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>Builds a service. <paramref name="configure" /> runs before validation.</summary>
    public TomlService(Action<TomlOptions>? configure = null)
    {
        var options = new TomlOptions();
        configure?.Invoke(options);
        options.Validate();
        _engine = new(options);
        _reader = new(_engine);
        _writer = new(_engine);
    }

    /// <inheritdoc />
    public ITomlReader Reader => _reader;

    /// <inheritdoc />
    public ITomlWriter Writer => _writer;

    /// <inheritdoc />
    public T ParseFile<T>(string tomlFilePath) => _reader.ParseFile<T>(tomlFilePath);

    /// <inheritdoc />
    public T ParseStream<T>(Stream tomlStream) => _reader.ParseStream<T>(tomlStream);

    /// <inheritdoc />
    public T ParseBytes<T>(byte[] tomlBytes, Encoding? encoding = null) => _reader.ParseBytes<T>(tomlBytes, encoding);

    /// <inheritdoc />
    public T Parse<T>(ReadOnlySpan<byte> toml, Encoding? encoding = null) => _reader.Parse<T>(toml, encoding);

    /// <inheritdoc />
    public T Parse<T>(ReadOnlySpan<char> toml) => _reader.Parse<T>(toml);

    /// <inheritdoc />
    public T ParseString<T>(string toml) => _reader.ParseString<T>(toml);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStringAsDataTable(string toml, bool hasFooterRow = false) => _reader.ParseStringAsDataTable(toml, hasFooterRow);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] tomlBytes, bool hasFooterRow = false, Encoding? encoding = null)
        => _reader.ParseBytesAsDataTable(tomlBytes, hasFooterRow, encoding);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream tomlStream, bool hasFooterRow = false) => _reader.ParseStreamAsDataTable(tomlStream, hasFooterRow);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseFileAsDataTable(string tomlFilePath, bool hasFooterRow = false) => _reader.ParseFileAsDataTable(tomlFilePath, hasFooterRow);

    /// <inheritdoc />
    public void ExportToToml<T>(T value, string tomlFilePath) => _writer.ExportToToml(value, tomlFilePath);

    /// <inheritdoc />
    public void ExportToToml<T>(T value, Stream tomlStream) => _writer.ExportToToml(value, tomlStream);

    /// <inheritdoc />
    public string ExportToTomlString<T>(T value) => _writer.ExportToTomlString(value);

    /// <inheritdoc />
    public byte[] ExportToTomlBytes<T>(T value, Encoding? encoding = null) => _writer.ExportToTomlBytes(value, encoding);

    /// <inheritdoc />
    public bool TryExport<T>(T value, Span<byte> destination, out int bytesWritten, Encoding? encoding = null) => _writer.TryExport(value, destination, out bytesWritten, encoding);

    /// <inheritdoc />
    public string ExportFormattedToTomlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields)
        => _writer.ExportFormattedToTomlString(items, fields);

    /// <inheritdoc />
    public void ExportFormattedToToml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream tomlStream)
        => _writer.ExportFormattedToToml(items, fields, tomlStream);

    /// <inheritdoc />
    public string ExportColumnsToTomlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns)
        => _writer.ExportColumnsToTomlString(items, columns);

    /// <inheritdoc />
    public void ExportColumnsToToml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream tomlStream)
        => _writer.ExportColumnsToToml(items, columns, tomlStream);

    /// <inheritdoc />
    public string ExportToTomlStringFromDataTable(DataTable.Models.DataTable dataTable) => _writer.ExportToTomlStringFromDataTable(dataTable);

    /// <inheritdoc />
    public void ExportToTomlFromDataTable(DataTable.Models.DataTable dataTable, string tomlFilePath) => _writer.ExportToTomlFromDataTable(dataTable, tomlFilePath);

    /// <inheritdoc />
    public void ExportToTomlFromDataTable(DataTable.Models.DataTable dataTable, Stream tomlStream) => _writer.ExportToTomlFromDataTable(dataTable, tomlStream);

    /// <inheritdoc />
    public byte[] ExportToTomlBytesFromDataTable(DataTable.Models.DataTable dataTable, Encoding? encoding = null) => _writer.ExportToTomlBytesFromDataTable(dataTable, encoding);

    /// <inheritdoc />
    public void ConvertToJson(string tomlFilePath, string jsonFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tomlFilePath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        File.WriteAllText(jsonFilePath, ConvertToJson(File.ReadAllText(tomlFilePath)));
    }

    /// <inheritdoc />
    public void ConvertToJson(Stream tomlStream, Stream jsonStream)
    {
        ArgumentHelpers.ThrowIfNull(tomlStream);
        ArgumentHelpers.ThrowIfNull(jsonStream);
        var bytes = Encoding.UTF8.GetBytes(ConvertToJson(Read(tomlStream)));
        jsonStream.Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc />
    public byte[] ConvertToJsonBytes(byte[] tomlBytes)
    {
        ArgumentHelpers.ThrowIfNull(tomlBytes);
        return Encoding.UTF8.GetBytes(ConvertToJson(Encoding.UTF8.GetString(tomlBytes)));
    }

    /// <inheritdoc />
    public string ConvertToJson(string toml)
    {
        ArgumentHelpers.ThrowIfNull(toml);
        return _engine.ToJson(toml);
    }

    /// <inheritdoc />
    public void ConvertFromJson(string jsonFilePath, string tomlFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tomlFilePath);
        File.WriteAllBytes(tomlFilePath, _engine.Encoding.GetBytes(ConvertFromJson(File.ReadAllText(jsonFilePath))));
    }

    /// <inheritdoc />
    public void ConvertFromJson(Stream jsonStream, Stream tomlStream)
    {
        ArgumentHelpers.ThrowIfNull(jsonStream);
        ArgumentHelpers.ThrowIfNull(tomlStream);
        var bytes = _engine.Encoding.GetBytes(ConvertFromJson(Read(jsonStream)));
        tomlStream.Write(bytes, 0, bytes.Length);
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
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        return reader.ReadToEnd();
    }

#if !NETSTANDARD2_0
    /// <inheritdoc />
    public Task<T> ParseFileAsync<T>(string tomlFilePath, CancellationToken ct = default) => _reader.ParseFileAsync<T>(tomlFilePath, ct);

    /// <inheritdoc />
    public Task<T> ParseStreamAsync<T>(Stream tomlStream, CancellationToken ct = default) => _reader.ParseStreamAsync<T>(tomlStream, ct);

    /// <inheritdoc />
    public Task<T> ParseBytesAsync<T>(byte[] tomlBytes, CancellationToken ct = default) => _reader.ParseBytesAsync<T>(tomlBytes, ct);

    /// <inheritdoc />
    public Task<T> ParseStringAsync<T>(string toml, CancellationToken ct = default) => _reader.ParseStringAsync<T>(toml, ct);

    /// <inheritdoc />
    public Task ExportToTomlAsync<T>(T value, string tomlFilePath, CancellationToken ct = default) => _writer.ExportToTomlAsync(value, tomlFilePath, ct);

    /// <inheritdoc />
    public Task ExportToTomlAsync<T>(T value, Stream tomlStream, CancellationToken ct = default) => _writer.ExportToTomlAsync(value, tomlStream, ct);

    /// <inheritdoc />
    public Task<string> ExportToTomlStringAsync<T>(T value, CancellationToken ct = default) => _writer.ExportToTomlStringAsync(value, ct);

    /// <inheritdoc />
    public Task<byte[]> ExportToTomlBytesAsync<T>(T value, CancellationToken ct = default) => _writer.ExportToTomlBytesAsync(value, ct);

    /// <inheritdoc />
    public Task ExportFormattedToTomlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream tomlStream, CancellationToken ct = default)
        => _writer.ExportFormattedToTomlAsync(items, fields, tomlStream, ct);

    /// <inheritdoc />
    public Task ExportColumnsToTomlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream tomlStream, CancellationToken ct = default)
        => _writer.ExportColumnsToTomlAsync(items, columns, tomlStream, ct);

    /// <inheritdoc />
    public Task ExportToTomlFromDataTableAsync(DataTable.Models.DataTable dataTable, string tomlFilePath, CancellationToken ct = default)
        => _writer.ExportToTomlFromDataTableAsync(dataTable, tomlFilePath, ct);

    /// <inheritdoc />
    public Task ExportToTomlFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream tomlStream, CancellationToken ct = default)
        => _writer.ExportToTomlFromDataTableAsync(dataTable, tomlStream, ct);

    /// <inheritdoc />
    public Task<string> ExportToTomlStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
        => _writer.ExportToTomlStringFromDataTableAsync(dataTable, ct);

    /// <inheritdoc />
    public Task<byte[]> ExportToTomlBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, Encoding? encoding = null, CancellationToken ct = default)
        => _writer.ExportToTomlBytesFromDataTableAsync(dataTable, encoding, ct);

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseStringAsDataTableAsync(string toml, bool hasFooterRow = false, CancellationToken ct = default)
        => _reader.ParseStringAsDataTableAsync(toml, hasFooterRow, ct);

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] tomlBytes, bool hasFooterRow = false, Encoding? encoding = null, CancellationToken ct = default)
        => _reader.ParseBytesAsDataTableAsync(tomlBytes, hasFooterRow, encoding, ct);

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream tomlStream, bool hasFooterRow = false, CancellationToken ct = default)
        => _reader.ParseStreamAsDataTableAsync(tomlStream, hasFooterRow, ct);

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string tomlFilePath, bool hasFooterRow = false, CancellationToken ct = default)
        => _reader.ParseFileAsDataTableAsync(tomlFilePath, hasFooterRow, ct);
#endif
}
