using System.Text;
using Lyo.Exceptions;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Rendering;

namespace Lyo.Reporting.Postgres.Rendering;

/// <summary>Writes the composition JSON as the report output, as a first-class machine-readable format.</summary>
public sealed class JsonReportRenderer : IReportRenderer
{
    public bool CanRender(ReportFormat format) => format == ReportFormat.Json;

    public async Task<ReportRenderResult> RenderAsync(ReportRenderRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.ReportDataJson);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.OutputFilePath);
        if (!CanRender(request.Format))
            throw new NotSupportedException($"{nameof(JsonReportRenderer)} cannot render format {request.Format}.");

        var bound = ReportCompositionProcessor.BindJson(request.ReportDataJson, request.Parameters);
        var json = ReportJson.Serialize(bound);
        await File.WriteAllTextAsync(request.OutputFilePath, json, Encoding.UTF8, ct).ConfigureAwait(false);
        return new() {
            FilePath = request.OutputFilePath,
            ContentType = ReportFormat.Json.ContentType,
            FileName = request.SuggestedFileName ?? "report" + ReportFormat.Json.Extension,
            ByteLength = new FileInfo(request.OutputFilePath).Length
        };
    }
}
