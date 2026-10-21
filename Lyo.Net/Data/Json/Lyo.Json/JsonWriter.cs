using System.Reflection;
using System.Text;
using Lyo.Exceptions;

namespace Lyo.Json;

/// <summary>Writes JSON with the options owned by <see cref="JsonService" />.</summary>
public sealed class JsonWriter : IJsonWriter
{
    private readonly JsonRuntime _runtime;

    internal JsonWriter(JsonRuntime runtime) => _runtime = runtime;

    /// <inheritdoc />
    public void ExportToJson<T>(T value, string jsonFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        File.WriteAllBytes(jsonFilePath, ExportToJsonBytes(value));
    }

    /// <inheritdoc />
    public void ExportToJson<T>(T value, Stream jsonStream)
    {
        ArgumentHelpers.ThrowIfNull(jsonStream);
        var bytes = ExportToJsonBytes(value);
        jsonStream.Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc />
    public string ExportToJsonString<T>(T value) => _runtime.Serialize(value);

    /// <inheritdoc />
    public byte[] ExportToJsonBytes<T>(T value, Encoding? encoding = null) => _runtime.SerializeBytes(value, encoding);

    /// <inheritdoc />
    public bool TryExport<T>(T value, Span<byte> destination, out int bytesWritten, Encoding? encoding = null)
    {
        var bytes = ExportToJsonBytes(value, encoding);
        if (bytes.Length > destination.Length) {
            bytesWritten = 0;
            return false;
        }

        bytes.CopyTo(destination);
        bytesWritten = bytes.Length;
        return true;
    }

    /// <inheritdoc />
    public void ExportNdjson<T>(IEnumerable<T> values, string jsonFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        File.WriteAllBytes(jsonFilePath, ExportNdjsonBytes(values));
    }

    /// <inheritdoc />
    public void ExportNdjson<T>(IEnumerable<T> values, Stream jsonStream)
    {
        ArgumentHelpers.ThrowIfNull(jsonStream);
        var bytes = _runtime.Encoding.GetBytes(ExportNdjsonString(values));
        jsonStream.Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc />
    public string ExportNdjsonString<T>(IEnumerable<T> values)
    {
        ArgumentHelpers.ThrowIfNull(values);
        var builder = new StringBuilder();
        foreach (var value in values) {
            builder.Append(_runtime.SerializeCompact(value));
            builder.Append('\n');
        }

        return builder.ToString();
    }

    /// <inheritdoc />
    public byte[] ExportNdjsonBytes<T>(IEnumerable<T> values) => Encoding.UTF8.GetBytes(ExportNdjsonString(values));

    /// <inheritdoc />
    public string ExportFormattedToJsonString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields)
        => _runtime.SerializePreservingKeys(FormattedRows.Strings(items, fields));

    /// <inheritdoc />
    public void ExportFormattedToJson<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream jsonStream)
        => Write(ExportFormattedToJsonString(items, fields), jsonStream);

    /// <inheritdoc />
    public string ExportColumnsToJsonString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns)
        => _runtime.SerializePreservingKeys(FormattedRows.Values(items, columns));

    /// <inheritdoc />
    public void ExportColumnsToJson<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream jsonStream)
        => Write(ExportColumnsToJsonString(items, columns), jsonStream);

    /// <inheritdoc />
    public string ExportToJsonStringFromDataTable(DataTable.Models.DataTable dataTable)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        return DataTableDocument.ToRows(dataTable).ToJsonString(_runtime.Serializer);
    }

    /// <inheritdoc />
    public void ExportToJsonFromDataTable(DataTable.Models.DataTable dataTable, string jsonFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        File.WriteAllBytes(jsonFilePath, ExportToJsonBytesFromDataTable(dataTable));
    }

    /// <inheritdoc />
    public void ExportToJsonFromDataTable(DataTable.Models.DataTable dataTable, Stream jsonStream) => Write(ExportToJsonStringFromDataTable(dataTable), jsonStream);

    /// <inheritdoc />
    public byte[] ExportToJsonBytesFromDataTable(DataTable.Models.DataTable dataTable, Encoding? encoding = null)
        => (encoding ?? _runtime.Encoding).GetBytes(ExportToJsonStringFromDataTable(dataTable));

    internal string ExportPreservingKeyOrder<T>(T value) => _runtime.SerializePreservingKeys(value);

    private void Write(string text, Stream stream)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        var bytes = _runtime.Encoding.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

#if !NETSTANDARD2_0
    /// <inheritdoc />
    public async Task ExportToJsonAsync<T>(T value, string jsonFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        var bytes = ExportToJsonBytes(value);
        await File.WriteAllBytesAsync(jsonFilePath, bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportToJsonAsync<T>(T value, Stream jsonStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(jsonStream);
        var bytes = ExportToJsonBytes(value);
        await jsonStream.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<string> ExportToJsonStringAsync<T>(T value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ExportToJsonString(value));
    }

    /// <inheritdoc />
    public Task<byte[]> ExportToJsonBytesAsync<T>(T value, Encoding? encoding = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ExportToJsonBytes(value, encoding));
    }

    /// <inheritdoc />
    public async Task ExportFormattedToJsonAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream jsonStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(jsonStream);
        var bytes = _runtime.Encoding.GetBytes(ExportFormattedToJsonString(items, fields));
        await jsonStream.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportColumnsToJsonAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream jsonStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(jsonStream);
        var bytes = _runtime.Encoding.GetBytes(ExportColumnsToJsonString(items, columns));
        await jsonStream.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportToJsonFromDataTableAsync(DataTable.Models.DataTable dataTable, string jsonFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        var bytes = ExportToJsonBytesFromDataTable(dataTable);
        await File.WriteAllBytesAsync(jsonFilePath, bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportToJsonFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream jsonStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(jsonStream);
        var bytes = _runtime.Encoding.GetBytes(ExportToJsonStringFromDataTable(dataTable));
        await jsonStream.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<string> ExportToJsonStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ExportToJsonStringFromDataTable(dataTable));
    }

    /// <inheritdoc />
    public Task<byte[]> ExportToJsonBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, Encoding? encoding = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ExportToJsonBytesFromDataTable(dataTable, encoding));
    }
#endif
}