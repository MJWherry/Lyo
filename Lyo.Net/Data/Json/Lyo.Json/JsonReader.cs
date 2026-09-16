using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lyo.Exceptions;
using Lyo.Result;

namespace Lyo.Json;

/// <summary>Reads JSON with the options owned by <see cref="JsonService" />.</summary>
public sealed class JsonReader : IJsonReader
{
    private readonly JsonRuntime _runtime;

    internal JsonReader(JsonRuntime runtime) => _runtime = runtime;

    /// <inheritdoc />
    public T ParseFile<T>(string jsonFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        using var stream = File.OpenRead(jsonFilePath);
        return ParseStream<T>(stream);
    }

    /// <inheritdoc />
    public T ParseStream<T>(Stream jsonStream)
    {
        ArgumentHelpers.ThrowIfNull(jsonStream);
        using var reader = new StreamReader(jsonStream, _runtime.Encoding, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        return ParseString<T>(reader.ReadToEnd());
    }

    /// <inheritdoc />
    public T ParseBytes<T>(byte[] jsonBytes, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(jsonBytes);
        return Parse<T>(jsonBytes.AsSpan(), encoding);
    }

    /// <inheritdoc />
    public T Parse<T>(ReadOnlySpan<byte> json, Encoding? encoding = null) => _runtime.DeserializeBytes<T>(json, encoding);

    /// <inheritdoc />
    public T Parse<T>(ReadOnlySpan<char> json) => _runtime.Deserialize<T>(json);

    /// <inheritdoc />
    public T ParseString<T>(string json)
    {
        ArgumentHelpers.ThrowIfNull(json);
        return _runtime.Deserialize<T>(json);
    }

    /// <inheritdoc />
    public List<T> ParseNdjsonFile<T>(string jsonFilePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        using var stream = File.OpenRead(jsonFilePath);
        return ParseNdjsonStream<T>(stream);
    }

    /// <inheritdoc />
    public List<T> ParseNdjsonStream<T>(Stream jsonStream)
    {
        ArgumentHelpers.ThrowIfNull(jsonStream);
        using var reader = new StreamReader(jsonStream, _runtime.Encoding, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        var values = new List<T>();
        while (reader.ReadLine() is { } line) {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            values.Add(ParseString<T>(line));
        }

        return values;
    }

    /// <inheritdoc />
    public List<T> ParseNdjsonString<T>(string ndjson)
    {
        ArgumentHelpers.ThrowIfNull(ndjson);
        using var reader = new StringReader(ndjson);
        var values = new List<T>();
        while (reader.ReadLine() is { } line) {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            values.Add(ParseString<T>(line));
        }

        return values;
    }

    /// <inheritdoc />
    public List<T> ParseNdjsonBytes<T>(byte[] ndjson)
    {
        ArgumentHelpers.ThrowIfNull(ndjson);
        return ParseNdjsonString<T>(Encoding.UTF8.GetString(ndjson));
    }

    /// <inheritdoc />
    public JsonImportResult<T> ParseArrayAsResult<T>(string json, string? rowsPath = null)
    {
        ArgumentHelpers.ThrowIfNull(json);
        if (string.IsNullOrWhiteSpace(json))
            return JsonImportResult<T>.DocumentError("JSON is empty.");

        JsonDocument document;
        try {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex) {
            return JsonImportResult<T>.DocumentError(ex.Message);
        }

        using (document) {
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Null)
                return JsonImportResult<T>.DocumentError("JSON root is null.");

            if (!string.IsNullOrWhiteSpace(rowsPath)) {
                if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(rowsPath, out var located) || located.ValueKind != JsonValueKind.Array)
                    return JsonImportResult<T>.DocumentError($"JSON root has no array at '{rowsPath}'.");

                return BindArray<T>(located);
            }

            if (root.ValueKind == JsonValueKind.Object)
                return JsonImportResult<T>.DocumentError("JSON root is an object. Set a rows path or send an array.");

            if (root.ValueKind != JsonValueKind.Array)
                return JsonImportResult<T>.DocumentError("JSON root is not an array.");

            return BindArray<T>(root);
        }
    }

