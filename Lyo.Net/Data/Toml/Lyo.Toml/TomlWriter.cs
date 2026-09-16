using System.Reflection;
using System.Text;
using Lyo.Exceptions;
using Lyo.Json;
using Lyo.Result;

namespace Lyo.Toml;

/// <summary>Writes TOML from the JSON contract, then emits a document with the service style.</summary>
public sealed class TomlWriter : ITomlWriter
{
    private readonly TomlEngine _engine;

    internal TomlWriter(TomlEngine engine) => _engine = engine;

    /// <inheritdoc />
    public void ExportToToml<T>(T value, string tomlFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tomlFilePath);
        File.WriteAllBytes(tomlFilePath, ExportToTomlBytes(value));
    }

    /// <inheritdoc />
    public void ExportToToml<T>(T value, Stream tomlStream)
    {
        ArgumentHelpers.ThrowIfNull(tomlStream);
        var bytes = ExportToTomlBytes(value);
        tomlStream.Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc />
    public string ExportToTomlString<T>(T value) => _engine.Export(value);

    /// <inheritdoc />
    public byte[] ExportToTomlBytes<T>(T value, Encoding? encoding = null)
    {
        var text = ExportToTomlString(value);
        return (encoding ?? _engine.Encoding).GetBytes(text);
    }

    /// <inheritdoc />
    public bool TryExport<T>(T value, Span<byte> destination, out int bytesWritten, Encoding? encoding = null)
    {
        var bytes = ExportToTomlBytes(value, encoding);
        if (bytes.Length > destination.Length) {
            bytesWritten = 0;
            return false;
        }

        bytes.CopyTo(destination);
        bytesWritten = bytes.Length;
        return true;
    }

    /// <inheritdoc />
    public string ExportFormattedToTomlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields)
        => _engine.ExportPreservingKeys(FormattedRows.Strings(items, fields));

    /// <inheritdoc />
    public void ExportFormattedToToml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream tomlStream)
        => Write(ExportFormattedToTomlString(items, fields), tomlStream);

    /// <inheritdoc />
    public string ExportColumnsToTomlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns)
        => _engine.ExportPreservingKeys(FormattedRows.Values(items, columns));

    /// <inheritdoc />
    public void ExportColumnsToToml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream tomlStream)
        => Write(ExportColumnsToTomlString(items, columns), tomlStream);

    /// <inheritdoc />
    public string ExportToTomlStringFromDataTable(DataTable.Models.DataTable dataTable)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        return _engine.ExportRows(DataTableDocument.ToRows(dataTable));
    }

    /// <inheritdoc />
    public void ExportToTomlFromDataTable(DataTable.Models.DataTable dataTable, string tomlFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tomlFilePath);
        File.WriteAllBytes(tomlFilePath, ExportToTomlBytesFromDataTable(dataTable));
    }

    /// <inheritdoc />
    public void ExportToTomlFromDataTable(DataTable.Models.DataTable dataTable, Stream tomlStream)
        => Write(ExportToTomlStringFromDataTable(dataTable), tomlStream);

    /// <inheritdoc />
    public byte[] ExportToTomlBytesFromDataTable(DataTable.Models.DataTable dataTable, Encoding? encoding = null)
        => (encoding ?? _engine.Encoding).GetBytes(ExportToTomlStringFromDataTable(dataTable));

    private void Write(string text, Stream stream)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        var bytes = _engine.Encoding.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

#if !NETSTANDARD2_0
    /// <inheritdoc />
    public async Task ExportToTomlAsync<T>(T value, string tomlFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tomlFilePath);
        await File.WriteAllBytesAsync(tomlFilePath, ExportToTomlBytes(value), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportToTomlAsync<T>(T value, Stream tomlStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(tomlStream);
        var bytes = ExportToTomlBytes(value);
        await tomlStream.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<string> ExportToTomlStringAsync<T>(T value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ExportToTomlString(value));
    }

    /// <inheritdoc />
    public Task<byte[]> ExportToTomlBytesAsync<T>(T value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ExportToTomlBytes(value));
    }

    /// <inheritdoc />
    public async Task ExportFormattedToTomlAsync<T>(
        IEnumerable<T> items,
        IReadOnlyDictionary<string, Func<T, string>> fields,
        Stream tomlStream,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(tomlStream);
        var bytes = _engine.Encoding.GetBytes(ExportFormattedToTomlString(items, fields));
        await tomlStream.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportColumnsToTomlAsync<T>(
        IEnumerable<T> items,
        IReadOnlyDictionary<string, PropertyInfo> columns,
        Stream tomlStream,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(tomlStream);
        var bytes = _engine.Encoding.GetBytes(ExportColumnsToTomlString(items, columns));
        await tomlStream.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportToTomlFromDataTableAsync(DataTable.Models.DataTable dataTable, string tomlFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tomlFilePath);
        await File.WriteAllBytesAsync(tomlFilePath, ExportToTomlBytesFromDataTable(dataTable), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportToTomlFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream tomlStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(tomlStream);
        var bytes = _engine.Encoding.GetBytes(ExportToTomlStringFromDataTable(dataTable));
        await tomlStream.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<string> ExportToTomlStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ExportToTomlStringFromDataTable(dataTable));
    }

    /// <inheritdoc />
    public Task<byte[]> ExportToTomlBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, Encoding? encoding = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ExportToTomlBytesFromDataTable(dataTable, encoding));
    }
#endif
}
