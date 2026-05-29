using System.Reflection;
using Lyo.Api.Models.Enums;

namespace Lyo.Api.Services.Export;

/// <summary>Writes export query results to a specific file format (CSV, XLSX, JSON, …).</summary>
public interface IExportFormatHandler
{
    ExportFormat Format { get; }

    Task<(Stream Stream, string ContentType, string FileName)> WriteProjectedAsync(
        IReadOnlyList<object?> items,
        Dictionary<string, Func<object?, string>>? formatters,
        CancellationToken ct);

    Task<(Stream Stream, string ContentType, string FileName)> WriteTypedAsync<T>(IReadOnlyList<T> items, Dictionary<string, PropertyInfo>? columns, CancellationToken ct);
}