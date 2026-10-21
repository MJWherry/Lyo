using System.Text;
using Lyo.Exceptions;
using Lyo.Json;
using Lyo.Result;

namespace Lyo.Yaml;

/// <summary>Reads YAML by converting it to a JSON node, then deserializing with the service contract.</summary>
public sealed class YamlReader : IYamlReader
{
    private readonly YamlEngine _engine;

    internal YamlReader(YamlEngine engine) => _engine = engine;

    /// <inheritdoc />
    public T ParseFile<T>(string yamlFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(yamlFilePath);
        using var stream = File.OpenRead(yamlFilePath);
        return ParseStream<T>(stream);
    }

    /// <inheritdoc />
    public T ParseStream<T>(Stream yamlStream)
    {
        ArgumentHelpers.ThrowIfNull(yamlStream);
        using var reader = new StreamReader(yamlStream, _engine.Encoding, true, 1024, true);
        return ParseString<T>(reader.ReadToEnd());
    }

    /// <inheritdoc />
    public T ParseBytes<T>(byte[] yamlBytes, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(yamlBytes);
        return Parse<T>(yamlBytes.AsSpan(), encoding);
    }

    /// <inheritdoc />
    public T Parse<T>(ReadOnlySpan<byte> yaml, Encoding? encoding = null)
    {
        var effective = encoding ?? _engine.Encoding;
        return ParseString<T>(DocumentBytes.Decode(effective, yaml));
    }

    /// <inheritdoc />
    public T Parse<T>(ReadOnlySpan<char> yaml) => ParseString<T>(yaml.ToString());

    /// <inheritdoc />
    public T ParseString<T>(string yaml)
    {
        ArgumentHelpers.ThrowIfNull(yaml);
        return _engine.Parse<T>(yaml);
    }

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStringAsDataTable(string yaml, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(yaml);
        return _engine.ParseRows(yaml, hasFooterRow);
    }

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] yamlBytes, bool hasFooterRow = false, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(yamlBytes);
        return ParseStringAsDataTable(DocumentBytes.Decode(encoding ?? _engine.Encoding, yamlBytes), hasFooterRow);
    }

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream yamlStream, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(yamlStream);
        using var reader = new StreamReader(yamlStream, _engine.Encoding, true, 1024, true);
        return ParseStringAsDataTable(reader.ReadToEnd(), hasFooterRow);
    }

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseFileAsDataTable(string yamlFilePath, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(yamlFilePath);
        using var stream = File.OpenRead(yamlFilePath);
        return ParseStreamAsDataTable(stream, hasFooterRow);
    }

#if !NETSTANDARD2_0
    /// <inheritdoc />
    public async Task<T> ParseFileAsync<T>(string yamlFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(yamlFilePath);
        await using var stream = new FileStream(yamlFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        return await ParseStreamAsync<T>(stream, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<T> ParseStreamAsync<T>(Stream yamlStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(yamlStream);
        using var reader = new StreamReader(yamlStream, _engine.Encoding, true, 1024, true);
        var yaml = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        return ParseString<T>(yaml);
    }

    /// <inheritdoc />
    public Task<T> ParseBytesAsync<T>(byte[] yamlBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(yamlBytes);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ParseBytes<T>(yamlBytes));
    }

    /// <inheritdoc />
    public Task<T> ParseStringAsync<T>(string yaml, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(yaml);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ParseString<T>(yaml));
    }

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseStringAsDataTableAsync(string yaml, bool hasFooterRow = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(yaml);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ParseStringAsDataTable(yaml, hasFooterRow));
    }

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(
        byte[] yamlBytes,
        bool hasFooterRow = false,
        Encoding? encoding = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(yamlBytes);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ParseBytesAsDataTable(yamlBytes, hasFooterRow, encoding));
    }

    /// <inheritdoc />
    public async Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream yamlStream, bool hasFooterRow = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(yamlStream);
        using var reader = new StreamReader(yamlStream, _engine.Encoding, true, 1024, true);
        var yaml = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        return await ParseStringAsDataTableAsync(yaml, hasFooterRow, ct);
    }

    /// <inheritdoc />
    public async Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string yamlFilePath, bool hasFooterRow = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(yamlFilePath);
        await using var stream = new FileStream(yamlFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        return await ParseStreamAsDataTableAsync(stream, hasFooterRow, ct).ConfigureAwait(false);
    }
#endif
}