using System.Reflection;
using Lyo.Api.Models.Enums;
using Lyo.Api.Services.Export;
using Lyo.Xml;

namespace Lyo.Api.Export.Xml;

/// <summary>Writes XML exports through <see cref="IXmlService" />. The payload is one document whose root is the exported list.</summary>
public sealed class XmlExportFormatHandler(IXmlService xml) : IExportFormatHandler
{
    /// <inheritdoc />
    public ExportFormat Format => ExportFormat.Xml;

    /// <inheritdoc />
    public async Task<(Stream Stream, string ContentType, string FileName)> WriteProjectedAsync(
        IReadOnlyList<object?> items,
        Dictionary<string, Func<object?, string>>? formatters,
        CancellationToken ct)
    {
        var stream = new MemoryStream();
        if (formatters is not null)
            await xml.ExportFormattedToXmlAsync(items, formatters, stream, ct).ConfigureAwait(false);
        else
            await xml.ExportToXmlAsync(items, stream, ct).ConfigureAwait(false);

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
            await xml.ExportColumnsToXmlAsync(items, columns, stream, ct).ConfigureAwait(false);
        else
            await xml.ExportToXmlAsync(items, stream, ct).ConfigureAwait(false);

        stream.Position = 0;
        return (stream, Format.FileType.MimeType, Format.DefaultFileName);
    }
}