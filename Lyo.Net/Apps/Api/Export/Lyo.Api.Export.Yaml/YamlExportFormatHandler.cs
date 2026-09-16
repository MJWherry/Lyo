using System.Reflection;
using Lyo.Api.Models.Enums;
using Lyo.Api.Services.Export;
using Lyo.Yaml;

namespace Lyo.Api.Export.Yaml;

/// <summary>Writes YAML exports through <see cref="IYamlService" />. The payload is one document whose root is the exported list.</summary>
public sealed class YamlExportFormatHandler(IYamlService yaml) : IExportFormatHandler
{
    /// <inheritdoc />
    public ExportFormat Format => ExportFormat.Yaml;

    /// <inheritdoc />
    public async Task<(Stream Stream, string ContentType, string FileName)> WriteProjectedAsync(
        IReadOnlyList<object?> items,
        Dictionary<string, Func<object?, string>>? formatters,
        CancellationToken ct)
    {
        var stream = new MemoryStream();
        if (formatters is not null)
            await yaml.ExportFormattedToYamlAsync(items, formatters, stream, ct).ConfigureAwait(false);
        else
            await yaml.ExportToYamlAsync(items, stream, ct).ConfigureAwait(false);
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
            await yaml.ExportColumnsToYamlAsync(items, columns, stream, ct).ConfigureAwait(false);
        else
            await yaml.ExportToYamlAsync(items, stream, ct).ConfigureAwait(false);
        stream.Position = 0;
        return (stream, Format.FileType.MimeType, Format.DefaultFileName);
    }
}
