using System.Reflection;
using Lyo.Api.Models.Enums;
using Lyo.Api.Services.Export;
using Lyo.Toml;

namespace Lyo.Api.Export.Toml;

/// <summary>Writes TOML exports through <see cref="ITomlService" />. The payload is one document whose root is the exported list.</summary>
public sealed class TomlExportFormatHandler(ITomlService toml) : IExportFormatHandler
{
    /// <inheritdoc />
    public ExportFormat Format => ExportFormat.Toml;

    /// <inheritdoc />
    public async Task<(Stream Stream, string ContentType, string FileName)> WriteProjectedAsync(
        IReadOnlyList<object?> items,
        Dictionary<string, Func<object?, string>>? formatters,
        CancellationToken ct)
    {
        var stream = new MemoryStream();
        if (formatters is not null)
            await toml.ExportFormattedToTomlAsync(items, formatters, stream, ct).ConfigureAwait(false);
        else
            await toml.ExportToTomlAsync(items, stream, ct).ConfigureAwait(false);
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
            await toml.ExportColumnsToTomlAsync(items, columns, stream, ct).ConfigureAwait(false);
        else
            await toml.ExportToTomlAsync(items, stream, ct).ConfigureAwait(false);
        stream.Position = 0;
        return (stream, Format.FileType.MimeType, Format.DefaultFileName);
    }
}
