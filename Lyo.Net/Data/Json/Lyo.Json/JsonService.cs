using System.Reflection;
using System.Text;
using System.Text.Json;
using Lyo.Exceptions;
using Lyo.Result;

namespace Lyo.Json;

/// <summary>Facade over <see cref="JsonReader" /> and <see cref="JsonWriter" />. One instance, including <see cref="Default" />, is safe for concurrent parse and export.</summary>
public sealed class JsonService : IJsonService
{
    /// <summary>Service with the Lyo JSON defaults: camelCase, nulls omitted, compact output, UTF-8 without a BOM.</summary>
    public static JsonService Default { get; } = new();

    private readonly JsonReader _reader;
    private readonly JsonWriter _writer;

    static JsonService() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>Builds a service with the Lyo JSON defaults.</summary>
    public JsonService() : this(new JsonOptions())
    {
    }

    /// <summary>Builds a service from a fresh <see cref="JsonOptions" /> edited by <paramref name="configure" />.</summary>
    public JsonService(Action<JsonOptions> configure) : this(Runtime(configure))
    {
    }

    /// <summary>Builds a service from <paramref name="options" />. Naming, omission, encoding, and comments come from that instance.</summary>
    public JsonService(JsonOptions options) : this(Runtime(options))
    {
    }

    /// <summary>Builds a service from <paramref name="serializerOptions" />. The instance is copied and used as-is, so a host contract is not wrapped in <see cref="JsonOptions" />.</summary>
    public JsonService(JsonSerializerOptions serializerOptions) : this(Runtime(serializerOptions))
    {
    }

    private JsonService(JsonRuntime runtime)
    {
        _reader = new(runtime);
        _writer = new(runtime);
    }

    private static JsonRuntime Runtime(Action<JsonOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var options = new JsonOptions();
        configure(options);
        return Runtime(options);
    }

    private static JsonRuntime Runtime(JsonOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        return JsonRuntime.FromOptions(options);
    }

    private static JsonRuntime Runtime(JsonSerializerOptions serializerOptions)
    {
        ArgumentHelpers.ThrowIfNull(serializerOptions);
        return JsonRuntime.FromHost(serializerOptions);
    }

    /// <inheritdoc />
    public IJsonReader Reader => _reader;

    /// <inheritdoc />
    public IJsonWriter Writer => _writer;

    /// <inheritdoc />
    public T ParseFile<T>(string jsonFilePath) => _reader.ParseFile<T>(jsonFilePath);

    /// <inheritdoc />
    public T ParseStream<T>(Stream jsonStream) => _reader.ParseStream<T>(jsonStream);

    /// <inheritdoc />
    public T ParseBytes<T>(byte[] jsonBytes, Encoding? encoding = null) => _reader.ParseBytes<T>(jsonBytes, encoding);

    /// <inheritdoc />
    public T Parse<T>(ReadOnlySpan<byte> json, Encoding? encoding = null) => _reader.Parse<T>(json, encoding);

    /// <inheritdoc />
    public T Parse<T>(ReadOnlySpan<char> json) => _reader.Parse<T>(json);

    /// <inheritdoc />
    public T ParseString<T>(string json) => _reader.ParseString<T>(json);

    /// <inheritdoc />
    public List<T> ParseNdjsonFile<T>(string jsonFilePath) => _reader.ParseNdjsonFile<T>(jsonFilePath);

    /// <inheritdoc />
    public List<T> ParseNdjsonStream<T>(Stream jsonStream) => _reader.ParseNdjsonStream<T>(jsonStream);

    /// <inheritdoc />
    public List<T> ParseNdjsonString<T>(string ndjson) => _reader.ParseNdjsonString<T>(ndjson);

    /// <inheritdoc />
    public List<T> ParseNdjsonBytes<T>(byte[] ndjson) => _reader.ParseNdjsonBytes<T>(ndjson);

    /// <inheritdoc />
    public JsonImportResult<T> ParseArrayAsResult<T>(string json, string? rowsPath = null) => _reader.ParseArrayAsResult<T>(json, rowsPath);

