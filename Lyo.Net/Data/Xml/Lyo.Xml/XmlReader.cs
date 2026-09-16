using System.Text;
using Lyo.Exceptions;
using Lyo.Json;
using Lyo.Result;

namespace Lyo.Xml;

/// <summary>Reads XML by converting it to a JSON node, then deserializing with the service contract.</summary>
public sealed class XmlReader : IXmlReader
{
    private readonly XmlEngine _engine;

    internal XmlReader(XmlEngine engine) => _engine = engine;

    /// <inheritdoc />
    public T ParseFile<T>(string xmlFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xmlFilePath);
        using var stream = File.OpenRead(xmlFilePath);
        return ParseStream<T>(stream);
    }

    /// <inheritdoc />
    public T ParseStream<T>(Stream xmlStream)
    {
        ArgumentHelpers.ThrowIfNull(xmlStream);
        using var reader = new StreamReader(xmlStream, _engine.Encoding, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        return ParseString<T>(reader.ReadToEnd());
    }

    /// <inheritdoc />
    public T ParseBytes<T>(byte[] xmlBytes, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(xmlBytes);
        return Parse<T>(xmlBytes.AsSpan(), encoding);
    }

    /// <inheritdoc />
    public T Parse<T>(ReadOnlySpan<byte> xml, Encoding? encoding = null)
    {
        var effective = encoding ?? _engine.Encoding;
        return ParseString<T>(DocumentBytes.Decode(effective, xml));
    }

    /// <inheritdoc />
    public T Parse<T>(ReadOnlySpan<char> xml) => ParseString<T>(xml.ToString());

    /// <inheritdoc />
    public T ParseString<T>(string xml)
    {
        ArgumentHelpers.ThrowIfNull(xml);
        return _engine.Parse<T>(xml);
    }

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStringAsDataTable(string xml, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(xml);
        return _engine.ParseRows(xml, hasFooterRow);
    }

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] xmlBytes, bool hasFooterRow = false, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(xmlBytes);
        return ParseStringAsDataTable(DocumentBytes.Decode(encoding ?? _engine.Encoding, xmlBytes), hasFooterRow);
    }

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream xmlStream, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(xmlStream);
        using var reader = new StreamReader(xmlStream, _engine.Encoding, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        return ParseStringAsDataTable(reader.ReadToEnd(), hasFooterRow);
    }

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseFileAsDataTable(string xmlFilePath, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xmlFilePath);
        using var stream = File.OpenRead(xmlFilePath);
        return ParseStreamAsDataTable(stream, hasFooterRow);
    }

#if !NETSTANDARD2_0
    /// <inheritdoc />
    public async Task<T> ParseFileAsync<T>(string xmlFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xmlFilePath);
        await using var stream = new FileStream(xmlFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        return await ParseStreamAsync<T>(stream, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<T> ParseStreamAsync<T>(Stream xmlStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xmlStream);
        using var reader = new StreamReader(xmlStream, _engine.Encoding, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        var xml = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        return ParseString<T>(xml);
    }

    /// <inheritdoc />
    public Task<T> ParseBytesAsync<T>(byte[] xmlBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xmlBytes);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ParseBytes<T>(xmlBytes));
    }

    /// <inheritdoc />
    public Task<T> ParseStringAsync<T>(string xml, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xml);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ParseString<T>(xml));
    }

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseStringAsDataTableAsync(string xml, bool hasFooterRow = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xml);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ParseStringAsDataTable(xml, hasFooterRow));
    }

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] xmlBytes, bool hasFooterRow = false, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xmlBytes);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ParseBytesAsDataTable(xmlBytes, hasFooterRow, encoding));
    }

    /// <inheritdoc />
    public async Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream xmlStream, bool hasFooterRow = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(xmlStream);
        using var reader = new StreamReader(xmlStream, _engine.Encoding, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        var xml = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        return ParseStringAsDataTable(xml, hasFooterRow);
    }

    /// <inheritdoc />
    public async Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string xmlFilePath, bool hasFooterRow = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xmlFilePath);
        await using var stream = new FileStream(xmlFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        return await ParseStreamAsDataTableAsync(stream, hasFooterRow, ct).ConfigureAwait(false);
    }
#endif
}
