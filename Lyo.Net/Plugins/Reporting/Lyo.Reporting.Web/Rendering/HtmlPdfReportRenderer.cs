using System.Text;
using Lyo.Exceptions;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Web.Components;
using Lyo.Web.WebRenderer;

namespace Lyo.Reporting.Web.Rendering;

/// <summary>Renders reports to HTML or PDF via Blazor <see cref="ReportViewer{T}" /> and <see cref="IWebRendererService" />.</summary>
public sealed class HtmlPdfReportRenderer(IWebRendererService webRenderer) : IReportRenderer
{
    public bool CanRender(ReportFormat format) => format is ReportFormat.Html or ReportFormat.Pdf;

    public async Task<ReportRenderResult> RenderAsync(ReportRenderRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.ReportDataJson);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.OutputFilePath);
        if (!CanRender(request.Format))
            throw new NotSupportedException($"{nameof(HtmlPdfReportRenderer)} cannot render format {request.Format}.");

        var report = ReportCompositionProcessor.BindJson(request.ReportDataJson, request.Parameters);
        string html;
        var rootTypeName = report.Layout?.RootComponentType;
        if (!string.IsNullOrWhiteSpace(rootTypeName)) {
            var rootType = ReportEmbedBinder.TryResolveComponent(rootTypeName, out var resolveError);
            if (rootType is null)
                throw new ReportValidationException(resolveError ?? "Root component type could not be resolved.");

            var bound = ReportEmbedBinder.Bind(rootType, null, ReportCompositionProcessor.ToMap(request.Parameters), out var bindError);
            if (bindError is not null)
                throw new ReportValidationException(bindError);

            html = await webRenderer.RenderToHtmlAsync(rootType, bound, ct).ConfigureAwait(false);
        }
        else {
            var parameters = new Dictionary<string, object> { ["Report"] = report, ["InlineChartScript"] = true };
            html = await webRenderer.RenderToHtmlAsync<ReportViewer<object>>(parameters, ct).ConfigureAwait(false);
        }
        var fileName = request.SuggestedFileName;
        if (request.Format == ReportFormat.Html) {
            fileName ??= "report" + ReportFormat.Html.Extension;
            await File.WriteAllTextAsync(request.OutputFilePath, html, Encoding.UTF8, ct).ConfigureAwait(false);
            return new() {
                FilePath = request.OutputFilePath,
                ContentType = ReportFormat.Html.ContentType,
                FileName = fileName,
                ByteLength = new FileInfo(request.OutputFilePath).Length
            };
        }

        fileName ??= "report" + ReportFormat.Pdf.Extension;
        var pdfBytes = await webRenderer.ConvertHtmlToPdfAsync(html, ct).ConfigureAwait(false);
        await File.WriteAllBytesAsync(request.OutputFilePath, pdfBytes, ct).ConfigureAwait(false);
        return new() {
            FilePath = request.OutputFilePath,
            ContentType = ReportFormat.Pdf.ContentType,
            FileName = fileName,
            ByteLength = pdfBytes.LongLength
        };
    }
}
