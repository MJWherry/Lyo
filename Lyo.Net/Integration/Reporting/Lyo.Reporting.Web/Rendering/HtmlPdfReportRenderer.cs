using System.Text;
using System.Text.Json;
using Lyo.Exceptions;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Web.Components;
using Lyo.Web.WebRenderer;

namespace Lyo.Reporting.Web.Rendering;

/// <summary>Renders reports to HTML or PDF using Blazor <see cref="ReportViewer{T}" /> and <see cref="IWebRendererService" />.</summary>
public sealed class HtmlPdfReportRenderer(IWebRendererService webRenderer) : IReportRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public bool CanRender(ReportFormat format) => format is ReportFormat.Html or ReportFormat.Pdf;

    public async Task<ReportRenderResult> RenderAsync(ReportRenderRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.ReportDataJson);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.OutputFilePath);
        if (!CanRender(request.Format))
            throw new NotSupportedException($"{nameof(HtmlPdfReportRenderer)} cannot render format {request.Format}.");

        var report = JsonSerializer.Deserialize<Report<object>>(request.ReportDataJson, JsonOptions) ?? throw new ReportValidationException("Failed to deserialize report JSON.");
        var parameters = new Dictionary<string, object> { ["Report"] = report };
        var html = await webRenderer.RenderToHtmlAsync<ReportViewer<object>>(parameters, ct).ConfigureAwait(false);
        var fileName = request.SuggestedFileName;
        if (request.Format == ReportFormat.Html) {
            fileName ??= "report.html";
            await File.WriteAllTextAsync(request.OutputFilePath, html, Encoding.UTF8, ct).ConfigureAwait(false);
            return new() {
                FilePath = request.OutputFilePath,
                ContentType = "text/html; charset=utf-8",
                FileName = fileName,
                ByteLength = new FileInfo(request.OutputFilePath).Length
            };
        }

        fileName ??= "report.pdf";
        var pdfBytes = await webRenderer.ConvertHtmlToPdfAsync(html, ct).ConfigureAwait(false);
        await File.WriteAllBytesAsync(request.OutputFilePath, pdfBytes, ct).ConfigureAwait(false);
        return new() {
            FilePath = request.OutputFilePath,
            ContentType = "application/pdf",
            FileName = fileName,
            ByteLength = pdfBytes.LongLength
        };
    }
}