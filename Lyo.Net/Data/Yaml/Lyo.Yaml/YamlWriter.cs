using System.Reflection;
using System.Text;
using Lyo.Exceptions;
using Lyo.Json;
using Lyo.Result;

namespace Lyo.Yaml;

/// <summary>Writes YAML from the JSON contract, then emits YAML with the service style.</summary>
public sealed class YamlWriter : IYamlWriter
{
    private readonly YamlEngine _engine;

    internal YamlWriter(YamlEngine engine) => _engine = engine;

    /// <inheritdoc />
    public void ExportToYaml<T>(T value, string yamlFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(yamlFilePath);
        File.WriteAllBytes(yamlFilePath, ExportToYamlBytes(value));
    }

    /// <inheritdoc />
    public void ExportToYaml<T>(T value, Stream yamlStream)
    {
        ArgumentHelpers.ThrowIfNull(yamlStream);
        var bytes = ExportToYamlBytes(value);
        yamlStream.Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc />
    public string ExportToYamlString<T>(T value) => _engine.Export(value);

    /// <inheritdoc />
    public byte[] ExportToYamlBytes<T>(T value, Encoding? encoding = null)
    {
        var text = ExportToYamlString(value);
        return (encoding ?? _engine.Encoding).GetBytes(text);
    }

    /// <inheritdoc />
    public bool TryExport<T>(T value, Span<byte> destination, out int bytesWritten, Encoding? encoding = null)
    {
        var bytes = ExportToYamlBytes(value, encoding);
        if (bytes.Length > destination.Length) {
            bytesWritten = 0;
            return false;
        }

        bytes.CopyTo(destination);
        bytesWritten = bytes.Length;
        return true;
    }

    /// <inheritdoc />
    public string ExportFormattedToYamlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields)
        => _engine.ExportPreservingKeys(FormattedRows.Strings(items, fields));

    /// <inheritdoc />
    public void ExportFormattedToYaml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream yamlStream)
        => Write(ExportFormattedToYamlString(items, fields), yamlStream);

    /// <inheritdoc />
    public string ExportColumnsToYamlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns)
        => _engine.ExportPreservingKeys(FormattedRows.Values(items, columns));

    /// <inheritdoc />
    public void ExportColumnsToYaml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream yamlStream)
        => Write(ExportColumnsToYamlString(items, columns), yamlStream);

    /// <inheritdoc />
    public string ExportToYamlStringFromDataTable(DataTable.Models.DataTable dataTable)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        return _engine.ExportRows(DataTableDocument.ToRows(dataTable));
    }

    /// <inheritdoc />
    public void ExportToYamlFromDataTable(DataTable.Models.DataTable dataTable, string yamlFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(yamlFilePath);
        File.WriteAllBytes(yamlFilePath, ExportToYamlBytesFromDataTable(dataTable));
    }

    /// <inheritdoc />
    public void ExportToYamlFromDataTable(DataTable.Models.DataTable dataTable, Stream yamlStream)
        => Write(ExportToYamlStringFromDataTable(dataTable), yamlStream);

    /// <inheritdoc />
    public byte[] ExportToYamlBytesFromDataTable(DataTable.Models.DataTable dataTable, Encoding? encoding = null)
        => (encoding ?? _engine.Encoding).GetBytes(ExportToYamlStringFromDataTable(dataTable));

    private void Write(string text, Stream stream)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        var bytes = _engine.Encoding.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

#if !NETSTANDARD2_0
    /// <inheritdoc />
    public async Task ExportToYamlAsync<T>(T value, string yamlFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(yamlFilePath);
        await File.WriteAllBytesAsync(yamlFilePath, ExportToYamlBytes(value), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportToYamlAsync<T>(T value, Stream yamlStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(yamlStream);
        var bytes = ExportToYamlBytes(value);
        await yamlStream.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<string> ExportToYamlStringAsync<T>(T value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ExportToYamlString(value));
    }

    /// <inheritdoc />
    public Task<byte[]> ExportToYamlBytesAsync<T>(T value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ExportToYamlBytes(value));
    }

    /// <inheritdoc />
    public async Task ExportFormattedToYamlAsync<T>(
        IEnumerable<T> items,
        IReadOnlyDictionary<string, Func<T, string>> fields,
        Stream yamlStream,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(yamlStream);
        var bytes = _engine.Encoding.GetBytes(ExportFormattedToYamlString(items, fields));
        await yamlStream.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportColumnsToYamlAsync<T>(
        IEnumerable<T> items,
        IReadOnlyDictionary<string, PropertyInfo> columns,
        Stream yamlStream,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(yamlStream);
        var bytes = _engine.Encoding.GetBytes(ExportColumnsToYamlString(items, columns));
        await yamlStream.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportToYamlFromDataTableAsync(DataTable.Models.DataTable dataTable, string yamlFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(yamlFilePath);
        await File.WriteAllBytesAsync(yamlFilePath, ExportToYamlBytesFromDataTable(dataTable), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportToYamlFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream yamlStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(yamlStream);
        var bytes = _engine.Encoding.GetBytes(ExportToYamlStringFromDataTable(dataTable));
        await yamlStream.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<string> ExportToYamlStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ExportToYamlStringFromDataTable(dataTable));
    }

    /// <inheritdoc />
    public Task<byte[]> ExportToYamlBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, Encoding? encoding = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ExportToYamlBytesFromDataTable(dataTable, encoding));
    }
#endif
}
