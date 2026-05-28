using System.Reflection;
using Lyo.Api.Models.Enums;
using Lyo.Api.Services.Export;
using Lyo.Common.Records;
using Lyo.Xlsx.Models;

namespace Lyo.Api.Export.Xlsx;

/// <summary>XLSX export format handler using <see cref="IXlsxService" />.</summary>
public sealed class XlsxExportFormatHandler(IXlsxService xlsxService) : IExportFormatHandler
{
    public ExportFormat Format => ExportFormat.Xlsx;

    public async Task<(Stream Stream, string ContentType, string FileName)> WriteProjectedAsync(
        IReadOnlyList<object?> items,
        Dictionary<string, Func<object?, string>>? formatters,
        CancellationToken ct)
    {
        var stream = new MemoryStream();
        if (formatters is not null)
            await xlsxService.ExportToXlsxAsync(items, formatters, stream, null, ct).ConfigureAwait(false);
        else {
            var props = typeof(object).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToList();
            await xlsxService.ExportToXlsxAsync(items, props, stream, null, ct).ConfigureAwait(false);
        }

        stream.Position = 0;
        return (stream, FileTypeInfo.Xlsx.MimeType, "export.xlsx");
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> WriteTypedAsync<T>(
        IReadOnlyList<T> items,
        Dictionary<string, PropertyInfo>? columns,
        CancellationToken ct)
    {
        var stream = new MemoryStream();
        if (columns is not null)
            await xlsxService.ExportToXlsxAsync(items, columns, stream, null, ct).ConfigureAwait(false);
        else {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToList();
            await xlsxService.ExportToXlsxAsync(items, props, stream, null, ct).ConfigureAwait(false);
        }

        stream.Position = 0;
        return (stream, FileTypeInfo.Xlsx.MimeType, "export.xlsx");
    }
}
