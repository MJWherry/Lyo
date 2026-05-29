using System.Reflection;
using System.Text.Json;
using Lyo.Api.Models.Enums;
using Lyo.Common.Records;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Api.Services.Export;

/// <summary>Built-in JSON export format handler.</summary>
public sealed class JsonExportFormatHandler(IServiceProvider serviceProvider) : IExportFormatHandler
{
    public ExportFormat Format => ExportFormat.Json;

    public Task<(Stream Stream, string ContentType, string FileName)> WriteProjectedAsync(
        IReadOnlyList<object?> items,
        Dictionary<string, Func<object?, string>>? formatters,
        CancellationToken ct)
        => Task.FromResult(WriteJson(items, "export.json"));

    public Task<(Stream Stream, string ContentType, string FileName)> WriteTypedAsync<T>(IReadOnlyList<T> items, Dictionary<string, PropertyInfo>? columns, CancellationToken ct)
        => Task.FromResult(WriteJson(items, "export.json"));

    private (Stream Stream, string ContentType, string FileName) WriteJson<T>(IEnumerable<T> items, string fileName)
    {
        var serializerOptions = serviceProvider.GetService<IOptions<JsonOptions>>()?.Value.SerializerOptions;
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, items, serializerOptions ?? JsonSerializerOptions.Default);
        stream.Position = 0;
        return (stream, FileTypeInfo.Json.MimeType, fileName);
    }
}