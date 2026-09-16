using System.Text;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Enums;
using Lyo.Exceptions;

namespace Lyo.Api.Import;

/// <summary>Dispatches import requests. A registered <see cref="IImportCommitter" /> is not called.</summary>
public sealed class ImportService : IImportService
{
    private readonly JsonImportFormatHandler _binder;
    private readonly IImportCommitter? _committer;
    private readonly Dictionary<ImportFormat, IImportFormatHandler> _handlers;
    private readonly ImportOptions _options;

    /// <summary>Builds a service from the handlers that were registered. <paramref name="committer" /> is accepted and never invoked.</summary>
    public ImportService(IEnumerable<IImportFormatHandler> handlers, ImportOptions? options = null, IImportCommitter? committer = null)
    {
        ArgumentHelpers.ThrowIfNull(handlers);
        _options = options ?? new ImportOptions();
        _options.Validate();
        _committer = committer;
        JsonImportFormatHandler? binder = null;
        _handlers = new();
        foreach (var handler in handlers) {
            if (handler is JsonImportFormatHandler json)
                binder = json;

            _handlers[handler.Format] = handler;
        }

        _binder = binder ?? new JsonImportFormatHandler();
    }

    /// <inheritdoc />
    public ImportResult<T> Import<T>(ImportRequest request)
        where T : new()
    {
        ArgumentHelpers.ThrowIfNull(request);
        _ = _committer;

        var duplicate = DuplicateTarget(request.Fields);
        if (duplicate is not null)
            return ImportResult<T>.Fail($"Two sources map to '{duplicate}'.");

        if (ByteLength(request) > _options.MaxImportBytes)
            return ImportResult<T>.Fail("Import exceeds the byte cap.");

        if (!_handlers.TryGetValue(request.Format, out var handler))
            return ImportResult<T>.Fail($"No import handler is registered for {request.Format}.");

        var converted = handler.Convert(request);
        if (!converted.IsSuccess)
            return ImportResult<T>.Fail(converted.Message ?? "Import document is not valid.");

        var read = _binder.Read<T>(new() {
            Format = ImportFormat.Json,
            Content = converted.Json,
            JsonMode = converted.JsonMode,
            RowsPath = converted.RowsPath,
            Fields = request.Fields
        });
        if (!read.IsSuccess)
            return read;

        if (read.Rows.Count + read.Issues.Count > _options.MaxImportRows)
            return ImportResult<T>.Fail("Import exceeds the row cap.");

        return read;
    }

    /// <inheritdoc />
    public string WriteErrorReport(IReadOnlyList<ImportIssue> issues)
    {
        ArgumentHelpers.ThrowIfNull(issues);
        var builder = new StringBuilder();
        builder.AppendLine("Index,Field,Message,Raw");
        foreach (var issue in issues)
            builder.Append(issue.Index).Append(',').Append(Escape(issue.Field)).Append(',').Append(Escape(issue.Message)).Append(',').Append(Escape(issue.Raw)).Append('\n');

        return builder.ToString();
    }

    private static string? DuplicateTarget(IReadOnlyList<ImportFieldMap>? fields)
    {
        if (fields is null || fields.Count < 2)
            return null;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields) {
            if (string.IsNullOrWhiteSpace(field.Target))
                continue;

            if (!seen.Add(field.Target))
                return field.Target;
        }

        return null;
    }

    private static int ByteLength(ImportRequest request)
    {
        var text = string.IsNullOrEmpty(request.Content) ? 0 : Encoding.UTF8.GetByteCount(request.Content);
        var bytes = request.ContentBytes?.Length ?? 0;
        return Math.Max(text, bytes);
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.IndexOfAny([',', '"', '\n', '\r']) < 0)
            return value;

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
