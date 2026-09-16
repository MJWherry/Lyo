using System.Text;
using Lyo.Exceptions;
using Lyo.Json;
using Lyo.Result;

namespace Lyo.Toml;

/// <summary>Reads TOML by converting it to a JSON node, then deserializing with the service contract.</summary>
public sealed class TomlReader : ITomlReader
{
    private readonly TomlEngine _engine;

    internal TomlReader(TomlEngine engine) => _engine = engine;

    /// <inheritdoc />
    public T ParseFile<T>(string tomlFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tomlFilePath);
        using var stream = File.OpenRead(tomlFilePath);
        return ParseStream<T>(stream);
    }

    /// <inheritdoc />
    public T ParseStream<T>(Stream tomlStream)
    {
        ArgumentHelpers.ThrowIfNull(tomlStream);
        using var reader = new StreamReader(tomlStream, _engine.Encoding, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        return ParseString<T>(reader.ReadToEnd());
    }

    /// <inheritdoc />
    public T ParseBytes<T>(byte[] tomlBytes, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(tomlBytes);
        return Parse<T>(tomlBytes.AsSpan(), encoding);
    }

    /// <inheritdoc />
    public T Parse<T>(ReadOnlySpan<byte> toml, Encoding? encoding = null)
    {
        var effective = encoding ?? _engine.Encoding;
        return ParseString<T>(DocumentBytes.Decode(effective, toml));
    }

    /// <inheritdoc />
    public T Parse<T>(ReadOnlySpan<char> toml) => ParseString<T>(toml.ToString());

    /// <inheritdoc />
    public T ParseString<T>(string toml)
    {
        ArgumentHelpers.ThrowIfNull(toml);
        return _engine.Parse<T>(toml);
    }

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStringAsDataTable(string toml, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(toml);
        return _engine.ParseRows(toml, hasFooterRow);
    }

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] tomlBytes, bool hasFooterRow = false, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(tomlBytes);
        return ParseStringAsDataTable(DocumentBytes.Decode(encoding ?? _engine.Encoding, tomlBytes), hasFooterRow);
    }

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream tomlStream, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(tomlStream);
        using var reader = new StreamReader(tomlStream, _engine.Encoding, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        return ParseStringAsDataTable(reader.ReadToEnd(), hasFooterRow);
    }

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseFileAsDataTable(string tomlFilePath, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tomlFilePath);
        using var stream = File.OpenRead(tomlFilePath);
        return ParseStreamAsDataTable(stream, hasFooterRow);
    }

#if !NETSTANDARD2_0
    /// <inheritdoc />
    public async Task<T> ParseFileAsync<T>(string tomlFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tomlFilePath);
        await using var stream = new FileStream(tomlFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        return await ParseStreamAsync<T>(stream, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<T> ParseStreamAsync<T>(Stream tomlStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(tomlStream);
        using var reader = new StreamReader(tomlStream, _engine.Encoding, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        var toml = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        return ParseString<T>(toml);
    }

    /// <inheritdoc />
    public Task<T> ParseBytesAsync<T>(byte[] tomlBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(tomlBytes);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ParseBytes<T>(tomlBytes));
    }

    /// <inheritdoc />
    public Task<T> ParseStringAsync<T>(string toml, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(toml);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ParseString<T>(toml));
    }

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseStringAsDataTableAsync(string toml, bool hasFooterRow = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(toml);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ParseStringAsDataTable(toml, hasFooterRow));
    }

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] tomlBytes, bool hasFooterRow = false, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(tomlBytes);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ParseBytesAsDataTable(tomlBytes, hasFooterRow, encoding));
    }

    /// <inheritdoc />
    public async Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream tomlStream, bool hasFooterRow = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(tomlStream);
        using var reader = new StreamReader(tomlStream, _engine.Encoding, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        var toml = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        return ParseStringAsDataTable(toml, hasFooterRow);
    }

    /// <inheritdoc />
    public async Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string tomlFilePath, bool hasFooterRow = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tomlFilePath);
        await using var stream = new FileStream(tomlFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        return await ParseStreamAsDataTableAsync(stream, hasFooterRow, ct).ConfigureAwait(false);
    }
#endif
}