    /// <inheritdoc />
    public JsonImportResult<T> ParseNdjsonAsResult<T>(string ndjson) => _reader.ParseNdjsonAsResult<T>(ndjson);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStringAsDataTable(string json, bool hasFooterRow = false) => _reader.ParseStringAsDataTable(json, hasFooterRow);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] jsonBytes, bool hasFooterRow = false, Encoding? encoding = null)
        => _reader.ParseBytesAsDataTable(jsonBytes, hasFooterRow, encoding);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream jsonStream, bool hasFooterRow = false) => _reader.ParseStreamAsDataTable(jsonStream, hasFooterRow);

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseFileAsDataTable(string jsonFilePath, bool hasFooterRow = false) => _reader.ParseFileAsDataTable(jsonFilePath, hasFooterRow);

    /// <inheritdoc />
    public void ExportToJson<T>(T value, string jsonFilePath) => _writer.ExportToJson(value, jsonFilePath);

    /// <inheritdoc />
    public void ExportToJson<T>(T value, Stream jsonStream) => _writer.ExportToJson(value, jsonStream);

    /// <inheritdoc />
    public string ExportToJsonString<T>(T value) => _writer.ExportToJsonString(value);

    /// <inheritdoc />
    public byte[] ExportToJsonBytes<T>(T value, Encoding? encoding = null) => _writer.ExportToJsonBytes(value, encoding);

    /// <inheritdoc />
    public bool TryExport<T>(T value, Span<byte> destination, out int bytesWritten, Encoding? encoding = null)
        => _writer.TryExport(value, destination, out bytesWritten, encoding);

    /// <inheritdoc />
    public void ExportNdjson<T>(IEnumerable<T> values, string jsonFilePath) => _writer.ExportNdjson(values, jsonFilePath);

    /// <inheritdoc />
    public void ExportNdjson<T>(IEnumerable<T> values, Stream jsonStream) => _writer.ExportNdjson(values, jsonStream);

    /// <inheritdoc />
    public string ExportNdjsonString<T>(IEnumerable<T> values) => _writer.ExportNdjsonString(values);

    /// <inheritdoc />
    public byte[] ExportNdjsonBytes<T>(IEnumerable<T> values) => _writer.ExportNdjsonBytes(values);

    /// <inheritdoc />
    public string ExportFormattedToJsonString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields)
        => _writer.ExportFormattedToJsonString(items, fields);

    /// <inheritdoc />
    public void ExportFormattedToJson<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream jsonStream)
        => _writer.ExportFormattedToJson(items, fields, jsonStream);

    /// <inheritdoc />
    public string ExportColumnsToJsonString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns)
        => _writer.ExportColumnsToJsonString(items, columns);

    /// <inheritdoc />
    public void ExportColumnsToJson<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream jsonStream)
        => _writer.ExportColumnsToJson(items, columns, jsonStream);

    /// <inheritdoc />
    public string ExportToJsonStringFromDataTable(DataTable.Models.DataTable dataTable) => _writer.ExportToJsonStringFromDataTable(dataTable);

    /// <inheritdoc />
    public void ExportToJsonFromDataTable(DataTable.Models.DataTable dataTable, string jsonFilePath) => _writer.ExportToJsonFromDataTable(dataTable, jsonFilePath);

    /// <inheritdoc />
    public void ExportToJsonFromDataTable(DataTable.Models.DataTable dataTable, Stream jsonStream) => _writer.ExportToJsonFromDataTable(dataTable, jsonStream);

    /// <inheritdoc />
    public byte[] ExportToJsonBytesFromDataTable(DataTable.Models.DataTable dataTable, Encoding? encoding = null) => _writer.ExportToJsonBytesFromDataTable(dataTable, encoding);

    internal string ExportPreservingKeyOrder<T>(T value) => _writer.ExportPreservingKeyOrder(value);