    /// <inheritdoc />
    public JsonImportResult<T> ParseNdjsonAsResult<T>(string ndjson)
    {
        ArgumentHelpers.ThrowIfNull(ndjson);
        if (string.IsNullOrWhiteSpace(ndjson))
            return JsonImportResult<T>.DocumentError("JSON is empty.");

        var rows = new List<T>();
        var issues = new List<JsonImportIssue>();
        var index = 0;
        using var reader = new StringReader(ndjson);
        while (reader.ReadLine() is { } line) {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (TryBind<T>(line, index, out var row, out var issue))
                rows.Add(row!);
            else
                issues.Add(issue!);

            index++;
        }

        return JsonImportResult<T>.Success(rows, issues);
    }

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStringAsDataTable(string json, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(json);
        return DataTableDocument.FromRows(JsonNode.Parse(json), hasFooterRow);
    }

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] jsonBytes, bool hasFooterRow = false, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNull(jsonBytes);
        return ParseStringAsDataTable(JsonRuntime.Decode(encoding ?? _runtime.Encoding, jsonBytes), hasFooterRow);
    }

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream jsonStream, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNull(jsonStream);
        using var reader = new StreamReader(jsonStream, _runtime.Encoding, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        return ParseStringAsDataTable(reader.ReadToEnd(), hasFooterRow);
    }

    /// <inheritdoc />
    public Result<DataTable.Models.DataTable> ParseFileAsDataTable(string jsonFilePath, bool hasFooterRow = false)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        using var stream = File.OpenRead(jsonFilePath);
        return ParseStreamAsDataTable(stream, hasFooterRow);
    }

    private JsonImportResult<T> BindArray<T>(JsonElement array)
    {
        var rows = new List<T>();
        var issues = new List<JsonImportIssue>();
        var index = 0;
        foreach (var element in array.EnumerateArray()) {
            var raw = element.GetRawText();
            if (TryBind<T>(raw, index, out var row, out var issue))
                rows.Add(row!);
            else
                issues.Add(issue!);

            index++;
        }

        return JsonImportResult<T>.Success(rows, issues);
    }

    private bool TryBind<T>(string raw, int index, out T? row, out JsonImportIssue? issue)
    {
        try {
            row = _runtime.Deserialize<T>(raw);
            issue = null;
            return true;
        }
        catch (JsonException ex) {
            row = default;
            issue = new() { Index = index, Field = FieldFromPath(ex.Path), Message = ex.Message, Raw = raw };
            return false;
        }
    }

    private static string? FieldFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var dot = path.LastIndexOf('.');
        return dot >= 0 && dot < path.Length - 1 ? path[(dot + 1)..] : path;
    }

#if !NETSTANDARD2_0
    /// <inheritdoc />
    public async Task<T> ParseFileAsync<T>(string jsonFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        await using var stream = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        return await ParseStreamAsync<T>(stream, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<T> ParseStreamAsync<T>(Stream jsonStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(jsonStream);
        using var reader = new StreamReader(jsonStream, _runtime.Encoding, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        var text = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        return ParseString<T>(text);
    }

    /// <inheritdoc />
    public Task<T> ParseBytesAsync<T>(byte[] jsonBytes, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(jsonBytes);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ParseBytes<T>(jsonBytes, encoding));
    }

    /// <inheritdoc />
    public Task<T> ParseStringAsync<T>(string json, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(json);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ParseString<T>(json));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<T> ParseNdjsonFileAsync<T>(string jsonFilePath, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        await using var stream = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        await foreach (var value in ParseNdjsonStreamAsync<T>(stream, ct).ConfigureAwait(false))
            yield return value;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<T> ParseNdjsonStreamAsync<T>(Stream jsonStream, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(jsonStream);
        using var reader = new StreamReader(jsonStream, _runtime.Encoding, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        while (true) {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (line is null)
                yield break;
            if (string.IsNullOrWhiteSpace(line))
                continue;
            yield return ParseString<T>(line);
        }
    }

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseStringAsDataTableAsync(string json, bool hasFooterRow = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(json);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ParseStringAsDataTable(json, hasFooterRow));
    }

    /// <inheritdoc />
    public Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] jsonBytes, bool hasFooterRow = false, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(jsonBytes);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ParseBytesAsDataTable(jsonBytes, hasFooterRow, encoding));
    }

    /// <inheritdoc />
    public async Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream jsonStream, bool hasFooterRow = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(jsonStream);
        using var reader = new StreamReader(jsonStream, _runtime.Encoding, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        var text = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        return ParseStringAsDataTable(text, hasFooterRow);
    }

    /// <inheritdoc />
    public async Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string jsonFilePath, bool hasFooterRow = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jsonFilePath);
        await using var stream = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        return await ParseStreamAsDataTableAsync(stream, hasFooterRow, ct).ConfigureAwait(false);
    }
#endif
}
