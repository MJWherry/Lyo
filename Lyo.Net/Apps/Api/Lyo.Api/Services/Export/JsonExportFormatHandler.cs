using System.Reflection;
using Lyo.Api.Models.Enums;
using Lyo.Json;

namespace Lyo.Api.Services.Export;

/// <summary>Built-in handler for the JSON export format. Writes through <see cref="IJsonService" />.</summary>
public sealed class JsonExportFormatHandler(IJsonService json) : IExportFormatHandler
{
    /// <inheritdoc />
    public ExportFormat Format => ExportFormat.Json;

    /// <inheritdoc />
    public async Task<(Stream Stream, string ContentType, string FileName)> WriteProjectedAsync(
        IReadOnlyList<object?> items,
        Dictionary<string, Func<object?, string>>? formatters,
        CancellationToken ct)
    {
        var stream = new MemoryStream();
        if (formatters is not null)
            await json.ExportFormattedToJsonAsync(items, formatters, stream, ct).ConfigureAwait(false);
        else
            await json.ExportToJsonAsync(items, stream, ct).ConfigureAwait(false);
        stream.Position = 0;
        return (stream, Format.FileType.MimeType, Format.DefaultFileName);
    }

    /// <inheritdoc />
    public async Task<(Stream Stream, string ContentType, string FileName)> WriteTypedAsync<T>(
        IReadOnlyList<T> items,
        Dictionary<string, PropertyInfo>? columns,
        CancellationToken ct)
    {
        var stream = new MemoryStream();
        if (columns is not null)
            await json.ExportColumnsToJsonAsync(items, columns, stream, ct).ConfigureAwait(false);
        else
            await json.ExportToJsonAsync(items, stream, ct).ConfigureAwait(false);
        stream.Position = 0;
        return (stream, Format.FileType.MimeType, Format.DefaultFileName);
    }
}
