using System.Text;
using Lyo.Exceptions;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Rendering;

namespace Lyo.Reporting.Postgres.Rendering;

/// <summary>Writes the composition JSON as the report output — first-class machine-readable format.</summary>
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

        await File.WriteAllTextAsync(request.OutputFilePath, request.ReportDataJson, Encoding.UTF8, ct).ConfigureAwait(false);
        return new ReportRenderResult {
            FilePath = request.OutputFilePath,
            ContentType = "application/json; charset=utf-8",
            FileName = request.SuggestedFileName ?? "report.json",
            ByteLength = new FileInfo(request.OutputFilePath).Length
        };
    }
}
