using System.Reflection;
using Lyo.Api.Models.Enums;
using Lyo.Api.Services.Export;
using Lyo.Common.Records;
using Lyo.Csv.Models;

namespace Lyo.Api.Export.Csv;

/// <summary>CSV export format handler using <see cref="ICsvService" />.</summary>
public sealed class CsvExportFormatHandler(ICsvService csvService) : IExportFormatHandler
{
    public ExportFormat Format => ExportFormat.Csv;

    public async Task<(Stream Stream, string ContentType, string FileName)> WriteProjectedAsync(
        IReadOnlyList<object?> items,
        Dictionary<string, Func<object?, string>>? formatters,
        CancellationToken ct)
    {
        var stream = new MemoryStream();
        if (formatters is not null)
            await csvService.ExportToCsvStreamAsync(items, formatters, stream, ct).ConfigureAwait(false);
        else {
            var props = typeof(object).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToList();
            await csvService.ExportToCsvStreamAsync(items, props, stream, ct).ConfigureAwait(false);
        }

        stream.Position = 0;
        return (stream, FileTypeInfo.Csv.MimeType, "export.csv");
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> WriteTypedAsync<T>(
        IReadOnlyList<T> items,
        Dictionary<string, PropertyInfo>? columns,
        CancellationToken ct)
    {
        var stream = new MemoryStream();
        if (columns is not null)
            await csvService.ExportToCsvStreamAsync(items, columns, stream, ct).ConfigureAwait(false);
        else {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToList();
            await csvService.ExportToCsvStreamAsync(items, props, stream, ct).ConfigureAwait(false);
        }

        stream.Position = 0;
        return (stream, FileTypeInfo.Csv.MimeType, "export.csv");
    }
}