#if !NETSTANDARD2_0
    /// <inheritdoc />
    public IAsyncEnumerable<T> ParseNdjsonFileAsync<T>(string jsonFilePath, CancellationToken ct = default) => _reader.ParseNdjsonFileAsync<T>(jsonFilePath, ct);

    /// <inheritdoc />
    public IAsyncEnumerable<T> ParseNdjsonStreamAsync<T>(Stream jsonStream, CancellationToken ct = default) => _reader.ParseNdjsonStreamAsync<T>(jsonStream, ct);

    /// <inheritdoc />
    public Task<T> ParseFileAsync<T>(string jsonFilePath, CancellationToken ct = default) => _reader.ParseFileAsync<T>(jsonFilePath, ct);

    /// <inheritdoc />
    public Task<T> ParseStreamAsync<T>(Stream jsonStream, CancellationToken ct = default) => _reader.ParseStreamAsync<T>(jsonStream, ct);

    /// <inheritdoc />
    public Task<T> ParseBytesAsync<T>(byte[] jsonBytes, Encoding? encoding = null, CancellationToken ct = default) => _reader.ParseBytesAsync<T>(jsonBytes, encoding, ct);

    /// <inheritdoc />
    public Task<T> ParseStringAsync<T>(string json, CancellationToken ct = default) => _reader.ParseStringAsync<T>(json, ct);

    /// <inheritdoc />
    public Task ExportToJsonAsync<T>(T value, string jsonFilePath, CancellationToken ct = default) => _writer.ExportToJsonAsync(value, jsonFilePath, ct);

    /// <inheritdoc />
    public Task ExportToJsonAsync<T>(T value, Stream jsonStream, CancellationToken ct = default) => _writer.ExportToJsonAsync(value, jsonStream, ct);

    /// <inheritdoc />
    public Task<string> ExportToJsonStringAsync<T>(T value, CancellationToken ct = default) => _writer.ExportToJsonStringAsync(value, ct);

    /// <inheritdoc />
    public Task<byte[]> ExportToJsonBytesAsync<T>(T value, Encoding? encoding = null, CancellationToken ct = default) => _writer.ExportToJsonBytesAsync(value, encoding, ct);

    /// <inheritdoc />
    public Task ExportFormattedToJsonAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream jsonStream, CancellationToken ct = default)
        => _writer.ExportFormattedToJsonAsync(items, fields, jsonStream, ct);

    /// <inheritdoc />
    public Task ExportColumnsToJsonAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream jsonStream, CancellationToken ct = default)
        => _writer.ExportColumnsToJsonAsync(items, columns, jsonStream, ct);

    /// <inheritdoc />
    public Task ExportToJsonFromDataTableAsync(DataTable.Models.DataTable dataTable, string jsonFilePath, CancellationToken ct = default)
        => _writer.ExportToJsonFromDataTableAsync(dataTable, jsonFilePath, ct);

    /// <inheritdoc />
    public Task ExportToJsonFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream jsonStream, CancellationToken ct = default)
        => _writer.ExportToJsonFromDataTableAsync(dataTable, jsonStream, ct);

    /// <inheritdoc />
    public Task<string> ExportToJsonStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
        => _writer.ExportToJsonStringFromDataTableAsync(dataTable, ct);

    /// <inheritdoc />
    public Task<byte[]> ExportToJsonBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, Encoding? encoding = null, CancellationToken ct = default)
        => _writer.ExportToJsonBytesFromDataTableAsync(dataTable, encoding, ct);

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseStringAsDataTableAsync(string json, bool hasFooterRow = false, CancellationToken ct = default)
        => _reader.ParseStringAsDataTableAsync(json, hasFooterRow, ct);

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] jsonBytes, bool hasFooterRow = false, Encoding? encoding = null, CancellationToken ct = default)
        => _reader.ParseBytesAsDataTableAsync(jsonBytes, hasFooterRow, encoding, ct);

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream jsonStream, bool hasFooterRow = false, CancellationToken ct = default)
        => _reader.ParseStreamAsDataTableAsync(jsonStream, hasFooterRow, ct);

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string jsonFilePath, bool hasFooterRow = false, CancellationToken ct = default)
        => _reader.ParseFileAsDataTableAsync(jsonFilePath, hasFooterRow, ct);
#endif
}
