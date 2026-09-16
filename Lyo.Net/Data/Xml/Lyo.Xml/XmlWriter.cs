using System.Reflection;
using System.Text;
using Lyo.Exceptions;
using Lyo.Json;

namespace Lyo.Xml;

/// <summary>Writes XML from the JSON contract, then emits elements with the service style.</summary>
public sealed class XmlWriter : IXmlWriter
{
    private readonly XmlEngine _engine;

    internal XmlWriter(XmlEngine engine) => _engine = engine;

    /// <inheritdoc />
    public void ExportToXml<T>(T value, string xmlFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xmlFilePath);
        File.WriteAllBytes(xmlFilePath, ExportToXmlBytes(value));
    }

    /// <inheritdoc />
    public void ExportToXml<T>(T value, Stream xmlStream)
    {
        ArgumentHelpers.ThrowIfNull(xmlStream);
        var bytes = ExportToXmlBytes(value);
        xmlStream.Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc />
    public string ExportToXmlString<T>(T value) => _engine.Export(value);

    /// <inheritdoc />
    public byte[] ExportToXmlBytes<T>(T value, Encoding? encoding = null)
    {
        var text = ExportToXmlString(value);
        return (encoding ?? _engine.Encoding).GetBytes(text);
    }

    /// <inheritdoc />
    public bool TryExport<T>(T value, Span<byte> destination, out int bytesWritten, Encoding? encoding = null)
    {
        var bytes = ExportToXmlBytes(value, encoding);
        if (bytes.Length > destination.Length) {
            bytesWritten = 0;
            return false;
        }

        bytes.CopyTo(destination);
        bytesWritten = bytes.Length;
        return true;
    }

    /// <inheritdoc />
    public string ExportFormattedToXmlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields)
        => _engine.ExportPreservingKeys(FormattedRows.Strings(items, fields));

    /// <inheritdoc />
    public void ExportFormattedToXml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream xmlStream)
        => Write(ExportFormattedToXmlString(items, fields), xmlStream);

    /// <inheritdoc />
    public string ExportColumnsToXmlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns)
        => _engine.ExportPreservingKeys(FormattedRows.Values(items, columns));

    /// <inheritdoc />
    public void ExportColumnsToXml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream xmlStream)
        => Write(ExportColumnsToXmlString(items, columns), xmlStream);

    /// <inheritdoc />
    public string ExportToXmlStringFromDataTable(DataTable.Models.DataTable dataTable)
    {
        ArgumentHelpers.ThrowIfNull(dataTable);
        return _engine.ExportRows(DataTableDocument.ToRows(dataTable));
    }

    /// <inheritdoc />
    public void ExportToXmlFromDataTable(DataTable.Models.DataTable dataTable, string xmlFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xmlFilePath);
        File.WriteAllBytes(xmlFilePath, ExportToXmlBytesFromDataTable(dataTable));
    }

    /// <inheritdoc />
    public void ExportToXmlFromDataTable(DataTable.Models.DataTable dataTable, Stream xmlStream)
        => Write(ExportToXmlStringFromDataTable(dataTable), xmlStream);

    /// <inheritdoc />
    public byte[] ExportToXmlBytesFromDataTable(DataTable.Models.DataTable dataTable, Encoding? encoding = null)
        => (encoding ?? _engine.Encoding).GetBytes(ExportToXmlStringFromDataTable(dataTable));

    private void Write(string text, Stream stream)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        var bytes = _engine.Encoding.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

#if !NETSTANDARD2_0
    /// <inheritdoc />
    public async Task ExportToXmlAsync<T>(T value, string xmlFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xmlFilePath);
        await File.WriteAllBytesAsync(xmlFilePath, ExportToXmlBytes(value), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportToXmlAsync<T>(T value, Stream xmlStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xmlStream);
        var bytes = ExportToXmlBytes(value);
        await xmlStream.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<string> ExportToXmlStringAsync<T>(T value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ExportToXmlString(value));
    }

    /// <inheritdoc />
    public Task<byte[]> ExportToXmlBytesAsync<T>(T value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ExportToXmlBytes(value));
    }

    /// <inheritdoc />
    public async Task ExportFormattedToXmlAsync<T>(
        IEnumerable<T> items,
        IReadOnlyDictionary<string, Func<T, string>> fields,
        Stream xmlStream,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xmlStream);
        var bytes = _engine.Encoding.GetBytes(ExportFormattedToXmlString(items, fields));
        await xmlStream.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportColumnsToXmlAsync<T>(
        IEnumerable<T> items,
        IReadOnlyDictionary<string, PropertyInfo> columns,
        Stream xmlStream,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xmlStream);
        var bytes = _engine.Encoding.GetBytes(ExportColumnsToXmlString(items, columns));
        await xmlStream.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportToXmlFromDataTableAsync(DataTable.Models.DataTable dataTable, string xmlFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xmlFilePath);
        await File.WriteAllBytesAsync(xmlFilePath, ExportToXmlBytesFromDataTable(dataTable), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportToXmlFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream xmlStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xmlStream);
        var bytes = _engine.Encoding.GetBytes(ExportToXmlStringFromDataTable(dataTable));
        await xmlStream.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<string> ExportToXmlStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ExportToXmlStringFromDataTable(dataTable));
    }

    /// <inheritdoc />
    public Task<byte[]> ExportToXmlBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, Encoding? encoding = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ExportToXmlBytesFromDataTable(dataTable, encoding));
    }
#endif
}
